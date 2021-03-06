﻿
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

namespace DataVaultService
{
    public class FusionDataWorker
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async Task Work()
        {


            _logger.Info("Started Fusion file processing");
            var extractFolder = ConfigurationManager.AppSettings["DVLoadsRootFolderName"];
            var extractFolder_DEV = ConfigurationManager.AppSettings["DEV_DVLoadsRootFolderName"];

            
            var todaysFolderPath = Path.Combine(ConfigurationManager.AppSettings["DVLoadsRootFolderName"], DateTime.Now.ToString(ConfigurationManager.AppSettings["DVLoadsDailyFolderName"]));
            MakeTodaysFolder(todaysFolderPath);
            var fusionFileNameList = ConfigurationManager.AppSettings["FusionCSVFileNames"].Split(',').ToList();
            OracleUCMHelper t = new OracleUCM.OracleUCMHelper(ConfigurationManager.AppSettings["UCMEndpoint"],
                                                              ConfigurationManager.AppSettings["Username"],
                                                              ConfigurationManager.AppSettings["Password"]);
            DateTime now = DateTime.Now;


           
            foreach (var fusionFile in fusionFileNameList)
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
                          
                            //if (fusionFile.EndsWith(".zip")) ;
                            //{
                            //    var zipPath = Path.Combine(extractFolder, fusionFile);
                            //    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, Path.Combine(extractFolder,"RFI_c.csv"));
                            //}

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

    }
}
