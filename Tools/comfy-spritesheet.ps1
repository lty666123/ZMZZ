[CmdletBinding()]
param(
    [string]$PortableRoot = "E:\AI\ComfyUI_portable\ComfyUI_windows_portable_nvidia_cu128\ComfyUI_windows_portable",
    [string]$Launcher     = "run_nvidia_gpu_fast_fp16_accumulation.bat",
    [string]$WorkRoot     = "E:\unity-codex\2D ZMZZ\.verify\gen",
    [string]$Ckpt         = "waiIllustriousSDXL_v170.safetensors",
    [int]$Seed            = 20261014,
    [int]$Steps           = 28,
    [string]$Prompt       = ""
)

# 让模型「在一张图里画出 4 帧行走循环」，再切成 4 帧。
# 一次生成 → 角色天然一致，这是 AI 做逐帧最实用的路子。

$ErrorActionPreference = "Stop"
$Api       = "http://127.0.0.1:8188"
$BatPath   = Join-Path $PortableRoot $Launcher
$OutputDir = Join-Path $PortableRoot "ComfyUI\output"
$SheetDir  = Join-Path $WorkRoot "sheet"

if ([string]::IsNullOrWhiteSpace($Prompt)) {
    $Prompt = "sprite sheet, 4 frames in a row, walk cycle animation frames, side view, facing right, " +
              "1girl, solo, knight, silver hair, long straight hair, blue eyes, " +
              "blue and white armored dress, silver breastplate, white cape, silver gauntlets, black thigh boots, " +
              "full body, white background, simple background, evenly spaced, same character in every frame, " +
              "masterpiece, best quality, very aesthetic, absurdres"
}

$negative = (Get-Content -LiteralPath (Join-Path $WorkRoot "negative.txt") -Raw -Encoding UTF8).Trim() +
            ", text, watermark, signature, grid lines, different characters, inconsistent design, collage, cutout"

function Test-Comfy {
    try { Invoke-WebRequest -Uri "$Api/system_stats" -TimeoutSec 5 -UseBasicParsing | Out-Null; return $true }
    catch { return $false }
}

if (-not (Test-Comfy)) {
    Write-Host "[sheet] 启动 ComfyUI：$Launcher"
    Start-Process -FilePath $BatPath -WorkingDirectory $PortableRoot -WindowStyle Hidden | Out-Null
    $deadline = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline) { Start-Sleep -Seconds 5; if (Test-Comfy) { break } }
}
if (-not (Test-Comfy)) { Write-Host "[sheet] 服务没起来，退出"; exit 1 }
Write-Host "[sheet] 服务已就绪"

New-Item -ItemType Directory -Force -Path $SheetDir | Out-Null

$wf = @{
    "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $Ckpt } }
    "2" = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $Prompt; clip = @("1", 1) } }
    "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $negative; clip = @("1", 1) } }
    "4" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 1536; height = 640; batch_size = 1 } }
    "5" = @{ class_type = "KSampler"; inputs = @{ model = @("1", 0); positive = @("2", 0); negative = @("3", 0); latent_image = @("4", 0); seed = $Seed; steps = $Steps; cfg = 6.0; sampler_name = "dpmpp_2m"; scheduler = "karras"; denoise = 1.0 } }
    "6" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("5", 0); vae = @("1", 2) } }
    "7" = @{ class_type = "SaveImage"; inputs = @{ images = @("6", 0); filename_prefix = "sheet" } }
}

$body = @{ prompt = $wf; client_id = "codex" } | ConvertTo-Json -Depth 12 -Compress
Write-Host "[sheet] 提交（1536x640，一张图 4 帧，种子 $Seed）……"
$res = Invoke-RestMethod -Uri "$Api/prompt" -Method Post -ContentType "application/json" -Body $body -TimeoutSec 60
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
            $dst = Join-Path $SheetDir "sheet_raw.png"
            Copy-Item -LiteralPath $src -Destination $dst -Force
            Write-Host "[sheet] 已保存：$dst"
            exit 0
        }
    }
    catch { }
}
Write-Host "[sheet] 超时"
exit 1
