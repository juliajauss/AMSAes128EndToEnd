using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IdentityServerAPI.Models;
using IdentityModel.Client;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace IdentityServerAPI.Controllers
{
    [Route("home")]
    public class HomeController : Controller
    {
        public static string _scope = "CMSScope";
        public static string _discoveryClient = "http://localhost:5000";  //URL of my IdentityServer

        [HttpGet]
        public IActionResult Get()
        {
            return View("~/Views/Home.cshtml");
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            var clientname = model.Email;
            var secret = model.Password;

            // Request access token from our IdentityServer 
            var disco = await DiscoveryClient.GetAsync(_discoveryClient);           
            var tokenClient = new TokenClient(disco.TokenEndpoint, clientname, secret);
            var tokenResponse = await tokenClient.RequestClientCredentialsAsync(_scope);

            if (tokenResponse.IsError)
            {
                Debug.WriteLine(tokenResponse.Error);
                return View("~/Views/Home.cshtml");
            }
            Debug.WriteLine(tokenResponse.Json);

            //Send request to CMS with Token from IdentityServer
            var client = new HttpClient();
            client.SetBearerToken(tokenResponse.AccessToken);
            var response = await client.GetAsync("http://localhost:22943/identity");

            //Determine the group of the user. Is the user a "normal" employee or in the management?
            //var clientGroup = GetGroupOfClient(response);

            var content = response.Content.ReadAsStringAsync().Result;
            HttpContext.Session.SetString("httpResponseContent", content);

            // Call video page of CMS
            return RedirectToAction("Index", "Video"); //, new { clientGroups = clientGroup });
        }


    }
}
