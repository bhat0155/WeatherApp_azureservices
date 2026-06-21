output "vnet_id" {
  description = "id of virtual network"
  value       = azurerm_virtual_network.main.id
}

output "aks_subnet_id" {
  description = "subnet Id of AKS"
  value       = azurerm_subnet.aks.id
}

output "vm_subnet_id" {
  description = "subnet Id of VM"
  value       = azurerm_subnet.vm.id
}