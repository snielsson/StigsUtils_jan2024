// Copyright © 2023 TradingLens. All Rights Reserved.

using Microsoft.Extensions.Logging;

namespace StigsUtils.ActivityLog;

public class ActivityLogger : ILogger
{
  private readonly ILogger _logger;
  private readonly int _auditLevel;
  public ActivityLogger(ILogger logger, int auditLevel)
  {
    _logger = logger;
    _auditLevel = auditLevel;
  }
  
  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    var scopeState = new Dictionary<string, object?>
    {
      ["AuditLevel"] = _auditLevel
    };
    using IDisposable? scope = _logger.BeginScope(scopeState);
    _logger.Log(logLevel, eventId, state, exception, formatter);
  }
  public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);
  public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);
}