using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace emailBackup.Models
{
    internal class ConfigModel
    {
        public string BackupRoot { get; set; }
        public ConfigAccountModel[] Accounts { get; set; }
    }
}
