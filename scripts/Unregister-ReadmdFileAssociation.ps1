<#
.SYNOPSIS
Removes Readmd's per-user `.md` file association registration.

.DESCRIPTION
Deletes the HKCU registry entries created by `Register-ReadmdFileAssociation.ps1` and notifies the Windows shell that file association data changed. This does not remove the published application files.

.EXAMPLE
.\scripts\Unregister-ReadmdFileAssociation.ps1

Removes Readmd from `.md` Open With registration for the current user.
#>

$progId = "Readmd.Markdown"
$applicationName = "Readmd.exe"

Remove-Item -Path "HKCU:\Software\Classes\$progId" -Recurse -Force -ErrorAction SilentlyContinue
Remove-ItemProperty -Path "HKCU:\Software\Classes\.md\OpenWithProgids" -Name $progId -Force -ErrorAction SilentlyContinue
Remove-Item -Path "HKCU:\Software\Classes\Applications\$applicationName" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "HKCU:\Software\Readmd" -Recurse -Force -ErrorAction SilentlyContinue
Remove-ItemProperty -Path "HKCU:\Software\RegisteredApplications" -Name "Readmd" -Force -ErrorAction SilentlyContinue

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

Write-Host "Readmd file association entries were removed."
