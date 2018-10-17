using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateAssetUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Starting the Migration Utility >>> ");
            Console.WriteLine();
            //Instantiate the invoker object
            CommandInvoker _invoker = new CommandInvoker();
            while (true)
            {
               
                Console.WriteLine("1. Sync Audio");
                Console.WriteLine("2. Sync Document");
                Console.WriteLine("3. Delete Asset");
                Console.WriteLine("4. List Asset");
                Console.WriteLine("5. LifeCycle Configuration");
                Console.WriteLine("6. Exit");
                string input = Console.ReadLine();
                if (input.Equals("6", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                
                try
                {
                    int action = Convert.ToInt32(input);
                    switch (action)
                    {
                        case 1:
                            AssetAudioSyncCommand audioCommand = new AssetAudioSyncCommand();
                            _invoker.Invoke(audioCommand);
                            break;
                        case 2:
                            AssetDocSyncCommand docCommand = new AssetDocSyncCommand();
                            _invoker.Invoke(docCommand);
                            break;
                        case 3:
                            AssetDeleteCommand _command = new AssetDeleteCommand();
                            _invoker.Invoke(_command);
                            break;
                        case 4:
                            AssetListCommand listCommand = new AssetListCommand();
                            _invoker.Invoke(listCommand);
                            break;
                        case 5:
                            LifecycleConfigurationBucketCommand lifecycleCommand = new LifecycleConfigurationBucketCommand();
                            _invoker.Invoke(lifecycleCommand);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("No such command.");
                }
            }
            
            Console.ReadLine();
        }
    }
}
