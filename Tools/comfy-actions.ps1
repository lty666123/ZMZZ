[CmdletBinding()]
param(
    [string]$PortableRoot = "E:\AI\ComfyUI_portable\ComfyUI_windows_portable_nvidia_cu128\ComfyUI_windows_portable",
    [string]$Launcher     = "run_nvidia_gpu_fast_fp16_accumulation.bat",
    [string]$WorkRoot     = "E:\unity-codex\2D ZMZZ\.verify\gen",
    [string]$Ckpt         = "waiIllustriousSDXL_v170.safetensors",
    [int]$Seed            = 777001,
    [int]$Steps           = 28,
    [switch]$Force
)

# 批量出「同一个角色的多个动作姿势」：已生成过的自动跳过，最后拼一张对比图。
# 想重出某张就删掉 .verify\gen\actions 里对应的文件，或者加 -Force 全量重出。

$ErrorActionPreference = "Stop"
$Api       = "http://127.0.0.1:8188"
$BatPath   = Join-Path $PortableRoot $Launcher
$OutputDir = Join-Path $PortableRoot "ComfyUI\output"
$ActDir    = Join-Path $WorkRoot "actions"

# 角色固定部分（所有动作共用，保证是同一个人）
$base = "1girl, solo, knight, silver hair, long straight hair, blue eyes, " +
        "blue and white armored dress, silver breastplate, white cape, " +
        "silver gauntlets, black thigh boots, holding longsword, " +
        "white background, simple background, masterpiece, best quality, very aesthetic, absurdres"

# 动作清单：全身站立 / 移动 / 战斗 / 状态 / 头像
$actions = [ordered]@{
    "01_idle"     = "full body, standing, idle, arms at sides, facing viewer, calm"
    "02_walk"     = "full body, walking, side view, facing right, arms at sides"
    "03_run"      = "full body, running, side view, facing right, dynamic pose, motion blur"
    "04_jump"     = "full body, jumping, mid-air, legs bent, side view, facing right"
    "05_fall"     = "full body, falling, mid-air, arms out, side view, facing right"
    "06_land"     = "full body, landing, crouching, one knee bent, side view"
    "07_attack_h" = "full body, horizontal sword slash, swinging sword, dynamic pose, motion lines"
    "08_attack_u" = "full body, sword held overhead, upper slash, dynamic pose, attacking"
    "09_attack_t" = "full body, thrusting sword forward, stab, lunge, dynamic pose"
    "10_dash"     = "full body, dashing, low stance, motion blur, side view, facing right"
    "11_guard"    = "full body, blocking, holding sword in front, defensive stance, guard"
    "12_hit"      = "full body, hurt, flinching, recoiling, pained expression, staggered"
    "13_die"      = "full body, lying on ground, defeated, eyes closed, on back"
    "14_cast"     = "full body, casting magic, glowing magic circle, raised hand"
    "15_victory"  = "full body, victory pose, sword raised, smiling, confident"
    "16_portrait" = "upper body, close-up, portrait, looking at viewer, calm, detailed face"
}

$negative = (Get-Content -LiteralPath (Join-Path $WorkRoot "negative.txt") -Raw -Encoding UTF8).Trim()

function Test-Comfy {
    try { Invoke-WebRequest -Uri "$Api/system_stats" -TimeoutSec 5 -UseBasicParsing | Out-Null; return $true }
    catch { return $false }
}

if (-not (Test-Comfy)) {
    Write-Host "[gen] 启动 ComfyUI：$Launcher"
    Start-Process -FilePath $BatPath -WorkingDirectory $PortableRoot -WindowStyle Hidden | Out-Null
    $deadline = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline) { Start-Sleep -Seconds 5; if (Test-Comfy) { break } }
}
if (-not (Test-Comfy)) { Write-Host "[gen] 服务没起来，退出"; exit 1 }
Write-Host "[gen] 服务已就绪（$Ckpt / $Steps 步 / 种子 $Seed）"

