using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataVaultService;
using Quartz;


namespace DataVaultService.Jobs
{
    public class LoadFusionDataJob : IJob
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public async Task Execute(IJobExecutionContext context)
        {
            FusionDataWorker FusionWorker = new FusionDataWorker();
            try
            {
                log.Info("-----------------------------------------------------------------------------------------------------");
                log.Info("LoadFusionData Job Starting...");
                await FusionWorker.Work();
                log.Info("LoadFusionData Job Done!");
                log.Info("-----------------------------------------------------------------------------------------------------");
            }
            catch(Exception ex)
            {
                log.Error(ex.ToString());
            }
        }
    }
}
