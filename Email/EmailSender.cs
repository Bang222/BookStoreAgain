using BookStore.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

public class EmailSender : IEmailSender
{
    public EmailSender(IOptions<EmailSenderOptions> options)
    {
        this.Options = options.Value;
    }

    public EmailSenderOptions Options { get; set; }

    public Task SendEmailAsync(string email, string subject, string message)
    {
        return Execute(email, subject, message);
    }

    public Task Execute(string to, string subject, string message)
    {
        // create message
        var email = new MimeMessage();
        email.Sender = MailboxAddress.Parse(Options.Sender);
        if (!string.IsNullOrEmpty(Options.Name))
        email.Sender.Name = Options.Name;
        email.From.Add(email.Sender);
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = message };

        // send email
        using (var smtp = new SmtpClient())
        {
            smtp.Connect(Options.Host, Options.Port, Options.Host_SecureSocketOptions);
            smtp.Authenticate(Options.User, Options.Pass);
            smtp.Send(email);
            smtp.Disconnect(true);
        }

        return Task.FromResult(true);
    }
}

