using Amazon;

namespace ImageStorage.Library.Config
{
    public class S3SvcConfiguration : IS3SvcConfiguration
    {
        public string BucketName { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public RegionEndpoint RegionPoint { get; set; }
    }
}