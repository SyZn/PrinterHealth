[CmdletBinding()]
Param(
    [PSCredential]
    $ServiceLogin,

    [string]
    $ServiceDLLPath=""
)

$serviceDllName = "PrinterHealthWebService.dll"
$serviceName = "PrinterHealthWeb"
$serviceDisplayName = "PrinterHealthWeb"
$serviceDescription = "EDVLAB: Displays the health of printers in a web interface."

$curDir = (Get-Location).Path
$idncs = Join-Path -Path $curDir -ChildPath "Install-DotNetCoreService.ps1" -Resolve -ErrorAction SilentlyContinue
If ($idncs -eq $null)
{
    Throw "This script requires the Install-DotNetCoreService.ps1 script from the PowerShell Admin Utils to be in the current directory."
}

& .\Install-DotNetCoreService.ps1 `
    -ServiceLogin $ServiceLogin `
    -ServiceDLLPath $ServiceDLLPath `
    -ServiceDLLName $serviceDLLName `
    -ServiceName $serviceName `
    -ServiceDisplayName $serviceDisplayName `
    -ServiceDescription $serviceDescription
