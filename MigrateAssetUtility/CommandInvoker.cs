using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateAssetUtility
{
    class CommandInvoker
    {
        public void Invoke(ICommand cmd)
        {
            Console.WriteLine("Invoking.......");
            cmd.Execute();
        }
    }
}
