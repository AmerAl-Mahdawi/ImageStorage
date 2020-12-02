using ImageStorage.Library.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImageStorage.Library.Stores
{
    public interface IImageStore
    {
        Task<(IEnumerable<ImageModel>, string)> GetByFilterAsync(string description, string type, int pageNumber, int pageSize);
        Task<string> UploadAsync(ImageModel image, Stream imageStream);
    }
}