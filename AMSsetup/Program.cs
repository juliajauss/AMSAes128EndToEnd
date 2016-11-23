using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;
using System.Configuration;
using System.Threading;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using System.Security.Cryptography;
using IdentityServerAPI.Models;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace AMSsetup
{
    class Program
    {
        private static readonly string _mediaServicesAccountName = ConfigurationManager.AppSettings["MediaServicesAccountName"];
        private static readonly string _mediaServicesAccountKey = ConfigurationManager.AppSettings["MediaServicesAccountKey"];
        static string _storageAccountName = ConfigurationManager.AppSettings["MediaServicesStorageAccountName"];
        static string _storageAccountKey = ConfigurationManager.AppSettings["MediaServicesStorageAccountKey"];
        private static CloudMediaContext _context = null;
        private static MediaServicesCredentials _cachedCredentials = null;
        static VideoDB _videoDB;
        static string _primaryVerificationKey = ConfigurationManager.AppSettings["PrimaryVerificationKey"];

        // A Uri describing the issuer of the token. Must match the value in the token for the token to be considered valid.
        private static readonly Uri _sampleIssuer = new Uri(ConfigurationManager.AppSettings["Issuer"]);

        // The Audience of the token. Must match the value in the token for the token to be considered valid.
        private static readonly string _clientGroupStaff = (ConfigurationManager.AppSettings["Staff"]);
        private static readonly string _clientGroupManagement = (ConfigurationManager.AppSettings["Management"]);

        //The Video Files that you want to use 
        private static readonly string _staffVideoFile = ConfigurationManager.AppSettings["StaffVideoFile"];
        private static readonly string _mgmtVideoFile = ConfigurationManager.AppSettings["ManagementVideoFile"];
        private static readonly string _upload = ConfigurationManager.AppSettings["Upload"];

        static void Main(string[] args)
        {
            Initialize();
            SelectMediaServicesAccount();

            //Upload assets or use existing ones
            IAsset assetStaff;
            IAsset assetManagement;

            if (_upload == "true")
            {
                assetStaff = UploadFileAndCreateAsset(_staffVideoFile); //Upload Asset 1
                assetManagement = UploadFileAndCreateAsset(_mgmtVideoFile); //Upload Asset 2
            }
            else
            { //Or use already existing assets (specify the ID of the asset in your app.config, e.g. StaffVideoFile = "nb:cid:UUID:8e7255dd-39cf-4b61-b578-74aa279ecba1"
                assetStaff = GetAsset(_staffVideoFile);
                assetManagement = GetAsset(_mgmtVideoFile);
            }

            //Encode Assets
            IAsset encodedassetStaff = EncodeToAdaptiveBitrateMP4s(assetStaff);
            IAsset encodedassetManagement = EncodeToAdaptiveBitrateMP4s(assetManagement);

            //Setup AES 128 Encryption for my assets
            var guidKey = SetupAESEncryption(encodedassetStaff, _clientGroupStaff);
            var guidKey2 = SetupAESEncryption(encodedassetManagement, _clientGroupManagement);

            //Publish Videos & Create Locators
            PublishAssetGetURLs(encodedassetStaff, _clientGroupStaff, guidKey);
            PublishAssetGetURLs(encodedassetManagement, _clientGroupManagement, guidKey2);

            //Save Videos in my local Json "Database"
            SaveAssetsinDB();
        }

        private static void Initialize()
        {
            Console.Title = "Azure Media Services Setup";
            _videoDB = new VideoDB();
            _videoDB.videos = new List<Video>();
        }

        /// <summary>
        /// 
        /// </summary>
        static void SelectMediaServicesAccount()
        {
            try
            {
                _cachedCredentials = new MediaServicesCredentials(_mediaServicesAccountName, _mediaServicesAccountKey); // Create and cache the Media Services credentials in a static class variable.
                _context = new CloudMediaContext(_cachedCredentials);  // Used the chached credentials to create CloudMediaContext.
            }
            catch (Exception exception)
            {
                exception = MediaServicesExceptionParser.Parse(exception);
                Console.Error.WriteLine(exception.Message);
            }
            finally
            {
                Console.Out.WriteLine("Successfully set up your Media Services Account.");
            }
        }

        /// <summary>
        ///  Option StorageEncrypted is needed for AES Encryption
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        static public IAsset UploadFileAndCreateAsset(string fileName)
        {
            IAsset inputAsset = _context.Assets.CreateFromFile(fileName, AssetCreationOptions.None, (af, p) => { Console.WriteLine("Uploading '{0}' - Progress: {1:0.##}%", af.Name, p.Progress); });
            Console.WriteLine("Asset with ID: {0} and name: {1} created.", inputAsset.Id, inputAsset.Name);
            return inputAsset;
        }

        static public IAsset EncodeToAdaptiveBitrateMP4s(IAsset asset)
        {
            AssetCreationOptions options = AssetCreationOptions.None; //StorageEncrypted;

            // Prepare a job with a single task to transcode the specified asset into a multi-bitrate asset.
            IJob job = _context.Jobs.CreateWithSingleTask("Media Encoder Standard", "H264 Multiple Bitrate 720p", asset, (asset.Name + "_Adaptive Bitrate MP4"), options);
            Console.WriteLine("Submitting transcoding job...");

            // Submit the job and wait until it is completed.
            job.Submit();
            job = job.StartExecutionProgressTask(j => { Console.WriteLine("Job state: {0}", j.State); Console.WriteLine("Job progress: {0:0.##}%", j.GetOverallProgress()); }, CancellationToken.None).Result;
            Console.WriteLine("Transcoding job finished.");

            IAsset outputAsset = job.OutputMediaAssets[0];
            return outputAsset;
        }

        static public void PublishAssetGetURLs(IAsset asset, string clientGroup, Guid guid)
        {
            // Publish the output asset by creating an Origin locator for adaptive streaming, and a SAS locator for progressive download.
            ILocator streamingLocator = _context.Locators.Create(LocatorType.OnDemandOrigin, asset, AccessPermissions.Read, TimeSpan.FromDays(30));
            ILocator sasLocator = _context.Locators.Create(LocatorType.Sas, asset, AccessPermissions.Read, TimeSpan.FromDays(30));
            Console.WriteLine("Created Locators for asset {0}.", asset.Name);
            IEnumerable<IAssetFile> mp4AssetFiles = asset.AssetFiles.ToList().Where(af => af.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

            // Get a reference to the streaming manifest file from the collection of files in the asset. 
            var manifestFile = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();

            //Create a new video object to save the uris 
            Video video = new Video
            {
                key = guid,
                primaryVerificationKey = _primaryVerificationKey, 
                id = asset.Id,
                allowedClientGroup = clientGroup,
                filename = asset.Name,
                assetFile = asset.GetManifestAssetFile().GetSasUri().ToString(),
                manifest = streamingLocator.Path + manifestFile.Name + "/manifest",
                hlsUri = asset.GetHlsUri(),
                smoothStreamingUri = asset.GetSmoothStreamingUri(),
                mpegdashUri = asset.GetMpegDashUri()//,
                //progressiveDownloadUris = mp4AssetFiles.Select(af => af.GetSasUri()).ToList()
            };
            _videoDB.videos.Add(video);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="videos"></param>
        private static void SaveAssetsinDB()//VideoDB[] videos)
        {
            string filename = "C:/Users/juliajau/documents/visual studio 2015/Projects/AzureMediaServicesProject/AMSsetup/AppData/VideoDatabase.json";
            var json = JsonConvert.SerializeObject(_videoDB);
            File.WriteAllText(filename, json);
        }

        static IAsset GetAsset(string assetId)
        {
            var assetInstance = from a in _context.Assets where a.Id == assetId select a;
            IAsset asset = assetInstance.FirstOrDefault();
            return asset;
        }

        //Encryption Stuff

        private static Guid SetupAESEncryption(IAsset encodedAsset, string clientGroup)
        {
            //1.Create a content key and associate it with the encoded asset 
            IContentKey key = CreateEnvelopeTypeContentKey(encodedAsset);
            Console.WriteLine("Created key {0} for the asset {1} ", key.Id, encodedAsset.Id);
            Console.WriteLine();

            //2.Configure the content keys authorization policy (How do you get the encryption key: Token/IP/Open)
            // True => you need a claim in your JWT Token specifying the key id guid 
            string tokenTemplateString = AddTokenRestrictedAuthorizationPolicy(key, clientGroup, true);
            Console.WriteLine("Added authorization policy: {0}", key.AuthorizationPolicyId);
            Console.WriteLine();

            //3.Create Asset Delivery Policy (Dynamic or non-dynamic Encryption)
            CreateAssetDeliveryPolicy(encodedAsset, key);
            Console.WriteLine("Created asset delivery policy. \n");
            Console.WriteLine();

            // Deserializes a string containing an Xml representation of a TokenRestrictionTemplate back into a TokenRestrictionTemplate class instance.
            TokenRestrictionTemplate tokenTemplate = TokenRestrictionTemplateSerializer.Deserialize(tokenTemplateString);

            // Generate a test token based on the data in the given TokenRestrictionTemplate.
            // Note, you need to pass the key id Guid because we specified TokenClaim.ContentKeyIdentifierClaim in during the creation of TokenRestrictionTemplate.
            Guid rawkey = EncryptionUtils.GetKeyIdAsGuid(key.Id);

            //The GenerateTestToken method returns the token without the word “Bearer” in front
            //so you have to add it in front of the token string. 
            string testToken = TokenRestrictionTemplateSerializer.GenerateTestToken(tokenTemplate, null, rawkey);
            Console.WriteLine("The authorization token is:\nBearer {0}", testToken);
            Console.WriteLine();

            return rawkey;
        }
        
        static public IContentKey CreateEnvelopeTypeContentKey(IAsset asset)
        {
            // Create envelope encryption content key
            Guid keyId = Guid.NewGuid();
            byte[] contentKey = GetRandomBuffer(16);

            // Associate the key with the asset
            IContentKey key = _context.ContentKeys.Create(keyId, contentKey, "ContentKey", ContentKeyType.EnvelopeEncryption);
            asset.ContentKeys.Add(key);  

            return key;
        }

        public static string AddTokenRestrictedAuthorizationPolicy(IContentKey contentKey, string clientGroup, bool contentKeyIdentifierClaim)
        {
            string tokenTemplateString = GenerateTokenRequirements(clientGroup, contentKeyIdentifierClaim);

            IContentKeyAuthorizationPolicy policy = _context.ContentKeyAuthorizationPolicies.CreateAsync("HLS token restricted authorization policy").Result;
            List<ContentKeyAuthorizationPolicyRestriction> restrictionList = new List<ContentKeyAuthorizationPolicyRestriction>();

            ContentKeyAuthorizationPolicyRestriction myrestriction = new ContentKeyAuthorizationPolicyRestriction
            {
                        Name = "Token Authorization Policy",
                        KeyRestrictionType = (int)ContentKeyRestrictionType.TokenRestricted,
                        Requirements = tokenTemplateString
            };
            restrictionList.Add(myrestriction);

            //You could have multiple options 
            //BaselineHttp specifies that we use the AES key server from AMS 
            IContentKeyAuthorizationPolicyOption policyOption = _context.ContentKeyAuthorizationPolicyOptions.Create("Token Authorization policy option",  ContentKeyDeliveryType.BaselineHttp, restrictionList, null);  // no key delivery data is needed for HLS                                     
            policy.Options.Add(policyOption);

            // Add ContentKeyAutorizationPolicy to ContentKey
            contentKey.AuthorizationPolicyId = policy.Id;
            IContentKey updatedKey = contentKey.UpdateAsync().Result;
            
            Console.WriteLine("Adding Key to Asset: Key ID is " + updatedKey.Id);

            return tokenTemplateString;
        }

        static private string GenerateTokenRequirements(string clientGroup, bool contentKeyIdentifierClaim)
        {
            TokenRestrictionTemplate template = new TokenRestrictionTemplate(TokenType.JWT);
            template.PrimaryVerificationKey = new SymmetricVerificationKey(Convert.FromBase64String(_primaryVerificationKey));
            template.Issuer = _sampleIssuer.ToString();

            if (clientGroup == _clientGroupStaff)
                template.Audience = _clientGroupStaff;
            else if (clientGroup == _clientGroupManagement)
                template.Audience = _clientGroupManagement;
            
            if (contentKeyIdentifierClaim)
                template.RequiredClaims.Add(TokenClaim.ContentKeyIdentifierClaim);

            string testToken = TokenRestrictionTemplateSerializer.GenerateTestToken(template);
            Console.WriteLine("The authorization token is:\nBearer {0}", testToken);

            return TokenRestrictionTemplateSerializer.Serialize(template);
        }


        /// <summary>
        /// When configuring delivery policy, you can choose to associate it with a key acquisition URL that has a KID appended or
        // or a key acquisition URL that does not have a KID appended in which case a content key can be reused. 

        // EnvelopeKeyAcquisitionUrl:  contains a key ID in the key URL.
        // EnvelopeBaseKeyAcquisitionUrl:  the URL does not contains a key ID

        // The following policy configuration specifies: 
        // key url that will have KID=<Guid> appended to the envelope and
        // the Initialization Vector (IV) to use for the envelope encryption.

        /// </summary>
        /// <param name="asset"></param>
        /// <param name="key"></param>
        static public void CreateAssetDeliveryPolicy(IAsset asset, IContentKey key)
        {
            //Where do I get the Encrytion Key?
            Uri keyAcquisitionUri = key.GetKeyDeliveryUrl(ContentKeyDeliveryType.BaselineHttp);

            // Removed in March 2016.In order to use EnvelopeBaseKeyAcquisitionUrl and reuse the same policy for several assets
            //string envelopeEncryptionIV = Convert.ToBase64String(GetRandomBuffer(16));

            Dictionary<AssetDeliveryPolicyConfigurationKey, string> assetDeliveryPolicyConfiguration = new Dictionary<AssetDeliveryPolicyConfigurationKey, string> {
                { AssetDeliveryPolicyConfigurationKey.EnvelopeKeyAcquisitionUrl, keyAcquisitionUri.ToString()}};

            IAssetDeliveryPolicy assetDeliveryPolicy = _context.AssetDeliveryPolicies.Create(
                "AssetDeliveryPolicy for HLS, SmoothStreaming and MPEG-DASH",
                AssetDeliveryPolicyType.DynamicEnvelopeEncryption,
                AssetDeliveryProtocol.SmoothStreaming | AssetDeliveryProtocol.HLS | AssetDeliveryProtocol.Dash,
                assetDeliveryPolicyConfiguration);

            // Add AssetDelivery Policy to the asset
            asset.DeliveryPolicies.Add(assetDeliveryPolicy);

            Console.WriteLine("Adding Asset Delivery Policy: " + assetDeliveryPolicy.AssetDeliveryPolicyType);
        }

        static private byte[] GetRandomBuffer(int size)
        {
            byte[] randomBytes = new byte[size];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            return randomBytes;
        }
    }
}
