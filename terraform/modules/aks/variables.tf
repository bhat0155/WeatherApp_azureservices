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

variable "aks_subnet_id" {
  type        = string
  description = "subnetId from networking module"
}

variable "acr_id" {
  type        = string
  description = "acr resource id -grants aks pull access automatically"
}

variable "node_count" {
  type    = number
  default = 2
}

variable "vm_size" {
  type        = string
  description = "VM size for AKS nodes"
  default     = "Standard_D2as_v6"
}
