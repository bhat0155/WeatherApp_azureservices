output "public_ip"{
     description = "ssh in this ip to access vm"
    value = azurerm_public_ip.bastion.ip_address
}