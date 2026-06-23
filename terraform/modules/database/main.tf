
// server
resource "azurerm_mssql_server" "main" {
  name                         = "sql-weatherapp-${var.environment}"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = "sqladmin"
  administrator_login_password = var.sql_admin_password


  tags = {
    environment = var.environment
    project     = "weatherapp"
    managed_by  = "terraform"
  }

}

// database
resource "azurerm_mssql_database" "main" {
  name      = "weatherapp-db-${var.environment}"
  server_id = azurerm_mssql_server.main.id
  sku_name  = "Basic"

   tags = {
    environment = var.environment
    project     = "weatherapp"
    managed_by  = "terraform"
  }
}

// firewall rule: allow azure resources to interact with db
resource "azurerm_mssql_firewall_rule" "main" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}