namespace CMS.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using CMS.Models;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json.Linq;
    using System.Linq;

    [Route("video")]
    public class VideoController : Controller
    {
        private static string DataBaseFilename { get { return @"..\VideoDatabase.json"; } }
        //C:\Users\juliajau\documents\visual studio 2015\VideoDatabase.json'.

        // GET: /<controller>/
        public IActionResult Index()
        {
            var audience = VerifyUserAndGetClientGroup();
            if (audience == "")
                return Unauthorized();

            //Generate a JWT Token for the client regarding the users groups (Audience: A "normal employee" has different rights than a management employee and should get access to different videos)
            JWTHelper jwthelper = new JWTHelper();
            var videos = jwthelper.GetVideos(DataBaseFilename, audience);

            //I have only one video per clientGroup
            var video = videos.First();

            //Decrypt all Videos the client has access to 
            var token = jwthelper.CreateJWTToken(video);

            ViewBag.JWTToken = token; 
            ViewBag.VideoURL = video.manifest;

            return View("~/Views/Video.cshtml");
        }

        private string VerifyUserAndGetClientGroup()
        {
            //Normally you should very the auth token etc. but for demo purposes I just do this 
            var content = HttpContext.Session.GetString("httpResponseContent");
            var contentArray = JArray.Parse(content);
            string audience = "";
            foreach (JObject con in contentArray.Children<JObject>())
            {
                if (con.Properties().First().Value.ToString() == "client_Group")
                    audience = con.Properties().ElementAt(1).Value.ToString();
            }
            return audience;

        }
    }
}
