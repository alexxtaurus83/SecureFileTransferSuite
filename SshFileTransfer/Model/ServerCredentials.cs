using System.Collections.Generic;
using System.Xml.Serialization; // Used for XML serialization attributes.

namespace SshFileTransfer.Model
{
    /// <summary>
    /// Represents a collection of server credentials, typically deserialized from an XML file.
    /// The root XML element name is "ServerCredentials".
    /// </summary>
    [XmlRoot(ElementName = "ServerCredentials")]
    public class ServerCredentials
    {
        /// <summary>
        /// A list of individual Credentials objects.
        /// Corresponds to XML elements named "Credentials".
        /// </summary>
        [XmlElement(ElementName = "Credentials")]
        public List<Credentials> Credentials { get; set; }

        /// <summary>
        /// Default constructor for XML serialization.
        /// </summary>
        public ServerCredentials()
        {
        }

        /// <summary>
        /// Constructor to initialize with a list of credentials.
        /// </summary>
        /// <param name="credentials">A list of Credentials objects.</param>
        public ServerCredentials(List<Credentials> credentials)
        {
            Credentials = credentials;
        }
    }

    /// <summary>
    /// Represents a single set of server login credentials (Host, Login, Password).
    /// The root XML element name is "Credentials".
    /// </summary>
    [XmlRoot(ElementName = "Credentials")]
    public class Credentials
    {
        /// <summary>
        /// The hostname or IP address of the server.
        /// Corresponds to an XML attribute named "Host".
        /// </summary>
        [XmlAttribute(AttributeName = "Host")]
        public string Host { get; set; }

        /// <summary>
        /// The username for logging into the server.
        /// Corresponds to an XML attribute named "Login".
        /// </summary>
        [XmlAttribute(AttributeName = "Login")]
        public string Login { get; set; }

        /// <summary>
        /// The password for the specified login.
        /// Corresponds to an XML attribute named "Password".
        /// This password is expected to be encrypted when stored.
        /// </summary>
        [XmlAttribute(AttributeName = "Password")]
        public string Password { get; set; }

        /// <summary>
        /// Default constructor for XML serialization.
        /// </summary>
        public Credentials()
        {
        }

        /// <summary>
        /// Constructor to initialize credentials with specific host, login, and password.
        /// </summary>
        /// <param name="host">The server host.</param>
        /// <param name="login">The login username.</param>
        /// <param name="password">The encrypted password.</param>
        public Credentials(string host, string login, string password)
        {
            Host = host;
            Login = login;
            Password = password;
        }
    }
}