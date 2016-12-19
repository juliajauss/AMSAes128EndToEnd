namespace CMS.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;

    public class VideoDB
    {
        public List<Video> videos { get; set; } = new List<Video>();

        public VideoDB() { }
        public VideoDB(IEnumerable<Video> videos)
        {

            this.videos.AddRange(videos);
        }

        public void Save(string path)
        {
            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText(path, json);
        }

        public static VideoDB LoadFromFile(string filename)
        {
            var jsonText = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<VideoDB>(jsonText);
        }
    }

    public class Video
    {
        public object key { get; set; }
        public string primaryVerificationKey { get; set; }
        public string assetFile { get; set; }
        public string manifest { get; set; }
        public object VideoTitle { get; set; }
        public string allowedClientGroup { get; set; }
        public string id { get; set; }
        public string filename { get; set; }
        public object hlsUri { get; set; }
        public object mpegdashUri { get; set; }
        public object smoothStreamingUri { get; set; }

        //public List<Uri> progressiveDownloadUris { get; set; }
    }
}
