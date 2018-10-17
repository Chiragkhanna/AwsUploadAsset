using Amazon.S3;
using Gartner.GlobalAssemblies.AdminBusiness.Data;
using Gartner.GlobalAssemblies.Global;
using MigrateAssetUtility.DataFactory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using Gartner.GlobalAssemblies.Global.Helpers;

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
                AmazonS3UploadHelper cdnManager = new AmazonS3UploadHelper();
                int chunckSize = 0;
                int defaultchunckSize = 30000;
                Console.WriteLine("Fetch asset for syncing");
                int totalCount = _dataFactory.GetAppointmentAssetCount((int)AssetTypes.Appointment_Document, true);
                int chunkCounter = 1;
                int chunkTotal = totalCount/ defaultchunckSize;
                if (chunkTotal == 0)
                    chunkTotal++;
                Console.WriteLine("Upload asset will take place in chunks");
                Console.WriteLine("Upload asset started at {0}", DateTime.Now.ToLongTimeString());
                DateTime dtStartDateTime = DateTime.Now;

                do
                {
                    List<Assets> assets = _dataFactory.GetAppointmentAsset((int)AssetTypes.Appointment_Document, true, defaultchunckSize);//asset type
                    
                    //loop through each asset and upload to CDN
                    
                    int counter = 1;

                    foreach (var item in assets)
                    {
                        ProgressHelper.ShowPercentProgress(string.Format("Uploading documents of chunk {0} out of {1} of which  ", chunkCounter, chunkTotal), counter, assets.Count);
                        counter++;
                        try
                        {
                            
                            var assetRequest = new UploadAssetRequestParameters();
                            item.TargetPath = item.TargetPath.ToLower();
                            item.TargetPath = item.TargetPath.Replace(@"\\", @"/").Replace(@"\", @"/");
                            //string directoryName = Path.GetDirectoryName(item.TargetPath);
                            int pos = item.TargetPath.LastIndexOf("/");
                            string bucketRelativePath = item.TargetPath.Substring(0, pos);
                            assetRequest.BucketName = ConfigurationManager.AppSettings["S3BucketName"] + @"/private/" + bucketRelativePath;
                            assetRequest.FilePath = ConfigurationManager.AppSettings["FileStoreRoot"] + item.TargetPath;
                            assetRequest.Tags = new Dictionary<string, string>() { { "Asset Type", "Appointment Document" }, { "Asset Id", item.AssetId.ToString() } };
                            if (item.AppointmentAssets.Count > 0)
                                assetRequest.Tags.Add("Asset Entity Id", item.AppointmentAssets.FirstOrDefault().appointmentID.ToString());
                            int monthDiff = S3Uploader.GetMonthDifference(DateTime.UtcNow, item.CreatedDateTime ?? DateTime.UtcNow);
                            S3StorageClass storageClass = S3StorageClass.Standard;
                            if (monthDiff > 6)
                                storageClass = S3StorageClass.StandardInfrequentAccess;
                            //else if (monthDiff > 36)
                            //    storageClass = S3StorageClass.Glacier;
                            assetRequest.AssetId = item.AssetId;
                            assetRequest.s3StorageClass = storageClass;
                            //Uploading file
                            string extension = Path.GetExtension(item.TargetPath);
                            if (!string.IsNullOrEmpty(extension) && extension == "pdf")
                                assetRequest.ContentType = "application/pdf";


                            if (storageClass != S3StorageClass.Glacier)
                            {
                                string objectKey = cdnManager.UploadFile(assetRequest);
                                if (!string.IsNullOrEmpty(objectKey))
                                    _dataFactory.UpdateAssetPath(item.AssetId, bucketRelativePath + @"/" + objectKey, @"private/"+ bucketRelativePath + @"/" + objectKey);
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorUtility.LogError(ex, GartnerApplication.Unknown, string.Format("Error: upload image Asset (Asset Id : {0}) error: {1}", item.AssetId, ex.Message));
                            Console.WriteLine(ex.Message);
                        }
                    }
                    chunkCounter++;
                    chunckSize = chunckSize + defaultchunckSize;

                } while (totalCount > chunckSize);
                DateTime dtEndDateTime = DateTime.Now;
                Console.WriteLine("Uploading of appointment documents completed at {0}", dtEndDateTime.ToLongTimeString());
                Console.WriteLine("Time took to upload appointment documents is {0}", (dtEndDateTime - dtStartDateTime).Duration().TotalMinutes);

            }
            catch (Exception e)
            {
                ErrorUtility.LogError(e, GartnerApplication.Unknown, string.Format("Error: Upload Doc error: " + e.Message));
                Console.WriteLine("Exception while uploading document : {0}", e.Message);
            }
        }
    }
}
