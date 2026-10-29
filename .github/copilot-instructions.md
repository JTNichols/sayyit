# Copilot Instructions

## Product Description

**Sayyit** is a web application similar to X.com and Reddit.com where users are assigned **left/right political scores** (visible to all) that enable community-driven, self-moderated content management. The entire platform is moderated by users with balanced political approval: moderation actions like banning or blocking require equal approval from users on both the left and the right.

## Development Environment & Tooling

- **Language & Framework**: .NET 10, C#
- **IDE**: Visual Studio Community 2026 / VS Code
- **Terminal**: Windows PowerShell / CMD
- **OS**: Windows
- Environments: Sayyit currently has three environments:
  - **Local**: named "Development", a local development environment on a developer's machine
  - **Remotely hosted development**: named "Dev" (hosted in Azure)
  - **Remotely hosted Production**: named "Prod" (hosted in Azure)

## Repositories & Structure

### Primary Application Repository
- **Repo**: JTNichols/sayyit (main application)
- **Components**:
  - Frontend: Blazor WebAssembly standalone app
  - Backend: Single RESTful API (.NET 10)
  - Database: Sayyit.Db SQL project (deployed as DACPAC)
- **All algorithms & rules** are documented in the repo README.md

### Infrastructure as Code Repository
- **Repo**: JTNichols/sayyit-iac
- **Format**: Azure Bicep, main.dev.bicep for dev environment, main.prod.bicep for production
- **Source of Truth**: E:\Sync\Jason\Dev\Repo\SayyitInfrastructure\infra – always inspect for hosting, auth, deployment, app settings, Azure resources, or CI/CD changes

## Application Architecture

