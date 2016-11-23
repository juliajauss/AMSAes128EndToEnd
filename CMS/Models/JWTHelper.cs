using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;

namespace IdentityServerAPI.Models
{
    public class JWTHelper
    {
        private static readonly string _issuer = "http://localhost:5000/identity";
        private string _clientGroup;

        public JWTHelper(string clientGroup)
        {
            _clientGroup = clientGroup;
        }

        /// <summary>
        /// This method is to decrypt all videos the client has access to. To decrypt the videos there are 3 steps necessary
        ///     1. Fetch manifest 
        ///        = get URL where you can request the decryption key for the video. 
        ///        Open the manifest of each video the client has access to. I saved the URL of the manifest in my VideoDatabase.json. 
        ///        In the manifest is the URL with a KID (that points to Azure Media Key Delivery Services) where you can ask for the Decryptionkey of your videos. 
        ///        Such that not everybody can get access to the decryption key of your videos, you have to verify that you have access via a JWT (Json Web Token). 
        ///        
        ///     2. C
        ///      
        /// </summary>
        public async Task<string> DecryptVideo(Video video)
        {
            var keyAquisitionUri = await FetchManifest(video);
            string token = CreateJWTToken(video);
            await GetDeliveryKey(keyAquisitionUri, token);
            return token;
        }

        public async Task<Uri> FetchManifest(Video video)
        {
            var httpClient = new HttpClient();
            var textFromFile = await httpClient.GetAsync(video.manifest);
            var stream = await textFromFile.Content.ReadAsStringAsync();
            var uri = GetURI(stream);

            return uri;
        }

        private async Task<byte[]> GetDeliveryKey(Uri keyDeliveryUri, string token)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(keyDeliveryUri);

            request.Method = "POST";
            request.ContentType = "text/xml";

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers["Authorization"] = token;
            }

            var response = await request.GetResponseAsync();

            var stream = response.GetResponseStream();
            if (stream == null) { throw new NullReferenceException("Response stream is null");}

            var buffer = new byte[256];
            var length = 0;
            while (stream.CanRead && length <= buffer.Length)
            {
                var nexByte = stream.ReadByte();
                if (nexByte == -1)
                {
                    break;
                }
                buffer[length] = (byte)nexByte;
                length++;
            }

            // AES keys must be exactly 16 bytes (128 bits).
            var key = new byte[length];
            Array.Copy(buffer, key, length);


            return key;
        }

        //Get URL with keyId (kid), looks like this in the manifest: keyUriTemplate = "https://wamsbayclus001kd-hs.cloudapp.net/HlsHandler.ashx?kid=da3813af-55e6-48e7-aa9f-a4d6031f7b4d"/>
        private static string GetIV(string item)
        {
            var result = "";
            var ivStartTag = "IV";
            var ivEndTag = "\"";

            if (item.Trim().Contains(ivStartTag))
            {
                var indexStart = item.IndexOf(ivStartTag)+4;
                result = item.Substring(indexStart);
                var indexEnd = result.IndexOf(ivEndTag)-1;
                result = result.Substring(0, indexEnd);
            }
            return result;
        }

        //Get IV, looks like this in Manifest:  <sea:CryptoPeriod IV="0xD7D7D7D7D7D7D7D7D7D7D7D7D7D7D7D7" 
        private static Uri GetURI(string item)
        {
            var result = "";
            var uriStartTag = "keyUriTemplate";
            var uriEndTag = "\"";

            if (item.Trim().Contains(uriStartTag))
            {
                var indexStart = item.IndexOf(uriStartTag) + 16;
                result = item.Substring(indexStart);
                var indexEnd = result.IndexOf(uriEndTag);
                result = result.Substring(0, indexEnd);
            }
            return new Uri(result);
        }

        //Logged-on user requests JWT for Azure Key Service
        //Braucht mindestens: HMAC SHA-256 (symmetric key) or RSA SHA-256 (asymmetric key, x509 certificate)

        private string CreateJWTToken(Video video)
        {
            var audience = video.allowedClientGroup;
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(video.primaryVerificationKey)), SecurityAlgorithms.HmacSha256); 
            JwtSecurityToken jwtToken = new JwtSecurityToken(issuer: _issuer, audience: audience, signingCredentials: signingCredentials, expires: DateTime.Now.AddDays(5));

            jwtToken.Payload.Add("urn:microsoft:azure:mediaservices:contentkeyidentifier", video.key);
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            string jwtTokenString = handler.WriteToken(jwtToken);

            return ("Bearer=" + jwtTokenString);
        }

        public List<Video> GetVideos()
        {
            var databaseContent = System.IO.File.ReadAllText(@"..\..\AzureMediaServicesProject\AMSsetup\AppData\VideoDatabase.json");
            var videoDB = JsonConvert.DeserializeObject<VideoDB>(databaseContent);

            List<Video> videos = new List<Video>();

                foreach (var vid in videoDB.videos)
                {
                    if (vid.allowedClientGroup == _clientGroup)
                    {
                        videos.Add(vid);
                    }
                }

            return videos;
        }

        //private List<string> GetGroupOfClient(HttpResponseMessage response)
        //{
        //    var content = response.Content.ReadAsStringAsync().Result;
        //    var contentArray = JArray.Parse(content);
        //    List<string> clientGroups = new List<string>();
        //    foreach (JObject con in contentArray.Children<JObject>())
        //    {
        //        if (con.Properties().First().Value.ToString() == "client_Group")
        //            clientGroups.Add(con.Properties().ElementAt(1).Value.ToString());
        //    }
        //    return clientGroups;
        //}
    }
}
