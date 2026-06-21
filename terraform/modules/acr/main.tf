resource "azurerm_container_registry" "main" {
  name                = "acrweatherapp${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "Basic"
  admin_enabled       = true

  tags = {
    environment = var.environment
    project     = "weatherapp"
    managed_by  = "terraform"
  }
}
