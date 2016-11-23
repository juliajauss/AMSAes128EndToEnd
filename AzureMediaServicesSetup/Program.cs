using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;

namespace AzureMediaServicesSetup
{
    public class Program
    {

        private static readonly string MediaServicesAccountName = "julesmediaservice";
        private static readonly string MediaServicesAccountKey = "5BmDEqCzMvZolvmhvXqq7rmncS/DetmssS5MQa716Js=";
        private static readonly string MediaServicesStorageAccountName = "amsscenariodiag372";
        private static readonly string MediaServicesStorageAccountKey = "UZtf1RxVzKMSgCaa/TiFEpBjzNc7z3LUfcY5kxaDhV/Djljek6fGx1x+ItRLTPTgCH8oIZcOMuyRY381+eW1SQ==";
        private static CloudMediaContext _context = null;
        private static MediaServicesCredentials _cachedCredentials = null;

        public static void Main(string[] args)
        {
            Console.Out.WriteLine("TEST");
            SelectMediaServicesAccount();

        }

        static void SelectMediaServicesAccount()
        {
            try
            {
                // Create and cache the Media Services credentials in a static class variable.
                _cachedCredentials = new MediaServicesCredentials(MediaServicesAccountName, MediaServicesAccountKey);

                // Used the chached credentials to create CloudMediaContext.
                _context = new CloudMediaContext(_cachedCredentials);
            }
            catch (Exception exception)
            {
                // Parse the XML error message in the Media Services response and create a new
                // exception with its content.
                exception = MediaServicesExceptionParser.Parse(exception);
                Console.Error.WriteLine(exception.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Uploads to assets to Azure Media Services.
        /// </summary>
        static void UploadTwoAssets()
        {

            IAsset inputAsset = UploadAsset(@"C:\Users\juliajau\Desktop\DenHaag_test.mp4", AssetCreationOptions.None);
            IAsset encodedAsset = EncodeToAdaptiveBitrateMP4s(inputAsset, AssetCreationOptions.None);
            //PublishAssetGetURLs(encodedAsset);

            SaveAssetsinDB();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        static public IAsset UploadAsset(string fileName, AssetCreationOptions options)
        {
            IAsset inputAsset = _context.Assets.CreateFromFile(
                fileName,
                options,
                (af, p) =>
                {
                    Console.WriteLine("Uploading '{0}' - Progress: {1:0.##}%", af.Name, p.Progress);
                });

            Console.WriteLine("Asset {0} created.", inputAsset.Id);

            return inputAsset;
        }

        /// <summary>
        /// Saves the uploaded assets in our small database. 
        /// </summary>
        static void SaveAssetsinDB()
        {
            File.WriteAllText(@"..\..\..\video-database.json", "blubb");
        }

        static public IAsset EncodeToAdaptiveBitrateMP4s(IAsset asset, AssetCreationOptions options)
        {

            // Prepare a job with a single task to transcode the specified asset
            // into a multi-bitrate asset.

            IJob job = _context.Jobs.CreateWithSingleTask(
                "Media Encoder Standard",
                "H264 Multiple Bitrate 720p",
                asset,
                "Adaptive Bitrate MP4",
                options);

            Console.WriteLine("Submitting transcoding job...");


            // Submit the job and wait until it is completed.
            job.Submit();

            job = job.StartExecutionProgressTask(
                j =>
                {
                    Console.WriteLine("Job state: {0}", j.State);
                    Console.WriteLine("Job progress: {0:0.##}%", j.GetOverallProgress());
                },
                CancellationToken.None).Result;

            Console.WriteLine("Transcoding job finished.");

            IAsset outputAsset = job.OutputMediaAssets[0];

            return outputAsset;
        }

        static public void PublishAssetGetURLs(IAsset asset)
        {
            // Publish the output asset by creating an Origin locator for adaptive streaming,
            // and a SAS locator for progressive download.

            _context.Locators.Create(
                LocatorType.OnDemandOrigin,
                asset,
                AccessPermissions.Read,
                TimeSpan.FromDays(30));

            _context.Locators.Create(
                LocatorType.Sas,
                asset,
                AccessPermissions.Read,
                TimeSpan.FromDays(30));


            IEnumerable<IAssetFile> mp4AssetFiles = asset
                    .AssetFiles
                    .ToList()
                    .Where(af => af.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

            // Get the Smooth Streaming, HLS and MPEG-DASH URLs for adaptive streaming,
            // and the Progressive Download URL.
            Uri smoothStreamingUri = asset.GetSmoothStreamingUri();
            Uri hlsUri = asset.GetHlsUri();
            Uri mpegDashUri = asset.GetMpegDashUri();

            // Get the URls for progressive download for each MP4 file that was generated as a result
            // of encoding.
            List<Uri> mp4ProgressiveDownloadUris = mp4AssetFiles.Select(af => af.GetSasUri()).ToList();


            // Display  the streaming URLs.
            Console.WriteLine("Use the following URLs for adaptive streaming: ");
            Console.WriteLine(smoothStreamingUri);
            Console.WriteLine(hlsUri);
            Console.WriteLine(mpegDashUri);
            Console.WriteLine();

            // Display the URLs for progressive download.
            Console.WriteLine("Use the following URLs for progressive download.");
            mp4ProgressiveDownloadUris.ForEach(uri => Console.WriteLine(uri + "\n"));
            Console.WriteLine();

            // Download the output asset to a local folder.
            string outputFolder = "job-output";
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            Console.WriteLine();
            Console.WriteLine("Downloading output asset files to a local folder...");
            asset.DownloadToFolder(
                outputFolder,
                (af, p) =>
                {
                    Console.WriteLine("Downloading '{0}' - Progress: {1:0.##}%", af.Name, p.Progress);
                });

            Console.WriteLine("Output asset files available at '{0}'.", Path.GetFullPath(outputFolder));
        }
    }
}