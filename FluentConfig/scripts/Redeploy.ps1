<#
.SYNOPSIS
  Rebuilds FluentConfig.dll and redeploys it into a running Streamer.bot install,
  restarting Streamer.bot to release its file lock on the DLL.

.PARAMETER StreamerBotPath
  Path to the Streamer.bot install folder. Auto-detected from common locations if omitted.

.PARAMETER Configuration
  Build configuration (Debug or Release). Defaults to Debug.

.PARAMETER PerfTrace
  Force-enable FC_PERF_TRACE (-p:FluentConfigPerfTrace=true) regardless of Configuration.
  Useful for troubleshooting a Release build without switching to the Vite dev server.

.PARAMETER Watch
  If set, watches Host\**\*.cs for changes and re-runs the rebuild+redeploy loop automatically
  (debounced ~1.5s) instead of running once and exiting. Ctrl+C to stop.

.PARAMETER WhatIf
  Show what would happen without stopping Streamer.bot, building, copying, or relaunching.

.EXAMPLE
  .\Redeploy.ps1
.EXAMPLE
  .\Redeploy.ps1 -Configuration Release
.EXAMPLE
  .\Redeploy.ps1 -Configuration Release -PerfTrace
.EXAMPLE
  .\Redeploy.ps1 -Watch
.EXAMPLE
  .\Redeploy.ps1 -WhatIf
#>
param(
    [string]$StreamerBotPath,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$PerfTrace,
    [switch]$Watch,
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot   # FluentConfig\
$hostProj = Join-Path $repoRoot 'Host\Host.csproj'

function Resolve-StreamerBotPath {
    param([string]$Explicit)
    if ($Explicit -and (Test-Path $Explicit)) { return $Explicit }
    $candidates = @(
        "F:\Stream\Streamer.bot-x64-1.0.7"
    )
    if ($env:STREAMER_BOT_PATH) {
        $candidates = @($env:STREAMER_BOT_PATH) + $candidates
    }
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c 'Microsoft.Web.WebView2.Wpf.dll')) { return $c }
    }
    throw "Could not auto-detect Streamer.bot install path. Pass -StreamerBotPath explicitly."
}

