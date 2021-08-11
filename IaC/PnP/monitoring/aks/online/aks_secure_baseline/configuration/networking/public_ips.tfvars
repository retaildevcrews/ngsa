public_ip_addresses = {
  firewall_re1 = {
    name                    = "pip-egress-re1-1"
    resource_group_key      = "vnet_hub_re1"
    sku                     = "Standard"
    allocation_method       = "Static"
    ip_version              = "IPv4"
    idle_timeout_in_minutes = "4"

  }

  firewall_pip2_re1 = {
    name                    = "pip-egress-re1-2"
    resource_group_key      = "vnet_hub_re1"
    sku                     = "Standard"
    allocation_method       = "Static"
    ip_version              = "IPv4"
    idle_timeout_in_minutes = "4"

  }

  bastion_host_re1 = {
    name                    = "pip-bastion-re1-1"
    resource_group_key      = "jumpbox_re1"
    sku                     = "Standard"
    allocation_method       = "Static"
    ip_version              = "IPv4"
    idle_timeout_in_minutes = "4"
  }

  agw_pip1_re1 = {
    name                    = "pip-agw-re1-1"
    resource_group_key      = "agw_re1"
    sku                     = "Standard"
    allocation_method       = "Static"
    ip_version              = "IPv4"
    zones                   = ["1"]
    idle_timeout_in_minutes = "4"

  }
  
}
