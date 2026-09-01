#nullable enable
using System.Diagnostics;
using NUnit.Framework;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Tests;

[TestFixture]
public sealed class GameProcessRunnerTests
{
    [Test]
    public void TestProcessStartInfo_ArgumentsAndEnv()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("--version");
        startInfo.EnvironmentVariables["TEST_VAR"] = "123";

        Assert.That(startInfo.ArgumentList.Count, Is.EqualTo(1));
        Assert.That(startInfo.ArgumentList[0], Is.EqualTo("--version"));
        Assert.That(startInfo.EnvironmentVariables["TEST_VAR"], Is.EqualTo("123"));
    }
}
