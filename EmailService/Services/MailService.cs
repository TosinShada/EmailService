using EmailService.Contracts;
using EmailService.DTO.Request;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using EmailService.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Threading.Tasks;
using EmailService.DTO.Response;
using System.Linq;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Org.BouncyCastle.Asn1.Ocsp;
using Microsoft.AspNetCore.Http;

namespace EmailService.Services
{
    public class MailService : IMailService
    {
        private readonly ILogger<MailService> _logger;
        private readonly MailServerConfig _mailServerConfig;
        private readonly AuthKeys _authKeys;

        public MailService(
            IOptions<MailServerConfig> mailServerConfig,
            IOptions<AuthKeys> authKeys,
            ILogger<MailService> logger)
        {
            _mailServerConfig = mailServerConfig.Value;
            _authKeys = authKeys.Value;
            _logger = logger;
        }

        public async Task<Response<string>> SendHTMLMail(SendHTMLRequest request)
        {
            try
            {
                var message = await ConvertMimeMessage(request.DisplayName, request.From, request.To, request.Cc, request.Bcc, request.Subject, request.MailMessage, request.Attachments);

                await SendMailAsync(message);

                return new Response<string>("Successfully sent email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException, ex.Message);
                _logger.LogError(ex.StackTrace);
                throw new ApiException($"Internal server error.");
            }
        }

        public async Task<Response<string>> SendWithoutAttachment(SendWithoutAttachmentRequest request)
        {
            try
            {
                var message = await ConvertMimeMessage(request.DisplayName, request.From, request.To, request.Cc, request.Bcc, request.Subject, request.MailMessage);

                await SendMailAsync(message);

                return new Response<string>("Successfully sent email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException, ex.Message);
                _logger.LogError(ex.StackTrace);
                throw new ApiException($"Internal server error.");
            }
        }

        private async Task SendMailAsync(MimeMessage message)
        {
            string serverAddress = _mailServerConfig.ServerAddress;
            string username = _mailServerConfig.Username;
            string password = _mailServerConfig.Password;
            int port = _mailServerConfig.Port;
            bool isSsl = _mailServerConfig.IsUseSsl;
            bool isUseTls = _mailServerConfig.IsUseStartTls;

            using var client = new SmtpClient();

            if (isSsl)
            {
                // Set our custom SSL certificate validation callback.
                client.ServerCertificateValidationCallback = SslCertificateValidationCallback;

                await client.ConnectAsync(serverAddress, port, SecureSocketOptions.SslOnConnect);
            }
            else if (isUseTls)
            {
                client.Connect(serverAddress, port, SecureSocketOptions.StartTls);
            }

            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public bool BasicAuthenticate(string username, string password)
        {
            try
            {
                List<string> allowedCredentials = _authKeys.Basic.ToList();

                return allowedCredentials.Contains($"{username}:{password}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException, ex.Message);
                _logger.LogError(ex.StackTrace);
                throw new ApiException($"Internal server error.");
            }
        }

#nullable enable
        private async Task<MimeMessage> ConvertMimeMessage(string displayName, string from, string to, string cc, string bcc, string subject, string mailMessage, List<IFormFile>? files = null)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(displayName ?? _mailServerConfig.DisplayName, from ?? _mailServerConfig.From));
            message.To.Add(MailboxAddress.Parse(to.Trim()));

            if (!string.IsNullOrWhiteSpace(cc))
            {
                message.Cc.Add(MailboxAddress.Parse(cc.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(bcc))
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc.Trim()));
            }
            message.Subject = subject;

            var body = new BodyBuilder();
            body.HtmlBody = mailMessage;

            if (files != null)
            {
                foreach (var attachment in files)
                {
                    using (Stream stream = attachment.OpenReadStream())
                    {
                        var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        var att = new MimePart("application", "octet-stream")
                        {
                            Content = new MimeContent(memoryStream, ContentEncoding.Default),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                            ContentTransferEncoding = ContentEncoding.Base64,
                            FileName = attachment.FileName
                        };
                        body.Attachments.Add(att);
                    }
                }
            }

            message.Body = body.ToMessageBody();

            return message;
        }
#nullable disable

        private bool SslCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // If there are no errors, then everything went smoothly.
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            // Note: MailKit will always pass the host name string as the `sender` argument.
            var host = (string)sender;

            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
            {
                // This means that the remote certificate is unavailable. Notify the user and return false.
                Console.WriteLine("The SSL certificate was not available for {0}", host);
                return false;
            }

            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                // This means that the server's SSL certificate did not match the host name that we are trying to connect to.
                var certificate2 = certificate as X509Certificate2;
                var cn = certificate2 != null ? certificate2.GetNameInfo(X509NameType.SimpleName, false) : certificate.Subject;

                Console.WriteLine("The Common Name for the SSL certificate did not match {0}. Instead, it was {1}.", host, cn);
                return false;
            }

            // The only other errors left are chain errors.
            Console.WriteLine("The SSL certificate for the server could not be validated for the following reasons:");

            // The first element's certificate will be the server's SSL certificate (and will match the `certificate` argument)
            // while the last element in the chain will typically either be the Root Certificate Authority's certificate -or- it
            // will be a non-authoritative self-signed certificate that the server admin created. 
            foreach (var element in chain.ChainElements)
            {
                // Each element in the chain will have its own status list. If the status list is empty, it means that the
                // certificate itself did not contain any errors.
                if (element.ChainElementStatus.Length == 0)
                    continue;

                Console.WriteLine("\u2022 {0}", element.Certificate.Subject);
                foreach (var error in element.ChainElementStatus)
                {
                    // `error.StatusInformation` contains a human-readable error string while `error.Status` is the corresponding enum value.
                    Console.WriteLine("\t\u2022 {0}", error.StatusInformation);
                }
            }

            return false;
        }
    }
}
