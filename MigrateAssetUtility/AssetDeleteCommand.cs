using Amazon.S3;
using Gartner.GlobalAssemblies.AdminBusiness.Data;
using Gartner.GlobalAssemblies.Global;
using MigrateAssetUtility.DataFactory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using Amazon.Runtime;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Amazon;

namespace MigrateAssetUtility
{
    public class AssetDeleteCommand : ICommand
    {
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.GetBySystemName(ConfigurationManager.AppSettings["AWSRegion"].ToString());
        private static IAmazonS3 client;
        private static readonly string bucketName = ConfigurationManager.AppSettings["S3BucketName"].ToString();
        public void Execute()
        {
            Console.WriteLine("Delete Started.");
            try
            {
                //fetch all the resources to be uploaded 
                AssetDataFactory _dataFactory = new AssetDataFactory();
                S3Uploader cdnManager = new S3Uploader();
                Console.WriteLine("Please specify the relative path in bucket which need to be deleted");
                Console.WriteLine(bucketName);
                string inputRelativePath = Console.ReadLine();
                client = new AmazonS3Client(bucketRegion);
                ListingObjectsAsync(inputRelativePath).Wait();
            }
            catch (Exception e)
            {
                ErrorUtility.LogError(e, GartnerApplication.Unknown, string.Format("Error: Upload Doc error: " + e.Message));
            }
        }


        static async Task ListingObjectsAsync(string inputRelativePath)
        {
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName + inputRelativePath,
                    MaxKeys = 10
                };
                ListObjectsV2Response response;
                do
                {
                    response = await client.ListObjectsV2Async(request);

                    // Process the response.
                    foreach (S3Object entry in response.S3Objects)
                    {
                        Console.WriteLine("key = {0} size = {1}",
                            entry.Key, entry.Size);
                    }
                    Console.WriteLine("Next Continuation Token: {0}", response.NextContinuationToken);
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                Console.WriteLine("S3 error occurred. Exception: " + amazonS3Exception.ToString());
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
                Console.ReadKey();
            }
        }
    }
}