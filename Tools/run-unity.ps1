[CmdletBinding()]
param(
    [ValidateSet("create", "template", "compile", "method", "test")]
    [string]$Mode = "compile",
    [string]$ExecuteMethod = ""
)

$ErrorActionPreference = "Stop"

$UnityExe    = "D:\unity not tuanjie\2022.3.62f3c1\Editor\Unity.exe"
$ProjectPath = "E:\unity-codex\2D ZMZZ\ZMZZ-Unity"
$LogDir      = "E:\unity-codex\2D ZMZZ\Logs"
$LogPath     = Join-Path $LogDir ("unity-" + $Mode + ".log")
$ResultsPath = Join-Path $LogDir "test-results.xml"
$TemplatePath = "D:\unity not tuanjie\2022.3.62f3c1\Editor\Data\Resources\PackageManager\ProjectTemplates\com.unity.template.universal-2d-2.1.3.tgz"

if (-not (Test-Path -LiteralPath $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

$unityArgList = switch ($Mode) {
    "create"  { @("-createProject", "`"$ProjectPath`"", "-quit", "-batchmode", "-nographics") }
    "template" { @("-createProject", "`"$ProjectPath`"", "-cloneFromTemplate", "`"$TemplatePath`"", "-quit", "-batchmode", "-nographics") }
    "compile" { @("-projectPath", "`"$ProjectPath`"", "-quit", "-batchmode", "-nographics") }
    "method"  { @("-projectPath", "`"$ProjectPath`"", "-quit", "-batchmode", "-nographics", "-executeMethod", $ExecuteMethod) }
    "test"    { @("-projectPath", "`"$ProjectPath`"", "-runTests", "-testPlatform", "PlayMode", "-testResults", "`"$ResultsPath`"", "-batchmode", "-nographics") }
}
$unityArgList += @("-logFile", "`"$LogPath`"")

Write-Host "[run-unity] mode    = $Mode"
Write-Host "[run-unity] exe     = $UnityExe"
Write-Host "[run-unity] project = $ProjectPath"
Write-Host "[run-unity] log     = $LogPath"

# 旧日志先移走，避免读到上一次的结果
if (Test-Path -LiteralPath $LogPath) {
    Remove-Item -LiteralPath $LogPath -Force
}

$proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgList -WindowStyle Hidden -PassThru -Wait
Write-Host "[run-unity] ExitCode = $($proc.ExitCode)"

if (Test-Path -LiteralPath $LogPath) {
    $lines = Get-Content -LiteralPath $LogPath
    Write-Host "----- log tail (last 30 lines) -----"
    $lines | Select-Object -Last 30 | ForEach-Object { Write-Host $_ }

    $notable = $lines | Select-String -Pattern "error CS|compiler errors|Fatal Error|Aborting batchmode|Failed to|Exception:"
    if ($notable) {
        Write-Host "----- notable lines -----"
        $notable | Select-Object -First 40 | ForEach-Object { Write-Host $_.Line }
    }
}
else {
    Write-Host "[run-unity] WARNING: log file was not created"
}

if ($Mode -eq "test") {
    if (Test-Path -LiteralPath $ResultsPath) {
        [xml]$xml = Get-Content -LiteralPath $ResultsPath
        $run = $xml.'test-run'
        Write-Host "----- 测试结果 -----"
        Write-Host ("total={0} passed={1} failed={2} skipped={3} result={4}" -f `
            $run.total, $run.passed, $run.failed, $run.skipped, $run.result)
        foreach ($case in $xml.SelectNodes("//test-case[@result!='Passed']")) {
            Write-Host ("FAILED: {0}" -f $case.fullname)
            $msg = $case.failure.message.'#cdata-section'
            if (-not $msg) { $msg = $case.failure.message }
            if ($msg) { Write-Host $msg }
        }
    }
    else {
        Write-Host "[run-unity] WARNING: 没找到测试结果文件 $ResultsPath"
    }
}
