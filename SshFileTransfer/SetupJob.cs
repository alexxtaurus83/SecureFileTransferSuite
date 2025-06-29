using System;
using System.Collections.Generic;
using FluentScheduler; // Core library for scheduling jobs.

namespace SshFileTransfer
{
    // This class provides methods to set up and schedule various jobs using FluentScheduler.
    class SetupJob
    {
        // Registry instance where all scheduled jobs are added.
        public Registry Registry = new Registry();

        /// <summary>
        /// Schedules a job to run repeatedly within a specified time window with a given interval.
        /// </summary>
        /// <param name="job">A Func that returns an IJob instance (the job to be scheduled).</param>
        /// <param name="starttime">A list of two integers [hour, minute] representing the start time of the execution window.</param>
        /// <param name="endtime">A list of two integers [hour, minute] representing the end time of the execution window.</param>
        /// <param name="interval">The interval in minutes at which the job should repeat within the window.</param>
        /// <param name="jobName">A unique name for the job.</param>
        /// <param name="toRunNow">If true, the job will execute immediately upon initialization, then follow the schedule.</param>
        public void FromStartTillEndWithIntervalInMinutes(Func<IJob> job, List<int> starttime, List<int> endtime, int interval, string jobName, bool toRunNow)
        {
            if (toRunNow)
            {
                // Schedule the job to run immediately and then every 'interval' minutes within the specified time window.
                Registry.Schedule(job).NonReentrant().WithName(jobName).ToRunNow().AndEvery(interval).Minutes().Between(starttime[0], starttime[1], endtime[0], endtime[1]);
            }
            else
            {
                // Schedule the job to run every 'interval' minutes within the specified time window (no immediate run).
                Registry.Schedule(job).NonReentrant().WithName(jobName).ToRunEvery(interval).Minutes().Between(starttime[0], starttime[1], endtime[0], endtime[1]);
            }

            // Commented-out examples of other FluentScheduler capabilities:
            // var b = registry.Schedule<UpdateJob>().ToRunEvery(5).Seconds().Between(11, 47, 11, 48);
            // registry.Schedule<UpdateJob>().ToRunNow().AndEvery(2).Seconds();
            // registry.Schedule<UpdateJob>().ToRunOnceIn(5).Seconds();
            // registry.Schedule<UpdateJob>().ToRunNow().AndEvery(1).Months().OnTheFirst(DayOfWeek.Monday).At(3, 0);
            // registry.Schedule<UpdateJob>().AndThen<UpdateJob>().ToRunNow().AndEvery(5).Minutes();
            // JobManager.AddJob(() => Console.WriteLine("Late job!"), (s) => s.ToRunEvery(5).Seconds());
        }

        /// <summary>
        /// Schedules a job to run once per day at a specific time.
        /// </summary>
        /// <param name="job">A Func that returns an IJob instance (the job to be scheduled).</param>
        /// <param name="jobName">A unique name for the job.</param>
        /// <param name="starttime">A list of two integers [hour, minute] representing the exact time to run.</param>
        /// <param name="toRunNow">If true, the job will execute immediately upon initialization, then follow the schedule.</param>
        internal void OncePerListProvided(Func<IJob> job, string jobName, List<int> starttime, bool toRunNow)
        {
            if (toRunNow)
            {
                // Schedule the job to run immediately and then once every day at the specified start time.
                Registry.Schedule(job).NonReentrant().WithName(jobName).ToRunNow().AndEvery(1).Days().At(starttime[0], starttime[1]);
            }
            else
            {
                // Schedule the job to run once every day at the specified start time (no immediate run).
                Registry.Schedule(job).NonReentrant().WithName(jobName).ToRunEvery(1).Days().At(starttime[0], starttime[1]);
            }
        }
    }
}