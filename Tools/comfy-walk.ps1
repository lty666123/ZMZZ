[CmdletBinding()]
param(
    [string]$PortableRoot = "E:\AI\ComfyUI_portable\ComfyUI_windows_portable_nvidia_cu128\ComfyUI_windows_portable",
    [string]$Launcher     = "run_nvidia_gpu_fast_fp16_accumulation.bat",
    [string]$WorkRoot     = "E:\unity-codex\2D ZMZZ\.verify\gen",
    [string]$Ckpt         = "waiIllustriousSDXL_v170.safetensors",
    [int]$Seed            = 777001,
    [int]$Steps           = 22,
    [double]$Denoise      = 0.75,
    [string]$Prefix       = "walk2"
)

# 生成一套「能连起来播的」行走逐帧序列。
# 第 1 帧正常出图当基准；后面每帧都以第 1 帧为底图做 img2img（denoise 低），
# 这样角色的脸和服装被锁住，只有姿势在变。

$ErrorActionPreference = "Stop"
$Api       = "http://127.0.0.1:8188"
$BatPath   = Join-Path $PortableRoot $Launcher
$OutputDir = Join-Path $PortableRoot "ComfyUI\output"
$InputDir  = Join-Path $PortableRoot "ComfyUI\input"
$WalkDir   = Join-Path $WorkRoot $Prefix
$BasePng   = Join-Path $InputDir ($Prefix + "_base.png")

# 角色固定部分（每帧都一样）
# 注意：这里故意不写"持剑"，因为剑和披风会挡住腿，看不出步态
$char = "1girl, solo, knight, silver hair, long straight hair, blue eyes, " +
        "blue and white armored dress, silver breastplate, white cape, " +
        "silver gauntlets, black thigh boots, " +
        "side view, facing right, full body, white background, simple background, " +
        "masterpiece, best quality, very aesthetic, absurdres"

# 6 帧行走循环：跨步 <-> 并腿过渡，交替出现，动作差别做大
$phases = @(
    "walking, left leg forward, right leg back, legs wide apart, mid-stride, arms swinging",
    "walking, legs together, right foot lifted, passing pose, arms at sides",
    "walking, right leg forward, left leg back, legs wide apart, mid-stride, arms swinging",
    "walking, legs together, left foot lifted, passing pose, arms at sides",
    "walking, left leg forward, right leg back, legs wide apart, mid-stride, arms swinging",
    "walking, legs together, right foot lifted, passing pose, arms at sides"
)

$negative = (Get-Content -LiteralPath (Join-Path $WorkRoot "negative.txt") -Raw -Encoding UTF8).Trim()

function Test-Comfy {
    try { Invoke-WebRequest -Uri "$Api/system_stats" -TimeoutSec 5 -UseBasicParsing | Out-Null; return $true }
    catch { return $false }
}

if (-not (Test-Comfy)) {
    Write-Host "[walk] 启动 ComfyUI：$Launcher"
    Start-Process -FilePath $BatPath -WorkingDirectory $PortableRoot -WindowStyle Hidden | Out-Null
    $deadline = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline) { Start-Sleep -Seconds 5; if (Test-Comfy) { break } }
}
if (-not (Test-Comfy)) { Write-Host "[walk] 服务没起来，退出"; exit 1 }
Write-Host "[walk] 服务已就绪（$Ckpt / $Steps 步 / 种子 $Seed / 重绘 $Denoise）"

New-Item -ItemType Directory -Force -Path $WalkDir, $InputDir | Out-Null

function Submit-And-Wait($wf) {
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

for ($i = 0; $i -lt $phases.Count; $i++) {
    $idx = "{0:d2}" -f $i
    $positive = $char + ", " + $phases[$i]

    if ($i -eq 0) {
        # 第一帧：正常出图
        $wf = @{
            "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $Ckpt } }
            "2" = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $positive; clip = @("1", 1) } }
            "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $negative; clip = @("1", 1) } }
            "4" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = 768; height = 1152; batch_size = 1 } }
            "5" = @{ class_type = "KSampler"; inputs = @{ model = @("1", 0); positive = @("2", 0); negative = @("3", 0); latent_image = @("4", 0); seed = $Seed; steps = $Steps; cfg = 6.0; sampler_name = "dpmpp_2m"; scheduler = "karras"; denoise = 1.0 } }
            "6" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("5", 0); vae = @("1", 2) } }
            "7" = @{ class_type = "SaveImage"; inputs = @{ images = @("6", 0); filename_prefix = ($Prefix + "_" + $idx) } }
        }
    }
    else {
        # 后续帧：以第一帧为底图做 img2img
        $wf = @{
            "1"  = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $Ckpt } }
            "10" = @{ class_type = "LoadImage"; inputs = @{ image = ($Prefix + "_base.png") } }
            "11" = @{ class_type = "VAEEncode"; inputs = @{ pixels = @("10", 0); vae = @("1", 2) } }
            "2"  = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $positive; clip = @("1", 1) } }
            "3"  = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $negative; clip = @("1", 1) } }
            "5"  = @{ class_type = "KSampler"; inputs = @{ model = @("1", 0); positive = @("2", 0); negative = @("3", 0); latent_image = @("11", 0); seed = $Seed; steps = $Steps; cfg = 6.0; sampler_name = "dpmpp_2m"; scheduler = "karras"; denoise = $Denoise } }
            "6"  = @{ class_type = "VAEDecode"; inputs = @{ samples = @("5", 0); vae = @("1", 2) } }
            "7"  = @{ class_type = "SaveImage"; inputs = @{ images = @("6", 0); filename_prefix = ($Prefix + "_" + $idx) } }
        }
    }

    Write-Host "[walk] 第 $idx 帧 ……  $(Get-Date -Format 'HH:mm:ss')"
    $src = Submit-And-Wait $wf
    if ($src -and (Test-Path -LiteralPath $src)) {
        $dst = Join-Path $WalkDir ($Prefix + "_" + $idx + ".png")
        Copy-Item -LiteralPath $src -Destination $dst -Force
        if ($i -eq 0) { Copy-Item -LiteralPath $src -Destination $BasePng -Force }
        Write-Host "[walk]   ok -> $dst"
    }
    else {
        Write-Host "[walk]   !! 第 $idx 帧失败"
    }
}
Write-Host "[walk] 序列完成，共 $($phases.Count) 帧，目录：$WalkDir"
