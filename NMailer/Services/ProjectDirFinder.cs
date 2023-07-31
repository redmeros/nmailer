using Microsoft.Extensions.Options;
using NMailer.Models;

namespace NMailer.Services;

public interface IProjectDirFinder
{
    public string GetCorrespondenceDirectory(string projectNo, bool ensureExists = true);
}

public class ProjectDirFinder : IProjectDirFinder
{
    private readonly MailerConfig _config;
    
    public ProjectDirFinder(
        IOptions<MailerConfig> options)
    {
        _config = options.Value;
    }

    
    
    public string GetCorrespondenceDirectory(string projectNo, bool ensureExists = true)
    {
        string dirname;
        try
        {
            
            var allDirsPaths = Directory.GetDirectories(_config.RepoDir);
            var matchedDir = allDirsPaths.First(w => new DirectoryInfo(w).Name.StartsWith(projectNo));
            if (string.IsNullOrEmpty(matchedDir) || !Directory.Exists(matchedDir))
            {
                throw new NMailerException($"Cannot find directory for projectNo '{projectNo}' in '{_config.RepoDir}");
            }

            dirname = Path.Join(matchedDir, GetCorrespondenceDirName(projectNo));
            if (ensureExists)
            {
                Directory.CreateDirectory(dirname);
            }
        }
        catch (Exception ex)
        {
            throw new NMailerException($"Error during searching project directory for {projectNo} in {_config.RepoDir}", ex);
        }

        return dirname;
    }
    private string GetCorrespondenceDirName(string projectNo)
    {
        return _config.CorrespondenceDirectoryPattern.Replace("{ProjectNo}", projectNo);
    }
}