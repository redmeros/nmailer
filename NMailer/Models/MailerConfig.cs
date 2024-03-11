namespace NMailer.Models;

public sealed class MailerConfig
{
    public string RepoDir { get; set; } = default!;
    public string SubjectRegex { get; set; } = default!;
    public int CheckInterval { get; set; } = 10;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Server { get; set; } = default!;
    public int ImapPort { get; set; }
    public int SmtpPort { get; set; }
    
    public bool SmtpAuthenticate { get; set; }
    public string CorrespondenceDirectoryPattern { get; set; } = default!;
    public string AttachmentDirName { get; set; } = default!;

    public string SmtpSenderAddress { get; set; } = default!;
}