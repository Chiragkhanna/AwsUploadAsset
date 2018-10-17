using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Gartner.GlobalAssemblies.Global;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MigrateAssetUtility
{
    public class LifecycleConfigurationBucketCommand : ICommand
    {
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.GetBySystemName(ConfigurationManager.AppSettings["AWSRegion"].ToString());
        private static IAmazonS3 client;
        private static readonly string bucketName = ConfigurationManager.AppSettings["S3BucketName"].ToString();
        public void Execute()
        {
            Console.WriteLine(" Press 1 for Adds a lifecycle configuration to a bucket.");
            Console.WriteLine("Press 2 for Retrieves the lifecyle configuration and updates it by adding another rule.");
            Console.WriteLine("Press 3 for Adds the modified lifecycle configuration to the bucket.Amazon S3 replaces the existing lifecycle configuration.");
            Console.WriteLine("Press 4 for Retrieves the configuration again and verifies it by printing the number of rules in the configuration.");
            Console.WriteLine("Press 5 for Deletes the lifecyle configuration.and verifies the deletion");
            LifecycleConfigAsync().Wait();
            
        }
        private static async Task LifecycleConfigAsync()
        {
            client = new AmazonS3Client(bucketRegion);
            string input = Console.ReadLine();
            try
            {
                int action = Convert.ToInt32(input);
                var lifeCycleConfiguration = new LifecycleConfiguration();
                switch (action)
                {
                    case 1:
                         await AddUpdateDeleteLifecycleConfigAsync();
                        break;
                    case 2:
                        lifeCycleConfiguration = await RetrieveLifecycleConfigAsync(client);
                        foreach (var rule in lifeCycleConfiguration.Rules)
                        {
                            Console.WriteLine("Rule Id {0} with status of {1} and expiration Days is ",rule.Id, rule.Status, rule.Expiration != null ? rule.Expiration.Days: 0);
                            Console.WriteLine("Transition");
                            foreach (var trans in rule.Transitions)
                            {
                                Console.WriteLine("Storage class {0}  and expiration Days is ",trans.StorageClass, trans.Days);
                            }
                            Console.WriteLine("Filter");
                            if (rule.Filter.LifecycleFilterPredicate is LifecyclePrefixPredicate)
                            {
                                LifecyclePrefixPredicate prefix = rule.Filter.LifecycleFilterPredicate as LifecyclePrefixPredicate;
                                Console.WriteLine("Filter Prefix {0}  and expiration Days is ", prefix.Prefix);
                            }
                            if (rule.Filter.LifecycleFilterPredicate is LifecycleTagPredicate)
                            {
                                LifecycleTagPredicate tags = rule.Filter.LifecycleFilterPredicate as LifecycleTagPredicate;
                                Console.WriteLine("Filter Tag Key {0}  and value ", tags.Tag.Key, tags.Tag.Value);
                            }
                            Console.WriteLine("*************************************");
                        }
                        break;
                    case 3:
                        await RemoveLifecycleConfigAsync(client);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                ErrorUtility.LogError(e, GartnerApplication.Unknown, string.Format("Error: Lifecycle Error: " + e.Message));
            }
        }
        private static async Task AddUpdateDeleteLifecycleConfigAsync()
        {
            try
            {
                var lifeCycleConfiguration = new LifecycleConfiguration()
                {
                    Rules = new List<LifecycleRule>
                        {
                            new LifecycleRule
                            {
                                 Id = "Archive immediately rule",
                                 Filter = new LifecycleFilter()
                                 {
                                     LifecycleFilterPredicate = new LifecyclePrefixPredicate()
                                     {
                                         Prefix = "glacierobjects/"
                                     }
                                 },
                                 Status = LifecycleRuleStatus.Enabled,
                                 Transitions = new List<LifecycleTransition>
                                 {
                                      new LifecycleTransition
                                      {
                                           Days = 0,
                                           StorageClass = S3StorageClass.Glacier
                                      }
                                  },
                            },
                            new LifecycleRule
                            {
                                 Id = "Archive and then delete rule",
                                  Filter = new LifecycleFilter()
                                 {
                                     LifecycleFilterPredicate = new LifecyclePrefixPredicate()
                                     {
                                         Prefix = "audio/"
                                     }
                                 },
                                 Status = LifecycleRuleStatus.Enabled,
                                 Transitions = new List<LifecycleTransition>
                                 {
                                      new LifecycleTransition
                                      {
                                           Days = 1,
                                           StorageClass = S3StorageClass.StandardInfrequentAccess
                                      },
                                      new LifecycleTransition
                                      {
                                        Days = 2,
                                        StorageClass = S3StorageClass.Glacier
                                      }
                                 },
                                 Expiration = new LifecycleRuleExpiration()
                                 {
                                       Days = 10
                                 }
                            }
                        }
                };

                // Add the configuration to the bucket. 
                await AddExampleLifecycleConfigAsync(client, lifeCycleConfiguration);

                // Retrieve an existing configuration. 
                lifeCycleConfiguration = await RetrieveLifecycleConfigAsync(client);

                // Add a new rule.
                lifeCycleConfiguration.Rules.Add(new LifecycleRule
                {
                    Id = "NewRule",
                    Filter = new LifecycleFilter()
                    {
                        LifecycleFilterPredicate = new LifecyclePrefixPredicate()
                        {
                            Prefix = "YearlyDocuments/"
                        }
                    },
                    Expiration = new LifecycleRuleExpiration()
                    {
                        Days = 3650
                    }
                });

                // Add the configuration to the bucket. 
                await AddExampleLifecycleConfigAsync(client, lifeCycleConfiguration);

                // Verify that there are now three rules.
                lifeCycleConfiguration = await RetrieveLifecycleConfigAsync(client);
                Console.WriteLine("Expected # of rulest=3; found:{0}", lifeCycleConfiguration.Rules.Count);

                // Delete the configuration.
                await RemoveLifecycleConfigAsync(client);

                // Retrieve a nonexistent configuration.
                lifeCycleConfiguration = await RetrieveLifecycleConfigAsync(client);

            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        static async Task AddExampleLifecycleConfigAsync(IAmazonS3 client, LifecycleConfiguration configuration)
        {

            PutLifecycleConfigurationRequest request = new PutLifecycleConfigurationRequest
            {
                BucketName = bucketName,
                Configuration = configuration
            };
            var response = await client.PutLifecycleConfigurationAsync(request);
        }

        static async Task<LifecycleConfiguration> RetrieveLifecycleConfigAsync(IAmazonS3 client)
        {
            GetLifecycleConfigurationRequest request = new GetLifecycleConfigurationRequest
            {
                BucketName = bucketName
            };
            var response = await client.GetLifecycleConfigurationAsync(request);
            var configuration = response.Configuration;
            return configuration;
        }

        static async Task RemoveLifecycleConfigAsync(IAmazonS3 client)
        {
            DeleteLifecycleConfigurationRequest request = new DeleteLifecycleConfigurationRequest
            {
                BucketName = bucketName
            };
            await client.DeleteLifecycleConfigurationAsync(request);
        }
    }
}
