Add-Type -Assembly System.EnterpriseServices
if($ScriptDir -eq $null) 
{
  $ScriptDir = Split-Path $MyInvocation.InvocationName | Resolve-Path
}
$publish = New-Object System.EnterpriseServices.Internal.Publish
$AssemblyDir = $ScriptDir | Join-Path -ChildPath 'ManualMFAdapter' 
$ass=[System.Reflection.Assembly]::LoadFile((Join-Path -Path $AssemblyDir -ChildPath 'ManualMFAdapter.dll'))
$fn = $ass.FullName
$publish.GacInstall((Join-Path -Path $AssemblyDir -ChildPath 'ManualMFAdapter.dll'))
Get-ChildItem $AssemblyDir -Attributes Directory | Select-Object -Expand Name | ForEach-Object {$publish.GacInstall((Join-Path -Path $AssemblyDir -ChildPath "$($_)\ManualMFAdapter.resources.dll"))}

. (Join-Path -Path $ScriptDir -ChildPath "Get-DBParameters.ps1") 
$CONNSTRING = "Server=$SQLSERVER;Integrated Security=true;Database=$DATABASE_NAME"
$xml = New-Object 'System.Configuration.ConfigXmlDocument'
$configfile = Join-Path $AssemblyDir -ChildPath 'ManualMFAdapter.config'
$xml.Load($configfile)
$connNode = $xml.SelectSingleNode('/configuration/connectionStrings/add[@name="Default"]')
$connNode.SetAttribute('connectionString',$CONNSTRING)
$xml.Save($configfile)

Register-AdfsAuthenticationProvider -TypeName "ManualMF.ManualMFAdapter, $fn" -Name ManualMFAdapter -ConfigurationFilePath $configfile
Restart-Service adfssrv
Set-AdfsGlobalAuthenticationPolicy -AdditionalAuthenticationProvider @('ManualMFAdapter')

$garule = Get-AdfsAdditionalAuthenticationRule
if ($garule.IndexOf("http://schemas.microsoft.com/claims/multipleauthn") -lt 0) {
  $garule += 'c:[Type == "http://schemas.microsoft.com/ws/2012/01/insidecorporatenetwork", Value == "false"] => issue(Type = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod", Value = "http://schemas.microsoft.com/claims/multipleauthn");'
  Set-AdfsAdditionalAuthenticationRule -AdditionalAuthenticationRules $garule
}