New-Item -ItemType Directory -Force -Path $ActDir | Out-Null

function Invoke-Comfy([string]$positive, [string]$prefix) {
    $wf = @{
        "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $Ckpt } }
        "2" = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $positive; clip = @("1", 1) } }
        "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $negative; clip = @("1", 1) } }
        "4" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 832; height = 1216; batch_size = 1 } }
        "5" = @{ class_type = "KSampler"; inputs = @{ model = @("1", 0); positive = @("2", 0); negative = @("3", 0); latent_image = @("4", 0); seed = $Seed; steps = $Steps; cfg = 6.0; sampler_name = "dpmpp_2m"; scheduler = "karras"; denoise = 1.0 } }
        "6" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("5", 0); vae = @("1", 2) } }
        "7" = @{ class_type = "SaveImage"; inputs = @{ images = @("6", 0); filename_prefix = $prefix } }
    }
    $body = @{ prompt = $wf; client_id = "codex" } | ConvertTo-Json -Depth 12 -Compress
    $res  = Invoke-RestMethod -Uri "$Api/prompt" -Method Post -ContentType "application/json" -Body $body -TimeoutSec 60
    $promptId = $res.prompt_id

    $deadline = (Get-Date).AddMinutes(10)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 4
        try {
            $h = Invoke-RestMethod -Uri "$Api/history/$promptId" -TimeoutSec 20
            $entry = $h.$promptId
            if ($entry -and $entry.outputs) {
                $img = $entry.outputs."7".images[0]
                $src = Join-Path $OutputDir $img.filename
                if ($img.subfolder) { $src = Join-Path (Join-Path $OutputDir $img.subfolder) $img.filename }
                return $src
            }
        }
        catch { }
    }
    return $null
}

$done = 0; $skip = 0; $fail = 0
foreach ($name in $actions.Keys) {
    $dst = Join-Path $ActDir ($name + ".png")
    if ((Test-Path -LiteralPath $dst) -and -not $Force) {
        Write-Host "[gen] 跳过 $name（已存在）"
        $skip++
        continue
    }

    Write-Host "[gen] 生成 $name ……  $(Get-Date -Format 'HH:mm:ss')"
    $src = Invoke-Comfy ($base + ", " + $actions[$name]) ("act_" + $name)
    if ($src -and (Test-Path -LiteralPath $src)) {
        Copy-Item -LiteralPath $src -Destination $dst -Force
        Write-Host "[gen]   ok -> $dst"
        $done++
    }
    else {
        Write-Host "[gen]   !! $name 失败"
        $fail++
    }
}

# 拼对比图（4 列）
Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem -LiteralPath $ActDir -File -Filter *.png | Sort-Object Name
if ($files.Count -gt 0) {
    $cellW = 280; $cellH = 409; $pad = 6
    $cols = 4
    $rows = [math]::Ceiling($files.Count / $cols)
    $sheet = New-Object System.Drawing.Bitmap (($cellW + $pad) * $cols + $pad), (($cellH + $pad) * $rows + $pad)
    $g = [System.Drawing.Graphics]::FromImage($sheet)
    $g.Clear([System.Drawing.Color]::FromArgb(28, 28, 32))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $i = 0
    foreach ($f in $files) {
        $col = $i % $cols
        $row = [math]::Floor($i / $cols)
        $img = [System.Drawing.Image]::FromFile($f.FullName)
        $g.DrawImage($img, ($pad + $col * ($cellW + $pad)), ($pad + $row * ($cellH + $pad)), $cellW, $cellH)
        $img.Dispose()
        $i++
    }
    $g.Dispose()
    $sheetPath = Join-Path $WorkRoot "actions_sheet.png"
    $sheet.Save($sheetPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $sheet.Dispose()
    Write-Host "[gen] 对比图：$sheetPath（$($files.Count) 张）"
}
Write-Host "[gen] 结束：新生成 $done，跳过 $skip，失败 $fail"
