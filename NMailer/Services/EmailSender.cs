using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using NMailer.Models;

namespace NMailer.Services;

public interface IEmailSender
{
    Task ReplyWithError(MimeMessage msg, Exception error, CancellationToken ct);
}

public class EmailSender : IEmailSender
{
    private readonly ISmtpClient _smtpClient;
    private readonly ILogger<EmailSender> _logger;
    private readonly MailerConfig _config;

    public EmailSender(
        ISmtpClient smtpClient,
        ILogger<EmailSender> logger,
        IOptions<MailerConfig> options)
    {
        _smtpClient = smtpClient;
        _logger = logger;
        _config = options.Value;
    }
    
    public async Task ReplyWithError(MimeMessage msg, Exception error, CancellationToken ct)
    {
        var reply = new MimeMessage();
        reply.From.Add(new MailboxAddress("NMailer - noReply", _config.SmtpSenderAddress));
        
        if (msg.ReplyTo.Count > 0)
        {
            reply.To.AddRange(msg.ReplyTo);
        } else if (msg.From.Count > 0)
        {
            reply.To.AddRange(msg.From);
        }
        else if (msg.Sender != null)
        {
            reply.To.Add(msg.Sender);
        }

        reply.Subject = "Error: " + msg.Subject;

        if (!string.IsNullOrEmpty(msg.MessageId))
        {
            reply.InReplyTo = msg.MessageId;
            foreach (var id in msg.References)
            {
                reply.References.Add(id);
            }
            reply.References.Add(msg.MessageId);
        }

        var body = new Multipart();
        body.Add(new TextPart(TextFormat.Text)
        {
            Text = @"I'm sorry to inform You that your massage cannot be proceed with NMailer please see message below: " + "\n\n" + error.UnwrapMessages()
        });
        
        var att = new MimePart("application/unknown");
        var memoryStream = new MemoryStream();
        await msg.WriteToAsync(memoryStream, ct);
        att.Content = new MimeContent(memoryStream);
        att.FileName = "messageContent.eml";
        body.Add(att);

        reply.Body = body;
        
        _smtpClient.ServerCertificateValidationCallback = (_, _, _, _) => true;
        await _smtpClient.ConnectAsync(_config.Server, _config.SmtpPort, false, ct);
        // to jest gdyby jarek włączył autentykację
        if (_config.SmtpAuthenticate)
        {
            await _smtpClient.AuthenticateAsync(_config.Username, _config.Password, ct);
        }
        await _smtpClient.SendAsync(reply, ct);
        await _smtpClient.DisconnectAsync(true, ct);
        _logger.LogInformation("Error info was sent to: {ErrorSendAddress}", reply.To);
    } 
}