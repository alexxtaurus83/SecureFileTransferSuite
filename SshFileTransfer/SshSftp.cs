using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using HeyRed.Mime; // Used for MIME type detection.
using Renci.SshNet; // Core library for SSH, SCP, and SFTP.
using Renci.SshNet.Sftp; // Specific namespace for SFTP operations.

namespace SshFileTransfer
{
    // This class encapsulates SSH, SFTP, and SCP operations.
    class SshSftp
    {
        // Private utility instance for common tasks like exception handling and MIME detection.
        private readonly CoreUtil _util = new CoreUtil();

        /// <summary>
        /// Creates a ConnectionInfo object for SSH/SFTP/SCP using an OpenSSH private key file.
        /// </summary>
        /// <param name="opensshkey">Path to the OpenSSH private key file.</param>
        /// <param name="remotehost">The hostname or IP address of the remote server.</param>
        /// <param name="username">The username for authentication.</param>
        /// <returns>A ConnectionInfo object configured for private key authentication.</returns>
        internal ConnectionInfo MyConInfoSsl(string opensshkey, string remotehost, string username)
        {
            // Open and read the private key file.
            var s = File.OpenRead(opensshkey);
            // Create a PrivateKeyConnectionInfo object. This type of ConnectionInfo uses private key authentication.
            var cnifo = new PrivateKeyConnectionInfo(remotehost, username, new PrivateKeyFile(s))
            {
                Timeout = TimeSpan.FromMinutes(150), // Set a generous timeout for connection.
                RetryAttempts = 5 // Number of times to retry connection.
            };
            return cnifo;
        }

        /// <summary>
        /// Creates a ConnectionInfo object for SSH/SFTP/SCP using username and password authentication.
        /// </summary>
        /// <param name="remotehost">The hostname or IP address of the remote server.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <returns>A ConnectionInfo object configured for password authentication.</returns>
        internal ConnectionInfo MyConInfoPlain(string remotehost, string username, string password)
        {
            // Create a PasswordConnectionInfo object. This type uses username/password authentication.
            var cnifo = new PasswordConnectionInfo(remotehost, username, password)
            {
                Timeout = TimeSpan.FromMinutes(1), // Set a timeout for connection.
                RetryAttempts = 5 // Number of times to retry connection.
            };
            return cnifo;
        }

        /// <summary>
        /// Attempts to establish a connection to an SFTP/SCP server with retry logic.
        /// </summary>
        /// <param name="connectionInfo">The connection details (host, username, password/key).</param>
        /// <param name="type">The type of client to connect ('scpclient' or 'sftpclient').</param>
        /// <returns>A connected BaseClient (ScpClient or SftpClient) or null if connection fails after retries.</returns>
        internal BaseClient BaseConnect(ConnectionInfo connectionInfo, string type)
        {
            BaseClient baseClient = null; // Initialize baseClient as null.
            var failedToConnect = false; // Flag to track connection status.

            // Local function to encapsulate the connection logic, used for initial attempt and retries.
            void ConnectToFtpSserver()
            {
                try
                {
                    Data.Logger.Information("Init FTPS connection..."); // Log initiation (Note: The log states FTPS, but this is an SSH/SFTP client).

                    // Instantiate the correct client type based on the 'type' parameter.
                    if (type.ToLower().Equals("scpclient"))
                    {
                        baseClient = new ScpClient(connectionInfo);
                    }
                    else if (type.ToLower().Equals("sftpclient")) // Changed to else if to ensure only one is chosen.
                    {
                        baseClient = new SftpClient(connectionInfo);
                    }

                    Data.Logger.Information($"Trying to connect to {connectionInfo.Host}");
                    baseClient.Connect(); // Attempt to connect. This call blocks until connection is established or fails/times out.
                    if (!baseClient.IsConnected) return; // If not connected, return (likely an exception was caught or timed out).
                    Data.Logger.Information($"Connected to {connectionInfo.Host}");
                    // Log connection details for debugging.
                    Data.Logger.Information($"CurrentServerEncryption: {baseClient.ConnectionInfo.CurrentServerEncryption} | ServerVersion: {baseClient.ConnectionInfo.ServerVersion}");
                    failedToConnect = false; // Connection successful.
                }
                catch (Exception ex)
                {
                    // Log the exception and set failedToConnect flag.
                    Data.Logger.Information($"We get an exception but will try to connect again: {ex.Message}");
                    failedToConnect = true;
                }
            }

            // Initial connection attempt.
            ConnectToFtpSserver();

            // Retry logic with delays if the initial connection failed.
            if (failedToConnect)
            {
                Data.Logger.Information("For some reason server wasn't responding after a period of time. Sleeping 10 seconds and try again - 1st time.");
                Thread.Sleep(10000); // Wait 10 seconds.
                ConnectToFtpSserver(); // Second attempt.
            }

            if (failedToConnect)
            {
                Data.Logger.Information("For some reason server wasn't responding after a period of time. Sleeping 10 seconds and try again - 2nd time.");
                Thread.Sleep(10000); // Wait 10 seconds.
                ConnectToFtpSserver(); // Third attempt.
            }

            if (failedToConnect)
            {
                Data.Logger.Information("For some reason server wasn't responding after a period of time. Sleeping 10 seconds and try again - last attempt.");
                Thread.Sleep(10000); // Wait 10 seconds.
                ConnectToFtpSserver(); // Fourth and final attempt.
            }
            return baseClient; // Return the connected client or null.
        }

