using Microsoft.AspNetCore.Mvc;
using EmailService.Contracts;
using EmailService.DTO.Request;
using System.Threading.Tasks;
using EmailService.DTO.Response;
using Microsoft.AspNetCore.Authorization;

namespace EmailService.API.v1
{
    [Route("api/v1/[controller]")]
    [Authorize]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IMailService _mailService;
        public EmailController(IMailService mailService)
        {
            _mailService = mailService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] SendHTMLRequest mailRequest)
        {
            if (!ModelState.IsValid) { throw new ApiException("Invalid request"); }

            return Ok(await _mailService.SendHTMLMail(mailRequest));
        }

        [HttpPost]
        [Route("without_attachment")]
        public async Task<IActionResult> WithoutAttachment([FromBody] SendWithoutAttachmentRequest mailRequest)
        {
            if (!ModelState.IsValid) { throw new ApiException("Invalid request"); }

            return Ok(await _mailService.SendWithoutAttachment(mailRequest));
        }
    }
}