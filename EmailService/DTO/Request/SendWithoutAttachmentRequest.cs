using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EmailService.DTO.Request
{
    public class SendWithoutAttachmentRequest
    {
        public string From { get; set; }
        public string DisplayName { get; set; }
        [Required]
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        [Required]
        public string MailMessage { get; set; }
        [Required]
        public string Subject { get; set; }
    }
}
