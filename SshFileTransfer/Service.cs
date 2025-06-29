using System;
using System.Collections.Generic;
using System.IO;
using FluentScheduler; // For scheduling jobs.
using SshFileTransfer.Model; // For data models (Servers, ServerCredentials, JobData, TransferDetails).
using SshFileTransfer.Schedule; // For scheduled job classes (SshCopyJob, CleanDataJob).
using System.Configuration; // For accessing AppSettings.

namespace SshFileTransfer
{
    // This class represents the main service logic that is configured and run by Topshelf.
    class Service
    {
        private readonly SetupJob _setupJob = new SetupJob(); // Instance to set up and schedule jobs.
        private CoreUtil _cu = new CoreUtil(); // Utility class for common operations.
        private ServerCredentials _severCredentials; // Stores deserialized server credentials.

        /// <summary>
        /// This method is called when the Windows service starts.
        /// It loads server configurations and schedules file transfer and cleanup jobs.
        /// </summary>
        public void Start()
        {
            // Register a handler for Ctrl+C to stop the service gracefully.
            Console.CancelKeyPress += (s, e) => Stop();
            Data.MaiLogger.Information("SendSSH started " + DateTime.Now); // Log service start.

            // Deserialize server configurations from "Servers.xml".
            Servers servers = _cu.DeserializeXml<Servers>(File.ReadAllText("Servers.xml"),"UTF-8"); // 
            // Deserialize server credentials from "ServerCredentials.xml".
            _severCredentials = _cu.DeserializeXml<ServerCredentials>(File.ReadAllText("ServerCredentials.xml"), "UTF-8"); // 


            // Iterate through each server configured in Servers.xml.
            foreach (var server in servers.Server) // 
            {
                // Get credentials for the current server.
                var credentials = GetServerCredentials(server);
                if (credentials is null) continue; // Skip if no credentials found.

                var foldersToClean = new List<TransferDetails>(); // List to hold folders marked for midnight cleaning.
                // Decrypt the password using a utility (assuming csCrypt.Crypt.DecryptStringAes exists).
                var pass = csCrypt.Crypt.DecryptStringAes(credentials.Password); // 

                // Iterate through each schedule defined for the current server.
                foreach (var schedule in server.Schedule) // 
                {
                        // Create JobData object for the current job.
                        var jobData = new JobData(credentials.Host, credentials.Login, pass); // 
                        // Parse start and end times for the schedule.
                        var starttime = new List<int> { int.Parse(schedule.StartTime.Split(':')[0]), int.Parse(schedule.StartTime.Split(':')[1]) }; // 
                        var endTime = new List<int> { int.Parse(schedule.EndTime.Split(':')[0]), int.Parse(schedule.EndTime.Split(':')[1]) }; // 
                        var transferDetailsList = new List<TransferDetails>(); // List to hold details for copy operations.

                        // Iterate through each copy job within the schedule.
                        foreach (var job in schedule.CopyJob) // 
                        {
                            // If 'CleanAtMidnight' is true, add to the foldersToClean list.
                            if (job.CleanAtMidnight.Equals(true)) foldersToClean.Add(new TransferDetails(job.NetworkFolder, job.RemoteFolder)); // 
                            // Add transfer details for the copy job.
                            transferDetailsList.Add(new TransferDetails(job.Type, job.NetworkFolder, job.RemoteFolder, job.CleanAfterTransfer)); // 
                            Data.Logger.Information($"{job.Description} ({job.Type}): { job.NetworkFolder}<->{ job.RemoteFolder}"); // Log job details.
                        }

                        jobData.TransferDetails = transferDetailsList; // Assign transfer details to jobData.

                        // Define a Func that returns a new SshCopyJob instance.
                        IJob CopyJob() => new SshCopyJob(jobData); // 

                        // Schedule the copy job based on whether StartTime equals EndTime (once a day) or an interval.
                        if (schedule.StartTime.Equals(schedule.EndTime)) // 
                        {
                            _setupJob.OncePerListProvided(CopyJob, $"{server.Description}|{server.Host}", starttime, false); // 
                            Data.Logger.Information($"Job {server.Description}: {schedule.CopyJob.Count} | {server.Host} ({schedule.StartTime})"); // 
                        }
                        else
                        {
                            _setupJob.FromStartTillEndWithIntervalInMinutes(CopyJob, starttime, endTime, schedule.Interval, $"{server.Description}|{server.Host}", false); // 
                            Data.Logger.Information($"Job {server.Description}: {schedule.CopyJob.Count} | {server.Host} ({schedule.StartTime}-{schedule.EndTime}:{schedule.Interval})"); // 
                        }
                    }
                // If there are folders to clean at midnight, schedule a CleanDataJob.
                if (foldersToClean.Count <= 0) continue;
                IJob CleanJob() => new CleanDataJob(new JobData(credentials.Host, credentials.Login, pass, foldersToClean)); 
                // Parse the clean time from AppSettings.
                var cleanTime = new List<int> { int.Parse(ConfigurationManager.AppSettings["CleanTime"].Split(':')[0]), int.Parse(ConfigurationManager.AppSettings["CleanTime"].Split(':')[1]) };
                _setupJob.OncePerListProvided(CleanJob, $"Clean folders for {server.Description} - {foldersToClean.Count}", cleanTime, false); // 
                Data.Logger.Information($"Job clean folders for {server.Description} - {foldersToClean.Count} ({cleanTime[0]}:{cleanTime[1]})"); // 
            }

            // Register a global exception handler for FluentScheduler's JobManager.
            JobManager.JobException += info => { Data.Logger.Fatal(info.Exception, "JobManager.JobException"); };
            // Initialize the JobManager with the configured registry. This starts the scheduling.
            JobManager.Initialize(_setupJob.Registry);
        }

        /// <summary>
        /// Retrieves server credentials from the loaded _severCredentials based on the server's host.
        /// </summary>
        /// <param name="server">The server object for which to find credentials.</param>
        /// <returns>Credentials object if found, otherwise null.</returns>
        public Credentials GetServerCredentials(Server server) // 
        {
            Credentials cr = null;
            foreach (var credentials in _severCredentials.Credentials) // 
            {
                if (credentials.Host.Equals(server.Host)) 
                {
                    cr = new Credentials(server.Host, credentials.Login, credentials.Password); 
                }
            }
            return cr;
        }

        /// <summary>
        /// This method is called when the Windows service stops or shuts down.
        /// It logs the service stop event.
        /// </summary>
        public void Stop()
        {
            Data.Logger.Information("SendSSH stopped " + DateTime.Now); // Log service stop to general logger.
            Data.MaiLogger.Information("SendSSH stopped " + DateTime.Now); // Log service stop to mail logger.
        }
    }
}