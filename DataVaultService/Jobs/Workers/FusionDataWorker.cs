
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using DataVaultService.OracleUCM;
using SVGLeasePlanService.Data.DTO;

namespace DataVaultService
{
    public class FusionDataWorker
    {
        //private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //private readonly LDBRepository _repo = new LDBRepository();
      
        public async Task Work()
        {
            // OracleUCMHelper t = new OracleUCM.OracleUCMHelper(Settings.Default.UCMEndpoint, Settings.Default.Username, Settings.Default.Password);
            OracleUCMHelper t = new OracleUCM.OracleUCMHelper(ConfigurationManager.AppSettings["UCMEndpoint"],
                                                              ConfigurationManager.AppSettings["Username"],
                                                              ConfigurationManager.AppSettings["Password"]);
            List <OracleUCMFile> files = t.ListFiles("Shaded_Plan*");

            Console.WriteLine($"All files:");
            foreach (var f in files.OrderBy(x => x.dCreateDate))
            {
                DateTime dt = f.dCreateDate.Value;
                DateTime dtLocal = dt.ToLocalTime();
                Console.WriteLine($"\t{f.dOriginalName} - {f.dCreateDate} - {dtLocal.ToString()} - {f.VaultFileSize}");
            }

            var latestFile = files.OrderByDescending(x => x.dCreateDate).First();
            Console.WriteLine(String.Format("{0}: {1}, {2}", latestFile.dOriginalName, latestFile.dCreateDate, latestFile.dID));

            t.GetFile("dID", latestFile.dID.ToString(), @"c:\temp\testUCMfile_Shaded_Plan.csv");
            Console.WriteLine(files.Count);

            Console.ReadLine();

        }

    }
}
