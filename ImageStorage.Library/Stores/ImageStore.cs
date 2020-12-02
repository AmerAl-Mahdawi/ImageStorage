using Amazon.S3;
using Amazon.S3.Model;
using ImageStorage.Library.Config;
using ImageStorage.Library.Internal;
using ImageStorage.Library.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageStorage.Library.Stores
{
    public class ImageStore : IImageStore
    {
        private readonly ISqlDataAccess _sql;
        private readonly IS3SvcConfiguration _s3SvcConfiguration;
        private readonly IConfiguration _config;

        public ImageStore(ISqlDataAccess sql, IS3SvcConfiguration s3SvcConfiguration, IConfiguration config)
        {
            _sql = sql;
            _s3SvcConfiguration = s3SvcConfiguration;
            _config = config;
        }

        public async Task<(IEnumerable<ImageModel>, string)> GetByFilterAsync(string description, string type, int pageNumber, int pageSize)
        {
            try
            {
                var host = _config.GetSection("AwsElasticSearch:Host").Value;
                var indexName = _config.GetSection("AwsElasticSearch:IndexName").Value;

                  string url = $"{ host }/{ indexName }/_search";

                string jsonString = "{\"from\": " + pageNumber + ", \"size\": " + pageSize;
                jsonString += ", \"query\": { \"bool\":{ \"must\":[ { \"match\": { \"Description\": \"";
                jsonString += description + "\"}},{\"term\": {\"Type\": \"" + type + "\"}}]}}}";

                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(url),
                        Content = new StringContent(jsonString, Encoding.UTF8, "application/json"),
                    };

                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    var responseBodyString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var responseBodyJson = JsonSerializer.Deserialize<JsonElement>(responseBodyString);

                    var searchResult = responseBodyJson.GetProperty("hits").GetProperty("hits");

                    if (searchResult.GetArrayLength() > 0)
                    {
                        var imageModelList = new List<ImageModel>();
                        var image = new ImageModel();

                        foreach (var item in searchResult.EnumerateArray())
                        {
                            image = JsonSerializer.Deserialize<ImageModel>(item.GetProperty("_source").GetRawText());
                            image.Id = Int32.Parse(item.GetProperty("_id").ToString());
                            imageModelList.Add(image);
                        }

                        return (imageModelList, "");

                    }

                    return (new List<ImageModel>(), "");

                }
            }
            catch
            {
                return (new List<ImageModel>(), "Failed: Failed to search for the provided filter in DB!");
            }
        }

        private async Task<string> UploadToS3(ImageModel image, Stream imageStream)
        {
            try
            {
                // Upload the image to S3
                IAmazonS3 client = new AmazonS3Client(_s3SvcConfiguration.AccessKey,
                                                      _s3SvcConfiguration.SecretKey,
                                                      _s3SvcConfiguration.RegionPoint);

                PutObjectRequest request = new PutObjectRequest()
                {
                    ContentType = image.Type,
                    InputStream = imageStream,
                    BucketName = _s3SvcConfiguration.BucketName,
                    Key = image.Name
                };

                PutObjectResponse response = await client.PutObjectAsync(request);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    return "successful";
                }

                throw new InvalidOperationException();
            }
            catch
            {
                return $"Failed: Image { image.Name } failed to upload to S3 bucket!";
            }
        }

        private async Task<string> SyncDataWithES(ImageModel image)
        {
            try
            {
                // Sync data with ES

                var host = _config.GetSection("AwsElasticSearch:Host").Value;
                var indexName = _config.GetSection("AwsElasticSearch:IndexName").Value;

                string url = $"{ host }/{ indexName }/_doc/{ image.Id }";

                string jsonString = "{\"Name\": \"" + image.Name + "\", \"Description\": \"" + image.Description;
                jsonString += "\", \"Type\": \"" + image.Type + "\", \"Size\": " + image.Size + "}";

                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri(url),
                        Content = new StringContent(jsonString, Encoding.UTF8, "application/json"),
                    };

                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    return "successful";
                }
            }
            catch
            {
                return $"Failed: Data of image { image.Name } failed to sync with ES!";
            }
        }

        public async Task<string> UploadAsync(ImageModel image, Stream imageStream)
        {
            try
            {
                _sql.StartTransaction();

                image.Id = (await _sql.SaveDataInTransactionAsync("dbo.spImages_Insert", image)).FirstOrDefault();

                if (image.Id != 0)
                {
                    // Upload the image to S3

                    string s3Response = await UploadToS3(image, imageStream);

                    if (s3Response == "successful") // Image was uploaded successfuly to S3
                    {
                        string esResponse = await SyncDataWithES(image);

                        if (esResponse == "successful") // Image data synced with ES
                        {
                            _sql.CommitTransaction();
                            return $"Successful Upload: Image { image.Name } has been uploaded successfully!";
                        }
                        throw new InvalidOperationException(esResponse);
                    }
                    throw new InvalidOperationException(s3Response);
                }
                throw new InvalidOperationException($"Failed: Image { image.Name } failed to get saved in DB!");
            }
            catch (InvalidOperationException ex)
            {
                _sql.RollbackTransaction();
                return ex.Message;
            }
            catch
            {
                return $"Failed: Image { image.Name } failed to upload";
            }
        }
    }
}