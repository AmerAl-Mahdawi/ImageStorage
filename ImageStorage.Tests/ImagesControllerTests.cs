using ImagesStorage.Controllers;
using ImageStorage.Library.Models;
using ImageStorage.Library.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImageStorage.Tests
{
    public class ImagesControllerTests
    {
        [Theory]
        [InlineData(null, null, 0, 0, 0, 10, 400, "Invalid image size: it should be between 1 and 500 (512000 B) KB\n")]
        [InlineData(null, null, -1, 0, 0, 10, 400, "Invalid image size: it should be between 1 and 500 (512000 B) KB\n")]
        [InlineData(null, null, 1, 0, 0, 10, 400, "Image minimum size hould be less than maximum size \n")]
        [InlineData(null, null, 10, 2, 0, 10, 400, "Image minimum size hould be less than maximum size \n")]
        [InlineData(null, null, 10, 512001, 0, 10, 400, "Invalid image size: it should be between 1 and 500 (512000 B) KB\n")]
        [InlineData(null, null, 10, 20, -1, 10, 400, "Invalid page number: it should be >= 0!\n")]
        [InlineData(null, null, 10, 20, 0, 0, 400, "Invalid page size: it should be >= 1!\n")]
        [InlineData(null, null, 10, 20, 0, -1, 400, "Invalid page size: it should be >= 1!\n")]
        public async Task GetAsync_ValidateInput(string description,
                                                 string type,
                                                 int minSize,
                                                 int maxSize,
                                                 int pageNumber,
                                                 int pageSize,
                                                 int statusCode,
                                                 string responseMessage)
        {
            // Arrange
            var controller = new ImagesController(null);

            // Act
            var result = await controller.GetAsync(description, type, minSize, maxSize, pageNumber, pageSize);
            var actual = result as ObjectResult;

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(statusCode, actual.StatusCode);
            Assert.Equal(responseMessage, actual.Value);
        }

        private (IEnumerable<ImageModel>, string) GetByFilterSample(string str)
        {
            var output = new List<ImageModel>
                {
                    new ImageModel
                    {
                        Id = 1,
                        Name = "MockImage1",
                        Description = "MockDescription1",
                        Type = "MockType1",
                        Size = 1000
                    },
                    new ImageModel
                    {
                        Id = 2,
                        Name = "MockImage2",
                        Description = "MockDescription2",
                        Type = "MockType2",
                        Size = 2000
                    },
                    new ImageModel
                    {
                        Id = 3,
                        Name = "MockImage3",
                        Description = "MockDescription3",
                        Type = "MockType3",
                        Size = 3000
                    },
                    new ImageModel
                    {
                        Id = 4,
                        Name = "MockImage4",
                        Description = "MockDescription4",
                        Type = "MockType4",
                        Size = 4000
                    },
                    new ImageModel
                    {
                        Id = 5,
                        Name = "MockImage5",
                        Description = "MockDescription5",
                        Type = "MockType5",
                        Size = 5000
                    }
                };
            str += "";
            return (output, str);
        }

        [Theory]
        [InlineData("Any", 1, 400)]
        [InlineData("", 1, 200)]
        [InlineData("", 30, 200)]
        public async Task GetAsync_ShouldValidateResponse(string mockValue, int pageSize, int statusCode)
        {
            // Arrange
            var imageStoreMock = new Mock<IImageStore>();
            imageStoreMock.Setup(p => p.GetByFilterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                          .Returns(Task.FromResult(GetByFilterSample(mockValue)));

            // Act
            var controller = new ImagesController(imageStoreMock.Object);
            var result = await controller.GetAsync(It.IsAny<string>(), It.IsAny<string>(), 1, 512000, 0, pageSize);

            // Assert
            var actual = result as ObjectResult;

            Assert.NotNull(actual);
            Assert.Equal(statusCode, actual.StatusCode);
        }

        [Theory]
        [InlineData("", 1, "file", "ImageName", "image/jpeg", 400, "Image description is required!\n")]
        [InlineData("abc", 1, "file", "ImageName", "image/jpeg", 400, "Image description is required!\n")]
        [InlineData("description", 1, "file", "ImageName", "image/any", 400, "Image type should be either png or jpeg!\n")]
        [InlineData("description", 512001, "file", "ImageName", "image/jpeg", 400, "Image size should be 500 KB (512000 B) or less!\n")]
        [InlineData("description", 0, "file", "ImageName", "image/jpeg", 400, "Image size should be greater than 0!\n")]
        [InlineData("description", -1, "file", "ImageName", "image/jpeg", 400, "Image size should be greater than 0!\n")]
        [InlineData("description", 1, "", "ImageName", "image/jpeg", 400, "Please attach an image with your request!")]
        [InlineData("description", 1, null, "ImageName", "image/jpeg", 400, "Please attach an image with your request!")]
        public async Task PostAsync_ValidateInput(string descriptionKey,
                                                 long size,
                                                 string name,
                                                 string fileName,
                                                 string contentType,
                                                 int statusCode,
                                                 string responseMessage)
        {
            // Arrange
            var input = new Dictionary<string, StringValues>()
            {
                { descriptionKey, It.IsAny<string>() }
            };

            var images = new FormFileCollection();

            IFormFile image = CreateImageFile(null, size, name, fileName, contentType);

            images.Add(image);

            var formCollection = new FormCollection(input, images);

            var controller = new ImagesController(null);

            // Act
            var result = await controller.PostAsync(formCollection);
            var actual = result as ObjectResult;

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(statusCode, actual.StatusCode);
            Assert.Equal(responseMessage, actual.Value);
        }

        private IFormFile CreateImageFile(MemoryStream memStream, long size, string name, string fileName, string contentType)
        {
            return new FormFile(memStream, It.IsAny<long>(), size, name, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        [Theory]
        [InlineData("Any", 500)]
        [InlineData("Successful Upload: Image ", 201)]
        public async Task PostAsync_ShouldValidateResponse(string mockValue, int statusCode)
        {
            // Arrange   
            var input = new Dictionary<string, StringValues>()
            {
                { "description", It.IsAny<string>() }
            };

            var memStream = new MemoryStream(100);

            var images = new FormFileCollection()
            {
                CreateImageFile(memStream, 1, "file", "ImageName", "image/jpeg"),
            };

            var formCollection = new FormCollection(input, images);

            var imageStoreMock = new Mock<IImageStore>();
            imageStoreMock.Setup(p => p.UploadAsync(It.IsAny<ImageModel>(), It.IsAny<Stream>()))
                          .Returns(Task.FromResult(mockValue));

            // Act
            var controller = new ImagesController(imageStoreMock.Object);

            var result = await controller.PostAsync(formCollection);

            // Assert
            var actual = result as ObjectResult;

            Assert.NotNull(actual);
            Assert.Equal(statusCode, actual.StatusCode);
        }

        [Fact]
        public async Task PostAsync_FileNotProvided()
        {
            // Arrange
            var input = new Dictionary<string, StringValues>()
            {
                { "dectionary", It.IsAny<string>() }
            };

            var formCollection = new FormCollection(input);

            var controller = new ImagesController(null);

            // Act
            var result = await controller.PostAsync(formCollection);
            var actual = result as ObjectResult;

            // Assert
            var expected = 400;

            Assert.NotNull(actual);
            Assert.Equal(expected, actual.StatusCode);
        }
    }
}