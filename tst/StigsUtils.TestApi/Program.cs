// Copyright © 2023 TradingLens. All Rights Reserved.

using System.Diagnostics;
using OpenTelemetry.Trace;
using StigsUtils.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services
       .AddOpenTelemetry()
       .WithTracing(
          x => x.SetSampler(new AlwaysOffSampler())
                .AddAspNetCoreInstrumentation()
        );

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
  "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
      var forecast = Enumerable.Range(1, 5).Select(index =>
                                                     new WeatherForecast
                                                     (
                                                       DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                                                       Random.Shared.Next(-20, 55),
                                                       summaries[Random.Shared.Next(summaries.Length)]
                                                     )).ToArray();
      
      return new { SpanIdLong = Activity.Current?.SpanId.ToLong(), TraceIdGuid = Activity.Current?.TraceId.ToGuid() , forecast};
    })
   .WithName("GetWeatherForecast")
   .WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}