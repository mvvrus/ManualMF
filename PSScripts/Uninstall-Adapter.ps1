Add-Type -Assembly System.EnterpriseServices
$publish = New-Object System.EnterpriseServices.Internal.Publish
$AssemblyDir = Split-Path $MyInvocation.InvocationName | Join-Path -ChildPath 'ManualMFAdapter' | Resolve-Path
$ass=[System.Reflection.Assembly]::LoadFile((Join-Path -Path $AssemblyDir -ChildPath 'ManualMFAdapter.dll'))
$providers=Get-AdfsGlobalAuthenticationPolicy | Select -Expand AdditionalAuthenticationProvider | Where {$_ -ne 'ManualMFAdapter'}
Set-AdfsGlobalAuthenticationPolicy -AdditionalAuthenticationProvider $providers
Unregister-AdfsAuthenticationProvider ManualMFAdapter  -Confirm:$false
Restart-Service adfssrv
Get-ChildItem $AssemblyDir -Attributes Directory | Select-Object -Expand Name | ForEach-Object {$publish.GacRemove((Join-Path -Path $AssemblyDir -ChildPath "$($_)\ManualMFAdapter.resources.dll"))}
$publish.GacRemove((Join-Path -Path $AssemblyDir -ChildPath 'ManualMFAdapter.dll'))
