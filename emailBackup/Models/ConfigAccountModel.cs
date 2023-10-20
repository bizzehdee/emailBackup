using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace emailBackup.Models
{
    internal class ConfigAccountModel
    {
        public string Server { get; set; }
        public short Port { get; set; }
        public bool UseSSL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string BackupDirectory { get; set; }
    }
}
