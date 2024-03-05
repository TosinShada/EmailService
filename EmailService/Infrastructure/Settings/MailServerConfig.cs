using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailService.Infrastructure.Settings
{
    public class MailServerConfig
    {
        public string ServerAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public bool IsUseSsl { get; set; }
        public bool IsUseStartTls { get; set; }
        public string DisplayName { get; set; }
        public string From { get; set; }
    }
}
