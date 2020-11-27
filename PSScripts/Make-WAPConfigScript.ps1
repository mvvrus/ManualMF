$TIMEOUT = 1200

if($ScriptDir -eq $null) 
{
  $ScriptDir = Split-Path $MyInvocation.InvocationName | Resolve-Path
}
. (Join-Path -Path $ScriptDir -ChildPath "Get-AdfsInfo.ps1") 
$adfsfqdn = get_adfs_fqdn
$apiurl="https://$adfsfqdn/api/AccessWaiterEP/"
$thumbprint = get_adfs_cert_thumbprint
$script = "Add-WebApplicationProxyApplication -BackendServerUrl '$apiurl' -ExternalCertificateThumbprint '$thumbprint' -ExternalUrl '$apiurl' -Name 'AccessWaiter' -ExternalPreAuthentication PassThrough", `
          "Get-WebApplicationProxyApplication AccessWaiter | Set-WebApplicationProxyApplication -InactiveTransactionsTimeoutSec $TIMEOUT"
Set-Content -Path ((Join-Path -Path $ScriptDir -ChildPath "Configure-ApiWapPublication.ps1")).ToString() -Value $script
