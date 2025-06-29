using System;
using System.IO;
using System.Linq;
using FluentScheduler; // Library for scheduling jobs.
using Renci.SshNet; // Library for SSH.Net client.
using SshFileTransfer.Model; // Contains data models like JobData, TransferDetails.

namespace SshFileTransfer.Schedule
{
    // Defines a scheduled job for cleaning data, implementing FluentScheduler's IJob interface.
    internal class CleanDataJob : IJob
    {
        private readonly JobData _jobData; // Data specific to this cleaning job (host, login, password, folders).
        private SftpClient _sftp; // SFTP client instance for remote operations.

        /// <summary>
        /// Constructor for the CleanDataJob.
        /// </summary>
        /// <param name="jobData">Job-specific configuration data.</param>
        public CleanDataJob(JobData jobData)
        {
            _jobData = jobData;
        }
        
        /// <summary>
        /// The main execution method for the scheduled job.
        /// This method cleans remote and local folders as specified in jobData.
        /// </summary>
        public void Execute()
        {
            Data.Logger.Information($"Starting clean job: {_jobData.Host}|{_jobData.TransferDetails.Count}"); // Log job start.
            try
            {
                // If there are no transfer details, there's nothing to clean.
                if (_jobData.TransferDetails.Count == 0) return;

                var sshSftp = new SshSftp(); // Instantiate SshSftp to use its connection and file operations.
                // Establish an SFTP connection using plain username/password authentication.
                _sftp = (SftpClient) sshSftp.BaseConnect(sshSftp.MyConInfoPlain(_jobData.Host, _jobData.Login, _jobData.Password), "SftpClient");

                // Check if the SFTP connection was successful.
                if (_sftp == null || !_sftp.IsConnected)
                {
                    Data.Logger.Error($"Unable to continue. Unable to connect to the server {_jobData.Host}");
                    return; // Exit if unable to connect.
                }

                // Iterate through each transfer detail to perform cleaning.
                foreach (var details in _jobData.TransferDetails)
                {
                    // Remove files from the specified remote folder via SFTP.
                    sshSftp.RemoveFilesFromDirViaSsh(_sftp, details.RemoteFolder);
                    
                    // Clean local network folder: delete all files.
                    var directory = new DirectoryInfo(details.NetworkFolder);
                    directory.EnumerateFiles().ToList().ForEach(f => f.Delete());
                    // Clean local network folder: delete all subdirectories (recursively).
                    directory.EnumerateDirectories().ToList().ForEach(d => d.Delete(true));
                }

                // Disconnect and dispose of the SFTP client if it was connected.
                if (_sftp == null) return;
                if (_sftp.IsConnected) _sftp.Disconnect();
                _sftp.Dispose();
                Data.Logger.Information($"SFTP ({_jobData.Host}) disconnected."); // Log disconnection.
            }
            catch (Exception ex)
            {
                // Handle and log any exceptions that occur during the cleaning process.
                var cu = new CoreUtil();
                cu.RaiseExeption(_jobData.Host, "clean job", ex);
            }
        }
    }
}