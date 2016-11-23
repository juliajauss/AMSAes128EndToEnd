using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using IdentityServerAPI.Models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Linq;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace IdentityServerAPI.Controllers
{
    [Route("video")]
    public class VideoController : Controller
    {
        // GET: /<controller>/
        public async Task<IActionResult> Index()//List<string> clientGroups)
        {
            var clientGroup = VerifyUserAndGetClientGroup();
            if (clientGroup == "")
                return Unauthorized(); // View("~/Views/Home.cshtml");

            //Generate a JWT Token for the client regarding the users groups 
            //(A "normal employee" has different rights than a management employee and should get access to different videos)
            JWTHelper jwthelper = new JWTHelper(clientGroup);
            var videos = jwthelper.GetVideos();

            //I have only one video per clientGroup
            var video = videos[0];

            //Decrypt all Videos the client has access to 
            var token = await jwthelper.DecryptVideo(video);

            ViewBag.JWTToken = token; 
            ViewBag.VideoURL = video.manifest;

            return View("~/Views/Video.cshtml");
        }

        private string VerifyUserAndGetClientGroup()
        {
            //Normally you should very the auth token etc. but for demo purposes I just do this 
            var content = HttpContext.Session.GetString("httpResponseContent");
            var contentArray = JArray.Parse(content);
            string clientGroup = "";
            foreach (JObject con in contentArray.Children<JObject>())
            {
                if (con.Properties().First().Value.ToString() == "client_Group")
                    clientGroup = con.Properties().ElementAt(1).Value.ToString();
            }
            return clientGroup;

        }
    }
}
