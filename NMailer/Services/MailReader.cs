
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.Extensions.Options;
using MimeKit;
using NMailer.Models;

namespace NMailer.Services;

public interface IMailReader
{
    Task ReadEmails(CancellationToken ct);
}

public class ImapMailReader : IMailReader
{
    private readonly ILogger<ImapMailReader> _logger;
    private readonly ISubjectParser _subjectParser;
    private readonly IImapClient _imapClient;
    private readonly IProjectDirFinder _projectDirFinder;
    private readonly IEmailSender _emailSender;
    private readonly IHostEnvironment _env;
    private readonly MailerConfig _config;
    private readonly Guid _guid = Guid.NewGuid();

    public ImapMailReader(
        IOptions<MailerConfig> configOptions,
        ILogger<ImapMailReader> logger,
        ISubjectParser subjectParser,
        IImapClient imapClient,
        IProjectDirFinder projectDirFinder,
        IEmailSender emailSender,
        IHostEnvironment env
        )
    {
        _logger = logger;
        _subjectParser = subjectParser;
        _imapClient = imapClient;
        _projectDirFinder = projectDirFinder;
        _emailSender = emailSender;
        _env = env;
        _config = configOptions.Value;
    }

    public async Task ReadEmails(CancellationToken ct)
    {
        _logger.LogInformation( "Guid of mail reader: {Guid}", _guid.ToString());
        try
        {
            _imapClient.ServerCertificateValidationCallback = ((_, _, _, _) => true);
            await _imapClient.ConnectAsync(_config.Server, _config.ImapPort, false, ct);
            await _imapClient.AuthenticateAsync(_config.Username, _config.Password, ct);
            var inbox = _imapClient.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, ct);
            _logger.LogInformation("Total messages in Inbox: {Count}", inbox.Count);

            for (var i = 0; i < inbox.Count; i++)
            {
                var msg = await inbox.GetMessageAsync(i, ct);
                try
                {
                    var subResult = _subjectParser.ParseSubject(msg.Subject);
                    var corDirectory = _projectDirFinder.GetCorrespondenceDirectory(subResult.ProjectNo);
                    _logger.LogInformation("Found directory {CorDirectory} for projectNo {ProjectNo}", corDirectory,
                        subResult.ProjectNo);
                    var date = msg.Date.Date.ToString("yyyy-MM-dd");

                    var messageDir = CreateMessageDir(date, subResult.Subject, corDirectory);
                    _logger.LogInformation("Message dir is {MessageDir}", messageDir);
                    var emlFilePath = await WriteMessageToFile(msg, subResult.Subject, messageDir, ct);
                    _logger.LogInformation("Message written to {EmlFile}", emlFilePath);

                    var savedAttachments = await SaveAttachments(msg, messageDir, ct);
                    foreach (var savedAtt in savedAttachments)
                    {
                        _logger.LogInformation("Saved attachment to: {SavedAtt}", savedAtt);
                    }

                    await inbox.StoreAsync(i,
                        new StoreFlagsRequest(StoreAction.Add, MessageFlags.Deleted) { Silent = true }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError( ex, "Error during processing message with  {Subject}. Marking for deletion and sending error message", msg.Subject);
                    _logger.LogError("Unwrapped message: \n{Msg}", ex.UnwrapMessages());
                    await _emailSender.ReplyWithError(msg, ex, ct);
                    await inbox.StoreAsync(i,
                        new StoreFlagsRequest(StoreAction.Add, MessageFlags.Deleted) { Silent = true }, ct);
                }
            }

            if (!_env.IsDevelopment())
            {
                await inbox.ExpungeAsync(ct);
            }
        }
        finally
        {
            await _imapClient.DisconnectAsync(true, ct);
        }
    }

    private string CreateMessageDir(string date, string subject, string corDir)
    {
        var dirName = $"{date} - {SanitizeFileName(subject)}";
        var dirPath = Path.Join(corDir, dirName);
        Directory.CreateDirectory(dirPath);
        return dirPath;
    }
    
    private string GetEmailFileName(string subject)
    {
        return SanitizeFileName(subject) + ".eml";
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format( @"([{0}]*\.+$)|([{0}]+)", invalidChars );

        return Regex.Replace(fileName, invalidRegStr, "_");
    }

    private async Task<IEnumerable<string>> SaveAttachments(MimeMessage msg, string messageDir, CancellationToken ct)
    {
        var savedFiles = new List<string>();
        var iter = new MimeIterator(msg);
        var dirPath = Path.Join(messageDir, _config.AttachmentDirName);

        Directory.CreateDirectory(dirPath);
        
        while (iter.MoveNext())
        {
            var multipart = iter.Parent as Multipart;
            var att = iter.Current as MimePart;
            if (att is null)
            {
                continue;
            }
            var filepath = Path.Join(dirPath, att.FileName);
            try
            {

                if (multipart is not null && att.IsAttachment)
                {
                    if (string.IsNullOrEmpty(att.FileName))
                    {
                        continue;
                    }

                    await using var file = File.Create(filepath);
                    await att.Content.DecodeToAsync(file, ct);
                    savedFiles.Add(filepath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during saving file {FileName} to {FilePath}", att.FileName, filepath);
            }
        }
        return savedFiles;
    }
    
    private async Task<string> WriteMessageToFile(MimeMessage msg, string subject, string messageDir, CancellationToken ct)
    {
        var filename = GetEmailFileName(subject);
        var filePath = Path.Join(messageDir, filename);
        await using var file = File.Create(filePath);
        await msg.WriteToAsync(file, ct);
        await file.FlushAsync(ct);
        return filePath;
    }
}