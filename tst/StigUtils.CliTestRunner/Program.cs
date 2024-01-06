using System.Diagnostics;
using StigUtils.CliTestRunner;

var start = DateTime.UtcNow;
try
{
  Console.WriteLine("Cli Test runner started at " + start);
  AuditLogTests.Test1();
}
finally
{
  var end = DateTime.UtcNow;
  Console.WriteLine($"Cli Test runner ended at {end}, elapsed = {(end - start).TotalMilliseconds} ms.");
  if (Debugger.IsAttached)
  {
    Console.WriteLine("Press enter to exit.");
    Console.ReadLine();
  }
}
