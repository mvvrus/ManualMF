function get_adfs_cert_hash()
{
  return (Get-AdfsCertificate -CertificateType "Service-Communications").Certificate.GetCertHash()
}

function get_adfs_service_account()
{
 return (Get-WmiObject -Class win32_service -Filter "Name='adfssrv'").StartName
}

function get_adfs_cert_thumbprint()
{
  return (Get-AdfsCertificate -CertificateType "Service-Communications").Thumbprint
}

function get_adfs_fqdn()
{
  return (Get-AdfsProperties).HostName
}
