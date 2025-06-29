using Serilog.Core; // Used for Serilog Logger.

namespace SshFileTransfer
{
    // Static class to hold global data, specifically Serilog loggers.
    static class Data
    {
        // Logger for critical application errors, possibly sending emails.
        public static Logger MaiLogger; // Typo: Likely meant "MailLogger"
        // General purpose logger for standard application events and debugging.
        public static Logger Logger;
    }
}