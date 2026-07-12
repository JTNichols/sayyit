# This script runs commands against an existing EntraID external configuration tenant in the domain sayyit.onmicrosoft.com.
# Section 1. sets up a GitHub Actions OIDC identity for Azure deployment in a specific repo and branch in the 'dev' environment.
# It only needs to be run once per repo/branch combination.
# It requires an existing AZ CLI login, an existing MS Entra tenant and an existing resource group in that same domain/subscription.
# For a different environment, a new resource group should be created
 

# to run, log into Azure CLI (az login) and then run following command in PowerShell, replacing the parameters w/ new repo and/or environment
# .\EntraApp-registrations.ps1 -Repo "JTNichols/sayyit-iac" -EnvironmentName "dev" -ResourceGroupName "sayyit_rg1"


param(
    [Parameter(Mandatory = $true)]
    [string]$Repo, # e.g. "JTNichols/sayyit-iac"

    [Parameter(Mandatory = $true)]
    [string]$EnvironmentName, # e.g. "dev" or "prod"

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName # e.g. "sayyit_rg1"
)
# -------------------------------------------------------------------------
# Section 1: Validate inputs and Azure context
# 
# Validates the repository name and Azure login context.
# Checks that the target resource group exists.
# Creates or reuses a Microsoft Entra app registration for the environment.
# Creates or reuses a service principal for that app.
# Verify Repo name
# -------------------------------------------------------------------------

$Repo = $Repo.Trim()
if ($Repo -notmatch '^[^/\s]+/[^/\s]+$') {
    throw "Repo must be in the format 'OWNER/REPO', for example 'JTNichols/sayyit-iac'."
}

$Branch = "env/$EnvironmentName"
$AppName = "sayyit-github-actions-$EnvironmentName"
# Use concatenation to avoid PowerShell parsing/scope ambiguity with multiple colons
$Subject = 'repo:' + $Repo + ':ref:refs/heads/' + $Branch
Write-Host "Setting up OIDC identity for repo '$Repo' branch '$Branch' on resource group '$ResourceGroupName'..."
Write-Host "Expected federated credential subject: $Subject"
# Get subscription and tenant from current az login context
$SubscriptionId = az account show --query id -o tsv
$SubscriptionScope = "/subscriptions/$SubscriptionId"
$TenantId       = az account show --query tenantId -o tsv

# Verify subscription
if (-not $SubscriptionId -or -not $TenantId) {
    throw "Azure CLI is not logged in or unable to read subscription/tenant. Run 'az login' and try again."
}

# Verify resource group
az group show --name $ResourceGroupName --query id -o tsv | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Resource group '$ResourceGroupName' was not found in the current subscription."
}

$RgScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName"

Write-Host "Using subscription: $SubscriptionId"
Write-Host "Using tenant:      $TenantId"
Write-Host "Scope:             $RgScope"

# ---------------------------------------------------------------------------------------
# Section 2:  Create (or reuse) app registration for GitHub federated credential
# ---------------------------------------------------------------------------------------
Write-Host "Ensuring Microsoft Entra app registration '$AppName' exists..."

$existingApp = az ad app list `
    --filter "displayName eq '$AppName'" `
    --query "[0].{appId:appId,id:id}" `
    -o json | ConvertFrom-Json

if ($existingApp -and $existingApp.appId) {
    $AppId = $existingApp.appId
    $AppObjectId = $existingApp.id
    Write-Host "Using existing app registration."
}
else {
    $AppId = az ad app create `
        --display-name $AppName `
        --query appId -o tsv

    if (-not $AppId) {
        throw "Failed to create app registration."
    }

    $AppObjectId = az ad app show `
        --id $AppId `
        --query id -o tsv
}

Write-Host "AppId:       $AppId"
Write-Host "AppObjectId: $AppObjectId"

# ---------------------------------------------------------------------------------------
# Section 3: Create service principal (SP) for the Github Federated Credential app if it doesn't exist
# ---------------------------------------------------------------------------------------
Write-Host "Ensuring service principal for app '$AppId' exists..."

$existingSp = az ad sp list `
    --filter "appId eq '$AppId'" `
    --query "[0].id" `
    -o tsv
    Hujo063406
if ($existingSp) {
    $SpObjectId = $existingSp
    Write-Host "Using existing service principal."
}
else {
    $SpObjectId = az ad sp create `
        --id $AppId `
        --query id -o tsv
}

if (-not $SpObjectId) {
    throw "Failed to resolve service principal object ID."
}
Write-Host "Service principal objectId: $SpObjectId"

# -----------------------------
# Section 4: Create federated credential as JSON, if it doesn't exist
# credential subject will be of form: repo:OWNER/REPO:ref:refs/heads/BRANCH, so for 
# dev it's repo:JTNIchols/sayyit:ref:refs/heads/env/dev
# -----------------------------
$federatedCredentialName = "github-$($Repo.Replace('/','-'))-$($Branch.Replace('/','-'))"

$fcPath = ".\federated-credential.json"

# Build federated credential payload as a PowerShell object and serialize to JSON
$federatedObj = [ordered]@{
    name        = $federatedCredentialName
    issuer      = 'https://token.actions.githubusercontent.com'
    subject     = $Subject
    description = "GitHub Actions OIDC for $Repo branch $Branch"
    audiences   = @('api://AzureADTokenExchange')
}

