using Amazon.S3;
using Gartner.GlobalAssemblies.AdminBusiness.Data;
using Gartner.GlobalAssemblies.Global;
using MigrateAssetUtility.DataFactory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;

namespace MigrateAssetUtility
{
    public class AssetDocSyncCommand : ICommand
    {
        public void Execute()
        {
            Console.WriteLine("doc Started.");
            try
            {
                //fetch all the resources to be uploaded 
                AssetDataFactory _dataFactory = new AssetDataFactory();
                S3Uploader cdnManager = new S3Uploader();
                int chunckSize = 0;
                Console.WriteLine("Fetch asset for syncing");
                int totalCount = _dataFactory.GetAppointmentAssetCount((int)AssetTypes.Appointment_Document, true);
                Console.WriteLine("Upload asset is in progress");
                do
                {
                    List<Assets> assets = _dataFactory.GetAppointmentAsset((int)AssetTypes.Appointment_Document, true, 2);//asset type

                    //loop through each asset and upload to CDN

                    foreach (var item in assets)
                    {
                        var assetRequest = new UploadAssetRequestParameters();
                        string directoryName = Path.GetDirectoryName(item.TargetPath);
                        //int pos = item.TargetPath.LastIndexOf("/") + 1;
                        string bucketRelativePath = directoryName;//item.TargetPath.Substring(0, pos);
                        assetRequest.BucketName = ConfigurationManager.AppSettings["S3BucketName"] + @"/private/" + directoryName;
                        assetRequest.FilePath = ConfigurationManager.AppSettings["FileStoreRoot"] + item.TargetPath;
                        assetRequest.Tags = new Dictionary<string, string>() { { "Asset Type", "Appointment Document" }, { "Asset Id", item.AssetId.ToString() } };
                        if (item.AppointmentAssets.Count > 0)
                            assetRequest.Tags.Add("Asset Entity Id", item.AppointmentAssets.FirstOrDefault().appointmentID.ToString());
                        int monthDiff = S3Uploader.GetMonthDifference(DateTime.UtcNow, item.UpdatedDateTime ?? item.CreatedDateTime.Value);
                        S3StorageClass storageClass = S3StorageClass.Standard;
                        if (monthDiff > 6 && monthDiff < 36)
                            storageClass = S3StorageClass.StandardInfrequentAccess;
                        else if(monthDiff > 36)
                            storageClass = S3StorageClass.Glacier;
                        string objectKey = cdnManager.UploadFile(assetRequest, storageClass);
                        if (!string.IsNullOrEmpty(objectKey))
                            _dataFactory.UpdateAssetPath(item.AssetId, bucketRelativePath + objectKey);
                    }
                    chunckSize = chunckSize + 2;
                } while (totalCount > chunckSize);
                Console.WriteLine("Upload asset is completed");
            }
            catch (Exception e)
            {
                ErrorUtility.LogError(e, GartnerApplication.Unknown, string.Format("Error: Upload Doc error: " + e.Message));
            }
        }
    }
}
