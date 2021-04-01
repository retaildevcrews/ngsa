managed_identities = {
  ingress = {
    name               = "mi-monitoring-dev-ingress-controller"
    resource_group_key = "devops_re1"
  }
  apgw_keyvault_secrets = {
    name               = "mi-monitoring-dev-agw-secrets"
    resource_group_key = "devops_re1"
  }
}