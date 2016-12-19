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

        // GET: /<controller>/
        public IActionResult Index()
        {
            var audience = VerifyUserAndGetClientGroup();
            if (audience == "")
                return Unauthorized();

            //Generate a JWT Token for the client regarding the users audience (staff|management) 
            JWTHelper jwthelper = new JWTHelper();
            var videos = jwthelper.GetVideos(DataBaseFilename, audience);

            //I have only one video per audience
            var video = videos.First();
            var token = jwthelper.CreateJWTToken(video);

            ViewBag.JWTToken = token; 
            ViewBag.VideoURL = video.manifest;

            return View("~/Views/Video.cshtml");
        }

        /// <summary>
        /// Get the audience of the client using the session. 
        /// </summary>
        /// <returns></returns>
        private string VerifyUserAndGetClientGroup()
        {
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
