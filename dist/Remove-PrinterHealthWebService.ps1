[CmdletBinding()]
Param()

$serviceName = "PrinterHealthWeb"

# check if the service management script is available
$curDir = (Get-Location).Path
$rs = Join-Path -Path $curDir -ChildPath "Remove-Service.ps1" -Resolve -ErrorAction SilentlyContinue
If ($rs -eq $null)
{
    Throw "This script requires the Remove-Service.ps1 script from the PowerShell Admin Utils to be in the current directory."
}

& .\Remove-Service.ps1 -ServiceName $serviceName
