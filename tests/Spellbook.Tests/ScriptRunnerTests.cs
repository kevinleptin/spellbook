using System.IO;
using Spellbook.Services;

namespace Spellbook.Tests;

public class ScriptRunnerTests
{
    [Fact]
    public void BuildArguments_QuotesPath()
        => Assert.Equal(
            "-ExecutionPolicy Bypass -File \"C:\\my scripts\\a.ps1\"",
            ScriptRunner.BuildArguments(@"C:\my scripts\a.ps1", ""));

    [Fact]
    public void BuildArguments_AppendsArgsVerbatim()
        => Assert.Equal(
            "-ExecutionPolicy Bypass -File \"C:\\s\\a.ps1\" -a 1 --flag \"x y\"",
            ScriptRunner.BuildArguments(@"C:\s\a.ps1", "-a 1 --flag \"x y\""));

    [Fact]
    public void BuildArguments_EmptyArgs_NoTrailingSpace()
        => Assert.DoesNotMatch(@"\s$", ScriptRunner.BuildArguments(@"C:\s\a.ps1", "  "));

    [Theory]
    [InlineData("http://example.com", LaunchKind.Url)]
    [InlineData("https://example.com/page?a=1", LaunchKind.Url)]
    [InlineData("HTTPS://EXAMPLE.COM", LaunchKind.Url)]
    [InlineData(@"C:\s\a.ps1", LaunchKind.Script)]
    [InlineData(@"C:\s\a.PS1", LaunchKind.Script)]
    [InlineData(@"C:\Program Files\app\chrome.exe", LaunchKind.Program)]
    [InlineData(@"C:\data\report.xlsx", LaunchKind.Program)]
    [InlineData(@"C:\s\noextension", LaunchKind.Program)]
    public void GetLaunchKind_ClassifiesByPath(string path, LaunchKind expected)
        => Assert.Equal(expected, ScriptRunner.GetLaunchKind(path));

    [Fact]
    public void GetLaunchKind_ExistingDirectory_IsFolder()
        => Assert.Equal(LaunchKind.Folder, ScriptRunner.GetLaunchKind(Path.GetTempPath()));
}
