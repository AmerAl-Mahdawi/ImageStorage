namespace ImageStorage.Library.Models
{
    public class ImageModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public long Size { get; set; }
    }
}