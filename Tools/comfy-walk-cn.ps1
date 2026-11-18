[CmdletBinding()]
param(
    [string]$PortableRoot = "E:\AI\ComfyUI_portable\ComfyUI_windows_portable_nvidia_cu128\ComfyUI_windows_portable",
    [string]$Launcher     = "run_nvidia_gpu_fast_fp16_accumulation.bat",
    [string]$WorkRoot     = "E:\unity-codex\2D ZMZZ\.verify\gen",
    [string]$Ckpt         = "waiIllustriousSDXL_v170.safetensors",
    [string]$ControlNet   = "diffusion_pytorch_model.safetensors",
    [int]$Seed            = 777001,
    [int]$Steps           = 26,
    [double]$CnStrength   = 0.9,
    [string]$Prefix       = "walkcn"
)

# ControlNet OpenPose 驱动：每帧喂一张骨架图，姿势由骨架决定（不是模型猜）。
# 骨架图在 .verify\gen\poses\pose_XX.png

$ErrorActionPreference = "Stop"
$Api       = "http://127.0.0.1:8188"
$BatPath   = Join-Path $PortableRoot $Launcher
$OutputDir = Join-Path $PortableRoot "ComfyUI\output"
$InputDir  = Join-Path $PortableRoot "ComfyUI\input"
$PoseDir   = Join-Path $WorkRoot "poses"
$OutDir    = Join-Path $WorkRoot $Prefix

# 只描述角色，不写姿势 —— 姿势交给骨架
$char = "1girl, solo, knight, silver hair, long straight hair, blue eyes, " +
        "blue and white armored dress, silver breastplate, white cape, " +
        "silver gauntlets, black thigh boots, " +
        "full body, side view, facing right, white background, simple background, " +
        "masterpiece, best quality, very aesthetic, absurdres"

$negative = (Get-Content -LiteralPath (Join-Path $WorkRoot "negative.txt") -Raw -Encoding UTF8).Trim()

function Test-Comfy {
    try { Invoke-WebRequest -Uri "$Api/system_stats" -TimeoutSec 5 -UseBasicParsing | Out-Null; return $true }
    catch { return $false }
}

if (-not (Test-Comfy)) {
    Write-Host "[cn] 启动 ComfyUI：$Launcher"
    Start-Process -FilePath $BatPath -WorkingDirectory $PortableRoot -WindowStyle Hidden | Out-Null
    $deadline = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline) { Start-Sleep -Seconds 5; if (Test-Comfy) { break } }
}
if (-not (Test-Comfy)) { Write-Host "[cn] 服务没起来，退出"; exit 1 }
Write-Host "[cn] 服务已就绪"

New-Item -ItemType Directory -Force -Path $OutDir, $InputDir | Out-Null

# 把骨架图拷进 ComfyUI 的 input 目录（LoadImage 只认那里）
$poses = Get-ChildItem -LiteralPath $PoseDir -Filter "pose_*.png" -File | Sort-Object Name
if ($poses.Count -eq 0) { Write-Host "[cn] 没找到骨架图：$PoseDir"; exit 1 }
foreach ($p in $poses) { Copy-Item -LiteralPath $p.FullName -Destination $InputDir -Force }
Write-Host "[cn] 已拷入 $($poses.Count) 张骨架图"

foreach ($p in $poses) {
    $idx = $p.BaseName
    $wf = @{
        "1"  = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $Ckpt } }
        "20" = @{ class_type = "ControlNetLoader"; inputs = @{ control_net_name = $ControlNet } }
        "10" = @{ class_type = "LoadImage"; inputs = @{ image = $p.Name } }
        "2"  = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $char; clip = @("1", 1) } }
        "3"  = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $negative; clip = @("1", 1) } }
        "11" = @{ class_type = "ControlNetApplyAdvanced"; inputs = @{ positive = @("2", 0); negative = @("3", 0); control_net = @("20", 0); image = @("10", 0); strength = $CnStrength; start_percent = 0.0; end_percent = 1.0 } }
        "4"  = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 768; height = 1152; batch_size = 1 } }
        "5"  = @{ class_type = "KSampler"; inputs = @{ model = @("1", 0); positive = @("11", 0); negative = @("11", 1); latent_image = @("4", 0); seed = $Seed; steps = $Steps; cfg = 6.0; sampler_name = "dpmpp_2m"; scheduler = "karras"; denoise = 1.0 } }
        "6"  = @{ class_type = "VAEDecode"; inputs = @{ samples = @("5", 0); vae = @("1", 2) } }
        "7"  = @{ class_type = "SaveImage"; inputs = @{ images = @("6", 0); filename_prefix = ($Prefix + "_" + $idx) } }
    }

    Write-Host "[cn] 帧 $idx ……  $(Get-Date -Format 'HH:mm:ss')"
    $body = @{ prompt = $wf; client_id = "codex" } | ConvertTo-Json -Depth 12 -Compress
    try {
        $res = Invoke-RestMethod -Uri "$Api/prompt" -Method Post -ContentType "application/json" -Body $body -TimeoutSec 60
    }
    catch {
        Write-Host "[cn]   提交失败：" + $_.Exception.Message
        if ($_.ErrorDetails) { Write-Host $_.ErrorDetails.Message }
        continue
    }

    $promptId = $res.prompt_id
    $deadline = (Get-Date).AddMinutes(10)
    $ok = $false
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 4
        try {
            $h = Invoke-RestMethod -Uri "$Api/history/$promptId" -TimeoutSec 20
            $entry = $h.$promptId
            if ($entry -and $entry.outputs) {
                $img = $entry.outputs."7".images[0]
                $src = Join-Path $OutputDir $img.filename
                if ($img.subfolder) { $src = Join-Path (Join-Path $OutputDir $img.subfolder) $img.filename }
                Copy-Item -LiteralPath $src -Destination (Join-Path $OutDir ($Prefix + "_" + $idx + ".png")) -Force
                Write-Host "[cn]   ok"
                $ok = $true
                break
            }
        }
        catch { }
    }
    if (-not $ok) { Write-Host "[cn]   !! 帧 $idx 失败" }
}

Write-Host "[cn] 完成，输出目录：$OutDir"
