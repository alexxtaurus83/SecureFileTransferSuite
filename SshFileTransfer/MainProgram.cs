using System;
using System.Configuration; // Used for ConfigurationManager to read AppSettings.
using System.IO; // Used for Directory.SetCurrentDirectory.
using Serilog; // Core Serilog logging library.
using Serilog.Debugging; // For Serilog internal logging.
using Serilog.Events; // For defining log event levels.

namespace SshFileTransfer
{
    // The main entry point for the SshFileTransfer application.
    class MainProgram
    {
        static void Main(string[] args)
        {
            // Set the current working directory to the application's base directory.
            // This is important for locating configuration files (e.g., Servers.xml, ServerCredentials.xml).
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // Enable Serilog's self-logging to the console, useful for debugging Serilog configuration issues.
            SelfLog.Enable(Console.Out);

            // Configure the "MaiLogger" (likely MailLogger) for sending critical logs via email.
            Data.MaiLogger = new LoggerConfiguration().WriteTo.Email // 
            (
                fromEmail: ConfigurationManager.AppSettings.Get("fromEmail"), // Sender email from AppSettings.
                toEmail: ConfigurationManager.AppSettings.Get("toEmail"),     // Recipient email from AppSettings.
                mailServer: ConfigurationManager.AppSettings.Get("mailServer"), // Mail server address from AppSettings.
                restrictedToMinimumLevel: LogEventLevel.Debug, // Log all messages from Debug level and above.
                mailSubject: ConfigurationManager.AppSettings.Get("EmailSubject"), // Email subject from AppSettings.
                period: TimeSpan.Parse("00:10:00"), // Send emails every 10 minutes.
                batchPostingLimit: 500 // Batch up to 500 log events before sending.
            ).Enrich.WithThreadId().CreateLogger(); // Enrich logs with thread ID.

            // Define a common output template for log messages.
            const string templt = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} <{ThreadId}>{NewLine}{Exception}";

            // Configure the general purpose "Logger" for console and file output.
            Data.Logger = new LoggerConfiguration().WriteTo.Console // 
                (
                    outputTemplate: templt, // Use the defined template.
                    restrictedToMinimumLevel: LogEventLevel.Debug // Log all messages from Debug level and above to console.
                )
                .Enrich.WithThreadId() // Enrich console logs with thread ID.
                .WriteTo.File // Also write logs to a file.
                (
                    @".\log\log.txt", // Log file path.
                    rollingInterval: RollingInterval.Day, // Create a new log file daily.
                    outputTemplate: templt, // Use the defined template for file logs.
                    retainedFileCountLimit: 365, // Keep logs for 365 days.
                    fileSizeLimitBytes: 1000000 // Rotate file if it exceeds 1MB.
                ).Enrich.WithThreadId().CreateLogger(); // Enrich file logs with thread ID.

            // Create an instance of ConfigureService and run the service configuration (Topshelf).
            var cs = new ConfigureService(); //
            cs.Configure(); // This call hands over control to Topshelf to set up and run the Windows service.
        }
    }
}