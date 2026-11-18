[CmdletBinding()]
param(
    [string]$ProjectPath = "E:\unity-codex\2D ZMZZ\ZMZZ-Unity",
    [string]$WorkDir     = "E:\unity-codex\2D ZMZZ\.verify\compile-check",
    [string]$OutDir      = "E:\unity-codex\2D ZMZZ\.verify\out",
    [string]$CscDll      = "D:\unity not tuanjie\2022.3.62f3c1\Editor\Data\DotNetSdkRoslyn\csc.dll"
)

# 不开 Unity 编辑器也能验证脚本能不能编译。
#
# 做法：Unity 每次编译都会把「完整的 csc 参数」写进 Library/Bee/artifacts/*/*.rsp，
# 里面有精确的引用列表、宏定义和警告设置。我们借用那份参数，把源文件列表换成
# 「当前磁盘上的全部 .cs」，再用 Unity 自带的 Roslyn 编译一遍。
#
# 两种工程都能处理：
#   · 有 asmdef 的（ZMZZ.Runtime / ZMZZ.Editor / ZMZZ.Tests.PlayMode）
#   · 没有 asmdef 的（Assembly-CSharp / Assembly-CSharp-Editor）

$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_HOME = Join-Path $WorkDir "dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
New-Item -ItemType Directory -Force -Path $WorkDir, $OutDir | Out-Null

if (-not (Test-Path -LiteralPath $CscDll)) {
    Write-Host "[check] 找不到 csc.dll：$CscDll"
    exit 1
}

$artifactsRoot = Join-Path $ProjectPath "Library\Bee\artifacts"
if (-not (Test-Path -LiteralPath $artifactsRoot)) {
    Write-Host "[check] 找不到 Library/Bee/artifacts —— 需要先用 Unity 打开过一次工程"
    exit 1
}

$dagDir = Get-ChildItem -LiteralPath $artifactsRoot -Directory |
          Where-Object { $_.Name -ne "rsp" } |
          Sort-Object LastWriteTime -Descending | Select-Object -First 1

$targets = New-Object System.Collections.ArrayList

# ---- 优先按带 asmdef 的工程找 ----
$zmzzNames = @("ZMZZ.Runtime", "ZMZZ.Editor", "ZMZZ.Tests.PlayMode")
$hasZmzz = $true
foreach ($n in $zmzzNames) {
    if (-not (Test-Path -LiteralPath (Join-Path $dagDir.FullName ($n + ".rsp")))) { $hasZmzz = $false }
}

if ($hasZmzz) {
    [void]$targets.Add(@{ Name = "ZMZZ.Runtime";        Sources = @("Assets\Scripts"); OnlyEditor = $false; ExcludeEditor = $false })
    [void]$targets.Add(@{ Name = "ZMZZ.Editor";         Sources = @("Assets\Editor");  OnlyEditor = $false; ExcludeEditor = $false })
    [void]$targets.Add(@{ Name = "ZMZZ.Tests.PlayMode"; Sources = @("Assets\Tests");   OnlyEditor = $false; ExcludeEditor = $false })
}
else {
    # ---- 没有 asmdef 的工程：脚本都在 Assembly-CSharp 里 ----
    if (Test-Path -LiteralPath (Join-Path $dagDir.FullName "Assembly-CSharp.rsp")) {
        [void]$targets.Add(@{ Name = "Assembly-CSharp";        Sources = @("Assets"); OnlyEditor = $false; ExcludeEditor = $true })
    }
    if (Test-Path -LiteralPath (Join-Path $dagDir.FullName "Assembly-CSharp-Editor.rsp")) {
        [void]$targets.Add(@{ Name = "Assembly-CSharp-Editor"; Sources = @("Assets"); OnlyEditor = $true;  ExcludeEditor = $false })
    }
}

if ($targets.Count -eq 0) {
    Write-Host "[check] 没找到可用的编译参数文件（*.rsp）"
    exit 1
}

$built = @{}
$failed = $false

foreach ($target in $targets) {
    $name = $target.Name
    $rspPath = Join-Path $dagDir.FullName ($name + ".rsp")

    $targetOut = Join-Path $OutDir $name
    New-Item -ItemType Directory -Force -Path $targetOut | Out-Null
    $outDll = Join-Path $targetOut ($name + ".dll")

    $lines = New-Object System.Collections.Generic.List[string]
    foreach ($line in (Get-Content -LiteralPath $rspPath)) {
        $text = $line.Trim()
        if ($text -eq "") { continue }

        # 输出路径换到我们的目录，绝不覆盖 Unity 正在用的编译产物
        if ($text -match '^[-/](out|refout):') { continue }

        # 旧的源文件清单丢掉，后面按磁盘实际情况重新生成
        if ($text -match '^"?Assets[\\/]') { continue }

        # 依赖自家程序集时，用刚编译出来的最新产物
        if ($text -match '^[-/]r:') {
            foreach ($dep in $built.Keys) {
                if ($text -like "*$dep*") { $text = '-r:"' + $built[$dep] + '"' }
            }
        }

        $lines.Add($text)
    }

    $lines.Add("-noconfig")
    $lines.Add('-out:"' + $outDll + '"')

    # 收集源文件
    $candidates = @()
    foreach ($src in $target.Sources) {
        $root = Join-Path $ProjectPath $src
        if (Test-Path -LiteralPath $root) {
            $candidates += Get-ChildItem -LiteralPath $root -Recurse -Filter *.cs -File
        }
    }

    $sourceFiles = $candidates | Where-Object {
        $isEditor = ($_.FullName -match '\\Editor\\')
        ($target.OnlyEditor -and $isEditor) -or
        ($target.ExcludeEditor -and -not $isEditor) -or
        (-not $target.OnlyEditor -and -not $target.ExcludeEditor)
    }

    foreach ($file in $sourceFiles) { $lines.Add('"' + ($file.FullName -replace '\\', '/') + '"') }

    $checkRsp = Join-Path $WorkDir ($name + ".check.rsp")
    Set-Content -LiteralPath $checkRsp -Value ($lines -join "`n") -Encoding UTF8

    Write-Host "[check] 编译 $name（$($sourceFiles.Count) 个源文件）..."

    Push-Location -LiteralPath $ProjectPath
    $compilerLog = & dotnet $CscDll "@$checkRsp" 2>&1
    $exitCode = $LASTEXITCODE
    Pop-Location

    $errors = $compilerLog | Select-String -Pattern "error CS"
    if ($errors -or $exitCode -ne 0) {
        Write-Host "[check] $name 编译失败："
        if ($errors) {
            $errors | Select-Object -First 60 | ForEach-Object {
                $line = $_.Line -replace [regex]::Escape($ProjectPath + "\"), ""
                Write-Host "   " $line
            }
        }
        else {
            $compilerLog | Select-Object -First 20 | ForEach-Object { Write-Host "   " $_ }
        }
        $failed = $true
    }
    else {
        Write-Host "[check] $name 通过"
        $built[$name] = $outDll
    }
}

if ($failed) { Write-Host "[check] 结果：存在编译错误" }
else { Write-Host "[check] 结果：全部编译通过" }
