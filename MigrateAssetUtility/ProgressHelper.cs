using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateAssetUtility
{
    public static class ProgressHelper
    {
        public static void ShowPercentProgress(string message, long processed, long total)
        {

            long percent = (100 * (processed + 1)) / total;
            Console.Write("\r{0}{1}% complete", message, percent);
            if (processed >= total - 1)
            {
                Console.WriteLine(Environment.NewLine);
            }
        }
    }
}
