using System.Text;
using Microsoft.Extensions.Options;
using NMailer.Models;
using NMailer.Services;

namespace NMailer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly MailerConfig _config;

    public Worker(
        ILogger<Worker> logger,
        IOptions<MailerConfig> configOptions,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = configOptions.Value;

    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting executing NMailer");
        DumpSettingsToLog();
        while (!ct.IsCancellationRequested)
        {
                var mailReader = _serviceProvider.GetService<IMailReader>();
                if (mailReader is null)
                {
                    throw new NMailerException("Cannot get mail reader");
                }

                _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.UtcNow);

                await mailReader.ReadEmails(ct);

                _logger.LogInformation("Waiting for {CheckInterval} seconds - according to settings",
                    _config.CheckInterval);
                await Task.Delay(_config.CheckInterval * 1000, ct);

        }
    }

    private void DumpSettingsToLog(LogLevel lvl = LogLevel.Information)
    {
        _logger.Log(lvl, "Current working dir is {WorkingDir}", Environment.CurrentDirectory);
        var sb = new StringBuilder();
        var props = typeof(MailerConfig).GetProperties();
        foreach (var prop in props)
        {
            sb.AppendLine($"{prop.Name} : {prop.GetValue(_config)}");
        }
        _logger.Log(lvl, "{Props}", sb.ToString());
    }
}