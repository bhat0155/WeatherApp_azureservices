// creating a kubernetes cluster
resource "azurerm_kubernetes_cluster" "main" {
  name                = "aks-weatherapp-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = "weatherapp-${var.environment}"

  default_node_pool {
    name           = "agentpool"
    node_count     = var.node_count
    vm_size        = var.vm_size
    vnet_subnet_id = var.aks_subnet_id
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin = "kubenet"
    service_cidr   = "10.1.0.0/16"
    dns_service_ip = "10.1.0.10"
  }

  tags = {
    environment = var.environment
    project     = "weatherapp"
    managed_by  = "terraform"
  }
}



// AKS permission to pull from ACR
resource "azurerm_role_assignment" "main" {
  scope                            = var.acr_id
  role_definition_name             = "AcrPull"
  principal_id                     = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
  skip_service_principal_aad_check = true
}