
dns_zones = {
  dns_zone1 = {
    name               = "ngsa-monitoring-dev.com" // Set as empty for CI. this will creation a random_domain_name.com
    resource_group_key = "agw_re1"

    # You can create dns records using the following nested structure
    records = {
      a = {
        agw = {
          name   = "@"
        #   records = ["10.0.0.0"]
          resource_id = {
              public_ip_address = {
                  key = "agw_pip1_re1"
              }
          }
        }
      }
    }
  }
}

domain_name_registrations = {
  #
  # Register for a random domain name
  # As dnsType as not be set 
  #
  random_domain = {
    name               = "" // Set as empty for CI. this will creation a random_domain_name.com
    resource_group_key = "agw_re1"

    auto_renew    = true
    privacy       = true
    lock_resource = false
    dns_zone = {
      # Set the resource ID of the existing DNS zone
      # id = "/subscriptions/[subscription_id]/resourceGroups/qaxu-rg-dns-domain-registrar/providers/Microsoft.Network/dnszones/ml0iaix4xgnz0jqd.com"
      #
      # or
      #
      # Set the 'key' of the dns_zone created in this deployment
      # Set 'lz_key' if the DNS zone referenced by the key attribute has been created in a remote deployment
      key = "dns_zone1"
    }

    contacts = {
      contactAdmin = {
        name_first   = "Jim"
        name_last    = "Keane"
        email        = "jim.keane@microsoft.com"
        phone        = "+1.5125551212"
        organization = "CSE"
        job_title    = "Lead"
        address1     = "Austin"
        address2     = ""
        postal_code  = "78701"
        state        = "Texas"
        city         = "Austin"
        country      = "US"
      }
      contactBilling = {
        same_as_admin = true
      }
      contactRegistrant = {
        same_as_admin = true
      }
      contactTechnical = {
        same_as_admin = true
      }
    }
  }
}  
