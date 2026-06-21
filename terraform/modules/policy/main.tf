# Azure Policy assignments enforce tag requirements at the subscription level.
# "Deny" effect means Azure will BLOCK any resource creation that is missing the tag.
# The built-in policy ID below is Microsoft's standard "Require a tag on resources" policy —
# we just assign it three times, once per required tag.

locals {
  # Built-in policy: "Require a tag on resources"
  require_tag_policy_id = "/providers/Microsoft.Authorization/policyDefinitions/871b6d14-10aa-478d-b590-94f262ecfa99"

  # Subscription scope — policy applies to ALL resources in the subscription
  subscription_scope = "/subscriptions/${var.subscription_id}"
}

# Enforce that every resource has an "environment" tag (e.g. staging, production)
resource "azurerm_subscription_policy_assignment" "require_environment_tag" {
  name                 = "require-tag-environment-${var.environment}"
  display_name         = "Require environment tag (${var.environment})"
  policy_definition_id = local.require_tag_policy_id
  subscription_id      = local.subscription_scope

  # jsonencode converts HCL map → JSON string that the policy API expects
  parameters = jsonencode({
    tagName = { value = "environment" }
  })
}

# Enforce that every resource has a "project" tag (e.g. weatherapp)
resource "azurerm_subscription_policy_assignment" "require_project_tag" {
  name                 = "require-tag-project-${var.environment}"
  display_name         = "Require project tag (${var.environment})"
  policy_definition_id = local.require_tag_policy_id
  subscription_id      = local.subscription_scope

  parameters = jsonencode({
    tagName = { value = "project" }
  })
}

# Enforce that every resource has a "managed_by" tag (e.g. terraform)
resource "azurerm_subscription_policy_assignment" "require_managed_by_tag" {
  name                 = "require-tag-managed-by-${var.environment}"
  display_name         = "Require managed_by tag (${var.environment})"
  policy_definition_id = local.require_tag_policy_id
  subscription_id      = local.subscription_scope

  parameters = jsonencode({
    tagName = { value = "managed_by" }
  })
}

# Only allow approved VM sizes — prevents accidental creation of expensive VMs
resource "azurerm_subscription_policy_assignment" "allowed_vm_sizes" {
  name                 = "allowed-vm-sizes-${var.environment}"
  display_name         = "Allowed VM sizes (${var.environment})"
  policy_definition_id = "/providers/Microsoft.Authorization/policyDefinitions/cccc23c7-8427-4f53-ad12-b6a63eb452b3"
  subscription_id      = local.subscription_scope

  parameters = jsonencode({
    listOfAllowedSKUs = { value = ["Standard_D2ads_v6", "Standard_D2ps_v6", "Standard_D4ps_v6"] }
  })
}