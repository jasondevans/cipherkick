using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager
{
    public class Vault
    {
        public String location { get; set; }
        public String friendlyName { get; set; }
        public bool isStartupVault { get; set; }
        public bool isTempVault { get; set; }
    }
}
