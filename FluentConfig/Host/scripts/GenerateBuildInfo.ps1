# Generates FluentConfigBuildInfo.g.cs with the build-time RepoUrl.
param(
    [Parameter(Mandatory = $true)][string]$OutPath,
    [Parameter(Mandatory = $true)][string]$RepoUrl
)

$dir = Split-Path -Parent $OutPath
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir | Out-Null
}

$escaped = $RepoUrl.Replace('\', '\\').Replace('"', '\"')
$content = @"
namespace FluentConfig
{
    internal static class FluentConfigBuildInfo
    {
        public const string RepoUrl = "$escaped";
    }
}
"@

Set-Content -Path $OutPath -Value $content -Encoding UTF8
