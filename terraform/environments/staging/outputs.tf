output "aks_cluster_name" {
  description = "Name of the cluster"
  value       = module.aks.cluster_name
}
output "acr_login_server" {
  description = "login of cluster"
  value       = module.acr.login_server
}
output "bastion_ip" {
  description = "bastion ip"
  value       = module.vm.public_ip
}
output "sql_connection_string" {
  description = "connection string"
  value       = module.database.connection_string
  sensitive   = true
}