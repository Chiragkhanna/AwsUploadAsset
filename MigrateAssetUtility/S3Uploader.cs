using Amazon.S3;
using Amazon.S3.Model;
using Gartner.GlobalAssemblies.Global;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MigrateAssetUtility
{
    public class S3Uploader
    {
        public static string GetS3FileName(string fileLocation,long assetId)
        {
            string filePath = Path.GetFullPath(fileLocation).TrimEnd(Path.DirectorySeparatorChar);
            string tempObjectKey = Path.GetFileName(filePath);
            int extensionPos = tempObjectKey.LastIndexOf(".");
            string extension = tempObjectKey.Substring(extensionPos);
            string filename = tempObjectKey.Substring(0, extensionPos);

            return filename + "_" + assetId.ToString()+ extension;
           
        }
        public static int GetMonthDifference(DateTime startDate, DateTime endDate )
        {
            int monthsApart = 12 * (startDate.Year - endDate.Year) + startDate.Month - endDate.Month;
            return Math.Abs(monthsApart);
        }
        
        public string UploadFile(UploadAssetRequestParameters request, S3StorageClass storageClass, long assetId)
        {
            string objectKey = GetS3FileName(request.FilePath,assetId);
         


            try
            {
                List<Amazon.S3.Model.Tag> tagSet = new List<Amazon.S3.Model.Tag>();
                if (request.Tags != null)
                {
                    foreach (KeyValuePair<string, string> entry in request.Tags)
                    {
                        tagSet.Add(new Amazon.S3.Model.Tag { Key = entry.Key, Value = entry.Value });
                    }
                }
                var awsRegion = Amazon.RegionEndpoint.GetBySystemName(ConfigurationManager.AppSettings["AWSRegion"].ToString());
                IAmazonS3 client = new AmazonS3Client(awsRegion);
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = request.BucketName,
                    Key = objectKey,
                    TagSet = tagSet,
                    StorageClass = storageClass
                };
                //upload by filepath or memorystream
                if (request.InputStream != null && request.InputStream.Length > 0)
                    putRequest.InputStream = new MemoryStream(request.InputStream);
                else putRequest.FilePath = request.FilePath;

                PutObjectResponse response = client.PutObject(putRequest);
                return objectKey;//to fetch the uploaded content append cloudfrontUrl + objectKey
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                string errorMessage = String.Empty;
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Check the provided AWS Credentials.");
                }
                else
                {
                    errorMessage = string.Format("Error occurred. Message:'{0}' when writing an object", amazonS3Exception.Message);
                    ErrorUtility.LogError(amazonS3Exception, GartnerApplication.UtilityService, string.Format("Error: AWS: UploadFile: " + errorMessage));
                }
            }
            catch (Exception e)
            {
                ErrorUtility.LogError(e, GartnerApplication.Unknown, string.Format("Error: UploadFile: " + e.Message));
            }
            return string.Empty;//empty string denote that process has got some error
        }
    }
}
