using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace ImageStorage.Integration.Tests
{
    public class ImageStorageIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public ImageStorageIntegrationTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("", "", 0, 10, 0, 1, HttpStatusCode.BadRequest, "Invalid image size: it should be between 1 and 500 (512000 B) KB\n")]
        [InlineData("", "", 1, 512001, 0, 1, HttpStatusCode.BadRequest, "Invalid image size: it should be between 1 and 500 (512000 B) KB\n")]
        [InlineData("", "", 20, 10, 0, 1, HttpStatusCode.BadRequest, "Image minimum size hould be less than maximum size \n")]
        [InlineData("", "", 1, 10, -1, 1, HttpStatusCode.BadRequest, "Invalid page number: it should be >= 0!\n")]
        [InlineData("", "", 1, 10, 0, 0, HttpStatusCode.BadRequest, "Invalid page size: it should be >= 1!\n")]
        [InlineData("", "", 1, 512000, 0, 1, HttpStatusCode.OK, "")]
        [InlineData("IMAGE_DESCRIPTION", "image/png", 1, 512000, 0, 1, HttpStatusCode.OK, "1")]
        [InlineData("IMAGE_DESCRIPTION", "image/png", 1, 512000, 0, 30, HttpStatusCode.OK, "1")]
        public async Task GetAsync_EndpointsReturnExpectedResponse(string description,
                                                                   string type,
                                                                   int minSize,
                                                                   int maxSize,
                                                                   int pageNumber,
                                                                   int pageSize,
                                                                   HttpStatusCode expectedCode,
                                                                   string expectedMessage)
        {
            // Arrange
            string parameters = $"?description={ description }&type={ type }&minSize={ minSize }&maxSize={ maxSize }";
            parameters += $"&pageNumber={ pageNumber }&pageSize={ pageSize }";

            var url = $"/api/images{ parameters }";
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            var actualMessage = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expectedCode, response.StatusCode);

            if (expectedCode == HttpStatusCode.BadRequest)
            {
                Assert.Equal(expectedMessage, actualMessage);
            }
            else
            {
                Assert.StartsWith("[", actualMessage);
            }
        }

        [Theory]
        [InlineData("description", "IntegrationTestDescription", "file", "ImageName", "image/jpeg", HttpStatusCode.Created)]
        [InlineData("description", "IntegrationTestDescription", "file", "ImageName", "image/png", HttpStatusCode.Created)]
        [InlineData("description", "IntegrationTestDescription", "file", "ImageName", "image/any", HttpStatusCode.BadRequest)]
        [InlineData("description", "IntegrationTestDescription", "file2", "ImageName", "image/jpeg", HttpStatusCode.BadRequest)]
        [InlineData("description2", "IntegrationTestDescription", "file", "ImageName", "image/jpeg", HttpStatusCode.BadRequest)]

        public async Task PostAsync_EndpointsReturnExpectedResponse(string key,
                                                                    string value,
                                                                    string keyName,
                                                                    string imageName,
                                                                    string imageType,
                                                                    HttpStatusCode expectedCode)
        {
            // Arrange
            var url = $"/api/images";
            var client = _factory.CreateClient();

            // Act
            var imageData = new byte[] { 0 };
            var requestContent = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(imageType);
            requestContent.Add(imageContent, keyName, imageName);
            requestContent.Add(new StringContent(value), key);
            var response = await client.PostAsync(url, requestContent);
            var actualMessage = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expectedCode, response.StatusCode);

            if (expectedCode == HttpStatusCode.Created)
            {
                Assert.StartsWith("Successful Upload: Image ", actualMessage);
            }
        }
    }
}
