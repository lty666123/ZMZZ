[CmdletBinding()]
param(
    [string]$PortableRoot = "E:\AI\ComfyUI_portable\ComfyUI_windows_portable_nvidia_cu128\ComfyUI_windows_portable",
    [string]$Launcher     = "run_nvidia_gpu_fast_fp16_accumulation.bat",
    [string]$WorkRoot     = "E:\unity-codex\2D ZMZZ\.verify\gen",
    [string]$Ckpt         = "waiIllustriousSDXL_v170.safetensors",
    [int]$Seed            = 777001,
    [int]$Width           = 832,
    [int]$Height          = 1216,
    [int]$Steps           = 28,
    [double]$Cfg          = 6.0
)

# 用便携版 ComfyUI 的 HTTP API 出一张二次元角色立绘。
# 工作流是标准 SDXL txt2img（只用核心节点），模型用 E 盘里那套 waiIllustriousSDXL。

$ErrorActionPreference = "Stop"
$Api       = "http://127.0.0.1:8188"
$BatPath   = Join-Path $PortableRoot $Launcher
$OutputDir = Join-Path $PortableRoot "ComfyUI\output"

function Test-Comfy {
    try { Invoke-WebRequest -Uri "$Api/system_stats" -TimeoutSec 5 -UseBasicParsing | Out-Null; return $true }
    catch { return $false }
}

if (-not (Test-Comfy)) {
    Write-Host "[comfy] 启动便携版：$Launcher"
    Start-Process -FilePath $BatPath -WorkingDirectory $PortableRoot -WindowStyle Hidden | Out-Null
    $deadline = (Get-Date).AddSeconds(300)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 5
        if (Test-Comfy) { break }
    }
}
if (-not (Test-Comfy)) { Write-Host "[comfy] 服务没起来"; exit 1 }
Write-Host "[comfy] 服务已就绪"

$positive = (Get-Content -LiteralPath (Join-Path $WorkRoot "positive.txt") -Raw -Encoding UTF8).Trim()
$negative = (Get-Content -LiteralPath (Join-Path $WorkRoot "negative.txt") -Raw -Encoding UTF8).Trim()

$wf = @{
    "1" = @{ class_type = "CheckpointLoaderSimple"; inputs = @{ ckpt_name = $Ckpt } }
    "2" = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $positive; clip = @("1", 1) } }
    "3" = @{ class_type = "CLIPTextEncode"; inputs = @{ text = $negative; clip = @("1", 1) } }
    "4" = @{ class_type = "EmptyLatentImage"; inputs = @{ width = $Width; height = $Height; batch_size = 1 } }
    "5" = @{ class_type = "KSampler"; inputs = @{ model = @("1", 0); positive = @("2", 0); negative = @("3", 0); latent_image = @("4", 0); seed = $Seed; steps = $Steps; cfg = $Cfg; sampler_name = "dpmpp_2m"; scheduler = "karras"; denoise = 1.0 } }
    "6" = @{ class_type = "VAEDecode"; inputs = @{ samples = @("5", 0); vae = @("1", 2) } }
    "7" = @{ class_type = "SaveImage"; inputs = @{ images = @("6", 0); filename_prefix = "role" } }
}

$body = @{ prompt = $wf; client_id = "codex" } | ConvertTo-Json -Depth 12 -Compress

Write-Host "[comfy] 提交任务：$Ckpt  ${Width}x${Height}  steps=$Steps cfg=$Cfg seed=$Seed"
try {
    $res = Invoke-RestMethod -Uri "$Api/prompt" -Method Post -ContentType "application/json" -Body $body -TimeoutSec 60
}
catch {
    Write-Host "[comfy] 提交失败：" + $_.Exception.Message
    if ($_.ErrorDetails) { Write-Host $_.ErrorDetails.Message }
    exit 1
}

$promptId = $res.prompt_id
Write-Host "[comfy] prompt_id = $promptId"

$deadline = (Get-Date).AddMinutes(30)
$outputs = $null
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    try {
        $h = Invoke-RestMethod -Uri "$Api/history/$promptId" -TimeoutSec 20
        $entry = $h.$promptId
        if ($entry) {
            if ($entry.status -and $entry.status.status_str -eq "error") {
                Write-Host "[comfy] 节点报错："
                $entry.status | ConvertTo-Json -Depth 8
                exit 1
            }
            if ($entry.outputs) { $outputs = $entry.outputs; break }
        }
    }
    catch { }
    Write-Host "[comfy] 生成中……"
}
if (-not $outputs) { Write-Host "[comfy] 超时未完成"; exit 1 }

$img = $outputs."7".images[0]
$src = Join-Path $OutputDir $img.filename
if ($img.subfolder) { $src = Join-Path (Join-Path $OutputDir $img.subfolder) $img.filename }
Write-Host "[comfy] 输出：$src"

New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null
$dst = Join-Path $WorkRoot ("role_seed$Seed.png")
Copy-Item -LiteralPath $src -Destination $dst
Write-Host "[comfy] 已保存：$dst"
