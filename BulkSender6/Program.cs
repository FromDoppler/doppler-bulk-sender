using Doppler.BulkSender.Configuration;
using Doppler.BulkSender.Processors;
using Doppler.BulkSender.Processors.PreProcess;
using Doppler.BulkSender.Processors.Status;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, loggerConfiguration) =>
    {
        loggerConfiguration.WriteTo.File(@"c:\temp\logs\bulksender6.log", Serilog.Events.LogEventLevel.Debug);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppConfiguration>(context.Configuration.GetSection(nameof(AppConfiguration)));

        //services.AddHostedService<FtpMonitor>();
        services.AddHostedService<LocalMonitor>();
        //services.AddHostedService<CleanProcessor>();
        //services.AddHostedService<ReportGenerator>();
        //services.AddHostedService<PreProcessWorker>();
        //services.AddHostedService<StatusWorker>();
    })

    .Build();

await host.RunAsync();
