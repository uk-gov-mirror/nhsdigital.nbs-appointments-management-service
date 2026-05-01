data "azurerm_client_config" "current" {}

# Create the Key Vault
resource "azurerm_key_vault" "nbs_mya_key_vault" {
  name                = "${var.application}-kv-${var.environment}-${var.loc}"
  location            = var.location
  resource_group_name = local.resource_group_name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  enable_rbac_authorization = true
  purge_protection_enabled  = true
}

resource "azurerm_key_vault_secret" "active_cosmos_account" {
  name         = "ActiveCosmosDBAccountName"
  value        = "${var.application}-cdb-${var.environment}-${var.loc}"
  key_vault_id = azurerm_key_vault.nbs_mya_key_vault.id

  depends_on = [
    azurerm_role_assignment.pipeline_secret_access
  ]
  
  lifecycle {
    ignore_changes = [value]
  }
}

resource "azurerm_role_assignment" "pipeline_secret_access" {
  scope                = azurerm_key_vault.nbs_mya_key_vault.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}


locals {
  principals_map = merge(
    # Container App Jobs
    { for v in azurerm_container_app_job.nbs_mya_booking_extracts_job : "booking_extracts_job_${v.name}" => v.identity[0].principal_id },
    { for v in azurerm_container_app_job.nbs_mya_capacity_extracts_job : "capacity_extracts_job_${v.name}" => v.identity[0].principal_id },

    # Container Apps
    { for v in azurerm_container_app.nbs_mya_auditor : "auditor_${v.name}" => v.identity[0].principal_id },
    { for v in azurerm_container_app.nbs_mya_aggregator : "aggregator_${v.name}" => v.identity[0].principal_id },

    # Function Apps (Standard) - Using try() to handle potential empty identity blocks
    { for i, v in azurerm_windows_function_app.nbs_mya_high_load_func_app : "high_load_${i}" => try(v.identity[0].principal_id, null) },
    { for i, v in azurerm_windows_function_app.nbs_mya_http_func_app : "http_${i}" => try(v.identity[0].principal_id, null) },
    { for i, v in azurerm_windows_function_app.nbs_mya_service_bus_func_app : "sb_func_${i}" => try(v.identity[0].principal_id, null) },
    { for i, v in azurerm_windows_function_app.nbs_mya_timer_func_app : "timer_${i}" => try(v.identity[0].principal_id, null) },

    # Function App Slots (Preview)
    { for i, v in azurerm_windows_function_app_slot.nbs_mya_high_load_func_app_preview : "high_load_preview_${i}" => try(v.identity[0].principal_id, null) },
    { for i, v in azurerm_windows_function_app_slot.nbs_mya_http_func_app_preview : "http_preview_${i}" => try(v.identity[0].principal_id, null) },
    { for i, v in azurerm_windows_function_app_slot.nbs_mya_service_bus_func_app_preview : "sb_preview_${i}" => try(v.identity[0].principal_id, null) },
    { for i, v in azurerm_windows_function_app_slot.nbs_mya_timer_func_app_preview : "timer_preview_${i}" => try(v.identity[0].principal_id, null) }
  )
}

resource "azurerm_role_assignment" "vault_access" {
  for_each             = local.principals_map
  scope                = azurerm_key_vault.nbs_mya_key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = each.value
}