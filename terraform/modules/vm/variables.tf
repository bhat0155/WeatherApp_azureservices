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

variable "vm_subnet_id" {
  type        = string
  description = "The subnet id of vm"
}

variable "admin_username" {
  type        = string
  description = "admin username for vm"
  default = "azureuser"
}

variable "ssh_public_key" {
  type        = string
  description = "ssh key for vm access - injected from variable group"
}
