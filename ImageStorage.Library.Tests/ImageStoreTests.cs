using Amazon.S3;
using Amazon.S3.Model;
using ImageStorage.Library.Config;
using ImageStorage.Library.Internal;
using ImageStorage.Library.Models;
using ImageStorage.Library.Stores;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ImageStorage.Library.Tests
{
    public class ImageStoreTests
    {
        [Fact]
        public async Task GetByFilterAsync_Fail()
        {
            // Arrange
            var controller = new ImageStore(null, null, null);

            // Act
            var actual = await controller.GetByFilterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>());

            // Assert
            Assert.Equal("Failed: Failed to search for the provided filter in DB!", actual.Item2);
        }

        private IEnumerable<int> SqlResponse(int id)
        {
            return new List<int> { id };
        }

        [Fact]
        public async Task UploadAsync_NotAbleToUploadToS3()
        {
            // Arrange

            // IConfiguration mock
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.SetupGet(x => x.GetSection("AwsElasticSearch:Host").Value)
                .Returns("mock host");

            configurationMock.SetupGet(x => x.GetSection("AwsElasticSearch:IndexName").Value)
                .Returns("mock index name");

            // ISqlDataAccess mock
            var sqlMock = new Mock<ISqlDataAccess>();
            sqlMock.Setup(x => x.SaveDataInTransactionAsync(It.IsAny<string>(), It.IsAny<ImageModel>()))
                .Returns(Task.FromResult(SqlResponse(1)));

            // IS3SvcConfiguration mock
            var s3Mock = new Mock<IS3SvcConfiguration>();
            s3Mock.Setup(x => x.BucketName)
                .Returns("TestBucket");

            var image = new ImageModel()
            {
                Name = "TestImage"
            };

            var memStream = new MemoryStream(100);

            // IAmazonS3 mock
            var amazonS3Mock = new Mock<IAmazonS3>();
            amazonS3Mock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny <CancellationToken>()))
                .Returns(Task.FromResult(new PutObjectResponse()));

            // Act
            var controller = new ImageStore(sqlMock.Object, s3Mock.Object, configurationMock.Object);

            var actual = await controller.UploadAsync(image, memStream);
            var expected = $"Failed: Image { image.Name } failed to upload to S3 bucket!";

            // Assert
            Assert.StartsWith(expected, actual);
        }

        [Fact]
        public async Task UploadAsync_NotAbleToSaveInDB()
        {
            // Arrange

            // IConfiguration mock
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.SetupGet(x => x.GetSection("AwsElasticSearch:Host").Value)
                .Returns("mock host");

            configurationMock.SetupGet(x => x.GetSection("AwsElasticSearch:IndexName").Value)
                .Returns("mock index name");

            // ISqlDataAccess mock
            var sqlMock = new Mock<ISqlDataAccess>();
            sqlMock.Setup(x => x.SaveDataInTransactionAsync(It.IsAny<string>(), It.IsAny<ImageModel>()))
                .Returns(Task.FromResult(SqlResponse(0)));

            // Act
            var controller = new ImageStore(sqlMock.Object, null, configurationMock.Object);

            var actual = await controller.UploadAsync(new ImageModel(), It.IsAny<Stream>());
            var expected = "Failed: Image ";

            // Assert
            Assert.StartsWith(expected, actual);
        }
    }
}