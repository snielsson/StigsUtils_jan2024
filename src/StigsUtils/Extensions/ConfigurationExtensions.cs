// Copyright © 2023 TradingLens. All Rights Reserved.

using Microsoft.Extensions.Configuration;

namespace StigsUtils.Extensions;

public static class ConfigurationExtensions
{
  public static string GetString(this IConfiguration configuration, string configurationKey, string? defaultValue = null)
    => configuration[configurationKey] ?? defaultValue ?? throw new ArgumentException("No value found for configuration key " + configurationKey);
}