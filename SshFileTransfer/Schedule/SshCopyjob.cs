using System;
using System.IO;
using System.Linq;
using System.Net; // Not directly used in this file for core logic, but often for network related info.
using System.Threading; // Used for Thread.Sleep.
using FluentScheduler; // Core library for scheduling jobs.
using Renci.SshNet; // Library for SSH.Net client.
using SshFileTransfer.Model; // Contains data models like JobData, TransferDetails.

namespace SshFileTransfer.Schedule
{
    // Defines a scheduled job for copying data via SSH (SFTP or SCP), implementing FluentScheduler's IJob interface.
    internal class SshCopyJob : IJob
    {
        private readonly JobData _jobData; // Data specific to this copy job (host, login, password, transfer details).
        private SftpClient _sftp; // SFTP client instance for file transfers.
        private ScpClient _scp; // SCP client instance for directory transfers.


        /// <summary>
        /// Constructor for the SshCopyJob.
        /// </summary>
        /// <param name="jobData">Job-specific configuration data.</param>
        public SshCopyJob(JobData jobData)
        {
            _jobData = jobData;
        }

        /// <summary>
        /// The main execution method for the scheduled job.
        /// This method performs file transfers (upload/download) based on the job's transfer details.
        /// </summary>
        public void Execute()
        {
            Data.Logger.Information($"Starting copy job: {_jobData.Host}|{_jobData.TransferDetails.Count}"); // Log job start.
            try
            {
                // If there are no transfer details, there's nothing to copy.
                if (_jobData.TransferDetails.Count == 0) return;

                var sshSftp = new SshSftp(); // Instantiate SshSftp to use its connection and file operations.

                // Iterate through each transfer detail to perform the specified operation.
                foreach (var details in _jobData.TransferDetails) // 
                {
                    // Handle "out" type (uploading files via SFTP).
                    if (details.Type.ToLower().Equals("out")) // 
                    {
                        // Skip if the local network folder has no files.
                        if (Directory.GetFiles(details.NetworkFolder).Length <= 0) continue; // 
                        // If SFTP client is not yet connected for this job, connect it.
                        if (_sftp == null)
                        {
                            _sftp = (SftpClient) sshSftp.BaseConnect(sshSftp.MyConInfoPlain(_jobData.Host, _jobData.Login, _jobData.Password), "SftpClient"); 
                            // If connection fails, log error and exit.
                            if (_sftp == null || !_sftp.IsConnected) // 
                            {
                                Data.Logger.Error($"Unable to continue. Unable to connect to the server {_jobData.Host}"); // 
                                return;
                            }
                        }
                        Data.Logger.Information($"FTP ({_jobData.Host}) connected: {_sftp.IsConnected}"); // 
                        // Perform the file copy (upload).
                        sshSftp.CopyViaSsh(_sftp, Directory.GetFiles(details.NetworkFolder), details.RemoteFolder); 
                        // If configured, clean the local network folder after transfer.
                        if (details.CleanAfterTransfer) // 
                        {
                            new DirectoryInfo(details.NetworkFolder).EnumerateFiles().ToList().ForEach(f => f.Delete()); // Delete all files. 
                            new DirectoryInfo(details.NetworkFolder).EnumerateDirectories().ToList().ForEach(d => d.Delete(true)); // Delete all subdirectories recursively. 
                            Data.Logger.Information($"Folder {details.NetworkFolder} was cleaned."); // 
                        }
                    }
                    // Handle "in" type (downloading files via SFTP).
                    else if (details.Type.ToLower().Equals("in")) // 
                    {
                        // If SFTP client is not yet connected for this job, connect it.
                        if (_sftp == null)
                        {
                            _sftp = (SftpClient) sshSftp.BaseConnect(sshSftp.MyConInfoPlain(_jobData.Host, _jobData.Login, _jobData.Password), "SftpClient"); 
                            // If connection fails, log error and exit.
                            if (_sftp == null || !_sftp.IsConnected) // 
                            {
                                Data.Logger.Error($"Unable to continue. Unable to connect to the server {_jobData.Host}"); // 
                                return;
                            }
                        }
                        // Perform the file download.
                        sshSftp.DownloadViaSsh(_sftp, details.RemoteFolder, details.NetworkFolder); 
                        // If configured, clean the remote folder after transfer.
                        if (details.CleanAfterTransfer) sshSftp.RemoveFilesFromDirViaSsh(_sftp, details.RemoteFolder); 
                    }
                    // Handle "outdir" type (uploading directories via SCP).
                    else if (details.Type.ToLower().Equals("outdir")) // 
                    {
                        // Skip if the local network folder does not exist.
                        if (!Directory.Exists(details.NetworkFolder)) continue; // 
                        // If SCP client is not yet connected for this job, connect it.
                        if (_scp == null)
                        {
                            _scp = (ScpClient) sshSftp.BaseConnect(sshSftp.MyConInfoPlain(_jobData.Host, _jobData.Login, _jobData.Password), "ScpClient"); 
                            // If connection fails, log error and exit.
                            if (_scp == null || !_scp.IsConnected) // 
                            {
                                Data.Logger.Error($"Unable to continue. Unable to connect to the server {_jobData.Host}"); // 
                                return;
                            }
                        }
                        Data.Logger.Information($"FTP ({_jobData.Host}) connected: {_scp.IsConnected.ToString()}"); //  // Note: Logs FTP but uses SCP.
                        // Perform the directory copy (upload).
                        sshSftp.CopyViaSsh(_scp, new DirectoryInfo(details.NetworkFolder), details.RemoteFolder); 
                        // If configured, clean the local network folder after transfer.
                        if (details.CleanAfterTransfer) // 
                        {
                            new DirectoryInfo(details.NetworkFolder).EnumerateFiles().ToList().ForEach(f => f.Delete()); // Delete all files. 
                            new DirectoryInfo(details.NetworkFolder).EnumerateDirectories().ToList().ForEach(d => d.Delete(true)); // Delete all subdirectories. 
                            Directory.Delete(details.NetworkFolder); // Delete the local directory itself. 
                            Data.Logger.Information($"Folder {details.NetworkFolder} was deleted."); // 
                        }
                    }
                    // Handle "indir" type (downloading directories via SCP).
                    else if (details.Type.ToLower().Equals("indir")) // 
                    {
                        // If the local network folder already exists, delete its contents and the folder itself.
                        if (Directory.Exists(details.NetworkFolder)) // 
                        {
                            new DirectoryInfo(details.NetworkFolder).EnumerateFiles().ToList().ForEach(f => f.Delete()); // Delete files. 
                            new DirectoryInfo(details.NetworkFolder).EnumerateDirectories().ToList().ForEach(d => d.Delete(true)); // Delete directories. 
                            Directory.Delete(details.NetworkFolder); // Delete the local directory itself. 
                            Data.Logger.Error($"{details.NetworkFolder} exist. And will be removed."); // 
                        }
                        // If SCP client is not yet connected for this job, connect it.
                        if (_scp == null)
                        {
                            _scp = (ScpClient)sshSftp.BaseConnect(sshSftp.MyConInfoPlain(_jobData.Host, _jobData.Login, _jobData.Password), "ScpClient"); 
                            // If connection fails, log error and exit.
                            if (_scp == null || !_scp.IsConnected) // 
                            {
                                Data.Logger.Error($"Unable to continue. Unable to connect to the server {_jobData.Host}"); // 
                                return;
                            }
                        }
                        // Perform the directory download.
                        sshSftp.DownloadViaSsh(_scp, new DirectoryInfo(details.NetworkFolder), details.RemoteFolder); 

                        // The 'CleanAfterTransfer' for 'indir' is commented out or incomplete in the original code.
                        if (details.CleanAfterTransfer) // 
                        {
                            //var tmpSftp = (SftpClient) baseConnect;
                            //(_scp, details.RemoteFolder); // This line is incomplete in original.
                        }
                    }
                }
                // Disconnect and dispose SFTP and SCP clients if they were connected.
                if (_sftp == null) return;
                if (_scp == null) return; // This line causes NullReferenceException if _sftp is not null but _scp is. Consider proper client management.
                if (_sftp.IsConnected) _sftp.Disconnect();
                if (_scp.IsConnected) _scp.Disconnect();
                _sftp.Dispose();
                _scp.Dispose();
                Data.Logger.Information($"SFTP ({_jobData.Host}) disconnected."); // 
            }
            catch (Exception ex)
            {
                // Handle and log any exceptions that occur during the copy process.
                var cu = new CoreUtil();
                cu.RaiseExeption(_jobData.Host, "",ex);
            }
        }
    }
}