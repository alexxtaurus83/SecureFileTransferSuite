# SshFileTransfer Application

## Basic Functionality

The `SshFileTransfer` application is a Windows service designed for **automated file and directory transfers** to and from remote servers using **SSH (Secure Shell) protocols, specifically SFTP (SSH File Transfer Protocol) and SCP (Secure Copy Protocol)**. It reads configurations from XML files to determine which files/directories to transfer, to which servers, and on what schedule. The application supports both **password-based and private key-based authentication** for SSH connections. It includes robust **logging capabilities** (console, file, and email for critical errors) and **exception handling** to monitor operations and alert on issues.

## Core Capabilities:

* **Secure File Transfer:** Utilizes SFTP for file uploads and downloads, and SCP for directory uploads and downloads, ensuring data is transferred securely over SSH.
* **Scheduled Operations:** Integrates with `FluentScheduler` to run transfer and cleanup jobs at specified intervals or specific times, based on configurable schedules.
* **Dynamic Configuration:** Reads server, schedule, and job details from `Servers.xml` and `ServerCredentials.xml` files, allowing for easy updates without code changes.
* **Automated Cleanup:** Features post-transfer cleaning of source folders (local or remote) and scheduled daily cleanup of designated directories.
* **Remote Command Execution:** Can execute commands on remote SSH servers.

## Possible Usage:

This application is ideal for scenarios requiring **reliable and automated data movement with security** across different servers, such as:

* **Automated Data Ingestion/Export:** Regularly moving data files (e.g., reports, logs, data feeds) between on-premises systems and remote servers, or between different server environments (development, staging, production).
* **Log File Collection:** Periodically downloading log files from remote application servers to a central logging server for analysis.
* **Backup and Archiving:** Uploading critical application data or backups to secure remote storage locations.
* **Deployment Automation:** Copying deployment artifacts or configuration files to remote application servers as part of a release pipeline.
* **Cross-Platform File Sync:** Synchronizing files or directories between Windows machines (where the service runs) and Linux/Unix servers (remote SSH hosts).
* **Firewall Connectivity Testing:** Although its primary role is transfer, the underlying SSH connection establishment can implicitly verify network reachability and open ports