<#
.SYNOPSIS
Publishes Readmd and refreshes its per-user `.md` file association.

.DESCRIPTION
Publishes the Release build to the stable artifacts directory used by Windows
Open With, then registers that exact executable for Markdown files. Run this
script whenever publishing a new local version so Explorer does not keep
launching an older build.

.PARAMETER OutputPath
Destination for the published application. Defaults to artifacts\Readmd in the
repository root.

.EXAMPLE
.\scripts\Publish-Readmd.ps1

Publishes and registers the current Readmd source in one step.
#>

[CmdletBinding()]
param(
    [string] $OutputPath
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot "artifacts\Readmd"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot $OutputPath
}

$projectPath = Join-Path $repositoryRoot "src\Readmd"
$registrationScript = Join-Path $PSScriptRoot "Register-ReadmdFileAssociation.ps1"

Write-Host "Publishing Readmd to $OutputPath..."
& dotnet publish $projectPath -c Release -o $OutputPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$executablePath = Join-Path $OutputPath "Readmd.exe"
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Published executable was not found at $executablePath."
}

& $registrationScript -ExePath $executablePath

Write-Host "Publish complete. Windows Open With now uses:"
Write-Host $executablePath
