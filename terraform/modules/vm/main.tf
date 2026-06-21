// public ip for bastion
resource "azurerm_public_ip" "bastion" {
  name                = "pip-bastion-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  allocation_method   = "Static"

  tags = {
    environment = var.environment
    project     = "weatherapp"
    managed_by  = "terraform"
  }
}

// nic
resource "azurerm_network_interface" "bastion" {
  name                = "nic-bastion-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = var.vm_subnet_id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.bastion.id
  }
}

// linux vm
resource "azurerm_linux_virtual_machine" "bastion" {
  name                = "vm-bastion-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  size                = "Standard_D4_v5"
  admin_username      = var.admin_username
  network_interface_ids = [
    azurerm_network_interface.bastion.id,
  ]

  admin_ssh_key {
    username   = var.admin_username
    public_key = var.ssh_public_key
  }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-jammy"
    sku       = "22_04-lts"
    version   = "latest"
  }

  tags = {
    environment = var.environment
    project     = "weatherapp"
    managed_by  = "terraform"
    role        = "bastion"
  }
}