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

                // and start it off
                await scheduler.Start();

                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<LoadFusionDataJob>()
                    .WithIdentity("FusuionJob", "group1")
                    .Build();

                // Trigger the job to run now, and then repeat every 10 seconds
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger1", "group1")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithIntervalInHours(int.Parse(ConfigurationManager.AppSettings["JobnIntervalHours"]))
                        .RepeatForever())
                    .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);

                // some sleep to show what's happening
                await Task.Delay(TimeSpan.FromSeconds(1));

                // and last shut down the scheduler when you are ready to close your program
                await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                await Console.Error.WriteLineAsync(se.ToString());
            }
        }
    }
}

