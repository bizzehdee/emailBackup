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

        private async Task ProcessAccount(ConfigAccountModeel account)
        {
            // Set the directory where the backup will be saved
            string backupDir = account.BackupDirectory;

            ImapClient client = new();

            await client.ConnectAsync(account.Server, account.Port, account.UseSSL);

            // Authenticate with the IMAP server using the specified username and password
            client.Authenticate(account.Username, account.Password);

            var folders = await client.GetFoldersAsync(new FolderNamespace('/', ""));

            foreach (var folder in folders)
            {
                Console.WriteLine($"Opening Folder {folder.FullName}");

                await folder.OpenAsync(FolderAccess.ReadOnly);
                // Get a list of all messages in the Inbox folder
                IEnumerable<UniqueId> uids = await folder.SearchAsync(SearchQuery.All);
                Console.WriteLine($"Downloading {uids.Count()} messages");

                // Loop through the messages and download each one
                foreach (UniqueId uid in uids.Reverse())
                {
                    // Save the message to a local file
                    string filename = Path.Combine(backupDir, folder.Name, uid + ".eml");

                    if(File.Exists(filename))
                    {
                        continue;
                    }

                    // Get the message as a MIME entity
                    var message = await folder.GetMessageAsync(uid);

                    var path = Path.Combine(backupDir, folder.Name);
                    if(Directory.Exists(path) == false)
                    {
                        Directory.CreateDirectory(path);
                    }

                    using (FileStream stream = File.Create(filename))
                    {
                        message.WriteTo(stream);
                    }

                    var fileInfo = new FileInfo(filename);
                    fileInfo.LastWriteTime = message.Date.DateTime;
                }

                await folder.CloseAsync(false);
            }

            // Disconnect from the IMAP server
            await client.DisconnectAsync(true);

            Console.WriteLine("Backup complete!");
        }
    }
}
