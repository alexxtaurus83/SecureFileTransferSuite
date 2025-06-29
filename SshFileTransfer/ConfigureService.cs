using Topshelf; // Topshelf library for creating Windows Services.
using Serilog; // Used for logging configuration.

namespace SshFileTransfer
{
    // This class handles the configuration and setup of the SshFileTransfer application as a Windows service using Topshelf.
    public class ConfigureService
    {
        /// <summary>
        /// Configures and runs the SshFileTransfer application as a Windows service.
        /// </summary>
        public void Configure()
        {
            // HostFactory.Run is the main entry point for Topshelf service configuration.
            HostFactory.Run(configure =>
            {
                // x.RunAsNetworkService(); // Commented: Configures service to run as Network Service account.
                // x.RunAsLocalService();   // Commented: Configures service to run as Local Service account.
                // configure.RunAsLocalSystem(); // Commented: Configures service to run as Local System account.
                configure.RunAsPrompt(); // Prompts the user for a username and password to run the service as.

                configure.UseSerilog(Data.Logger); // Integrates Serilog for logging service events.

                // Defines the Windows service behavior.
                configure.Service<Service>(service =>
                {
                    // Specifies how to construct an instance of the 'Service' class.
                    service.ConstructUsing(csdm => new Service());
                    // Defines actions to perform when the service is started.
                    service.WhenStarted(csdm => csdm.Start());
                    // Defines actions to perform when the service is stopped.
                    service.WhenStopped(csdm => csdm.Stop());
                    // Defines actions to perform when the system is shutting down.
                    service.WhenShutdown(csdm => csdm.Stop());
                });

                // Sets the name of the Windows service.
                configure.SetServiceName("SshFileTransfer");
                // Sets the display name of the service shown in the Windows Services manager.
                configure.SetDisplayName("SshFileTransfer service");
                // Sets the description of the service shown in the Windows Services manager.
                configure.SetDescription("SshFileTransfer service");
            });
        }
    }
}