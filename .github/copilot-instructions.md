# Copilot Instructions

## Product Description

**Sayyit** is a web application similar to X.com and Reddit.com where users are assigned **left/right political scores** (visible to all) that enable community-driven, self-moderated content management. The entire platform is moderated by users with balanced political approval: moderation actions like banning or blocking require equal approval from users on both the left and the right.

## Development Environment & Tooling

- **Language & Framework**: .NET 10, C#
- **IDE**: Visual Studio Community 2026 / VS Code
- **Terminal**: Windows PowerShell / CMD
- **OS**: Windows
- Environments: Sayyit currently has three environments. All three are loading their respective configurations correctly, don't add code to enforce environment selection:
  - **Local**: named "Development", a local development environment on a developer's machine
  - **Remotely hosted development**: named "Dev" (hosted in Azure)
  - **Remotely hosted Production**: named "Prod" (hosted in Azure)
- Diagnostics toggling for both **Sayyit.Web** and **Sayyit.API** must use `Diagnostics:ExposeLogs` from configuration instead of environment checks.
- Local appsettings values for `Diagnostics:ExposeLogs` may be overridden in Production by Azure App Service environment variables (`Diagnostics__ExposeLogs`).

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
- For debug/log visibility behavior in both the Blazor web app and API, use `Diagnostics:ExposeLogs` and do not gate debugging output on environment-name checks.

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

### Data Transfer Objects
- Use project Sayyit.Contracts for DTOs between API and Blazor frontend
- Whenever possible use the Sayyit.Contracts models for API endpoints and Blazor view objects instead of creating new classes
- Prefer adding new properties to existing DTOs rather than creating new DTOs unless the new property is a back-end secret or otherwise sensitive

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

## Feature-Oriented Organization: Sayyit.API and Sayyit.Web

### Required Organization
- Organize both `Sayyit.API` and `Sayyit.Web` by feature rather than by application-wide technical layers.
- Place feature-specific code under `<ProjectName>/Features/<FeatureName>/`.
- Choose feature boundaries appropriate to each project:
  - API features represent backend business capabilities and resource ownership.
  - Web features represent user-facing capabilities and workflows.
- Feature names and internal folder structures do not need to match between projects.
- For example, `Sayyit.Web/Features/Authentication` and `Sayyit.API/Features/Users` are correct, intentional boundaries. Do not rename either merely to make the projects symmetrical.
- A Web feature may use multiple API features, and an API feature may support multiple Web features.
- Example features include Authentication, Home, Users, Posts, Feeds, Scoring, Moderation, Blocking, and Notifications. Create only features needed by the requested work.
- Keep feature-specific code within its owning feature, including technical subfolders when useful.
- Do not create or expand application-wide technical folders for feature-specific code.
- This feature-oriented convention takes precedence over legacy layer-based organization for new code in both projects.
- Follow the current repository structure when making targeted changes. Do not perform unrelated folder moves or architectural refactors.

### Current Organization and Intended Direction

#### Sayyit.API
- The existing Users feature contains:
  - `Controllers/UserController.cs`
  - `Services/BootstrapService.cs`
  - `Persistence/SayyitUser.cs`
  - `Persistence/SayyitDbContext.cs`
- Preserve feature ownership for controllers, services, and entities.
- The shared `SayyitDbContext` is currently located inside the Users feature. This is an existing placement, not a requirement that all database access belong to Users.
- Use `Infrastructure/Persistence` as the default destination for the shared context when explicitly refactoring persistence organization or when cross-feature persistence work requires that change.
- Do not move the context solely as incidental cleanup during an unrelated task.

#### Sayyit.Web
- The existing Authentication feature contains:
  - `Authentication.razor`
  - `LoginDisplay.razor`
  - `RedirectToLogin.razor`
  - `Services/ApiAuthorizationMessageHandler.cs`
- The existing Home feature contains `Home.razor`.
- Shared application layout components belong in `Shared/Layout`, including `MainLayout.razor` and `NavMenu.razor`.
- `Pages/NotFound.razor` currently remains outside a feature as application-wide routing UI. Do not move it solely to enforce symmetry.
- Do not add feature-specific pages to the root `Pages` folder or feature-specific services to the root `Services` folder.
- The existing placement of `ApiAuthorizationMessageHandler` under Authentication is intentional and acceptable. Do not move it to Infrastructure merely because it is used by requests from multiple features.
- Do not create empty folders or retain unused folders merely to satisfy a template.

### API Feature Structure
- Keep each feature's controllers, application services, queries, validators, business rules, persistence entities, EF Core configurations, and AutoMapper profiles together.
- Feature-local technical folders such as `Controllers`, `Services`, and `Persistence` are allowed and match the existing Users feature.
- Do not create or expand API-wide `Controllers`, `Services`, `Repositories`, `Entities`, `Mapping`, or `DAL` folders for feature-specific code.

