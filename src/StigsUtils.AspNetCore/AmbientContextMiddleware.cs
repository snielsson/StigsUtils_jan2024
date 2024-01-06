// Copyright © 2023 TradingLens. All Rights Reserved.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace StigsUtils.AspNetCore;

public class AmbientContextMiddleware
{
  private readonly RequestDelegate _next;
  public AmbientContextMiddleware(RequestDelegate next)
  {
    _next = next;
  }
  public async Task InvokeAsync(HttpContext context)
  {
    var currentActivity = Activity.Current;
    if (currentActivity != null && (context.User.Identity?.IsAuthenticated ?? false))
    {
      string? userId = context.User.Identity.Name; // or another unique identifier from the user
      AmbientContext.UserId = userId;
    }
    await _next(context);
    AmbientContext.Clear();
  }
}