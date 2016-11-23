
using System;
using System.Collections.Generic;


namespace IdentityServerAPI.Models
{

    public class VideoDB
    {
        public List<Video> videos { get; set;  }
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
