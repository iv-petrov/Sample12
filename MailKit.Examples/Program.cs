using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace MailKit.Examples;

internal class Program
{
    static async Task Main(string[] args)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("IP", "IVPetrov@innoca.local"));
        message.To.Add(new MailboxAddress("IP", "igvapetrov@inno.tech"));
        message.Subject = "TEST";

        message.Body = new TextPart(TextFormat.Plain)
        {
            Text = @"TEST"
        };

        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var token = cancellationTokenSource.Token;
    
        using var client = new SmtpClient();
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await client.ConnectAsync("10.4.107.10", 587, SecureSocketOptions.StartTls, token);
        await client.AuthenticateAsync(@"innoca\IVPetrov", "HM0rVuto64", token);

        lock (client.SyncRoot)
        {
            client.Send(message);
        }

        await client.DisconnectAsync(true, token);

        Console.WriteLine("Email has been sent. Press any key to exit...");
        Console.ReadKey();
    }
}