function Invoke-RedeployOnce {
    $sbPath = Resolve-StreamerBotPath -Explicit $StreamerBotPath
    $dllsPath = Join-Path $sbPath 'dlls'
    if (-not (Test-Path $dllsPath)) {
        if ($WhatIf) { Write-Host "WhatIf: would create $dllsPath" -ForegroundColor DarkYellow }
        else { New-Item -ItemType Directory -Path $dllsPath | Out-Null }
    }

    Write-Host "==> Streamer.bot: $sbPath" -ForegroundColor Cyan
    if ($WhatIf) { Write-Host "==> WhatIf: dry-run only (no stop/build/copy/relaunch)" -ForegroundColor DarkYellow }

    $proc = Get-Process -Name 'Streamer.bot' -ErrorAction SilentlyContinue
    if ($proc) {
        if ($WhatIf) {
            Write-Host "WhatIf: would stop Streamer.bot (pid $($proc.Id))" -ForegroundColor DarkYellow
        }
        else {
            Write-Host "==> Stopping Streamer.bot (pid $($proc.Id))..." -ForegroundColor Yellow
            Stop-Process -Id $proc.Id -Force
            $proc.WaitForExit(10000) | Out-Null
            Start-Sleep -Milliseconds 500   # let file handles release
        }
    }

    $buildArgs = @(
        'build', $hostProj,
        '-c', $Configuration,
        "-p:StreamerBotPath=$sbPath"
    )
    if ($PerfTrace) { $buildArgs += '-p:FluentConfigPerfTrace=true' }

    if ($WhatIf) {
        Write-Host ("WhatIf: would run: dotnet " + ($buildArgs -join ' ')) -ForegroundColor DarkYellow
        Write-Host "WhatIf: would copy FluentConfig.dll (+ Newtonsoft.Json.dll if present) -> $dllsPath" -ForegroundColor DarkYellow
    }
    else {
        Write-Host "==> Building ($Configuration)$(if ($PerfTrace) { ' + PerfTrace' })..." -ForegroundColor Cyan
        & dotnet @buildArgs | Write-Host
        if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

        $outDir = Join-Path $repoRoot "Host\bin\$Configuration\net481"
        Write-Host "==> Deploying FluentConfig.dll -> $dllsPath" -ForegroundColor Cyan
        Copy-Item (Join-Path $outDir 'FluentConfig.dll') $dllsPath -Force
        if (Test-Path (Join-Path $outDir 'Newtonsoft.Json.dll')) {
            Copy-Item (Join-Path $outDir 'Newtonsoft.Json.dll') $dllsPath -Force
        }
        # Optional updater helper for StageUpdate swap-and-relaunch
        $helperSrc = Join-Path $repoRoot "UpdaterHelper\bin\$Configuration\net481\FluentConfig.UpdaterHelper.exe"
        if (-not (Test-Path $helperSrc)) {
            $helperProj = Join-Path $repoRoot 'UpdaterHelper\UpdaterHelper.csproj'
            if (Test-Path $helperProj) {
                dotnet build $helperProj -c $Configuration | Write-Host
            }
        }
        if (Test-Path $helperSrc) {
            Copy-Item $helperSrc $dllsPath -Force
            Write-Host "==> Deployed FluentConfig.UpdaterHelper.exe" -ForegroundColor Cyan
        }
        Remove-Item (Join-Path $dllsPath 'FluentConfig.dll.old') -ErrorAction SilentlyContinue
    }

    $exe = Get-ChildItem $sbPath -Filter 'Streamer.bot.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($WhatIf) {
        if ($exe) { Write-Host "WhatIf: would relaunch $($exe.FullName)" -ForegroundColor DarkYellow }
        else { Write-Host "WhatIf: Streamer.bot.exe not found (would warn)" -ForegroundColor DarkYellow }
        Write-Host "==> WhatIf done." -ForegroundColor Green
        return
    }

    Write-Host "==> Relaunching Streamer.bot..." -ForegroundColor Cyan
    if ($exe) {
        Start-Process $exe.FullName
        Write-Host "==> Done." -ForegroundColor Green
    }
    else {
        Write-Warning "Streamer.bot.exe not found at $sbPath - start it manually."
    }
}

if (-not $Watch) {
    Invoke-RedeployOnce
    return
}

if ($WhatIf) {
    Write-Warning "Watch mode ignores -WhatIf for the loop; running a single WhatIf pass instead."
    Invoke-RedeployOnce
    return
}

Write-Host "==> Watch mode: saving any .cs file under Host\ triggers rebuild+redeploy (debounced). Ctrl+C to stop." -ForegroundColor Magenta
$hostDir = Join-Path $repoRoot 'Host'
$state = [hashtable]::Synchronized(@{ Pending = $false })
$fsw = New-Object System.IO.FileSystemWatcher($hostDir, '*.cs')
$fsw.IncludeSubdirectories = $true
$fsw.EnableRaisingEvents = $true

$onChange = { $Event.MessageData.Pending = $true }
Register-ObjectEvent $fsw Changed -Action $onChange -MessageData $state | Out-Null
Register-ObjectEvent $fsw Created -Action $onChange -MessageData $state | Out-Null
Register-ObjectEvent $fsw Renamed -Action $onChange -MessageData $state | Out-Null

$lastRun = Get-Date
try {
    while ($true) {
        Start-Sleep -Milliseconds 500
        if ($state.Pending -and ((Get-Date) - $lastRun).TotalMilliseconds -gt 1500) {
            $state.Pending = $false
            $lastRun = Get-Date
            try { Invoke-RedeployOnce } catch { Write-Warning $_ }
        }
    }
}
finally {
    Get-EventSubscriber | ForEach-Object { Unregister-Event -SubscriptionId $_.SubscriptionId }
    $fsw.Dispose()
}
