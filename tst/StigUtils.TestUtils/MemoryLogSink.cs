// Copyright © 2023 TradingLens. All Rights Reserved.

using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace StigUtils.TestUtils;

public interface IMemoryLog
{
  event Action<LogEvent, string> OnLogEvent;
  string? Format { get; set; }
  (LogEvent LogEvent, string LogMessage)[] GetLogHistory();
}
public class MemoryLogSerilogSink : ILogEventSink, IMemoryLog, IDisposable
{
  public const int DefaultCapacity = 0;
  private int _capacity = DefaultCapacity;

  private MessageTemplateTextFormatter? _formatter;
  private readonly Queue<(LogEvent, string)> _queue;

  public MemoryLogSerilogSink(int capacity = DefaultCapacity, string? format = null)
  {
    _capacity = capacity;
    _queue = new(_capacity); 
    Format = format ?? DefaultFormat;
  }
  
  private string? _format = DefaultFormat;
  public const string DefaultFormat = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
  public int Capacity
  {
    get => _capacity;
    set
    {
      _capacity = value;
      lock (_queue) _queue.EnsureCapacity(_capacity);
    }
  }

  public string? Format
  {
    get => _format;
    set
    {
      _format = value;
      _formatter = _format == null ? null : new MessageTemplateTextFormatter(_format);
    }
  }

  public void Dispose() => OnLogEvent = null;

  public event Action<LogEvent, string>? OnLogEvent;
  
  public (LogEvent LogEvent, string LogMessage)[] GetLogHistory()
  {
    lock (_queue) return _queue.ToArray();
  }

  public void Emit(LogEvent logEvent)
  {
    var message = Render(logEvent);
    if (_capacity > 0)
    {
      lock (_queue)
      {
        while (_queue.Count >= Capacity) _queue.Dequeue();
        _queue.Enqueue((logEvent, message));
      }
    }
    OnLogEvent?.Invoke(logEvent, message);
  }

  private string Render(LogEvent logEvent)
  {
    if (_formatter != null)
    {
      var sb = new StringBuilder(128);
      using var writer = new StringWriter(sb);
      _formatter.Format(logEvent, writer);
      return sb.ToString();
    }
    return logEvent.RenderMessage();
  }
}