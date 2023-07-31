using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using NMailer;
using NMailer.Models;
using NMailer.Services;
using Serilog;

// var builder = Host.CreateDefaultBuilder(args);
    
var builder = Host.CreateApplicationBuilder(args);
var env = builder.Environment;

builder.Configuration.Sources.Clear();


builder.Configuration
    .AddJsonFile("appsettings.json", true, true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddLogging(blder =>
{
        blder.ClearProviders();
        blder.AddSerilog(dispose: true);
    });

//mailkit
builder.Services.AddTransient<ISmtpClient, SmtpClient>();
builder.Services.AddTransient<IImapClient, ImapClient>();

builder.Services.AddSingleton<IProjectDirFinder, ProjectDirFinder>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ISubjectParser, SubjectParser>();
builder.Services.AddTransient<IMailReader, ImapMailReader>();
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<MailerConfig>(builder.Configuration.GetSection(nameof(MailerConfig)));


var host = builder.Build();
await host.RunAsync();