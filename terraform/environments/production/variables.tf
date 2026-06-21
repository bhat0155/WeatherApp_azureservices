variable "sql_admin_password" {
  type        = string
  description = "sql password set via TF_VAR_sql_admin_password env in pipeline"
  sensitive   = true
}

variable "ssh_public_key" {
  type        = string
  description = "ssh key for bastion vm"
}