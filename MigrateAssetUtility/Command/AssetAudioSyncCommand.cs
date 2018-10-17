using Amazon.S3;
using Gartner.GlobalAssemblies.AdminBusiness.Data;
using Gartner.GlobalAssemblies.Global;
using Gartner.GlobalAssemblies.Global.Helpers;
using MigrateAssetUtility.DataFactory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateAssetUtility
{
    public class AssetAudioSyncCommand :ICommand
    {
        public void Execute()
        {
            Console.WriteLine("audio asset Syn Started.");
            try
            {
                
                //fetch all the resources to be uploaded 
                AssetDataFactory _dataFactory = new AssetDataFactory();
                Console.WriteLine("Fetch asset for syncing");
                List<Assets> assets = _dataFactory.GetAppointmentAsset((int)AssetTypes.Appointment_Audio, true);//asset type
                Console.WriteLine("asset has been fetched and now upload to S3 will be done");
                AmazonS3UploadHelper cdnManager = new AmazonS3UploadHelper();
                //loop through each asset and upload to CDN
                Console.WriteLine("Upload asset started at {0}",DateTime.Now.ToLongTimeString());
                DateTime dtStartDateTime = DateTime.Now;
                int counter = 1;
                foreach (var item in assets)
                {
                  
                    ProgressHelper.ShowPercentProgress("progress of audio upload ", counter, assets.Count());
                    counter++;
                    try
                    {
                        var assetRequest = new UploadAssetRequestParameters();

                        int pos = item.TargetPath.LastIndexOf("/");
                        string bucketRelativePath = item.TargetPath.Substring(0, pos);
                        assetRequest.BucketName = ConfigurationManager.AppSettings["S3BucketName"] + @"/private/" + bucketRelativePath;


                        assetRequest.BucketName = assetRequest.BucketName.ToLower();
                        assetRequest.BucketName = assetRequest.BucketName.Replace(@"\\", @"/").Replace(@"\", @"/");


                        assetRequest.FilePath = ConfigurationManager.AppSettings["FileStoreRoot"] + item.TargetPath;
                        assetRequest.Tags = new Dictionary<string, string>() { { "Asset Type", "Appointment Audio" }, { "Asset Id", item.AssetId.ToString() } };
                        if (item.AppointmentAssets.Count > 0)
                            assetRequest.Tags.Add("Asset Entity Id", item.AppointmentAssets.FirstOrDefault().appointmentID.ToString());
                        int monthDiff = S3Uploader.GetMonthDifference(DateTime.UtcNow, item.UpdatedDateTime ?? item.CreatedDateTime.Value);
                        S3StorageClass storageClass = S3StorageClass.Standard;
                        if (monthDiff > 6)
                            storageClass = S3StorageClass.StandardInfrequentAccess;
                        assetRequest.AssetId = item.AssetId;
                        assetRequest.s3StorageClass = storageClass;
                        //Uploading file

                        string objectKey = cdnManager.UploadFile(assetRequest);
                        if (!string.IsNullOrEmpty(objectKey))
                            _dataFactory.UpdateAssetPath(item.AssetId, bucketRelativePath + @"/" + objectKey, @"private/" + bucketRelativePath + @"/" + objectKey);
                    }
                    catch (Exception ex)
                    {
                        ErrorUtility.LogError(ex, GartnerApplication.Unknown, string.Format("Error: upload image Asset (Asset Id : {0}) error: {1}",item.AssetId, ex.Message));
                        Console.WriteLine(ex.Message);
                    }
                }
                DateTime dtEndDateTime = DateTime.Now;
                Console.WriteLine("Uploading of audio files completed at {0}", dtEndDateTime.ToLongTimeString());
                Console.WriteLine("Time took to upload audio files is {0}", (dtEndDateTime - dtStartDateTime).Duration().TotalMinutes);

            }
            catch (Exception ex)
            {
                ErrorUtility.LogError(ex, GartnerApplication.Unknown, string.Format("Error: upload image error: " + ex.Message));
                Console.WriteLine(ex.Message);
            }

        }
    }
}
