$searchId = az search service create -n cw-search-service `
--resource-group azure-ai-series `
--sku basic `
--auth-options aadOrApiKey `
--aad-auth-failure-mode http401WithBearerChallenge `
--identity-type SystemAssigned `
--location eastus2 `
--query id `
--output tsv

$clientId = az ad signed-in-user show --query id --output tsv

az role assignment create `
--assignee-object-id $clientId `
--assignee-principal-type User `
--role 'Search Service Contributor' `
--scope $searchId `
--query createdOn

$storageId=az storage account show `
-n cwlabsstorage `
-g azure-ai-series `
--query id `
--output tsv

az role assignment create `
--assignee-object-id $clientId `
--assignee-principal-type User `
--role 'Storage Blob Data Contributor' `
--scope $storageId `
--query createdOn

$searchIdentity=az search service show --name cw-search-service --resource-group azure-ai-series --query "identity.principalId" -o tsv

az role assignment create `
--assignee-object-id $searchIdentity `
--assignee-principal-type ServicePrincipal `
--role 'Storage Blob Data Contributor' `
--scope $storageId `
--query createdOn