Use the following structure as a guide, not as a requirement to create empty folders. The shared context location shown below is the default target when its relocation is part of the requested work:

```text
Sayyit.API/
  Features/
    Users/
      Controllers/
        UserController.cs
      Services/
        BootstrapService.cs
      Persistence/
        SayyitUser.cs

    Posts/
      Controllers/
        PostsController.cs
      Services/
        PostService.cs
      Queries/
        GetPostQuery.cs
      Rules/
        PostEditingRules.cs
      Mapping/
        PostMappingProfile.cs
      Persistence/
        Post.cs
        PostConfiguration.cs

    Moderation/
      Controllers/
        ModerationController.cs
      Services/
        ModerationService.cs
      Rules/
        VoterEligibilityRules.cs
        CrossSideApprovalRules.cs
      Persistence/
        ModerationCase.cs
        ModerationCaseConfiguration.cs

  Infrastructure/
    Persistence/
      SayyitDbContext.cs
    Authentication/
    Authorization/
    Errors/

  Common/
    Security/
    Pagination/

  Program.cs
```

### Web Feature Structure
- Keep each feature's routable pages, feature-owned components, API clients, services, UI state, and presentation models together.
- For small features, keep Razor components directly in the feature folder.
- Add feature-local folders such as `Components`, `Services`, `State`, or `Models` only when they improve navigation.
- A component belongs to the feature that owns its behavior, not necessarily the feature or layout that renders it.
- For example, `LoginDisplay.razor` belongs to Authentication even when rendered by `Shared/Layout/MainLayout.razor`.
- Keep `.razor.cs` code-behind files and `.razor.css` isolated styles beside their corresponding `.razor` files.
- Keep application-wide layout components in `Shared/Layout`.
- Put genuinely reusable, feature-neutral UI components in `Shared/Components` when needed.
- Do not move a feature-owned component into Shared merely because multiple components render it.
- Keep `App.razor`, `Program.cs`, and the root `_Imports.razor` at the project root unless the requested work explicitly requires otherwise.
- Keep public static assets and client configuration in the existing `wwwroot` structure.
- Do not introduce server-side Blazor, server endpoints, EF entities, or database access into the standalone WebAssembly project.

Use the following structure as a guide. Existing files and illustrative future files are shown together; do not create unused files or folders:

```text
Sayyit.Web/
  Features/
    Authentication/
      Authentication.razor
      LoginDisplay.razor
      RedirectToLogin.razor
      Services/
        ApiAuthorizationMessageHandler.cs

    Home/
      Home.razor

    Posts/
      PostDetails.razor
      PostDetails.razor.cs
      PostDetails.razor.css
      Components/
        PostCard.razor
        PostEditor.razor
      Services/
        PostsApiClient.cs

    Moderation/
      ModerationQueue.razor
      Components/
        ModerationCaseCard.razor
      Services/
        ModerationApiClient.cs

  Shared/
    Layout/
      MainLayout.razor
      NavMenu.razor
    Components/

  Pages/
    NotFound.razor

  wwwroot/
    index.html
    appsettings.json

  _Imports.razor
  App.razor
  Program.cs
```

### Large Features
- Split large features into operation-oriented or workflow-oriented subfolders instead of growing a single large service or component collection.
- API examples include:
  - `Features/Posts/CreatePost/`
  - `Features/Posts/EditPost/`
  - `Features/Posts/GetPost/`
- Web examples include:
  - `Features/Posts/Create/`
  - `Features/Posts/Details/`
  - `Features/Posts/Feed/`
- Keep operation-specific services, validators, mappings, and presentation models inside the corresponding operation or workflow folder.
- Keep entities and configurations shared by multiple API operations in the feature's `Persistence` folder.
- Keep components, API clients, or state shared by multiple Web workflows at the owning feature's common level.
- Do not require the Web and API to use identical operation names or folder depths.
- Query and operation class names do not imply a requirement for MediatR, CQRS infrastructure, or another framework.
- Use namespaces that match the folder structure, such as:
  - `Sayyit.API.Features.Posts.CreatePost`
  - `Sayyit.Web.Features.Posts.Details`

### API Responsibilities and Boundaries
- Keep controllers thin: handle HTTP concerns and delegate application workflows to feature services.
- Put scoring, moderation, eligibility, and authorization decisions in explicit server-side services or rule classes, not in controllers, mapping profiles, or EF configurations.
- Keep rules independent of HTTP and persistence concerns where practical.
- Give each entity and business rule a clear owning feature. Do not duplicate them across features.
- Coordinate cross-feature business workflows through explicit service boundaries; never call another feature's controller.
- Put genuinely shared technical infrastructure in `Infrastructure`.
- Put only small, genuinely shared application primitives in `Common`; do not use it as a miscellaneous folder.
- Do not add interfaces, repositories, separate class libraries, or generic abstractions solely to satisfy a folder template.

