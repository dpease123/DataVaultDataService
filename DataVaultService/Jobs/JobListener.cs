using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataVaultService.Jobs
{
    class JobListener
    {
        /// <summary>
        /// this listener class has methods which run before and after job execution
        /// </summary>
        public class JobListenerExample : IJobListener
        {


            /// <summary>
            /// this gets called after a job is executed
            /// </summary>
            /// <param name="context"></param>
            /// <param name="jobException"></param>
            public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
            {
                if (jobException != null)
                {
                    return;
                }

                if (context == null)
                {
                    throw new ArgumentNullException("Completed job does not have valid Job Execution Context");
                }

                var finishedJob = context.JobDetail;

                await context.Scheduler.DeleteJob(finishedJob.Key);

                var childJobs = finishedJob.JobDataMap.Get(Constants.NextJobKey) as List<JobKey>;

                if (childJobs == null)
                {
                    return;
                }

                foreach (var jobKey in childJobs)
                {
                    var newJob = await context.Scheduler.GetJobDetail(jobKey);

                    if (newJob == null)
                    {
                        Console.WriteLine($"Could not find Job with ID: {jobKey}");
                        continue;
                    }

                    var oldJobMap = context.JobDetail.JobDataMap.Get(Constants.PayloadKey) as Dictionary<string, object>;

                    newJob.JobDataMap.Put(Constants.PayloadKey, oldJobMap);

                    await context.Scheduler.AddJob(newJob, true, false);
                    await context.Scheduler.TriggerJob(jobKey);
                }
            
            }

            /// <summary>
            /// this gets called before a job is executed
            /// </summary>
            /// <param name="context"></param>
            public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<object>(null);
            }

            /// <summary>
            /// to dismiss/ban/veto a job, we should return true from this method
            /// </summary>
            /// <param name="context"></param>
            public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// returns name of the listener
            /// </summary>
                 }

    }

    public class Constants
    {
        public static readonly string PayloadKey = "FusionKey";
        public static readonly string NextJobKey = "SSISKey";
    }
}
