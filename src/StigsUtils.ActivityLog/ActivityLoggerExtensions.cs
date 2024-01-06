// Copyright © 2023 TradingLens. All Rights Reserved.

using Microsoft.Extensions.Logging;

namespace StigsUtils.ActivityLog;

public static class ActivityLoggerExtensions
{
  public static ILogger Activity(this ILogger logger, int auditLevel = 1) => new ActivityLogger(logger, auditLevel);
}