using EmailService.DTO.Request;
using EmailService.DTO.Response;
using System.Threading.Tasks;

namespace EmailService.Contracts
{
    public interface IMailService
    {
        Task<Response<string>> SendHTMLMail(SendHTMLRequest request);
        Task<Response<string>> SendWithoutAttachment(SendWithoutAttachmentRequest request);
        bool BasicAuthenticate(string username, string password);
    }
}
