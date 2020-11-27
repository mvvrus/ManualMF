# Constants
$SITENAME="Default Web Site"
$BINDING = '*:443:'
$POOLNAME = 'ManualMFPool'
$PASSWORD = ''
$CONNAME = 'Default'
$CONNSTRING = ''
$OPERATORS = "ManualMF_Operators"

function create_application($site,$path,$physicalPath,$poolname)
 {
 if(($site.Applications | Where-Object {$_.path -eq $path }) -eq $null) {
  $app = $site.Applications.CreateElement()
  $app.SetAttributeValue("path", "$path")
  if($poolname -eq $null) {$poolname = "DefaultAppPool";}
  $app.SetAttributeValue("applicationPool", $poolname)
  [Void]$site.Applications.Add($app)
  $vdir = $app.VirtualDirectories.CreateElement();
  $vdir.SetAttributeValue("path", "/");
  $vdir.SetAttributeValue("physicalPath", $physicalPath )
  [Void]$app.VirtualDirectories.Add($vdir)
 }
}

function set_connection_string($path) {
  $webcfg = $mgr.GetWebConfiguration($SITENAME,$path)
  $cstrs = $webcfg.GetSection('connectionStrings')
  $connstr = $cstrs.GetCollection() | Where-Object {$_['name'] -eq $CONNAME}
  if($connstr -eq $null) {
    $connstr = ($cstrs.GetCollection()).CreateElement('add')
    $connstr['name'] = $CONNAME
    $connstr['providerName'] = 'System.Data.SqlClient'  
    $connstr['connectionString'] = $CONNSTRING
    [void]($cstrs.GetCollection()).Add($connstr)
  }
  else {$connstr['connectionString'] = $CONNSTRING}
}

function set_app_setting($path,$key,$value) {
  $webcfg = $mgr.GetWebConfiguration($SITENAME,$path)
  $appset = $webcfg.GetSection('appSettings')
  $keyel = $appset.GetCollection() | Where-Object {$_['key'] -eq $key}
  if($keyel -eq $null) {
    $keyel = ($appset.GetCollection()).CreateElement('add')
    $keyel['key'] = $key
    $keyel['value'] = $value
    [void]($appset.GetCollection()).Add($keyel)
  }
  else {$keyel['value'] = $value}
}

# Load ADFS information functions and database parameters
if($ScriptDir -eq $null) 
{
  $ScriptDir = Split-Path $MyInvocation.InvocationName | Resolve-Path
}
. (Join-Path -Path $ScriptDir -ChildPath "Get-DBParameters.ps1") 
. (Join-Path -Path $ScriptDir -ChildPath "Get-AdfsInfo.ps1") 
$CONNSTRING = "Server=$SQLSERVER;Integrated Security=true;Database=$DATABASE_NAME"
# Get web site object
$webadm = [System.Reflection.Assembly]::LoadWithPartialName("Microsoft.Web.Administration")
$mgr = New-Object Microsoft.Web.Administration.ServerManager
#Create application pool
$pool = $mgr.ApplicationPools[$POOLNAME]
if($pool -eq $null) {
 $pool = $mgr.ApplicationPools.Add($POOLNAME)
 $pool.ProcessModel.IdentityType = 'SpecificUser'
 $pool.ProcessModel.UserName = ( get_adfs_service_account )
 $pool.ProcessModel.Password = $PASSWORD
}
#
$site=$mgr.Sites[$SITENAME]
# Configure SSL binding using ADFS service communication certificate
if(($site.Bindings | Where-Object {$_.BindingInformation -eq "$BINDING"}) -eq $null) {
 [Void]$site.Bindings.Add("$BINDING", (get_adfs_cert_hash),"My")
}
# Create an application for the AccessWaiterEP API endpoint
create_application $site '/API' (Join-Path -Path $ScriptDir -ChildPath 'AccessWaiterEP\') $POOLNAME
# Create the ManMFOperator application
create_application $site '/operator' (Join-Path -Path $ScriptDir -ChildPath 'ManMFOperator\') $POOLNAME
# Configure use of the Windows authentication for the ManMFOperator application
$ah=$mgr.GetApplicationHostConfiguration()
$winauth=$ah.GetSection("system.webServer/security/authentication/windowsAuthentication","$SITENAME/operator")
$winauth.SetAttributeValue("enabled","true")
$winauth.SetAttributeValue("useKernelMode","false")
$anonauth=$ah.GetSection("system.webServer/security/authentication/anonymousAuthentication","$SITENAME/operator")
$anonauth.SetAttributeValue("enabled","false")
$mgr.CommitChanges()
$mgr = New-Object Microsoft.Web.Administration.ServerManager
#Set connection string for AccessWaiterEP application 
set_connection_string  '/API'
#Set connection string for ManMFOperator application 
set_connection_string  '/operator'
#Set value for the group, containing operators (key="operators") in applicationSetting for ManMFOperator application
set_app_setting '/operator' "operators" $OPERATORS

$mgr.CommitChanges()

#Create group, containing operators (if required) and add the current user to it
$opgroup=Get-ADGroup -filter {name -eq $OPERATORS}
if($opgroup -eq $null) {
  $opgroup = New-ADGroup $OPERATORS -GroupScope DomainLocal -PassThru
}
$curuser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
if(($curuser | select -expand groups | ?{$_.Value -eq $opgroup.SID}) -eq $null) {
  Add-ADGroupMember $OPERATORS ($curuser.User.Value)
}
