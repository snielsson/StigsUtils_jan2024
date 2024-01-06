// Copyright © 2023 TradingLens. All Rights Reserved.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using StigsUtils;
using StigsUtils.Abstractions;
using StigsUtils.ActivityLog;
using StigUtils.TestUtils;

namespace StigUtils.CliTestRunner;

public class AuditLogTests
{
  public static void Test1() 
  { 
    var bootstrapper = new TestBootstrapper();
    var logger = bootstrapper.GetLogger("CliTestRunner");

    var activity = new Activity("CliTestRunner_Activity1").Start();
    AmbientContext.UserId = Guid.NewGuid().ToString();
    var ex = new ArgumentException("sdasdsad");
    logger.Activity(15).LogInformation(ex,"Testing Audit log {SubProjectId} {UserId} {AuditLevel2}", Guid.NewGuid(), Guid.NewGuid(), 117);
    logger.BeginScope(new { SubProjectId = Guid.Empty });
    logger.BeginScope(new Dictionary<string,string> {{"xx","yy"}});
    
    logger.LogInformation(116,"Testing non Audit info log ");
    logger.LogInformation(116,"Testing non Audit info log {SubProjectId}", Guid.NewGuid());
    logger.BeginScope(new { ProjectId = Guid.NewGuid() });
    logger.LogDebug("Testing non Audit debug log");
    logger.Activity(16).LogWarning(117, "Testing Audit warning log");
    var memlog = bootstrapper.MemoryLog;
    
    activity.Stop();
    
    var auditLog = bootstrapper.Get<IActivityLog>();
    auditLog.Dispose();

  }
}