output "cluster_name" {
  description = "Name of the cluster"
  value       = azurerm_kubernetes_cluster.main.name
}

output "kube_config" {
  description = "kube config"
  value       = azurerm_kubernetes_cluster.main.kube_config_raw
  sensitive   = true
}