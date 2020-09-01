
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using DataVaultService.OracleUCM;
using SVGLeasePlanService.Data.DTO;
using System.IO.Compression;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;

namespace DataVaultService
{
    public class FusionDataWorker
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async Task Work()
        {
            //await WorkAnotherWay();

            _logger.Info("Started Fusion file processing");
            var extractFolder = ConfigurationManager.AppSettings["DVLoadsRootFolderName"];
            var extractFolder_DEV = ConfigurationManager.AppSettings["DEV_DVLoadsRootFolderName"];

            var createFile = ConfigurationManager.AppSettings["CreateFile_URL"];
            var getFile = ConfigurationManager.AppSettings["GetFile_URL"];

            var todaysFolderPath = Path.Combine(ConfigurationManager.AppSettings["DVLoadsRootFolderName"], DateTime.Now.ToString(ConfigurationManager.AppSettings["DVLoadsDailyFolderName"]));
            MakeTodaysFolder(todaysFolderPath);
            var fusionFileNameList = ConfigurationManager.AppSettings["FusionCSVFileNames"].Split(',').ToList();
            OracleUCMHelper t = new OracleUCM.OracleUCMHelper(ConfigurationManager.AppSettings["UCMEndpoint_DEV"],
                                                              ConfigurationManager.AppSettings["Username"],
                                                              ConfigurationManager.AppSettings["Password"]);
            DateTime now = DateTime.Now;


           
            foreach (var fusionFile in fusionFileNameList.Where(x=> x.Contains("dpease")))
            {
               
             
                try 
                {
                    List<OracleUCMFile> files = t.ListFiles(fusionFile.Trim());
                    if (files.Any())
                    {

                        var latestFile = files.OrderByDescending(x => x.dCreateDate).First();
                        bool bDate = DateTime.TryParse(latestFile.dCreateDate.ToString(), out DateTime fDate);
                        if (!bDate)
                        {
                            _logger.Error($"{fusionFile} created at date is invalid - not processed.");
                            continue;
                        }
                     
                        if (fDate.ToLocalTime().Day == now.Day && fDate.ToLocalTime().Month == now.Month && fDate.ToLocalTime().Year == now.Year)
                        {
                            t.GetFile("dID", latestFile.dID.ToString(), Path.Combine(extractFolder, fusionFile));

                            if (fusionFile.EndsWith(".zip")) ;
                            {
                                var zipPath = Path.Combine(extractFolder, fusionFile);
                                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, Path.Combine(extractFolder, "RFI_c.csv"));
                            }

                        }
                        else
                            _logger.Error($"File is not for today - {fusionFile}");
                    }
                    else
                        _logger.Info($"No file for {fusionFile}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error with {fusionFile}. Message: {ex.ToString()}");
                }
            }



            //Copy all the files & Replaces any files with the same name
            foreach (string fusionFile in Directory.GetFiles(extractFolder, "*.csv", SearchOption.TopDirectoryOnly))
            {
                var f = Path.GetFileName(fusionFile);
                //copy to dev fusion extract folder
                File.Copy(fusionFile, Path.Combine(extractFolder_DEV, f), true);
                //copy to dated folder on prod box
                File.Copy(fusionFile, Path.Combine(todaysFolderPath, f), true);
            }
            _logger.Info("Completed Fusion file processing");
        }

        public void MakeTodaysFolder(string path)
        {
          
            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                    Console.WriteLine("That path exists already.");
                    Array.ForEach(Directory.GetFiles(path), File.Delete);
                    Directory.Delete(path);
                    
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);
                Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(path));

            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
        }

        public async Task WorkAnotherWay()
        {
            var TARGETURL = "https://cbsi-test.fa.us2.oraclecloud.com:443/crmUI/content/conn/FusionAppsContentRepository/uuid/dDocID%3a130733?XFND_SCHEME_ID=1&XFND_CERT_FP=A4EFB534A09060FFF06FDA424C97F7C0DACB66D1&XFND_RANDOM=4682175301820339741&XFND_EXPIRES=1598982674040&XFND_SIGNATURE=a4JekOfQdex2DFFQFWw80TORt8PcSd~y4WTGjsmLDaeh-o~VtylIo7SwjxJdPhHh-VwR9PDUeggoIO~qfcUimfmxGRy3zxYg6~48vo1iuHIaY2cPVGjoQcwylZ6YcYBqij8IdnkmoZbgkBjAUAGh-TcW2wdYcXBUJ0wj996nuGM_&Id=130733&download";


            // ... Use HttpClient.            
            HttpClient client = new HttpClient();
            var encoding = new ASCIIEncoding();
            var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(encoding.GetBytes(string.Format("{0}:{1}", ConfigurationManager.AppSettings["Username"], ConfigurationManager.AppSettings["Password"]))));
            client.DefaultRequestHeaders.Authorization = authHeader;
           

            HttpResponseMessage response = await client.GetAsync(TARGETURL);
            HttpContent content = response.Content;

            // ... Check Status Code                                
            Console.WriteLine("Response StatusCode: " + (int)response.StatusCode);

            // ... Read the string.
            string result = await content.ReadAsStringAsync();

            // ... Display the result.
            if (result != null &&
            result.Length >= 50)
            {
                Console.WriteLine(result.Substring(0, 50) + "...");
            }
        }

    }
}
