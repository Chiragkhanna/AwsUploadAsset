using Amazon.S3;
using Gartner.GlobalAssemblies.AdminBusiness.Data;
using Gartner.GlobalAssemblies.Global;
using Gartner.GlobalAssemblies.Global.Managers;
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
                S3Uploader cdnManager = new S3Uploader();
                //loop through each asset and upload to CDN
                Console.WriteLine("Upload asset is in progress");
                foreach (var item in assets)
                {
                    var assetRequest = new UploadAssetRequestParameters();
                    string directoryName = Path.GetDirectoryName(item.TargetPath);
                    string bucketRelativePath = directoryName;
                    assetRequest.BucketName = ConfigurationManager.AppSettings["S3BucketName"] + @"/private/" + bucketRelativePath;
                    assetRequest.FilePath = ConfigurationManager.AppSettings["FileStoreRoot"] + item.TargetPath;
                    assetRequest.Tags = new Dictionary<string, string>() { { "Asset Type", "Appointment Audio" }, { "Asset Id", item.AssetId.ToString() } };
                    if(item.AppointmentAssets.Count > 0)
                        assetRequest.Tags.Add( "Asset Entity Id", item.AppointmentAssets.FirstOrDefault().appointmentID.ToString());
                    int monthDiff = S3Uploader.GetMonthDifference(DateTime.UtcNow, item.UpdatedDateTime ?? item.CreatedDateTime.Value);
                    S3StorageClass storageClass = S3StorageClass.Standard;
                    if (monthDiff > 6 && monthDiff < 36)
                        storageClass = S3StorageClass.StandardInfrequentAccess;
                    else if (monthDiff > 36)
                        storageClass = S3StorageClass.Glacier;
                    string objectKey = cdnManager.UploadFile(assetRequest, storageClass);
                    if(!string.IsNullOrEmpty(objectKey))
                    _dataFactory.UpdateAssetPath(item.AssetId, bucketRelativePath + objectKey);

                }
                Console.WriteLine("Upload asset is completed");

            }
            catch (Exception e)
            {
                ErrorUtility.LogError(e, GartnerApplication.Unknown, string.Format("Error: upload image error: " + e.Message));
            }

        }
    }
}
