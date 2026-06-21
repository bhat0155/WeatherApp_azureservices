
output "acr_id" {
    description = "acr Id of acr"
    value = azurerm_container_registry.main.id
}


output "login_server" {
    description = "login server of acr"
    value = azurerm_container_registry.main.login_server
}

output "admin_username" {
    description = "admin username of arc"
    value = azurerm_container_registry.main.admin_username
}

output "admin_password" {
    description = "admin password of arc"
    value = azurerm_container_registry.main.admin_password
    sensitive = true
}
