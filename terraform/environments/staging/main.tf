# provider
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "tfstateweatherapp"
    container_name       = "tfstate-staging"
    key                  = "terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}

locals {
    environment = "staging"
    location    = "Canada Central"
}

resource "azurerm_resource_group" "main" {
    name     = "rg-weatherapp-${local.environment}"
    location = local.location
  }

// Adding the module calls
module "networking"{
    source = "../../modules/networking"
    environment = local.environment
    location = local.location
    resource_group_name = azurerm_resource_group.main.name
}

// acr module
module "acr"{
    source = "../../modules/acr"
    environment = local.environment
    location = local.location   
    resource_group_name = azurerm_resource_group.main.name
}

// aks module
module "aks" {
    source = "../../modules/aks"
    environment = local.environment
    location = local.location   
    resource_group_name = azurerm_resource_group.main.name
    aks_subnet_id = module.networking.aks_subnet_id
    acr_id = module.acr.acr_id
}

// database
module "database" {
    source = "../../modules/database"
    environment = local.environment
    location = local.location   
    resource_group_name = azurerm_resource_group.main.name
    sql_admin_password = var.sql_admin_password
}

// vm
module "vm"{
    source = "../../modules/vm"
    environment = local.environment
    location = local.location   
    resource_group_name = azurerm_resource_group.main.name
    vm_subnet_id = module.networking.vm_subnet_id
    ssh_public_key = var.ssh_public_key
}

// storage
  module "storage" {
    source              = "../../modules/storage"
    environment         = local.environment
    location            = local.location
    resource_group_name = azurerm_resource_group.main.name
  }
