param(
    [Parameter(Mandatory = $true)]
    [string] $ExePath
)

$resolvedExe = (Resolve-Path -LiteralPath $ExePath).Path
$progId = "Readmd.Markdown"
$applicationName = "Readmd.exe"
$classesRoot = "HKCU:\Software\Classes"
$capabilitiesRoot = "HKCU:\Software\Readmd\Capabilities"

function Set-DefaultValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [Parameter(Mandatory = $true)]
        [string] $Value
    )

    New-Item -Path $Path -Force | Out-Null
    Set-ItemProperty -Path $Path -Name "(default)" -Value $Value
}

Set-DefaultValue -Path "$classesRoot\$progId" -Value "Markdown Document"
Set-DefaultValue -Path "$classesRoot\$progId\DefaultIcon" -Value "`"$resolvedExe`",0"
Set-DefaultValue -Path "$classesRoot\$progId\shell\open\command" -Value "`"$resolvedExe`" `"%1`""

New-Item -Path "$classesRoot\.md\OpenWithProgids" -Force | Out-Null
New-ItemProperty -Path "$classesRoot\.md\OpenWithProgids" -Name $progId -Value ([byte[]]@()) -PropertyType Binary -Force | Out-Null

Set-DefaultValue -Path "$classesRoot\Applications\$applicationName\shell\open\command" -Value "`"$resolvedExe`" `"%1`""
New-Item -Path "$classesRoot\Applications\$applicationName\SupportedTypes" -Force | Out-Null
Set-ItemProperty -Path "$classesRoot\Applications\$applicationName\SupportedTypes" -Name ".md" -Value ""

New-Item -Path "$capabilitiesRoot\FileAssociations" -Force | Out-Null
Set-ItemProperty -Path $capabilitiesRoot -Name "ApplicationName" -Value "Readmd"
Set-ItemProperty -Path $capabilitiesRoot -Name "ApplicationDescription" -Value "Read-only Markdown viewer"
Set-ItemProperty -Path "$capabilitiesRoot\FileAssociations" -Name ".md" -Value $progId

New-Item -Path "HKCU:\Software\RegisteredApplications" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\RegisteredApplications" -Name "Readmd" -Value "Software\Readmd\Capabilities"

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class ShellNotify
{
    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
}
"@

[ShellNotify]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)

Write-Host "Readmd has been registered for .md Open With entries."
Write-Host "Use Windows Settings > Apps > Default apps, or Explorer > Open with, to set it as the default Markdown handler."
