using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MimeKit.Text;

namespace MailKit.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailsController : ControllerBase
    {
        private readonly SmtpClientPool _smtpClientPool;

        public EmailsController(SmtpClientPool smtpClientPool)
        {
            _smtpClientPool = smtpClientPool;
        }

        /// <summary>
        ///     Отправляет тестовое письмо
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Send(CancellationToken cancellationToken)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("DK", "d.o.kozhevnikov@gmail.com"));
            message.To.Add(new MailboxAddress("DK", "dimitron2003@mail.ru"));
            message.Subject = "TEST";

            message.Body = new TextPart(TextFormat.Plain)
            {
                Text = @"TEST"
            };

            await _smtpClientPool.SendAsync(message, cancellationToken);

            return Ok();
        }
    }
}
