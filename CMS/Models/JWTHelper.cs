namespace CMS.Models
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using Microsoft.IdentityModel.Tokens;
    using System.Linq;

    public class JWTHelper
    {
        private static readonly string _issuer = "http://localhost:5000/identity";

        public JWTHelper(){}

        //Logged-on user requests JWT for Azure Key Service
        //HMAC SHA-256 (symmetric key) or RSA SHA-256 (asymmetric key, x509 certificate)
        public string CreateJWTToken(Video video)
        {
            var audience = video.audience;
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(
                key: Convert.FromBase64String(video.primaryVerificationKey)), 
                algorithm: SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                issuer: _issuer, 
                audience: audience, 
                signingCredentials: signingCredentials, 
                expires: DateTime.Now.AddDays(5));

            jwtToken.Payload.Add("urn:microsoft:azure:mediaservices:contentkeyidentifier", video.key);

            var handler = new JwtSecurityTokenHandler();
            string jwtTokenString = handler.WriteToken(jwtToken);

            return $"Bearer={jwtTokenString}";
        }

        public IEnumerable<Video> GetVideos(string dbFilename, string audience)
        {
            return VideoDB
                .LoadFromFile(dbFilename).videos
                .Where(v => v.audience == audience);
        }
    }
}
