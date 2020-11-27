$ScriptDir = Split-Path $MyInvocation.InvocationName | Resolve-Path
Write-Host "Installing Web Server role"
. (Join-Path -Path $ScriptDir -ChildPath "IIS-prereq.ps1") 
Write-Host "Creating the database"
. (Join-Path -Path $ScriptDir -ChildPath "Create-Database.ps1") 
Write-Host "Installing the MFA adapter"
. (Join-Path -Path $ScriptDir -ChildPath "Install-Adapter.ps1") 
Write-Host "Installing web applications"
. (Join-Path -Path $ScriptDir -ChildPath "Install-Web.ps1") 
Write-Host "Creating Web Application Proxy configuration script"
. (Join-Path -Path $ScriptDir -ChildPath "Make-WAPConfigScript.ps1")
$WAPScript = (Join-Path -Path $ScriptDir -ChildPath "Configure-ApiWapPublication.ps1").ToString() 
Write-Host "Next step: copy the script file \"$WAPScript" to your Web Application Proxy server"
Write-Host "           and run it as Asministrator"
