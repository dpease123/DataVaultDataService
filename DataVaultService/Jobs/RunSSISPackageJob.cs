using DataVaultService.Jobs.Workers;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataVaultService.Jobs
{
    [PersistJobDataAfterExecution]
    class RunSSISPackageJob : IJob
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public async Task Execute(IJobExecutionContext context)
        {
            SSISJobWorker Worker = new SSISJobWorker();
            try
            {
                log.Info("-----------------------------------------------------------------------------------------------------");
                log.Info("RunSSISPackage Job Starting...");
                await Worker.Work();
                log.Info("RunSSISPackage Job Done!");
                log.Info("-----------------------------------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }
    }
}

