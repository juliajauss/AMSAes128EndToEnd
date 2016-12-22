namespace AMSsetup
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using Microsoft.WindowsAzure.MediaServices.Client;
    using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;
    using System.Configuration;
    using System.Threading;
    using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
    using System.Security.Cryptography;
    using IdentityServerAPI.Models;
    using System.Threading.Tasks;

    class Settings
    {
        public string Issuer { get; set; }
        public byte[] primaryVerificationKey { get; set; }
    }

    class VideoToPublish
    {
        public string Filename { get; set; }
        public string Audience { get; set; }
    }

    class Program
    {
        public static readonly Action<string> log = (msg) => Console.Out.WriteLine(msg);

        static void Main(string[] args)
        {
            Console.Title = "Azure Media Services Setup";
            Func<string, string> env = k => Environment.GetEnvironmentVariable(k);
            Func<string, string> appconfig = k => ConfigurationManager.AppSettings[k];

            var videosToEncode = new List<VideoToPublish>
            {
                new VideoToPublish { Filename = @"..\..\..\ams101.mp4", Audience = "Staff" },
                new VideoToPublish { Filename = @"..\..\..\ams102.mp4", Audience = "Management" }
            };

            var settings = new Settings
            {
                Issuer = appconfig("Issuer"),
                primaryVerificationKey = Convert.FromBase64String(appconfig("PrimaryVerificationKey"))
            };
            
            //Setup Azure Media Services Account
            var context = new CloudMediaContext(new MediaServicesCredentials(
                clientId: env("AMSACCNAME"), 
                clientSecret: env("AMSACCOUNTKEY")));

            Func<VideoToPublish, Task<Video>> UploadAndEncodeAsync = async (videoToPublish) =>
            {
                //Upload video (mezzanine) and create asset (or use existing asset)
                var mezzanineAsset = await UploadFileAndCreateAssetOrUseExistingAsync(
                    context: context,
                    fileName: videoToPublish.Filename);

                //Encode Asset
                var encodedAsset = await EncodeToAdaptiveBitrateMP4sAsync(
                    context: context, 
                    asset: mezzanineAsset);

                //Setup AES 128 Encryption for my assets
                var guidKey = await SetupAESEncryptionAsync(
                    context: context,
                    settings: settings,
                    encodedAsset: encodedAsset,
                    audience: videoToPublish.Audience);

                //Publish Videos & Create Locators
                return await PublishAssetGetURLsAsync(
                    context: context,
                    asset: encodedAsset,
                    audience: videoToPublish.Audience,
                    guid: guidKey,
                    primaryVerificationKey: settings.primaryVerificationKey);
            };

            var allTasks = videosToEncode.Select(UploadAndEncodeAsync).ToArray();
            try
            {
                Task.WaitAll(allTasks); //Wait for everything to be completed
            }
            catch (AggregateException e)
            {
                Console.WriteLine("\nThe following exceptions have been thrown by WaitAll(): (THIS WAS EXPECTED)");
                for (int j = 0; j < e.InnerExceptions.Count; j++)
                {
                    Console.WriteLine("\n-------------------------------------------------\n{0}", e.InnerExceptions[j].ToString());
                }
            }


            //Save upload, encoded and decrypted videos in your Video Database! 

            var videoDB = new VideoDB(allTasks.Select(_ => _.Result));
            videoDB.Save(path: @"..\..\..\VideoDatabase.json");
        }

        static public async Task<IAsset> UploadFileAndCreateAssetOrUseExistingAsync(CloudMediaContext context, string fileName)
        {
            var name = new FileInfo(fileName).Name;

            //Check if an Asset with this name is already existant and if yes, return it
            var inputAsset = context.Assets.Where(_ => _.Name == name).FirstOrDefault();
            if (inputAsset != null)
            {
                return inputAsset;
            }

            //Asset not existant -> Upload it! And output status to console 
            //Action<IAssetFile, UploadProgressChangedEventArgs> log = (af, p) => Program.log($"Uploading '{af.Name}' - Progress: {string.Format("0:0.##}%", p.Progress)}");

            inputAsset = await context.Assets.CreateFromFileAsync(
                filePath: fileName,
                options: AssetCreationOptions.None,
                //uploadProgressChangedCallback: log,
                cancellationToken: new CancellationTokenSource().Token);
            Program.log($"Asset with ID: {inputAsset.Id} and name: {inputAsset.Name} created.");

            return inputAsset;
        }

        static public async Task<IAsset> EncodeToAdaptiveBitrateMP4sAsync(CloudMediaContext context, IAsset asset)
        {
            //Is the asset already encoded? Then return it! 
            var outputAssetName = $"{asset.Name}_Adaptive Bitrate MP4";
            var outputAsset = context.Assets.Where(_ => _.Name == outputAssetName).FirstOrDefault();
            if (outputAsset != null)
            {
                return outputAsset;
            }

            // Prepare a job with a single task to transcode the specified asset into a multi-bitrate asset.
            IJob job = context.Jobs.CreateWithSingleTask(
                mediaProcessorName: "Media Encoder Standard",
                taskConfiguration: "H264 Multiple Bitrate 720p",
                inputAsset: asset,
                outputAssetName: outputAssetName,
                outputAssetOptions: AssetCreationOptions.None); 

            // Submit the job and wait until it is completed.
            Program.log("Submitting transcoding job...");
            job.Submit();

            Action<IJob> printDebugInfo = (j) =>
            {
                Program.log($"Job state: {j.State}");
                Program.log($"Job progress: {j.GetOverallProgress()}%");
            };
            job = await job.StartExecutionProgressTask(printDebugInfo, CancellationToken.None);
            Program.log("Transcoding job finished.");

            return job.OutputMediaAssets[0];
        }

        static public async Task<Video> PublishAssetGetURLsAsync(IAsset asset, CloudMediaContext context, string audience, Guid guid, byte[] primaryVerificationKey)
        {
            var defaultDuration = TimeSpan.FromDays(30);

            var locators = context.Locators
                .Where(locator => locator.AssetId == asset.Id)
                .ToList();

            var streamingLocator = locators.FirstOrDefault(locator => locator.Type == LocatorType.OnDemandOrigin);
            if (streamingLocator == null)
            {
                await context.Locators.CreateAsync(
                    locatorType: LocatorType.OnDemandOrigin,
                    asset: asset, 
                    permissions: AccessPermissions.Read, 
                    duration: defaultDuration);

                Program.log($"Created OnDemandOrigin locator for asset {asset.Name}.");
            }

            var sasLocator = locators.FirstOrDefault(locator => locator.Type == LocatorType.Sas);
            if (sasLocator == null)
            {
                await context.Locators.CreateAsync(
                    locatorType: LocatorType.Sas,
                    asset: asset, 
                    permissions: AccessPermissions.Read, 
                    duration: defaultDuration);

                Program.log($"Created Sas locator for asset {asset.Name}.");
            }

            // Get a reference to the streaming manifest file from the collection of files in the asset. 
            var manifestFile = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();

            //Create a new video object to save the uris 
            return new Video
            {
                VideoTitle = asset.Name,
                key = guid,
                primaryVerificationKey = Convert.ToBase64String(primaryVerificationKey),
                id = asset.Id,
                audience = audience,
                filename = asset.Name,
                assetFile = asset.GetManifestAssetFile().GetSasUri().ToString(),
                manifest = streamingLocator.Path + manifestFile.Name + "/manifest",
                hlsUri = asset.GetHlsUri(),
                smoothStreamingUri = asset.GetSmoothStreamingUri(),
                mpegdashUri = asset.GetMpegDashUri()//,
                //progressiveDownloadUris = asset.AssetFiles.Where(af => af.Name.ToLower().EndsWith(".mp4")).Select(af => af.GetSasUri()).ToList()
            };
        }


        static IAsset GetAsset(CloudMediaContext context, string assetId)
        {
            var assetInstance = from a in context.Assets where a.Id == assetId select a;
            IAsset asset = assetInstance.FirstOrDefault();
            return asset;
        }

        private static async Task<Guid> SetupAESEncryptionAsync(CloudMediaContext context, IAsset encodedAsset,  string audience, Settings settings)
        {
            //1.Create a content key and associate it with the encoded asset 
            IContentKey key = CreateEnvelopeTypeContentKey(encodedAsset, context);
            Program.log($"Created key {key.Id} for the asset {encodedAsset.Id}");

            //2.Configure the content keys authorization policy (How do you get the encryption key: Token/IP/Open) // True => you need a claim in your JWT Token specifying the key id guid 
            string tokenTemplateString = await AddTokenRestrictedAuthorizationPolicy(
                context: context,
                contentKey: key,
                audience: audience,
                contentKeyIdentifierClaim: true,
                issuer: settings.Issuer,
                primaryVerificationKey: settings.primaryVerificationKey);

            Program.log($"Added authorization policy: {key.AuthorizationPolicyId}");

            //3.Create Asset Delivery Policy (Dynamic or non-dynamic encryption)
            CreateAssetDeliveryPolicy(encodedAsset, key, context);
            Console.WriteLine("Created asset delivery policy. \n");
            Console.WriteLine();

            // Deserializes a string containing an Xml representation of a TokenRestrictionTemplate back into a TokenRestrictionTemplate class instance.
            TokenRestrictionTemplate tokenTemplate = TokenRestrictionTemplateSerializer.Deserialize(tokenTemplateString);

            // Generate a test token based on the data in the given TokenRestrictionTemplate.
            // Note, you need to pass the key id Guid because we specified TokenClaim.ContentKeyIdentifierClaim in during the creation of TokenRestrictionTemplate.
            Guid rawkey = EncryptionUtils.GetKeyIdAsGuid(key.Id);

            //The GenerateTestToken method returns the token without the word “Bearer” in front so you have to add it in front of the token string. 
            string testToken = TokenRestrictionTemplateSerializer.GenerateTestToken(tokenTemplate, null, rawkey);
            Console.WriteLine("The authorization token is:\nBearer {0}", testToken);
            Console.WriteLine();

            return rawkey;
        }
        
        static public IContentKey CreateEnvelopeTypeContentKey(IAsset asset, CloudMediaContext context)
        {
            //Check if there is already a content key associated with the asset
            IContentKey contentKey = asset.ContentKeys.FirstOrDefault(k => k.ContentKeyType == ContentKeyType.EnvelopeEncryption);

            // Create envelope encryption content key & Associate the key with the asset
            if (contentKey == null)
            {
                contentKey = context.ContentKeys.Create(
                    keyId: Guid.NewGuid(),
                    contentKey: GetKeyBytes(16),
                    name: "ContentKey",
                    contentKeyType: ContentKeyType.EnvelopeEncryption);

                asset.ContentKeys.Add(contentKey);
            }

            return contentKey;
        }

        private static byte[] GetKeyBytes(int size)
        {
            var randomBytes = new byte[size];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            return randomBytes;
        }

        public static async Task<string> AddTokenRestrictedAuthorizationPolicy(CloudMediaContext context, IContentKey contentKey, string issuer, string audience,  bool contentKeyIdentifierClaim, byte[] primaryVerificationKey)
        {
            string tokenTemplateString = GenerateTokenRequirements(
                issuer: issuer, 
                audience: audience, 
                contentKeyIdentifierClaim: contentKeyIdentifierClaim,
                primaryVerificationKey: primaryVerificationKey);

            IContentKeyAuthorizationPolicy policy = await context.ContentKeyAuthorizationPolicies.CreateAsync(name: "Token restricted authorization policy");
            List<ContentKeyAuthorizationPolicyRestriction> restrictionList = new List<ContentKeyAuthorizationPolicyRestriction>
            {
                new ContentKeyAuthorizationPolicyRestriction
                {
                        Name = "Token Authorization Policy",
                        KeyRestrictionType = (int)ContentKeyRestrictionType.TokenRestricted,
                        Requirements = tokenTemplateString
                }
            };

            //You could have multiple options; BaselineHttp specifies that we use the AES key server from AMS 
            IContentKeyAuthorizationPolicyOption policyOption = context.ContentKeyAuthorizationPolicyOptions.CreateAsync(
                name: "Token Authorization policy option", 
                deliveryType: ContentKeyDeliveryType.BaselineHttp, 
                restrictions: restrictionList,
                keyDeliveryConfiguration: null).Result;  // no key delivery data is needed for HLS                                     

            policy.Options.Add(policyOption);

            // Add ContentKeyAutorizationPolicy to ContentKey
            contentKey.AuthorizationPolicyId = policy.Id;
            IContentKey updatedKey = await contentKey.UpdateAsync();
            Program.log($"Adding Key to Asset: Key ID is {updatedKey.Id}");

            return tokenTemplateString;
        }

        static private string GenerateTokenRequirements(string issuer, string audience, bool contentKeyIdentifierClaim, byte[] primaryVerificationKey)
        {
            var template = new TokenRestrictionTemplate(TokenType.JWT)
            {
                PrimaryVerificationKey = new SymmetricVerificationKey(primaryVerificationKey),
                Issuer = issuer,
                Audience = audience
            };
            
            if (contentKeyIdentifierClaim)
                template.RequiredClaims.Add(TokenClaim.ContentKeyIdentifierClaim);

            //You can create a test token, useful to debug your final token if anything is not working as you want
            string testToken = TokenRestrictionTemplateSerializer.GenerateTestToken(template);
            Program.log($"The authorization token is: Bearer {testToken}");

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
        static public async void CreateAssetDeliveryPolicy(IAsset asset, IContentKey key, CloudMediaContext context)
        {
            Uri keyAcquisitionUri = await key.GetKeyDeliveryUrlAsync(ContentKeyDeliveryType.BaselineHttp);

            const string assetDeliveryPolicyName = "AssetDeliveryPolicy for HLS, SmoothStreaming and MPEG-DASH";
            IAssetDeliveryPolicy assetDeliveryPolicy = context.AssetDeliveryPolicies
                .Where(p => p.Name == assetDeliveryPolicyName)
                .ToList().FirstOrDefault();

            if (assetDeliveryPolicy == null)
            {
                assetDeliveryPolicy = await context.AssetDeliveryPolicies.CreateAsync(
                    name: assetDeliveryPolicyName,
                    policyType: AssetDeliveryPolicyType.DynamicEnvelopeEncryption,
                    deliveryProtocol: AssetDeliveryProtocol.SmoothStreaming | AssetDeliveryProtocol.HLS | AssetDeliveryProtocol.Dash,
                    configuration: new Dictionary<AssetDeliveryPolicyConfigurationKey, string> {
                        { AssetDeliveryPolicyConfigurationKey.EnvelopeBaseKeyAcquisitionUrl, keyAcquisitionUri.AbsoluteUri }
                    });

                // Add AssetDelivery Policy to the asset
                asset.DeliveryPolicies.Add(assetDeliveryPolicy);
            }

            Program.log("Adding Asset Delivery Policy: " + assetDeliveryPolicy.AssetDeliveryPolicyType);
        }
    }
}
