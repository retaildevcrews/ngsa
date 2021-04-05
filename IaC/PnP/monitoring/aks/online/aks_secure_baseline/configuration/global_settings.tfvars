global_settings = {
  passthrough    = false
  prefix = "mon-dev"
  random_length  = 0  
  default_region = "region1"
  regions = {
    region1 = "eastus2" # You can adjust the Azure Region you want to use to deploy AKS and the related services
    # region2 = "eastasia"            # Optional - Add additional regions
  }
}
