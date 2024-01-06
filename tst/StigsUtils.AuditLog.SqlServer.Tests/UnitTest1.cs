// Copyright © 2023 TradingLens. All Rights Reserved.

using Microsoft.Extensions.Logging;
using StigsUtils.ActivityLog;
using StigUtils.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace StigsUtils.AuditLog.SqlServer.Tests;

public class UnitTest1
{
  private readonly ITestOutputHelper _testOutputHelper;
  public UnitTest1(ITestOutputHelper testOutputHelper)
  {
    _testOutputHelper = testOutputHelper;
  }

  [Fact]
  public void Test1()
  {
    var bootstrapper = new TestBootstrapper(x => _testOutputHelper.WriteLine(x));
    ILogger logger = bootstrapper.GetLogger<UnitTest1>();
    logger.Activity(2).LogInformation("som audit log at info level");
  }
}