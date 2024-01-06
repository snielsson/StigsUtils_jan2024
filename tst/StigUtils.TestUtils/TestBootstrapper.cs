// Copyright © 2023 TradingLens. All Rights Reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using StigsUtils.Abstractions;
using StigsUtils.SqlServer;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace StigUtils.TestUtils;

public class TestBootstrapper
{
  private readonly IHost _host;
  public TestBootstrapper(Action<IServiceCollection>? configureServicesAction) : this(null, configureServicesAction) {}
  public TestBootstrapper(Action<string>? _outputWriter = null, Action<IServiceCollection>? configureServicesAction = null)
  {
    IHostBuilder builder = Host.CreateDefaultBuilder();
    builder.ConfigureAppConfiguration(configuration => configuration.AddJsonFile("appsettings.test.json"));
    MemoryLog = new MemoryLogSerilogSink(1000);
    builder.ConfigureServices(
      services => services
                 .AddSingleton(MemoryLog)
                 .AddSingleton<IActivityLog, ActivityLogSqlServerSink>()
                 .AddSerilog((serviceProvider, loggerConfiguration) =>
                  {
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                    var auditLogSink = serviceProvider.GetRequiredService<IActivityLog>();
                    loggerConfiguration.ReadFrom.Configuration(configuration)
                                       .WriteTo.Sink((ILogEventSink)MemoryLog)
                                       .WriteTo.Sink((ILogEventSink)auditLogSink);
                  })
    );
    if (configureServicesAction != null)
    {
      builder.ConfigureServices(configureServicesAction);
    }
    _host = builder.Build();
    if (_outputWriter != null)
    {
      MemoryLog.OnLogEvent += (_, m) =>
      {
        _outputWriter($"{m}");
      };
    }
  }
  public IMemoryLog MemoryLog { get; }
  public IServiceProvider Services => _host.Services;
  public ILogger GetLogger(string category) => Services.GetRequiredService<ILoggerFactory>().CreateLogger(category);
  public ILogger GetLogger<T>() => Services.GetRequiredService<ILogger<T>>();
  public T Get<T>() where T : notnull => Services.GetRequiredService<T>();
}