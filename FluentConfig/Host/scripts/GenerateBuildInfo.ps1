# Generates FluentConfigBuildInfo.g.cs with the build-time RepoUrl and framework version.
param(
    [Parameter(Mandatory = $true)][string]$OutPath,
    [Parameter(Mandatory = $true)][string]$RepoUrl,
    [Parameter(Mandatory = $true)][string]$Version
)

$dir = Split-Path -Parent $OutPath
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir | Out-Null
}

$escaped = $RepoUrl.Replace('\', '\\').Replace('"', '\"')
$escapedVersion = $Version.Replace('\', '\\').Replace('"', '\"')
$content = @"
namespace FluentConfig
{
    internal static class FluentConfigBuildInfo
    {
        public const string RepoUrl = "$escaped";
        public const string FrameworkVersion = "$escapedVersion";
    }
}
"@

Set-Content -Path $OutPath -Value $content -Encoding UTF8
