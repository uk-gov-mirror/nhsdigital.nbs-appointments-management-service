data "azurerm_client_config" "current" {}

# Create the Key Vault
resource "azurerm_key_vault" "nbs_mya_key_vault" {
  name                        = "${var.application}-kv-${var.environment}-${var.loc}"
  location                    = var.location
  resource_group_name         = local.resource_group_name
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  sku_name                    = "standard"
  
  enable_rbac_authorization   = true
  purge_protection_enabled    = true
}

resource "azurerm_key_vault_secret" "active_cosmos_account" {
  name         = "ActiveCosmosDBAccountName"
  value        = "${var.application}-cdb-${var.environment}-${var.loc}" 
  key_vault_id = azurerm_key_vault.nbs_mya_key_vault.id

  lifecycle {
    ignore_changes = [value]
  }
}

resource "azurerm_role_assignment" "pipeline_secret_access" {
  scope                = azurerm_key_vault.nbs_mya_key_vault.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}