        /// <summary>
        /// Executes a command on the remote SSH server.
        /// </summary>
        /// <param name="connectionInfo">The connection details.</param>
        /// <param name="remoteApplication">The command or application path to execute on the remote server.</param>
        /// <param name="showResult">If true, the command's output will be logged.</param>
        internal void LaunchViaSsh(ConnectionInfo connectionInfo, string remoteApplication, bool showResult)
        {
            // Create a new SshClient instance with the provided connection information.
            var sshClient = new SshClient(connectionInfo);
            try
            {
                sshClient.Connect(); // Connect to the SSH server.
                // Redundant connect call, as Connect() is blocking and throws on failure.
                if (!sshClient.IsConnected) sshClient.Connect();
                // Create an SSH command object with the specified remote application.
                var sshCmd = sshClient.CreateCommand(remoteApplication);
                // Execute the command and get the result.
                var result = sshCmd.Execute();
                // Log the result if showResult is true.
                if (showResult) { Data.Logger.Information(result); }
                // Log the exit status if it's not 1 (assuming 0 is success, anything else is an error).
                if (sshCmd.ExitStatus != 1) { Data.Logger.Information("Done with status " + sshCmd.ExitStatus); }
                sshClient.Disconnect(); // Disconnect from the SSH server.
            }
            catch (Exception ex)
            {
                // Raise a custom exception if issues occur during SSH command execution.
                _util.RaiseExeption("Issues with LaunchViaSsh", null, ex);
            }
            finally
            {
                // Ensure the SSH client is disposed to release resources.
                sshClient.Dispose();
            }
        }
        
        /// <summary>
        /// Copies multiple local files to a remote SFTP directory using a new SFTP connection.
        /// </summary>
        /// <param name="connectionInfo">The connection details.</param>
        /// <param name="files">An array of local file paths to copy.</param>
        /// <param name="destinationRemoteDir">The remote directory path on the SFTP server.</param>
        internal void CopyViaSsh(ConnectionInfo connectionInfo, string[] files, string destinationRemoteDir)
        {
            try
            {
                // If no files are provided, do nothing.
                if (!files.Any()) return;
                // Use a 'using' block to ensure the SftpClient is properly disposed.
                using (var sftp = new SftpClient(connectionInfo))
                {
                    sftp.Connect(); // Connect to the SFTP server.
                    // Redundant connect call.
                    if (!sftp.IsConnected) sftp.Connect();
                    // Iterate through each file to upload.
                    foreach (var file in files)
                    {
                        // Detect and log the MIME type of the file.
                        _util.DetectMime(file);
                        // Open the local file for reading.
                        using (var uplfileStream = File.OpenRead(file))
                        {
                            // Upload the file to the remote directory. Overwrite if exists (true).
                            sftp.UploadFile(uplfileStream, destinationRemoteDir + Path.GetFileName(file), true);
                            Data.Logger.Information($"Copying {file} to: {destinationRemoteDir}");
                        }
                    }
                    sftp.Disconnect(); // Disconnect from the SFTP server.
                }
            }
            catch (Exception ex)
            {
                // Raise a custom exception if issues occur during SCP file transfer.
                _util.RaiseExeption("Issues with CopyViaSsh", null, ex);
            }
        }

