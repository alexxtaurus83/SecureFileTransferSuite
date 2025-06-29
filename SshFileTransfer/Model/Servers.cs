using System.Collections.Generic;
using System.Xml.Serialization; // Used for XML serialization attributes.

namespace SshFileTransfer.Model
{
    /// <summary>
    /// Represents a single copy job configuration within a schedule.
    /// Corresponds to an XML element named "CopyJob".
    /// </summary>
    [XmlRoot(ElementName = "CopyJob")]
    public class CopyJob
    {
        /// <summary>
        /// The local network folder path involved in the transfer.
        /// Corresponds to an XML element named "NetworkFolder".
        /// </summary>
        [XmlElement(ElementName = "NetworkFolder")]
        public string NetworkFolder { get; set; }

        /// <summary>
        /// The remote folder path on the SSH/SFTP server.
        /// Corresponds to an XML element named "RemoteFolder".
        /// </summary>
        [XmlElement(ElementName = "RemoteFolder")]
        public string RemoteFolder { get; set; }

        /// <summary>
        /// A descriptive name for the copy job.
        /// Corresponds to an XML attribute named "Description".
        /// </summary>
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }

        /// <summary>
        /// The type of transfer operation (e.g., "out" for upload files, "in" for download files,
        /// "outdir" for upload directory, "indir" for download directory).
        /// Corresponds to an XML attribute named "Type".
        /// </summary>
        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        /// <summary>
        /// (Optional) An approver related to the job.
        /// Corresponds to an XML attribute named "Approver".
        /// </summary>
        [XmlAttribute(AttributeName = "Approver")]
        public string Approver { get; set; }

        /// <summary>
        /// Indicates if the associated network folder should be cleaned at midnight.
        /// Corresponds to an XML attribute named "CleanAtMidnight".
        /// </summary>
        [XmlAttribute(AttributeName = "CleanAtMidnight")]
        public bool CleanAtMidnight { get; set; }

        /// <summary>
        /// Indicates if the source folder (local for 'out', remote for 'in') should be cleaned after a successful transfer.
        /// Corresponds to an XML attribute named "CleanAfterTransfer".
        /// </summary>
        [XmlAttribute(AttributeName = "CleanAfterTransfer")]
        public bool CleanAfterTransfer { get; set; }
    }

    /// <summary>
    /// Defines a schedule for one or more copy jobs.
    /// Corresponds to an XML element named "Schedule".
    /// </summary>
    [XmlRoot(ElementName = "Schedule")]
    public class Schedule
    {
        /// <summary>
        /// A list of CopyJob configurations within this schedule.
        /// Corresponds to XML elements named "CopyJob".
        /// </summary>
        [XmlElement(ElementName = "CopyJob")]
        public List<CopyJob> CopyJob { get; set; }

        /// <summary>
        /// The start time of the job's active window (e.g., "08:00").
        /// Corresponds to an XML attribute named "StartTime".
        /// </summary>
        [XmlAttribute(AttributeName = "StartTime")]
        public string StartTime { get; set; }

        /// <summary>
        /// The end time of the job's active window (e.g., "17:00").
        /// Corresponds to an XML attribute named "EndTime".
        /// </summary>
        [XmlAttribute(AttributeName = "EndTime")]
        public string EndTime { get; set; }

        /// <summary>
        /// The interval (in minutes) at which the job should run within its active window.
        /// Corresponds to an XML attribute named "Interval".
        /// </summary>
        [XmlAttribute(AttributeName = "Interval")]
        public int Interval { get; set; }
    }

    /// <summary>
    /// Represents a single server configuration, including its schedules and copy jobs.
    /// Corresponds to an XML element named "Server".
    /// </summary>
    [XmlRoot(ElementName = "Server")]
    public class Server
    {
        /// <summary>
        /// A list of Schedule configurations for this server.
        /// Corresponds to XML elements named "Schedule".
        /// </summary>
        [XmlElement(ElementName = "Schedule")]
        public List<Schedule> Schedule { get; set; }

        /// <summary>
        /// A descriptive name for the server.
        /// Corresponds to an XML attribute named "Description".
        /// </summary>
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }

        /// <summary>
        /// The hostname or IP address of the server.
        /// Corresponds to an XML attribute named "Host".
        /// </summary>
        [XmlAttribute(AttributeName = "Host")]
        public string Host { get; set; }

        /// <summary>
        /// The SSH/SFTP port number for the server (default is 22).
        /// Corresponds to an XML attribute named "Port".
        /// </summary>
        [XmlAttribute(AttributeName = "Port")]
        public int Port { get; set; }
    }

    /// <summary>
    /// The root element for a collection of server configurations.
    /// Corresponds to an XML element named "Servers".
    /// </summary>
    [XmlRoot(ElementName = "Servers")]
    public class Servers
    {
        /// <summary>
        /// A list of Server configurations.
        /// Corresponds to XML elements named "Server".
        /// </summary>
        [XmlElement(ElementName = "Server")]
        public List<Server> Server { get; set; }
    }
}