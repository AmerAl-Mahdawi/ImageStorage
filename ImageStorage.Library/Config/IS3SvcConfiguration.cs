using Amazon;

namespace ImageStorage.Library.Config
{
    public interface IS3SvcConfiguration
    {
        string AccessKey { get; set; }
        string BucketName { get; set; }
        RegionEndpoint RegionPoint { get; set; }
        string SecretKey { get; set; }
    }
}