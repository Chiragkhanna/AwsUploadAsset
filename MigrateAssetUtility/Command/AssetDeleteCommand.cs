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
        private static IAmazonS3 s3Client;
        private static readonly string bucketName = ConfigurationManager.AppSettings["S3BucketName"].ToString();
        private static string prefix = string.Empty;
        private static List<KeyVersion> recursiveContentList = new List<KeyVersion>();
        public void Execute()
        {
            Console.WriteLine("Delete Started.");
            try
            {
                //fetch all the resources to be uploaded 
                AssetDataFactory _dataFactory = new AssetDataFactory();
                S3Uploader cdnManager = new S3Uploader();
                Console.WriteLine("Please specify the relative path for which content need to be deleted");
                Console.WriteLine(bucketName);
                prefix = Console.ReadLine();
                s3Client = new AmazonS3Client(bucketRegion);

                DeleteObjectAsyncByKey().Wait();
            }
            catch (Exception e)
            {
                ErrorUtility.LogError(e, GartnerApplication.Unknown, string.Format("Error: Upload Doc error: " + e.Message));
            }
        }
        static async Task<List<KeyVersion>> ListingObjectsAsync()
        {
            List<KeyVersion> keyVersion = new List<KeyVersion>();
            bool isContinue = false;
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = 20,
                    Delimiter = "/",
                    Prefix = prefix // "private/events/" + eventCode +"/sessions/"
                };
                ListObjectsV2Response response;
                do
                {
                    response = await s3Client.ListObjectsV2Async(request);
                    List<KeyVersion> tempkeyVersion = new List<KeyVersion>();
                    int counter = 1;
                    // Process the response.
                    foreach (S3Object entry in response.S3Objects)
                    {
                        Console.WriteLine("id = {0} key = {1} size = {2}",
                          counter, entry.Key, entry.Size);
                        tempkeyVersion.Add(new KeyVersion() { Key = entry.Key, VersionId = counter.ToString() });
                        counter++;
                    }
                    //Console.WriteLine("Next Continuation Token: {0}", response.NextContinuationToken);
                    request.ContinuationToken = response.NextContinuationToken;
                    //take comma seperated list ok item and put to List
                    //then ask for new list or delete
                    isContinue = false;
                    Console.WriteLine("1. Enter 1 for entering comma seperated list of objects to be deleted");
                    Console.WriteLine("2. Fetch new list of assets");

                    string input = Console.ReadLine();
                    try
                    {
                        int action = Convert.ToInt32(input);

                        switch (action)
                        {
                            case 1:
                                Console.WriteLine("Enter asset id corresponding to asset item");
                                input = Console.ReadLine();
                                var tagIds = new List<string>(input.Split(','));
                                foreach (var item in tagIds)
                                {
                                    var query = tempkeyVersion.Where(x => x.VersionId == item).FirstOrDefault();
                                    keyVersion.Add(new KeyVersion
                                    {
                                        Key = query.Key,
                                        VersionId = "1"
                                    });
                                }

                                break;
                            case 2:
                                isContinue = true;
                                break;

                            default:
                                throw new Exception();
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("No such command.");
                    }
                } while (response.IsTruncated && isContinue);

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
            return keyVersion;
        }
        private static async Task DeleteObjectAsyncByKey()
        {
            List<KeyVersion> keyVersion = new List<KeyVersion>();
            Console.WriteLine("1. Delete by giving exact Key");
            Console.WriteLine("2. Delete by fetching the list of objects");
            Console.WriteLine("3. Delete all asset which fall under relative path");
            string input = Console.ReadLine();
            try
            {
                int action = Convert.ToInt32(input);

                switch (action)
                {
                    case 1:
                        Console.WriteLine("Enter Key");
                        input = Console.ReadLine();
                        keyVersion.Add(new KeyVersion
                        {
                            Key = input,
                            VersionId = "1"
                        });
                        break;
                    case 2:
                        keyVersion = await ListingObjectsAsync();
                        break;
                    case 3:
                        //recursively fetch the asset and then remove them 
                        await RecursiveDeleteAllObjectsFromVersionedBucketAsync();
                        break;
                    default:
                        throw new Exception();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("No such command.");
            }


            //return deletedObjects;
        }
       
        private static async Task RecursiveDeleteAllObjectsFromVersionedBucketAsync()
        {
            recursiveContentList = new List<KeyVersion>();
            await Task.WhenAll( getContentRecursive(prefix));

            // Delete objects (without specifying object version in the request). 
            List<DeletedObject> deletedObjects = await NonVersionedDeleteAsync(recursiveContentList);
            //return deletedObjects;


        }
        private static async Task getContentRecursive(string prefix)
        {
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = 200,
                    Delimiter = "/",
                    Prefix = prefix // "private/events/" + eventCode +"/sessions/"
                };
                ListObjectsV2Response response;
                do
                {
                    response = await s3Client.ListObjectsV2Async(request);
                    
                    // Process the response.
                    foreach (S3Object entry in response.S3Objects)
                    {
                        Console.WriteLine("key = {0} size = {1}",entry.Key, entry.Size);
                        CollectAllRecursiveKeys(new KeyVersion() { Key = entry.Key, VersionId = "1" });
                    }
                    foreach (string d in response.CommonPrefixes)
                    {
                      await  getContentRecursive(d);
                    }
                    //Console.WriteLine("Next Continuation Token: {0}", response.NextContinuationToken);
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);

               
                
            }
            catch (System.Exception e)
            {
            }
        }

        private static void CollectAllRecursiveKeys(KeyVersion keys)
        {
            recursiveContentList.Add(keys);
        }
        private static async Task DeleteMultipleObjectsFromVersionedBucketAsync()
        {

            // Delete objects (specifying object version in the request).
            await DeleteObjectVersionsAsync();

            // Delete objects (without specifying object version in the request). 
            var deletedObjects = await DeleteObjectsAsync();

            // Additional exercise - remove the delete markers S3 returned in the preceding response. 
            // This results in the objects reappearing in the bucket (you can 
            // verify the appearance/disappearance of objects in the console).
            await RemoveDeleteMarkersAsync(deletedObjects);
        }

        private static async Task<List<DeletedObject>> DeleteObjectsAsync()
        {
            // Upload the sample objects.
            var keysAndVersions2 = await PutObjectsAsync(3);

            // Delete objects using only keys. Amazon S3 creates a delete marker and 
            // returns its version ID in the response.
            List<DeletedObject> deletedObjects = await NonVersionedDeleteAsync(keysAndVersions2);
            return deletedObjects;
        }

        private static async Task DeleteObjectVersionsAsync()
        {
            // Upload the sample objects.
            var keysAndVersions1 = await PutObjectsAsync(3);

            // Delete the specific object versions.
            await VersionedDeleteAsync(keysAndVersions1);
        }

        private static void PrintDeletionReport(DeleteObjectsException e)
        {
            var errorResponse = e.Response;
            Console.WriteLine("No. of objects successfully deleted = {0}", errorResponse.DeletedObjects.Count);
            Console.WriteLine("No. of objects failed to delete = {0}", errorResponse.DeleteErrors.Count);
            Console.WriteLine("Printing error data...");
            foreach (var deleteError in errorResponse.DeleteErrors)
            {
                Console.WriteLine("Object Key: {0}\t{1}\t{2}", deleteError.Key, deleteError.Code, deleteError.Message);
            }
        }

        static async Task VersionedDeleteAsync(List<KeyVersion> keys)
        {
            // a. Perform a multi-object delete by specifying the key names and version IDs.
            var multiObjectDeleteRequest = new DeleteObjectsRequest
            {
                BucketName = bucketName,
                Objects = keys // This includes the object keys and specific version IDs.
            };
            try
            {
                Console.WriteLine("Executing VersionedDelete...");
                DeleteObjectsResponse response = await s3Client.DeleteObjectsAsync(multiObjectDeleteRequest);
                Console.WriteLine("Successfully deleted all the {0} items", response.DeletedObjects.Count);
            }
            catch (DeleteObjectsException e)
            {
                PrintDeletionReport(e);
            }
        }

        static async Task<List<DeletedObject>> NonVersionedDeleteAsync(List<KeyVersion> keys)
        {
            
            //keys = keys.Take(1000).ToList();
            // Create a request that includes only the object key names.
            for (int i = 0; i < keys.Count; i = i + 1000)
            {
                var items = keys.Skip(i).Take(1000);


                DeleteObjectsRequest multiObjectDeleteRequest = new DeleteObjectsRequest();
                multiObjectDeleteRequest.BucketName = bucketName;

                foreach (var key in items)
                {
                    multiObjectDeleteRequest.AddKey(key.Key);
                }
                // Execute DeleteObjects - Amazon S3 add delete marker for each object
                // deletion. The objects disappear from your bucket. 
                DeleteObjectsResponse response;
                try
                {
                    Console.WriteLine("Executing NonVersionedDelete...");
                    response = await s3Client.DeleteObjectsAsync(multiObjectDeleteRequest);
                    Console.WriteLine("Successfully deleted all the {0} items", response.DeletedObjects.Count);
                }
                catch (DeleteObjectsException e)
                {
                    PrintDeletionReport(e);
                    throw; // Some deletes failed. Investigate before continuing.
                }
            }
            // This response contains the DeletedObjects list which we use to delete the delete markers.
            return new List<DeletedObject>();
        }

        private static async Task RemoveDeleteMarkersAsync(List<DeletedObject> deletedObjects)
        {
            var keyVersionList = new List<KeyVersion>();

            foreach (var deletedObject in deletedObjects)
            {
                KeyVersion keyVersion = new KeyVersion
                {
                    Key = deletedObject.Key,
                    VersionId = deletedObject.DeleteMarkerVersionId
                };
                keyVersionList.Add(keyVersion);
            }
            // Create another request to delete the delete markers.
            var multiObjectDeleteRequest = new DeleteObjectsRequest
            {
                BucketName = bucketName,
                Objects = keyVersionList
            };

            // Now, delete the delete marker to bring your objects back to the bucket.
            try
            {
                Console.WriteLine("Removing the delete markers .....");
                var deleteObjectResponse = await s3Client.DeleteObjectsAsync(multiObjectDeleteRequest);
                Console.WriteLine("Successfully deleted all the {0} delete markers",
                                            deleteObjectResponse.DeletedObjects.Count);
            }
            catch (DeleteObjectsException e)
            {
                PrintDeletionReport(e);
            }
        }

        static async Task<List<KeyVersion>> PutObjectsAsync(int number)
        {
            var keys = new List<KeyVersion>();

            for (var i = 0; i < number; i++)
            {
                string key = "ObjectToDelete-" + new System.Random().Next();
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    ContentBody = "This is the content body!",

                };

                var response = await s3Client.PutObjectAsync(request);
                KeyVersion keyVersion = new KeyVersion
                {
                    Key = key,
                    VersionId = response.VersionId
                };

                keys.Add(keyVersion);
            }
            return keys;
        }
    }
}