### Web Responsibilities and Boundaries
- Keep presentation and interaction logic in Razor components; move reusable workflows and API access into focused feature services.
- Use typed HTTP clients or well-encapsulated API client services rather than scattering raw HTTP calls across components.
- Preserve existing named-client registrations and authentication message-handler behavior unless the requested work requires changing them.
- Keep feature-specific UI state within its owning feature. Do not introduce application-wide state services for state used by only one feature.
- Components may compose components owned by other features when appropriate. Avoid coupling them to another feature's internal workflow or mutable state.
- Use explicit service boundaries for cross-feature workflows rather than invoking another feature's page or component as an application service.
- Client-side validation, visibility checks, and disabled controls are user-experience behavior, not security boundaries.
- Keep authoritative scoring, moderation, eligibility, ownership, and authorization enforcement in the API.
- Do not duplicate authoritative business-rule implementations in the Web project.
- The Web project must access backend data through the API, never through direct database connections.
- Do not introduce new state-management, mediation, or mapping packages solely to support the folder structure.

### Shared Code and Infrastructure
- Keep code inside its owning feature unless it is genuinely feature-neutral.
- Shared use alone does not automatically remove feature ownership. Authentication services, for example, may remain owned by Authentication while supporting the entire Web application.
- In the API, use `Infrastructure` for shared technical concerns and `Common` for small shared application primitives.
- In the Web project, use `Shared` primarily for feature-neutral UI and layout.
- Introduce Web `Infrastructure` only when a concrete, feature-neutral technical concern needs it; do not create it preemptively.
- Avoid catch-all Shared, Common, Helpers, Utilities, or Services folders that obscure ownership.
- Do not move server-side implementations into a shared client/server library merely because both projects use related terminology.

### EF Core and Database Ownership
- Use a shared `SayyitDbContext` by default, with `Infrastructure/Persistence` as its intended shared location.
- Respect the current context placement until relocating it is part of the requested work or necessary for cross-feature persistence changes.
- Keep feature-owned entities and `IEntityTypeConfiguration<T>` implementations under that feature's `Persistence` folder.
- Feature services and queries may use the context directly. Add focused repositories only when they provide a concrete benefit.
- Do not introduce one DbContext per feature without an explicit architectural decision.
- Keep SQL schema definitions and schema deployment in the existing `Sayyit.Db` SQL project and DACPAC workflow.
- Do not introduce EF migrations, `Database.Migrate()`, or `EnsureCreated()` as competing schema-management mechanisms.
- Preserve existing connection-string configuration and authentication patterns.
- Do not add EF Core persistence or database credentials to `Sayyit.Web`.

### Contracts, Presentation Models, and AutoMapper
- Keep shared API/frontend request and response DTOs in `Sayyit.Contracts`, following the existing DTO guidance.
- Do not duplicate shared DTOs inside API or Web feature folders.
- Do not expose EF entities as API contracts.
- Reuse shared DTOs in Web components where appropriate.
- Add feature-local presentation models only when the UI needs distinct presentation state or behavior; do not create one-to-one DTO copies without a concrete benefit.
- Do not add UI-only state to shared contracts merely to avoid a presentation model.
- If AutoMapper is used, keep each profile in its owning feature or operation folder.
- Use AutoMapper for data-shape mapping only, never for scoring, authorization, moderation decisions, or other business rules.
- Prefer explicit assignments for security-sensitive updates. Never broadly reverse-map client input onto trusted identity, ownership, scores, moderation state, or audit fields.
- Do not introduce AutoMapper or another mapping package solely because a Mapping folder appears in an example.

### Refactoring and Validation
- Keep organizational changes focused and preserve behavior unless the requested work explicitly changes behavior.
- When moving C# or Razor files, update namespaces, using directives, dependency-injection registrations, component references, and tests as needed.
- Keep Razor code-behind namespaces aligned with their components.
- Update root or feature-local `_Imports.razor` files as appropriate. Do not assume Razor imports apply to C# files.
- Preserve existing `@page` route templates, authentication callback paths, redirect URIs, API routes, and HTTP-client names unless explicitly changing those contracts.
- Do not change environment selection, appsettings loading, Azure configuration, or deployment workflows as incidental cleanup.
- Build affected projects after organizational changes.
- For Web routing or authentication moves, verify affected routes, unknown-route handling, sign-in, sign-out, authentication callbacks, and authenticated API access.
- **Ignore test requirements**; do not add tests or test-host scaffolding to the codebase.