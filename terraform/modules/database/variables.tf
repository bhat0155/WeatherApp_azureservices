
variable "environment" {
  type        = string
  description = "The environment for the app (e.g., staging, production)"
}

variable "location" {
  type        = string
  description = "The Azure region to deploy resources in (e.g., eastus, westus2)"
}

variable "resource_group_name" {
  type        = string
  description = "The name of the resource group to create"
}

variable "sql_admin_password" {
    type = string
    sensitive = true
}