        /// <summary>
        /// Copies multiple local files to a remote SFTP directory using an existing SFTP connection.
        /// </summary>
        /// <param name="sftp">An already connected SftpClient instance.</param>
        /// <param name="files">An array of local file paths to copy.</param>
        /// <param name="destinationRemoteDir">The remote directory path on the SFTP server.</param>
        internal void CopyViaSsh(SftpClient sftp, string[] files, string destinationRemoteDir)
        {
            try
            {
                // If no files are provided, do nothing.
                if (!files.Any()) return;
                // Iterate through each file to upload.
                foreach (var file in files)
                {
                    // Detect and log the MIME type of the file.
                    _util.DetectMime(file);
                    // Open the local file for reading.
                    using (var uplfileStream = File.OpenRead(file))
                    {
                        Data.Logger.Information($"Copying {file} to: {destinationRemoteDir}");
                        // Upload the file to the remote directory. Overwrite if exists (true).
                        sftp.UploadFile(uplfileStream, destinationRemoteDir + Path.GetFileName(file), true);
                        Data.Logger.Information($"done Copying {file} to: {destinationRemoteDir}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Raise a custom exception if issues occur during SCP file transfer.
                _util.RaiseExeption("Issues with CopyViaSsh", null, ex);
            }
        }

        /// <summary>
        /// Copies a local directory and its contents to a remote SCP directory using an existing SCP connection.
        /// </summary>
        /// <param name="scp">An already connected ScpClient instance.</param>
        /// <param name="localDirectoryInfo">The DirectoryInfo object for the local directory to copy.</param>
        /// <param name="destinationRemoteDir">The remote directory path on the SCP server.</param>
        internal void CopyViaSsh(ScpClient scp, DirectoryInfo localDirectoryInfo, string destinationRemoteDir)
        {
            try
            {
                Data.Logger.Information($"Copying {localDirectoryInfo.FullName} to: {destinationRemoteDir}");
                // Upload the entire directory to the remote destination.
                scp.Upload(localDirectoryInfo, destinationRemoteDir);
                Data.Logger.Information($"done Copying {localDirectoryInfo.FullName} to: {destinationRemoteDir}");
            }
            catch (Exception ex)
            {
                // Raise a custom exception if issues occur during SCP directory transfer.
                _util.RaiseExeption("Issues with CopyViaSsh", null, ex);
            }
        }

        /// <summary>
        /// Downloads files from a remote SFTP directory to a local directory.
        /// Only downloads files, skips directories and special entries ('.' and '..').
        /// </summary>
        /// <param name="sftp">An already connected SftpClient instance.</param>
        /// <param name="remoteDir">The remote directory path to download from.</param>
        /// <param name="localDir">The local directory path to save files to.</param>
        internal void DownloadViaSsh(SftpClient sftp, string remoteDir, string localDir)
        {
            try
            {
                Data.Logger.Information($"remote dir: {remoteDir}");
                // List all entries in the remote directory.
                var listDirectory = sftp.ListDirectory(remoteDir);
                var count = listDirectory.Count();
                if (count == 0) return; // If directory is empty, do nothing.

                // Iterate through each item in the remote directory.
                foreach (var ftpfile in listDirectory.Where(ftpfile => !(ftpfile.Name.Equals(".") | ftpfile.Name.Equals(".."))))
                {
                    // Get attributes of the remote file/directory.
                    SftpFileAttributes attrs = sftp.GetAttributes(ftpfile.FullName);
                    // If it's not a directory (i.e., it's a file).
                    if (!attrs.IsDirectory)
                    {
                        // Create a local file stream to save the downloaded file.
                        using (var fs = new FileStream(Path.Combine(localDir, ftpfile.Name), FileMode.Create))
                        {
                            Data.Logger.Information($"Started copying {ftpfile.FullName} to {localDir}");
                            // Download the file from the SFTP server.
                            sftp.DownloadFile(ftpfile.FullName, fs);
                            Data.Logger.Information($"done copying {ftpfile.FullName} to {localDir}");
                        }
                        // Detect and log the MIME type of the downloaded file.
                        _util.DetectMime(Path.Combine(localDir, ftpfile.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                // Raise a custom exception if issues occur during SFTP download.
                _util.RaiseExeption("Issues with DownloadViaSsh", null, ex);
            }
        }

        /// <summary>
        /// Downloads a remote directory and its contents to a local directory using an existing SCP connection.
        /// </summary>
        /// <param name="scp">An already connected ScpClient instance.</param>
        /// <param name="localDirectoryInfo">The DirectoryInfo object for the local destination directory.</param>
        /// <param name="remoteDir">The remote directory path to download.</param>
        internal void DownloadViaSsh(ScpClient scp, DirectoryInfo localDirectoryInfo, string remoteDir)
        {
            try
            {
                Data.Logger.Information($"Started copying data from {remoteDir} to {localDirectoryInfo.FullName}");
                // Download the entire remote directory to the local destination.
                scp.Download(remoteDir, localDirectoryInfo);
                Data.Logger.Information($"done copying from {remoteDir} to {localDirectoryInfo.FullName}");
            }
            catch (Exception ex)
            {
                // Raise a custom exception if issues occur during SCP directory download.
                _util.RaiseExeption("Issues with DownloadViaSsh", null, ex);
            }
        }

        /// <summary>
        /// Removes all files (but not directories) from a specified remote directory via SFTP using a new connection.
        /// </summary>
        /// <param name="connectionInfo">The connection details.</param>
        /// <param name="remoteDir">The remote directory from which to remove files.</param>
        internal void RemoveFilesFromDirViaSsh(ConnectionInfo connectionInfo, string remoteDir)
        {
            try
            {
                // Use a 'using' block to ensure the SftpClient is properly disposed.
                using (var sftp = new SftpClient(connectionInfo))
                {
                    sftp.Connect(); // Connect to the SFTP server.
                    // Redundant connect call.
                    if (!sftp.IsConnected) sftp.Connect();
                    // List all entries in the remote directory.
                    var listDirectory = sftp.ListDirectory(remoteDir);
                    var count = listDirectory.Count();
                    if (count == 0) return; // If directory is empty, do nothing.

                    // Iterate through each item and delete files (skipping '.' and '..').
                    foreach (var ftpfile in listDirectory.Where(ftpfile => !(ftpfile.Name.Equals(".") | ftpfile.Name.Equals(".."))))
                    {
                        sftp.DeleteFile(ftpfile.FullName); // Delete the file.
                        Data.Logger.Information($"removing {ftpfile.FullName} file");
                    }
                    sftp.Disconnect(); // Disconnect from the SFTP server.
                }
            }
            catch (Exception ex)
            {
                // Raise a custom exception if issues occur during file removal.
                _util.RaiseExeption("Issues with RemoveFilesFromDir", null, ex);
            }
        }

        /// <summary>
        /// Removes all files and directories (recursively) from a specified remote directory via an existing SFTP connection.
        /// </summary>
        /// <param name="sftp">An already connected SftpClient instance.</param>
        /// <param name="remoteDir">The remote directory from which to remove items.</param>
        internal void RemoveFilesFromDirViaSsh(SftpClient sftp, string remoteDir)
        {
            try
            {
                // List all entries in the remote directory.
                var listDirectory = sftp.ListDirectory(remoteDir);
                var count = listDirectory.Count();
                if (count == 0) return; // If directory is empty, do nothing.

                // Iterate through each item and delete files or directories (skipping '.' and '..').
                foreach (var ftpfile in listDirectory.Where(ftpfile => !(ftpfile.Name.Equals(".") | ftpfile.Name.Equals(".."))))
                {
                    Data.Logger.Information($"removing {ftpfile.FullName} item");
                    if (ftpfile.IsDirectory)
                    {
                        sftp.DeleteDirectory(ftpfile.FullName); // Delete the directory (recursive delete is default for Renci.SshNet.SftpClient.DeleteDirectory).
                    }
                    else
                    {
                        sftp.DeleteFile(ftpfile.FullName); // Delete the file.
                    }
                    Data.Logger.Information($"done removing {ftpfile.FullName} item");
                }
            }
            catch (Exception ex)
            {
                // Raise a custom exception if issues occur during file/directory removal.
                _util.RaiseExeption("Issues with RemoveFilesFromDir", null, ex);
            }
        }
    }
}