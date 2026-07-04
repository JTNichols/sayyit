Project overview

Sayyit is a politically balanced, user-moderated social platform inspired by X.com and Reddit.com, where moderation authority is distributed across users with visible left/right political scores. The core rule is that moderation actions such as blocking and banning must require balanced approval from both the left and the right rather than control by a single faction. The system should preserve transparent, rules-based moderation and keep implementation details aligned with the public algorithm and rule descriptions in the project repositories.
Repository context

    Primary application repo: JTNichols/sayyit.

    Infrastructure repo: JTNichols/sayyit-iac.

    Application stack: .NET 10, C#, REST API backend, Blazor WebAssembly standalone frontend, Azure SQL, Azure-hosted authentication and hosting.

    Local development environment: Windows, usually Visual Studio or VS Code, with PowerShell or CMD for terminal work.

    Cloud environment: Azure subscription sayyit.subscription, tenant/domain sayyit.onmicrosoft.com, resource group sayyit_rg1.

    Domain management: sayyit.com is managed through NearlyFreeSpeech.

Architecture assumptions

When generating code, documentation, tests, or refactors, assume the following unless the surrounding code clearly shows otherwise:

    Frontend is a Blazor WebAssembly standalone app.

    Backend is a single RESTful API written in C# on .NET 10.

    Persistent data is stored in Azure SQL.

    Authentication, app hosting, and infrastructure are in Azure.

    Infrastructure is defined in Bicep and deployed through the sayyit-iac repository.

Coding style
General

    Prefer clear, maintainable, production-style C# over clever shortcuts.

    Follow existing naming, folder, and namespace conventions in the repository before introducing new patterns.

    Keep files focused; avoid large mixed-responsibility classes.

    Favor dependency injection, interfaces where they improve testability, and small composable services.

    Use async/await for I/O-bound work. Do not block on tasks.

    Add null handling and input validation at API boundaries.

    Write code that is easy to review and reason about.

C# and .NET

    Target .NET 10 patterns already used by the solution.

    Prefer standard ASP.NET Core conventions for controllers, minimal APIs, middleware, configuration, and logging based on what the repo already uses.

    Use strongly typed models and DTOs for API contracts.

    Keep business rules out of controllers; place them in domain or application services.

    Use ILogger<T> for operational logging and avoid logging secrets, tokens, connection strings, or sensitive moderation data.

    When adding configuration, prefer appsettings plus environment overrides and document required settings.

Blazor WebAssembly

    Keep presentation logic in components and move reusable logic to services.

    Use typed HTTP clients or well-encapsulated API access layers rather than scattering raw HTTP calls across components.

    Optimize for responsive UI and clear state transitions, especially for moderation flows, voting, and score displays.

    Avoid tightly coupling components to backend response shapes when a view model improves clarity.

SQL and data access

    Design schema and queries for auditability, moderation transparency, and future analytics.

    Treat left/right scoring, moderation votes, and user actions as domain records that may need traceability. 

    Avoid destructive data operations unless clearly requested.

Domain rules to protect

Copilot should preserve these product assumptions in all generated code and suggestions:

    Moderation is community-driven, not centrally ideological.

    Left/right political scores are first-class domain concepts and visible to users.

    Moderation actions requiring political balance should be modeled explicitly and validated server-side.

    Authorization and moderation logic must not rely only on UI checks.

    Auditability matters: important moderation decisions should be traceable.

    Public trust matters: avoid hidden heuristics when explicit rules exist.

If a requested change could break political-balance moderation, call out the risk in comments or surrounding documentation.
API guidance

    Design REST endpoints around clear resources and actions.

    Validate all incoming models.

    Return appropriate HTTP status codes and problem details where useful.

    Keep authorization checks close to the sensitive operation.

    For moderation endpoints, explicitly verify balanced approval conditions in backend logic.

    Prefer idempotent patterns where retries are likely.

Testing guidance

    Add or update tests whenever behavior changes.

    Prioritize tests for moderation workflows, political-balance calculations, permission checks, score updates, and audit logging behavior.

    Cover edge cases such as tie conditions, duplicate votes, race conditions, missing identities, and invalid score ranges.

    Favor readable unit and integration tests over brittle implementation-heavy tests.

Security guidance

    Never hardcode secrets, connection strings, publish profiles, client IDs, or tenant IDs in source files.

    Assume secrets come from Azure Key Vault alone.

    Follow least-privilege access patterns for app identities and deployment identities.

    Validate and authorize all state-changing API operations.

    Treat moderation, identity, and political-score data as sensitive.

    Sanitize logs and error messages.

Azure and infrastructure guidance

    The infrastructure repository uses GitHub Actions with Azure federated identity login and Bicep deployments. Generated infrastructure-related changes should remain compatible with that workflow.

    Keep infrastructure changes in JTNichols/sayyit-iac aligned with Bicep-driven deployment.

    Assume GitHub Actions authenticate to Azure using AZURE_CLIENT_ID, AZURE_TENANT_ID, and AZURE_SUBSCRIPTION_ID rather than stored credentials.

    Preserve the resource group name sayyit_rg1 unless explicitly told to change it.

    Respect the existing pattern of a deployment principal receiving rights needed to deploy infrastructure and manage Key Vault secrets.

    Prefer Azure-native configuration and identity features over manual secret distribution.

    The environment includes an App Service plan, a web app, and a Key Vault with RBAC enabled, and the web app uses a system-assigned managed identity while the deployment principal is granted Key Vault secret management rights.
GitHub Actions guidance

    Keep workflow changes minimal and explicit.

    Prefer OIDC/federated credentials for Azure login over publish profiles or long-lived secrets when updating deployment automation.

    Preserve branch and environment assumptions unless the task explicitly changes release flow.

    Document any newly required repository secrets or environment variables.

The infrastructure workflow currently logs into Azure with azure/login@v2 using AZURE_CLIENT_ID, AZURE_TENANT_ID, and AZURE_SUBSCRIPTION_ID, then deploys main.bicep to sayyitrg1. The helper PowerShell script creates or reuses an Entra app and service principal, adds a GitHub federated credential for the target branch, and outputs those Azure identifiers for GitHub Actions secrets.
Documentation guidance

    When generating docs, explain why a change exists, not just what changed.

    Keep README and developer docs aligned with actual architecture.

    For setup steps, prefer Windows-friendly commands first, then optional cross-platform notes if helpful.

    Call out any Azure prerequisites, GitHub secrets, local environment variables, or required CLI tools.

Preferred response behavior for Copilot

When asked to implement a feature, Copilot should:

    Inspect nearby code and match existing patterns.

    Preserve the political-balance moderation model.

    Put domain rules in backend services, not just UI components.

    Add or update tests.

    Note any required config, schema, or infrastructure updates.

    Avoid introducing new frameworks or major architectural shifts unless requested.

When requirements are unclear, prefer small, reversible changes and leave concise TODO notes only where repository context is genuinely missing.
Avoid

    Do not replace balanced community moderation with admin-only shortcuts unless explicitly requested.

    Do not embed secrets or Azure credentials in code, scripts, or docs.

    Do not bypass backend validation for moderation or score-related actions.

    Do not introduce unnecessary packages or abstractions.

    Do not assume Linux-only local shell commands; default to Windows-friendly guidance for developer workflows.

Example prompts Copilot should handle well

    “Add a backend service that determines whether a ban vote has balanced left/right approval.”

    “Create a Blazor component that shows a user’s visible left/right score and moderation history.”

    “Refactor this moderation controller so policy checks live in a service and add tests.”

    “Update the Bicep and GitHub Actions workflow to support another environment using the current Azure OIDC pattern.”