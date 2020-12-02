using ImageStorage.Library.Models;
using ImageStorage.Library.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ImagesStorage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IImageStore _imageStore;

        public ImagesController(IImageStore imageStore)
        {
            _imageStore = imageStore;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(string description = "",
                                                  string type = "",
                                                  int minSize = 1,
                                                  int maxSize = 512000,
                                                  int pageNumber = 0,
                                                  int pageSize = 20)
        {
            try
            {
                string validation = "";
                bool flag = true;
                if (minSize < 1 || maxSize > 512000)
                {
                    validation += "Invalid image size: it should be between 1 and 500 (512000 B) KB\n";
                    flag = false;
                }
                else if (minSize > maxSize)
                {
                    validation += "Image minimum size hould be less than maximum size \n";
                    flag = false;
                }
                if (pageNumber < 0)
                {
                    validation += "Invalid page number: it should be >= 0!\n";
                    flag = false;
                }
                if (pageSize <= 0)
                {
                    validation += "Invalid page size: it should be >= 1!\n";
                    flag = false;
                }

                if (flag)
                {
                    if (pageSize > 20)
                    {
                        pageSize = 20;
                    }
                    var output = await _imageStore.GetByFilterAsync(description, type, pageNumber, pageSize);

                    if (output.Item2 == "")
                    {
                        return Ok(output.Item1);
                    }

                    return BadRequest(output.Item2);
                }

                return BadRequest(validation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error occurred while trying to upload the image!\n{ ex.Message }");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(IFormCollection request)
        {
            try
            {
                string validation = "";
                bool flag = true;

                var file = request.Files.GetFile("file");
                if (file is null)
                {
                    return BadRequest("Please attach an image with your request!");
                }

                StringValues description;

                if (!request.TryGetValue("description", out description))
                {
                    validation += "Image description is required!\n";
                    flag = false;
                }

                var size = file.Length;
                var imageName = file.FileName;
                var imageType = file.ContentType;

                if (imageType != "image/png" && imageType != "image/jpeg")
                {
                    validation += "Image type should be either png or jpeg!\n";
                    flag = false;
                }

                if (size > 512000)
                {
                    validation += "Image size should be 500 KB (512000 B) or less!\n";
                    flag = false;
                }
                else if (size <= 0)
                {
                    validation += "Image size should be greater than 0!\n";
                    flag = false;
                }
                if (flag)
                {
                    var image = new ImageModel()
                    {
                        Name = $"{ imageName }_{ Guid.NewGuid() }",
                        Description = description.ToString(),
                        Type = imageType,
                        Size = size
                    };

                    Stream imageStream = file.OpenReadStream();

                    var output = await _imageStore.UploadAsync(image, imageStream);

                    if (output.StartsWith("Successful Upload: Image "))
                    {
                        return StatusCode(201, output);
                    }

                    return StatusCode(500, output);
                }
                else
                {
                    return BadRequest(validation);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error occurred while trying to upload the image!\n{ ex.Message }");
            }
        }
    }
}