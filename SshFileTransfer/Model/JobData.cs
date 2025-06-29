using System.Collections.Generic;

namespace SshFileTransfer.Model
{
    // Represents the data structure for a scheduled job, holding connection and transfer details.
    public class JobData
    {
        public string Host { get; set; } // The remote host for the job.
        public string Login { get; set; } // The username for connecting to the remote host.
        public string Password { get; set; } // The password for connecting to the remote host.

        /// <summary>
        /// Constructor for JobData with host, login, and password.
        /// </summary>
        public JobData(string host, string login, string password)
        {
            Host = host;
            Login = login;
            Password = password;
        }

        /// <summary>
        /// Constructor for JobData including a list of transfer details.
        /// </summary>
        public JobData(string host, string login, string password, List<TransferDetails> transferDetails)
        {
            Host = host;
            Login = login;
            Password = password;
            TransferDetails = transferDetails;
        }

        /// <summary>
        /// Constructor for JobData with only transfer details (host/login/password would be set elsewhere).
        /// </summary>
        public JobData(List<TransferDetails> transferDetails)
        {
            TransferDetails = transferDetails;
        }

        // A list of specific file/directory transfer operations to perform within this job.
        public List<TransferDetails> TransferDetails { get; set; }
    }

    // Represents details for a single file/directory transfer operation within a job.
    public class TransferDetails
    {
        public string Type { get; set; } // Type of transfer (e.g., "out", "in", "outdir", "indir").
        public string NetworkFolder { get; set; } // Local path (e.g., shared network drive, local folder).
        public string RemoteFolder { get; set; } // Remote path on the SSH/SFTP server.
        public bool CleanAfterTransfer { get; set; } // Indicates if local/remote source should be cleaned after successful transfer.

        /// <summary>
        /// Constructor for TransferDetails with all properties.
        /// </summary>
        public TransferDetails(string type, string networkFolder, string remoteFolder, bool cleanAfterTransfer)
        {
            Type = type;
            NetworkFolder = networkFolder;
            RemoteFolder = remoteFolder;
            CleanAfterTransfer = cleanAfterTransfer;
        }

        /// <summary>
        /// Constructor for TransferDetails with only network and remote folder paths (for cleaning contexts).
        /// </summary>
        public TransferDetails(string networkFolder, string remoteFolder)
        {
            NetworkFolder = networkFolder;
            RemoteFolder = remoteFolder;
        }
    }
}