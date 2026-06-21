output "storage_account_name" {
  description = "name of storage accoung"
  value       = azurerm_storage_account.main.name
}

output "primary_connection_string" {
  description = "connection string storage account"
  value       = azurerm_storage_account.main.primary_connection_string
  sensitive   = true
}