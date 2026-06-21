resource "azurerm_storage_account" "main" {
  name                     = "stweatherapp${var.environment}"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  https_traffic_only_enabled = true
  min_tls_version = "TLS1_2"

   tags = {
    environment =  var.environment
    project     = "weatherapp"
    managed_by   = "terraform"
  }
}

// container storage
resource "azurerm_storage_container" "main" {
  name                  = "assets"
  storage_account_id   = azurerm_storage_account.main.id
  container_access_type = "private"
}