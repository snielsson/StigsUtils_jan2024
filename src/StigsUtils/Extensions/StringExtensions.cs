// Copyright © 2023 TradingLens. All Rights Reserved.

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace StigsUtils.Extensions;

public static class StringExtensions
{
  public static string ToAbsolutePath(this string s) => Path.GetFullPath(s);
  public static IEnumerable<string> NotMatchingAny(this IEnumerable<string> s, IEnumerable<Regex> regexes) => s.Where(x => !regexes.Any(regex => regex.IsMatch(x)));
}

public static class ActivityExtensions 
{
  public static long? ToNullableLong(this ActivitySpanId? spanId)
  {
    var s = spanId?.ToHexString() ?? "";
    if (s.Length == 0) return null;
    return long.Parse(s, NumberStyles.HexNumber);
  }
  public static long? ToLong(this ActivitySpanId spanId) => long.Parse(spanId.ToHexString(), NumberStyles.HexNumber);
  public static Guid ToGuid(this ActivityTraceId traceId) => Guid.Parse(traceId.ToHexString());
  public static Guid? AsGuid(this string? x) => x == null ? null : Guid.Parse(x);
}