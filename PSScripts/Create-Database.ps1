if($ScriptDir -eq $null) 
{
  $ScriptDir = Split-Path $MyInvocation.InvocationName | Resolve-Path
}
. (Join-Path -Path $ScriptDir -ChildPath "Get-DBParameters.ps1") 
. (Join-Path -Path $ScriptDir -ChildPath "Get-AdfsInfo.ps1") 

$conn = $null
function exec_nonquery($cmdtext) {
  $cmd = New-Object 'System.Data.SqlClient.SqlCommand' 
  $cmd.Connection = $conn
  $cmd.CommandText = $cmdtext
  [Void]$cmd.ExecuteNonQuery()
}

function check_exists($cmdtext) {
  $cmd = New-Object 'System.Data.SqlClient.SqlCommand' 
  $cmd.Connection = $conn
  $cmd.CommandText = $cmdtext
  $rdr=$cmd.ExecuteReader()
  $result = $rdr.HasRows
  $rdr.Close()
  return $result
}

$conn = New-Object 'System.Data.SqlClient.SqlConnection' "Server=$SQLSERVER;Integrated Security=true"
$conn.Open()
try {
  $dbo = get_adfs_service_account
  if( -not (check_exists "exec sp_helplogins [$dbo]") ) {
    exec_nonquery ("CREATE LOGIN [$dbo] FROM WINDOWS")
  }
  if( -not (check_exists "SELECT * FROM sys.databases WHERE name = '$DATABASE_NAME'") ) {
    exec_nonquery ("CREATE DATABASE [$DATABASE_NAME]")
  }
  exec_nonquery ("USE [$DATABASE_NAME]")
  exec_nonquery ("EXEC dbo.sp_changedbowner @loginame = N'$dbo'")
  if( (check_exists "SELECT * FROM sys.tables T,sys.schemas S WHERE S.name = 'dbo' AND S.schema_id = T.schema_id AND T.type = 'U' AND T.name='PERMISSIONS'") ) {
    exec_nonquery ("DROP TABLE [dbo].[PERMISSIONS]")
  }
  exec_nonquery ("CREATE TABLE [dbo].[PERMISSIONS]([UPN] [nvarchar](64) NOT NULL, [VALID_UNTIL] [datetime] NOT NULL, [FROM_IP] [varbinary](16) NULL, [REQUEST_STATE] [tinyint] NOT NULL, [EPACCESSTOKEN] [int] NULL, PRIMARY KEY CLUSTERED ([UPN] ASC))")
}
finally {
  $conn.Close();
}