$federatedJson = $federatedObj | ConvertTo-Json -Depth 4
$federatedJson | Set-Content -Path $fcPath -Encoding UTF8

# Debug output to help diagnose subject/serialization issues
Write-Host "Debug: Repo=[$Repo] Branch=[$Branch] Subject=[$Subject]"
Write-Host "Debug: Federated credential JSON content:" 
Get-Content $fcPath -Raw | Write-Host

Write-Host "Creating federated credential on app '$AppObjectId' for subject $Subject ..."
Write-Host "Listing federated credentials for app '$AppObjectId' and verifying subject matches expected assertion..."

# Get federated credentials JSON and parse safely
$fedsJson = az ad app federated-credential list --id $AppObjectId -o json 2>$null
$feds = @()
if ($fedsJson) {
    try {
        $feds = $fedsJson | ConvertFrom-Json
    }
    catch {
        Write-Host "Warning: unable to parse federated credentials JSON. Treating as empty list."
        $feds = @()
    }
}

# Look for an existing credential whose subject exactly equals the expected subject
$matching = @()
if ($feds) {
    $matching = $feds | Where-Object { $_.subject -eq $Subject }
}

if ($matching -and $matching.Count -gt 0) {
    $matchedName = $matching[0].name
    Write-Host "Found existing federated credential with matching subject: $matchedName"
}
else {
    # If a credential exists with the same name but different subject, warn the user
    $sameName = $null
    if ($feds) { $sameName = $feds | Where-Object { $_.name -eq $federatedCredentialName } }
    if ($sameName) {
        Write-Host "Note: a federated credential named '$federatedCredentialName' exists but its subject does not match the expected subject '$Subject'."
        Write-Host "Creating a new federated credential with the correct subject."
    }

    Write-Host "Creating federated credential on app '$AppObjectId' for subject $Subject ..."
    az ad app federated-credential create `
        --id $AppObjectId `
        --parameters "@$fcPath"
}

# -----------------------------
# Section 5: Assign Azure RBAC roles at SUBSCRIPTION scope, if they don't exist.
# Contributor + User Access Administrator
# -----------------------------
$rolesToEnsure = @(
    @{ Name = 'Contributor'; Scope = $SubscriptionScope },
    @{ Name = 'User Access Administrator'; Scope = $SubscriptionScope }
)

foreach ($role in $rolesToEnsure) {
    $existingAssignment = az role assignment list `
        --assignee-object-id $SpObjectId `
        --scope $role.Scope `
        --query "[?roleDefinitionName=='$($role.Name)'] | [0].id" `
        -o tsv

    if ($existingAssignment) {
        Write-Host "Role '$($role.Name)' already assigned at $($role.Scope)."
    }
    else {
        Write-Host "Assigning '$($role.Name)' role to service principal at subscription scope $($role.Scope) ..."
        az role assignment create `
            --assignee-object-id $SpObjectId `
            --assignee-principal-type ServicePrincipal `
            --role $role.Name `
            --scope $role.Scope
    }
}
 



# -----------------------------
# Section 6: Create web app sayyit-web-$EnvironmentName
# This is the app registration for the Blazor WebAssembly frontend.
# It is registered in the same tenant but is separate from the GitHub
# Actions deployment identity created in sections 1-5.
# -----------------------------
$WebAppName = "sayyit-web-$EnvironmentName"
Write-Host ""
Write-Host "Ensuring Microsoft Entra app registration '$WebAppName' exists..."

$existingWebApp = az ad app list `
    --filter "displayName eq '$WebAppName'" `
    --query "[0].{appId:appId,id:id}" `
    -o json | ConvertFrom-Json

if ($existingWebApp -and $existingWebApp.appId) {
    $WebAppId       = $existingWebApp.appId
    $WebAppObjectId = $existingWebApp.id
    Write-Host "Using existing app registration '$WebAppName'."
}
else {
    $WebAppId = az ad app create `
        --display-name $WebAppName `
        --sign-in-audience AzureADMyOrg `
        --query appId -o tsv

    if (-not $WebAppId) {
        throw "Failed to create app registration '$WebAppName'."
    }

    $WebAppObjectId = az ad app show `
        --id $WebAppId `
        --query id -o tsv
}

# -----------------------------
# Output values
# -----------------------------
Write-Host "Web AppId:       $WebAppId"
Write-Host "Web AppObjectId: $WebAppObjectId"

Write-Host ""
Write-Host "Done. Use these values for GitHub Actions secrets:"
Write-Host "  AZURE_CLIENT_ID      = $AppId"
Write-Host "  AZURE_TENANT_ID      = $TenantId"
Write-Host "  AZURE_SUBSCRIPTION_ID= $SubscriptionId"
Write-Host ""
Write-Host "App object ID:         $AppObjectId"
Write-Host "SP object ID:          $SpObjectId"
Write-Host "Scope used:            $RgScope"
Write-Host ""
Write-Host "Web app registration:"
Write-Host "  Name:                $WebAppName"
Write-Host "  Web AppId:           $WebAppId"
Write-Host "  Web AppObjectId:     $WebAppObjectId"
Write-Host ""
Write-Host "Remember to configure your workflow with:"
Write-Host "  permissions:"
Write-Host "    id-token: write"
Write-Host "    contents: read"
Write-Host "and use azure/login@v2 with these secrets."

