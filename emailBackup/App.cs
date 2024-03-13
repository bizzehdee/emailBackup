using emailBackup.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace emailBackup
{
    internal class App
    {
        private readonly IConfigurationRoot configuration;
        private readonly ConfigModel config = new ConfigModel();

        public App(IConfigurationRoot configuration) 
        {
            this.configuration = configuration;
        }

        public async Task Run()
        {
            configuration.Bind(config);

            if(config != null && config.Accounts != null)
            {
                foreach (var account in config.Accounts)
                {
                    await ProcessAccount(account);
                }
            }
        }

        private async Task ProcessAccount(ConfigAccountModel account)
        {
            // Set the directory where the backup will be saved
            var backupDir = account.BackupDirectory;

            var client = new ImapClient();

            try
            {
                await client.ConnectAsync(account.Server, account.Port, account.UseSSL);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to connect to {account.Server}:{account.Port}");
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                return;
            }

            // Authenticate with the IMAP server using the specified username and password
            try
            {
                client.Authenticate(account.Username, account.Password);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to authenticate to {account.Server}:{account.Port}");
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                return;
            }

            var folders = await client.GetFoldersAsync(new FolderNamespace('/', ""));
            var syncFolders = folders.Where(f => !account.IgnoreFolders.Contains(f.FullName)).ToList();

            foreach (var folder in syncFolders)
            {
                Console.WriteLine($"Opening Folder {folder.FullName}");

                try
                {
                    await folder.OpenAsync(FolderAccess.ReadOnly);
                }
                catch
                {
                    Console.WriteLine($"Failed to open Folder {folder.FullName}");
                    continue;
                }

                // Get a list of all messages in the Inbox folder
                var uids = await folder.SearchAsync(SearchQuery.All);
                if(uids == null || uids.Count == 0)
                {
                    Console.WriteLine($"No UIDs in {folder.FullName}. Skipping");
                    continue;
                }

                Console.WriteLine($"Downloading {uids.Count} messages");

                // Loop through the messages and download each one
                foreach (var uid in uids.Reverse())
                {
                    // Save the message to a local file
                    var filename = Path.Combine(config.BackupRoot, backupDir, folder.Name, uid + ".eml");

                    if (File.Exists(filename))
                    {
                        continue;
                    }

                    // Get the message as a MIME entity
                    MimeMessage message;

                    try
                    {
                        message = await folder.GetMessageAsync(uid);
                    }
                    catch
                    {
                        continue;
                    }

                    var path = Path.Combine(config.BackupRoot, backupDir, folder.Name);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    using (FileStream stream = File.Create(filename))
                    {
                        message.WriteTo(stream);
                    }

                    var fileInfo = new FileInfo(filename);

                    try
                    {
                        fileInfo.LastWriteTime = message.Date.DateTime;
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to set LastWriteTime for #{uid} \"{message.Subject}\" to {message.Date.DateTime}");
                    }
                }

                await folder.CloseAsync(false);
            }

            // Disconnect from the IMAP server
            await client.DisconnectAsync(true);

            Console.WriteLine("Backup complete!");
        }
    }
}
