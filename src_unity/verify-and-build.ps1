# Spellbook Unity 版:一键测试 + 构建(需已激活 Unity 许可证,Hub 里登录一次即可)
# 用法: powershell -File src_unity\verify-and-build.ps1 [-UnityExe <路径>]
param(
    [string]$UnityExe = "C:\Unity\6000.0.81f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "Spellbook.Unity"
$results = Join-Path $PSScriptRoot "test-results.xml"
$log = Join-Path $PSScriptRoot "unity-build.log"

if (-not (Test-Path $UnityExe)) {
    Write-Error "未找到 Unity.exe: $UnityExe (可用 -UnityExe 指定)"
}

Write-Host "[1/2] EditMode 测试…"
& $UnityExe -batchmode -nographics -projectPath $project `
    -runTests -testPlatform EditMode -testResults $results -logFile $log | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "测试失败(退出码 $LASTEXITCODE),详见 $log 与 $results"
}
Write-Host "测试通过。"

Write-Host "[2/2] 构建 Windows 版…"
& $UnityExe -batchmode -nographics -quit -projectPath $project `
    -executeMethod Spellbook.EditorTools.BuildScript.Build -logFile $log | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "构建失败(退出码 $LASTEXITCODE),详见 $log"
}
Write-Host "构建完成: $project\Builds\Windows\Spellbook Arcane.exe"
