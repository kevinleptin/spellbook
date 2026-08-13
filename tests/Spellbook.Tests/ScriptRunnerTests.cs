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
}
