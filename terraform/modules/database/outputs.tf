output "server_fqdn" {
  description = "domain name of server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "connection_string" {
  value     = "Server=${azurerm_mssql_server.main.fully_qualified_domain_name};Database=weatherapp-db-${var.environment};User Id=sqladmin;Password=${var.sql_admin_password};TrustServerCertificate=True;"
  sensitive = true
}