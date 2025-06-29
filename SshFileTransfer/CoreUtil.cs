using System;
using System.Configuration; // Used for accessing AppSettings from configuration files.
using System.Diagnostics; // Used for StackTrace to get exception details.
using System.IO; // Used for file and stream operations.
using System.IO.Compression; // Used for GZip compression/decompression.
using System.Net; // Used for Dns.GetHostName().
using System.Reflection; // Used for MethodBase.GetCurrentMethod().Name.
using System.Text; // Used for Encoding.
using System.Xml; // Used for XML serialization/deserialization.
using System.Xml.Serialization; // Used for XML serialization/deserialization.
using HeyRed.Mime; // Used for MIME type detection.

namespace SshFileTransfer
{
    // This class provides common utility functions for the SshFileTransfer application.
    class CoreUtil
    {
        /// <summary>
        /// Decompresses a byte array using GZip.
        /// </summary>
        /// <param name="data">The compressed byte array.</param>
        /// <returns>The decompressed byte array.</returns>
        public byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream); // Copy decompressed data to the result stream.
                var outdata = resultStream.ToArray(); // Convert stream to byte array.
                // Log compression ratio.
                Data.Logger.Information($"incoming byte size: {data.Length}, decompressed: {outdata.Length}. Ratio: " + (outdata.Length * 100) / data.Length);
                return outdata;
            }
        }

        /// <summary>
        /// Compresses a byte array using GZip.
        /// </summary>
        /// <param name="data">The uncompressed byte array.</param>
        /// <returns>The compressed byte array.</returns>
        public byte[] Compress(byte[] data)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionLevel.Fastest)) // Use fastest compression level.
                {
                    gzip.Write(data, 0, data.Length); // Write data to the gzip stream.
                } // GZipStream is flushed and closed here.
                var outdata = memory.ToArray(); // Get the compressed data as a byte array.
                // Log compression ratio.
                Data.Logger.Information($"incoming byte size: {data.Length}, compressed: {outdata.Length}. Ratio: " + (outdata.Length * 100) / data.Length);
                return outdata;
            }
        }

        /// <summary>
        /// Raises and logs an exception, potentially sending it to email if configured.
        /// </summary>
        /// <param name="mtext">A custom message related to the context of the exception.</param>
        /// <param name="stringExeption">An additional string description of the exception.</param>
        /// <param name="ex">The Exception object.</param>
        public void RaiseExeption(string mtext, string stringExeption, Exception ex)
        {
            // Construct a basic log message with context information.
            var body = MethodBase.GetCurrentMethod().Name + "|" + Dns.GetHostName() + "|" + Environment.UserName + "|" + mtext + "|" + stringExeption + "|";
            if (ex != null)
            {
                var trace = new StackTrace(ex, true); // Get stack trace for line numbers.
                var line = trace.GetFrame(0).GetFileLineNumber(); // Get the line number of the exception.
                var linenum = 0; // Initialize line number.
                try
                {
                    // Attempt to parse line number from stack trace string (less reliable).
                    linenum = Convert.ToInt32(ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(' ')));
                }
                catch
                {
                    Data.Logger.Error("Unable to get info from exception"); // Log if parsing fails.
                }
                // Append detailed exception information to the log body.
                body = body + ex.Message + " " + ex + " " + ex.StackTrace + " " + Environment.StackTrace + " " + line + " " + linenum;
            }
            Data.Logger.Error(body); // Log the error to the general logger.
            // Check if errors should also be sent via email (configured in AppSettings).
            if (bool.Parse(ConfigurationManager.AppSettings.Get("SendErrorsToEmail"))) Data.MaiLogger.Error(body); // Use MaiLogger (MailLogger).
        }

        /// <summary>
        /// Deserializes an XML string into an object of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize to.</typeparam>
        /// <param name="filestr">The XML string to deserialize.</param>
        /// <param name="fileEncoding">The encoding of the XML string (e.g., "UTF-8").</param>
        /// <returns>The deserialized object of type T.</returns>
        public T DeserializeXml<T>(string filestr, string fileEncoding)
        {
            using (MemoryStream memoryStream = new MemoryStream(Encoding.GetEncoding(fileEncoding).GetBytes(filestr)))
            {
                using (StreamReader xmlStreamReader = new StreamReader(memoryStream, Encoding.GetEncoding(fileEncoding)))
                {
                    using (XmlTextReader reader = new XmlTextReader(xmlStreamReader))
                    {
                        reader.Namespaces = false; // Disable namespace handling for simpler XML.
                        var xmlSerializerDeserialize = new XmlSerializer(typeof(T));
                        return (T)xmlSerializerDeserialize.Deserialize(reader); // Perform deserialization.
                    }
                }
            }
        }

        /// <summary>
        /// Serializes an object into an XML string.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="data">The object to serialize.</param>
        /// <param name="encoding">The desired encoding for the XML string (e.g., "UTF-8").</param>
        /// <returns>The serialized XML string.</returns>
        public string SerializeXml<T>(T data, string encoding)
        {
            using (var stringWriter = new StringWriter())
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.GetEncoding(encoding), // Set desired encoding.
                    Indent = true // Enable indentation for readability.
                };
                using (var writer = XmlWriter.Create(stringWriter, settings))
                {
                    // Write the XML processing instruction.
                    writer.WriteProcessingInstruction("xml", $@"version=""1.0"" encoding=""{encoding}""");
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    // Serialize the object, skipping default XML namespaces.
                    xmlSerializer.Serialize(writer, data, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                }
                return stringWriter.ToString(); // Return the XML as a string.
            }
        }

        /// <summary>
        /// Detects the MIME type of a given file.
        /// </summary>
        /// <param name="file">The path to the file.</param>
        /// <returns>The detected MIME type, or "not detected" if unable to determine.</returns>
        public string DetectMime(string file)
        {
            var result = MimeTypesMap.GetMimeType(file) ?? "not detected"; // Use HeyRed.Mime to get MIME type.
            Data.Logger.Information($"MimeTypesMap: {result}"); // Log the detected MIME type.
            return result;
        }
    }
}