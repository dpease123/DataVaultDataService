using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace DataVaultService.Jobs.Workers
{
    public class SSISJobWorker
    {
        public async Task Work()
        {
            var pkgPath = ConfigurationManager.AppSettings["JDEExtractSSISPacakes"];
        }
    }
}
