using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using System.Collections.Specialized;
using DataVaultService.Jobs;
using System.Configuration;

namespace DataVaultService
{
    public class Service
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void Start()
        {
            // write code here that runs when the Windows Service starts up. 
            log.Info("DataVaultDataService started.");
            RunProgram().GetAwaiter().GetResult();

        }
        public void Stop()
        {
            // write code here that runs when the Windows Service stops.  
            log.Info("DataVaultDataService stopped.");
        }

        private static async Task RunProgram()
        {
            try
            {
                // Grab the Scheduler instance from the Factory
                NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };

                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();
                // define the job and tie it to our HelloJob class
                IJobDetail jobDetail = JobBuilder.Create<LoadFusionDataJob>()
                    .WithIdentity("FusionJob")
                    .Build();

                // Trigger the job to run now, and then repeat every 10 seconds
                ITrigger trigger = TriggerBuilder.Create()
                    .ForJob(jobDetail)
                    .WithCronSchedule(ConfigurationManager.AppSettings["CRONTriggerInterval"])  //0 0 7 ? * MON-SUN * -- every day at 7:00AM
                    .WithIdentity("FusionTrigger")
                    .StartNow()
                    .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(jobDetail, trigger);
                await scheduler.Start();

                //// some sleep to show what's happening
                //await Task.Delay(TimeSpan.FromSeconds(1));

                // and last shut down the scheduler when you are ready to close your program
                //await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                await Console.Error.WriteLineAsync(se.ToString());
            }
        }
    }
}

