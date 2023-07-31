using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NMailer.Models;

namespace NMailer.Services;

public class SubjectParserResult
{
    public string ProjectNo { get; set; } = default!;
    public string Subject { get; set; } = default!;
}

public interface ISubjectParser
{
    SubjectParserResult ParseSubject(string subject);
}

public class SubjectParser : ISubjectParser
{
    private readonly MailerConfig _config;
    private readonly Regex _subjectRegex;
    
    public SubjectParser(
        IOptions<MailerConfig> configOptions)
    {
        _config = configOptions.Value;
        _subjectRegex = new Regex(_config.SubjectRegex, RegexOptions.Compiled);
    }

    public SubjectParserResult ParseSubject(string subject)
    {
        var match = _subjectRegex.Match(subject);
        if (!match.Success)
        {
            throw new NMailerException($"Subject '{subject}', does not match subject regex {_config.SubjectRegex}");
        }

        return new SubjectParserResult()
        {
            ProjectNo = match.Groups["ProjectNo"].Value,
            Subject = match.Groups["Subject"].Value
        };
    }
}