| Component | Technology | Notes |
|-----------|-----------|-------|
| Frontend | Blazor WebAssembly Standalone | Client-side only |
| Backend | RESTful API (.NET 10, C#) | Single API surface |
| Database | Azure SQL Database | Persistent storage |
| Hosting | Azure App Service | sayyit-<env>-apiapp, sayyit-<env>-webapp |
| Auth | Azure EntraID External ID | sayyit.onmicrosoft.com tenant |

## Azure Environment & Tenants

### Workforce Tenant (Infrastructure & Deployment)
- **Tenant**: sayyitadmin.onmicrosoft.com
- **Subscription**: sayyitadmin.subscription
- **Resource Group**: sayyitadmin_rg1

### External Configuration Tenant (Customer Identity)
- **Tenant**: sayyit.onmicrosoft.com
- **Subscription**: sayyit.subscription
- **Resource Group**: sayyit_rg1
- **Domain Host**: NearlyFreeSpeech.com (manages sayyit.com)
- **Auth Authority**: https://sayyit.ciamlogin.com
- **Behavior**: The Blazor app (sayyit.web) redirects users to this CIAM authority; the API (sayyit.api) trusts tokens from this authority

## Azure Resource Naming Convention

Pattern: `sayyit-<env>-<resource>`

| Resource | Pattern |
|----------|---------|
| Web App (Frontend) | sayyit-<env>-webapp |
| Web API (Backend) | sayyit-<env>-apiapp |
| Key Vault | sayyit-<env>-kv |
| SQL Server | sayyit-<env>-sqlServer |
| SQL Database | sayyit-<env>-db |

## Branch & Environment Conventions

- **Active Branch Format**: `env/<environment-name>` 
- **Mapping**: branch env/dev → environment name `dev`
- **Mapping**: branch main → environment name `prod`
- **OIDC Subject Format**: `repo:JTNichols/sayyit:ref:refs/heads/env/dev` (must match actual branch for GitHub federated credentials)

## Deployment & CI/CD Pipeline

### Application Deployment (GitHub Actions)
- **Trigger**: Pushed to env/dev or pull request to main
- **Auth Method**: OIDC with azure/login@v2
- **Required Secrets**: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID
- **Publish Profile**: Read from Azure Key Vault
- **Workflow File**: sayyit-web-action.yml

### Database Deployment (GitHub Actions)
- **Trigger**: Pushed to env/dev or pull request to main
- **Build**: Compiles Sayyit.Db → .dacpac artifact
- **Deploy**: Reads SQL secrets from Key Vault, deploys DACPAC to Azure SQL
- **Workflow File**: sayyit-iac-action.yml (for infrastructure), separate DB workflow

### Infrastructure Deployment (GitHub Actions)
- **Trigger**: Pushed to env/dev or pull request to main
- **Auth Method**: OIDC login (azure/login@v2)
- **Required Secrets**: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID
- **Deployment**: Runs Bicep script (main.dev.bicep or main.prod.bicep) to sayyit_rg1

## Key Vault & Secrets Management

- **Authorization**: RBAC (no access policies)
- **Created By**: main.bicep
- **GitHub Deployment Identity**: Permission to create/update secrets (via role assignment in infrastructure code)
- **Web App Managed Identity**: Permission to read secrets (system-assigned identity)
- **Key Secrets**:
  - `sqlServerAdministratorPassword` – SQL admin password
	- 
**Critical**: Never hardcode secrets, connection strings, publish profiles, client IDs, or tenant IDs in source files; all secrets must come from Key Vault or GitHub repository secrets.

## Important Files: 
### Review these files for context on GitHub Actions, OIDC, and Azure deployment patterns

| File | Purpose |
|------|---------|
| E:/Sync/Jason/Dev/Repo/SayyitInfrastructure/infra/Scripts/GH_to_AZ_credential_PR.ps1 | PowerShell script: adds OIDC app to sayyit.onmicrosoft.com for pull requests |
| E:/Sync/Jason/Dev/Repo/SayyitInfrastructure/infra/Scripts/GH_to_AZ_credential_push.ps1 | PowerShell script: adds OIDC app to sayyit.onmicrosoft.com for env/dev pushes |
| .github/workflows/sayyit-web-action.yml | App deployment workflow (Blazor front-end + .NET API) |
| .github/workflows/sayyit-iac-action.yml | Infrastructure deployment workflow |
| E:/Sync/Jason/Dev/Repo/SayyitInfrastructure/infra/main.dev.bicep | Bicep template deployed on env/dev changes |
| E:/Sync/Jason/Dev/Repo/SayyitInfrastructure/infra/main.prod.bicep | Bicep template deployed on main branch changes |

## Working Preferences

- Prefer solutions compatible with **Visual Studio, VS Code, Windows, and PowerShell**
- Prefer code matching **existing Azure naming and GitHub OIDC patterns** already used by Sayyit
- Prioritize information from **github.com/JTNichols/sayyit** when relevant
- For workforce tenant recommendations, use **sayyitadmin.onmicrosoft.com**

---

# Guidance for Copilot

## Architecture Assumptions

When generating code, documentation, tests, or refactors, assume the following unless the surrounding code clearly shows otherwise:

- Frontend is a Blazor WebAssembly standalone app (client-side only, no server-side Blazor)
- Backend is a single RESTful API written in C# on .NET 10
- Persistent data is stored in Azure SQL
- Authentication, app hosting, and infrastructure are managed in Azure
- Infrastructure is defined in Bicep and deployed through the sayyit-iac repository
- All moderation logic is validated server-side, never relying on UI checks alone

## Coding Style

### General Principles
- Prefer clear, maintainable, production-style C# over clever shortcuts
- Follow existing naming, folder, and namespace conventions in the repository before introducing new patterns
- Keep files focused; avoid large mixed-responsibility classes
- Favor dependency injection, interfaces where they improve testability, and small composable services
- Use async/await for I/O-bound work; do not block on tasks
- Add null handling and input validation at API boundaries
- Write code that is easy to review and reason about

### C# and .NET
- Target .NET 10 patterns already used by the solution
- Prefer standard ASP.NET Core conventions for controllers, minimal APIs, middleware, configuration, and logging based on what the repo already uses
- Use strongly typed models and DTOs for API contracts
- Keep business rules out of controllers; place them in domain or application services
- Use `ILogger<T>` for operational logging; avoid logging secrets, tokens, connection strings, or sensitive moderation data
- When adding configuration, prefer appsettings + environment overrides and document required settings

### Blazor WebAssembly
- Keep presentation logic in components; move reusable logic to services
- Use typed HTTP clients or well-encapsulated API access layers rather than scattering raw HTTP calls across components
- Optimize for responsive UI and clear state transitions, especially for moderation flows, voting, and score displays
- Avoid tightly coupling components to backend response shapes; use view models for clarity

### SQL & Data Access
- Design schema and queries for auditability, moderation transparency, and future analytics
- Treat left/right scoring, moderation votes, and user actions as domain records that may need traceability
- Avoid destructive data operations unless explicitly requested
-
### Data Transfer objects
- Use project Sayyit.Contracts for DTOs between API and Blazor frontend
- Whenever possible use the Sayyit.Contracts models for API endpoints and Blazor view objects instead of creating new classes
- prefer adding new properties to existing DTOs rather than creating new DTOs unless the new property is a back-end secret or otherwise sensitive

## Domain Rules to Protect

Copilot should preserve these product assumptions in all generated code:

- **Moderation is community-driven**, not centrally ideological
- **Left/right political scores are first-class domain concepts** and must be visible to users
- **Political-balance moderation** must be modeled explicitly and validated server-side
- **Authorization and moderation logic must not rely only on UI checks**
- **Auditability matters**: important moderation decisions must be traceable
- **Public trust matters**: avoid hidden heuristics when explicit rules exist
- **If a requested change could break political-balance moderation**, call out the risk in comments or documentation

## API Guidance

- Design REST endpoints around clear resources and actions
- Validate all incoming models
- Return appropriate HTTP status codes and problem details where useful
- Keep authorization checks close to the sensitive operation
- For moderation endpoints, explicitly verify balanced approval conditions in backend logic
- Prefer idempotent patterns where retries are likely

## Testing Guidance

- Add or update tests whenever behavior changes
- Prioritize tests for:
  - Moderation workflows
  - Political-balance calculations
  - Permission checks
  - Score updates
  - Audit logging behavior
- Cover edge cases: tie conditions, duplicate votes, race conditions, missing identities, invalid score ranges
- Favor readable unit and integration tests over brittle implementation-heavy tests

## Security Guidance

- **Never hardcode** secrets, connection strings, publish profiles, client IDs, or tenant IDs in source files
- Assume all secrets come from **Azure Key Vault alone**
- Follow **least-privilege access patterns** for app identities and deployment identities
- Validate and authorize **all state-changing API operations**
- Treat moderation, identity, and political-score data as **sensitive**
- Sanitize logs and error messages

## Azure & Infrastructure Guidance

- The infrastructure repository uses GitHub Actions with Azure federated identity login and Bicep deployments
- Generated infrastructure-related changes must remain compatible with that workflow
- Keep infrastructure changes in JTNichols/sayyit-iac aligned with Bicep-driven deployment
- Assume GitHub Actions authenticate using AZURE_CLIENT_ID, AZURE_TENANT_ID, and AZURE_SUBSCRIPTION_ID (not stored credentials)
- Preserve resource group name **sayyit_rg1** unless explicitly told to change it
- Respect the existing pattern: deployment principal has rights to deploy infrastructure and manage Key Vault secrets
- Prefer Azure-native configuration and identity features over manual secret distribution
- The environment includes: App Service plan, web app, Key Vault with RBAC enabled, web app with system-assigned managed identity, deployment principal with Key Vault secret management rights

## GitHub Actions Guidance

- Keep workflow changes minimal and explicit
- Prefer OIDC/federated credentials for Azure login over publish profiles or long-lived secrets
- Preserve branch and environment assumptions unless the task explicitly changes release flow
- Document any newly required repository secrets or environment variables
- Current workflow pattern: login with azure/login@v2, deploy main.<env>.bicep to sayyit_rg1, use helper scripts to create/manage federated credentials

## Documentation Guidance

- Explain **why** a change exists, not just what changed
- Keep README and developer docs aligned with actual architecture
- For setup steps: prefer **Windows-friendly commands first**, then optional cross-platform notes
- Call out any Azure prerequisites, GitHub secrets, local environment variables, or required CLI tools

## Expected Copilot Behavior

When asked to implement a feature:

1. Inspect nearby code and match existing patterns
2. Preserve the political-balance moderation model
3. Put domain rules in backend services, not just UI components
4. Add or update tests
5. Note any required config, schema, or infrastructure updates
6. Avoid introducing new frameworks or major architectural shifts unless explicitly requested
7. When requirements are unclear, prefer small, reversible changes and leave concise TODO notes only where repository context is genuinely missing

## What to Avoid

- ❌ Do not replace balanced community moderation with admin-only shortcuts unless explicitly requested
- ❌ Do not embed secrets or Azure credentials in code, scripts, or docs
- ❌ Do not bypass backend validation for moderation or score-related actions
- ❌ Do not introduce unnecessary packages or abstractions
- ❌ Do not assume Linux-only local shell commands; default to Windows-friendly guidance for developer workflows

## Example prompts Copilot should handle well
- “Add a backend service that determines whether a ban vote has balanced left/right approval.”
- “Create a Blazor component that shows a user’s visible left/right score and moderation history.”
- “Refactor this moderation controller so policy checks live in a service and add tests.”
- “Update the Bicep and GitHub Actions workflow to support another environment using the current Azure OIDC pattern.”