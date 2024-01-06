// Copyright © 2023 TradingLens. All Rights Reserved.

namespace StigsUtils;

public static class AmbientContext
{
  private static AsyncLocal<string?> _userId = new();
  public static string? UserId
  {
    get => _userId.Value ?? default;
    set => _userId.Value = value;
  }
  
  public static void Clear()
  {
    _userId.Value = null;
  }
}