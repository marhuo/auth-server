# CLAUDE.md — Backend

Backend-specific guidance for AuthServer.

## Overview

ASP.NET Core Web API on .NET 10 (C# 14), PostgreSQL via EF Core (Npgsql, snake_case naming), Serilog logging to PostgreSQL. Multi-tenant: every business entity belongs to a tenant. The domain model is still evolving; there are no tests yet.

The code follows the layered structure below; deviations are listed under Documented exceptions.

## Commands

Run from `backend/`:

```bash
dotnet build AuthServer.slnx
dotnet run --project AuthServer.Web

# Add a migration (Package Manager Console)
Add-Migration <Name> -OutputDir Persistence/Migrations -Project AuthServer.Infrastructure -StartupProject AuthServer.Web
```

TypeGen generates TypeScript types for the admin UI: `TypeGen generate`.

`AuthServer.Web/readme.txt` is the developer's personal command cheat sheet. Keep it, and keep it in sync with this section.

## Architecture

| Project | Responsibility | References |
|---|---|---|
| `AuthServer.Domain` | Aggregates, repository interfaces, validation primitives | — |
| `AuthServer.Application` | Use-case services, validators, request/DTO messages | Domain |
| `AuthServer.Infrastructure` | Infrastructure code: EF Core, repositories | Application, Domain |
| `AuthServer.Web` | Controllers, middleware, composition root | all |

There is no separate shared/helpers project. General-purpose helpers that belong to no feature (e.g. date utilities) go in the `AuthServer.Domain.Common` namespace, where every layer can reach them.

The dependency direction is a rule, not just the current state. Do not add project or package references that break it (e.g. EF Core or ASP.NET in Domain). If new code seems to need one, use an interface in the inner layer or ask.

Code is organized by feature: `Domain/Clients/`, `Application/Clients/Messages/`, `Application/Clients/Services/`, etc.

Service and pipeline setup goes in a static `XxxSetup` class in `Web/Setup/` (namespace `AuthServer.Web.Setup`) with `AddXxx` for services and `UseXxx` for the pipeline, e.g. `SerilogSetup` (`AddLogging`, `UseLogViewer`), `HangfireSetup` (`AddBackgroundJobs`, `UseBackgroundJobs`), `AuthenticationSetup` (`AddAuthServerAuthentication`, `AddAuthorizationPolicies`), `ErrorResponseSetup` (`AddErrorResponses`, `UseErrorResponses`), `RateLimitSetup` (`AddRateLimits`), `TurnstileSetup` (`AddTurnstile`) and `CorsSetup` (`AddCorsPolicy`). A class that configures one library is named after it (`SerilogSetup`, `HangfireSetup`); otherwise after the concern (`AuthenticationSetup`). Add new setup code there, not inline in `Program.cs`. A `UseXxx` method is added only when it does more than call one built-in middleware; built-in calls whose order matters (`UseRouting`, `UseRateLimiter`, `UseCors`, `UseAuthentication`, ...) stay in `Program.cs` so the pipeline order is visible there.

Hangfire recurring jobs are classes in `Web/Jobs/` (namespace `AuthServer.Web.Jobs`, e.g. `SerilogCleanupJob`, `ExpiredTokenCleanupJob`, `TenantDomainRecheckJob`) and are scheduled in `HangfireSetup.UseBackgroundJobs`. A job is thin like a controller: it calls an Application service. The exception is `SerilogCleanupJob`, which works directly on the log database because the log table belongs to `SerilogSetup`, not to any layer.

## Rules

### General

1. **Namespaces:** Every file has a file-scoped namespace that matches its folder path (e.g. `AuthServer.Domain.Clients`). No types in the global namespace.
2. **Async suffix:** Every method returning `Task`/`ValueTask` ends with `Async`. Exception: controller actions (ASP.NET strips the suffix, which breaks `CreatedAtAction(nameof(...))`).
3. **Guids:** Use `Guid.CreateVersion7()`. `Guid.NewGuid()` is banned (`BannedSymbols.txt`, build error). Version 7 Guids are time-ordered, so new ids are increasing, which keeps database indexes efficient.
4. **User-facing text** is bilingual: every validation message has `MessageEn` and `MessageFi`. All other strings (exception messages, logs, comments) are in English.
5. **Query in the database:** Filtering, sorting and paging run in the database via `IQueryable`. Loading a full set into memory is allowed only as a documented exception (see below) and must have a comment pointing here.
6. **Named arguments for aggregate construction:** Calls to an aggregate's static `Create(...)` factory (in services) and to its rehydration constructor (`ToDomain` in EF entities, and the `new Xxx(...)` inside `Create`) always use named arguments, one per line (`id: Id, name: Name, ...`). These methods have many parameters of the same type, and positional arguments in the wrong order would still compile.
7. **Comments describe their own layer only:** A comment says what this code does and, when needed, why. It never describes an upper layer: a service does not mention pages, controllers or what the user sees, and the domain does not mention services. Write `// Returns nothing on purpose: otherwise anyone could check which emails are registered.`, not `// The page always shows the same message.` Put notes about a single case next to the branch that handles it. Keep comments short.

### Domain

**Structure and naming**

- Aggregates are grouped into feature folders with a plural namespace: `Client` → `AuthServer.Domain.Clients`. The folder contains the aggregates, repository interfaces (`IXxxRepository`) and query classes.
- A small, closely related aggregate shares the folder of its main aggregate instead of getting its own. It still has its own repository and service. This is the developer's decision: when creating a new aggregate, ask which folder it belongs to.
- When a repository lookup needs several parameters, wrap them in a `XxxQuery` class in the Domain (e.g. `ClientPagedListQuery : PagedListQuery`). Application-side counterparts are `XxxRequest`.
- The `Entity` suffix is reserved for EF entities in Infrastructure. Never use it for domain types.
- `AuthServer.Domain.Common` holds building blocks used by almost every aggregate and is imported via `GlobalUsings.cs`. Do not add `using AuthServer.Domain.Common;` inside the Domain project.

**Aggregates**

- Aggregates inherit `AggregateRoot` (soft-deletable ones `SoftDeletableAggregateRoot`) and implement `ITenantScoped` when the data is tenant-specific. Data that belongs to a resource server (e.g. `Role`) is not tenant-scoped.
- The logged-in user and tenant are read from `CurrentUser`.
- Properties have `private set`.
- New instances are created through a static `Create(...)` factory that validates and trims input. The public full constructor is only for rehydration from persistence. When the new instance always starts in a specific state, the factory may have a more descriptive name that starts with `Create`, e.g. `User.CreateInvited` (a new user is always invited, `UserStatuses.Invited`). The rules for `Create` (named arguments, validation) apply to such factories too.
- State changes go through `SetXxx(...)` methods that: call `EnsureNotDeleted()`, validate, return early if nothing changed, otherwise update state.
- Field-level validation rules are `public static void ValidateXxx(ErrorCollector errors, string value)` methods on the aggregate. `Create`, `SetXxx` and the Application validator all call them, so each rule exists in one place.
- Time is passed in as a parameter (`DateTimeOffset now`); services get it from `TimeProvider`. Never call `DateTimeOffset.UtcNow` in the domain.
- Deletion is soft: `SoftDeletableAggregateRoot.Delete(now)` sets `DeletedAt` and exposes `Deleted => DeletedAt is not null`. The change is saved with the repository's `UpdateAsync`; repositories of soft-deletable aggregates have no `DeleteAsync` method.
- Validation constants (e.g. `MaxNameLength`) are `const` on the aggregate. Infrastructure reads them from there; never duplicate the value.
- Enum type names are plural: `ClientTypes`, `OrderDirections`.
- There are no domain events.

**Validation and exceptions**

- Use `ErrorCollector` to report several errors at once (`ThrowIfAny()`). Throwing a `ValidationException` directly on the first error is also fine.
- Error keys are property names (`nameof(Name)`).
- Validate in stages: if the input already has errors, throw before any check that needs the database. Never call a repository while errors are known. Example: `Name1` and `Name2` are both required and unique; if `Name1` is missing, do not query whether `Name2` is unique.
- `ValidationException` is shown to the end user through the API and UI. Its messages must never contain sensitive information.
- Situations that should never happen with a correctly working client (e.g. creating data for another tenant) are not validation errors. Throw a dedicated exception such as `TenantMismatchException` (guard with `TenantMismatchException.ThrowIfMismatch(...)`), which is not exposed to the user.
- **Domain models must be serializable with `System.Text.Json`:**
  - The full rehydration constructor is marked `[JsonConstructor]` and takes every piece of persisted state as a parameter. Parameter names match property names (`name` ↔ `Name`).
  - Computed properties (e.g. `Deleted`) are marked `[JsonIgnore]`.
  - No persisted state outside the constructor parameters. Do not add state that can only be set through methods.
  - A new aggregate or a new property must survive a serialize → deserialize round trip unchanged.

### Application

**Structure**

- Folders and namespaces mirror the Domain feature folders (`Clients/`), including shared folders for closely related aggregates.
- `Xxx/Messages/`: everything that crosses the Application boundary — requests, DTOs and their enums. **All** public input and output types live under a `Messages` namespace, so that TypeGen can generate TypeScript types from them. Never put a request or DTO elsewhere.
- `Xxx/Services/`: logic — services (command handlers) and validators.
- `Common/Messages/`: DTOs and requests shared by all features (`PagedListDto`, `PagedListRequest`, `ErrorResponseDto`). `Common/` (outside `Messages`) is for shared services.

**Messages**

- The Application layer never returns domain objects. It always maps them to DTOs.
- Naming: returned objects are `XxxDto` (`Client` → `ClientDto`). Requests (CQRS commands and queries) end with `Request`: `XxxCreateRequest`, `XxxUpdateRequest`, `XxxPagedListRequest`.
- Use `class` with `required` + `init` properties.
- Requests and DTOs never contain `TenantId`. The tenant always comes from the authenticated user (`CurrentUser.TenantId`).
- DTOs never expose domain enums. Define a `XxxDtoKinds`-style enum and map with `ToDomain()` / `ToDto()` extension members.
- Mapping domain → DTO is a static `XxxDto.ToDto(domain)` method.

**Services**

- Services (`XxxService`) orchestrate: load, validate, call domain methods, save. Business rules belong in the domain.
- Validation logic lives in validator classes (`XxxValidator`), not in services. Validators hold rules that need repository access (e.g. name uniqueness); rules that the aggregate can check on its own stay in the domain.
- Validators never depend on other validators. A validator uses repositories and the aggregates' static `ValidateXxx` methods only. When two validators need the same lookup, add a repository method for it (e.g. `IResourceServerRepository.GetAuthServerAsync`).
- The only check a service does itself is whether the loaded object exists: `?? throw new ValidationException(Errors.NotFound(...))`.
- Flow of a write operation: load → `validator.ValidateXxxAsync(...)` → domain call (`Xxx.Create(...)` or one `SetXxx(...)` per field) → repository `CreateAsync`/`UpdateAsync`. The validator first collects all input errors with the aggregate's static `ValidateXxx` methods and throws them together, then does the checks that need repositories. The domain calls therefore normally do not fail; their own validation is a safety net.
- Roles is the reference implementation of this structure: `Domain/Roles`, `Application/Roles`, `Infrastructure/Persistence/RoleRepository.cs`, `Controllers/Admin/RoleController.cs`.
- Services never depend on other services. A service may use repositories, validators and other non-service components, but not another `XxxService`. When two services need the same logic, move it to the domain or to a separate shared component that both use. Do not add service-to-service dependencies.
- Register services and validators in `Application/DependencyInjection.cs`.

### Infrastructure

**Structure**

- `Persistence/`: repository implementations of the Domain interfaces (`XxxRepository : IXxxRepository`) and the `DbContext` (`AuthDbContext`).
- `Persistence/Ef/`: everything EF Core specific — entities, base classes, serializers.
- `Persistence/Seed/`: database seeding run at startup (`CommonDatabaseSeeder` for every environment, `DevDatabaseSeeder` for development data). Seeders write EF entities directly.
- `<Feature>/`: implementations of other domain interfaces of a feature, e.g. `Users/PasswordHasher.cs` (`IPasswordHasher`), `Users/LoggingUserEmailSender.cs` (`IUserEmailSender`) and `OAuth/SigningKeyService.cs` (`ITokenSigner`, `ISigningKeyManager`).
- `Persistence/Migrations/`: generated EF migrations. Do not edit by hand. The project is at an early stage: the developer drops the database and regenerates a single `InitialCreate` migration after model changes. Do not create migrations and do not write data-migration or backward-compatibility code for schema changes; change the model and tell the developer that the database and `InitialCreate` need to be regenerated.

**EF entities**

- Named after the domain model with an `Entity` suffix: `Client` → `ClientEntity`. Child tables follow the same pattern: `ClientRedirectUriEntity`.
- Each aggregate entity has `FromDomain`, `UpdateFrom` and `ToDomain`.- Tenant and soft-delete filtering are applied globally by query filters via `ITenantScopedEntity` and `ISoftDeletableEntity`. Do not add these conditions manually.

**Soft delete**

- Business data is never physically deleted. Soft-deletable entities implement `ISoftDeletableEntity` and have a nullable `deleted_at` column; a row is deleted when `deleted_at IS NOT NULL`.
- The global query filter hides deleted rows. Read deleted rows only when explicitly needed, with `IgnoreQueryFilters()`, and keep the tenant condition in that case.
- Unique constraints on soft-deletable tables are partial (`.HasFilter("deleted_at IS NULL")`), so a deleted row does not block reusing its name or other unique value.

**Repositories**

- Repositories implement the domain interface and return domain objects, never EF entities.
- A query method belongs to the repository of the aggregate it reads, not of the aggregate it is filtered by. E.g. a user's role names are in `IRoleRepository` (`GetTenantRoleNamesAsync(userId, tenantId)`), and revoking a user's refresh tokens is in `IRefreshTokenRepository`.
- Methods that insert new data are named `CreateXxxAsync` / `CreateAsync`, never `Add...`/`Insert...` (e.g. `CreateAsync(Role role)`, `CreateAsync(PasswordToken token)`). Updates are `UpdateAsync`.
- Read queries use `AsNoTracking()`.
- Register repositories in `Infrastructure/DependencyInjection.cs`.

**Database queries**

- Never filter, join or sort on a column that has no index. When a new query needs a column that has no index, add the index (in `AuthDbContext.OnModelCreating`) in the same change.
- Tenant-scoped indexes start with `TenantId`, because every query is filtered by tenant.
- Indexes on soft-deletable tables are partial: `.HasFilter("deleted_at IS NULL")`.
- Substring search (`ILIKE '%term%'`) needs a trigram GIN index: `.HasMethod("gin").HasOperators("gin_trgm_ops")`. The `pg_trgm` extension is enabled.

### API

**Controller groups**

- `Controllers/Admin/`: controllers that serve the admin UI (`admin-ui`). They may return Application-layer DTOs and requests directly.
- `Controllers/Api/`: controllers for external integrations. They use their own DTOs on top of the Application layer: the controller maps its own request to an Application request, calls the service, and maps the returned Application DTO to its own DTO. This keeps the integration contract stable when the admin UI contract changes. Never return Application DTOs or accept Application requests directly from these controllers. The DTOs live in `Controllers/Api/Dto/` and follow the same naming as Application messages (`XxxDto`, `XxxCreateRequest`, ...).
- `Controllers/OAuth/`: OAuth 2.0 / OpenID Connect protocol endpoints. Their routes and payloads follow the protocol specifications, not the conventions below.

**Conventions**

- One controller per Application feature namespace (`Application/Users` → `UserController`), named after the folder's main aggregate. Endpoints of a secondary aggregate in the same folder are sub-routes of the main controller (`POST api/admin/user/invite`). Applies to `Admin/` and `Api/` controllers.
- Controllers are named in singular form: `ClientController`, `UserController`. File name matches the class name.
- Routes are singular and kebab-case: `api/admin/client`, `api/admin/resource-server`, `api/user`.
- Standard endpoints: `GET` (paged list, `[FromQuery]`), `GET {id:guid}`, `POST by-ids` (body: list of ids), `POST` (create, returns `201` with id), `PUT` (update, id in body, returns `204`), `DELETE {id:guid}` (returns `204`; takes the id only, no request object).
- Controllers are thin: they call service methods (several are fine when an endpoint combines use cases) and contain no business logic. Api controllers additionally do their DTO mapping.
- Errors: `ValidationException` is converted by `ValidationExceptionHandler` into `ErrorResponseDto`, always with status `400`. Clients distinguish error types by `ErrorDto.Code` (`Required`, `Taken`, `NotFound`, ...), not by HTTP status.

## Documented exceptions

- **The OAuth namespace is the protocol area and may use domain objects:** `Application/OAuth/Services` serves the login, forgot password and set password pages, the OAuth endpoints (`Controllers/OAuth/`) and the sign-in cookie validation (`Web/Authentication/UserSession`), not the admin API. Its services may return or take domain objects when the protocol needs their behavior: `UserAuthenticationService` (`User`: `CanSignIn`, lockout), `ClientAuthenticationService` (`Client`: allowed redirect URIs, grant types, scopes), `AuthorizationCodeService.RedeemAsync` and `TokenService.RenewRefreshTokenAsync` (`AuthorizationCode`, `RefreshToken`). These are not exposed through any API as such and are not TypeGen messages. Feature services (`UserService`, `ClientService`, ...) do not have sign-in or protocol methods.
- **Protocol logic in OAuth controllers:** `Controllers/OAuth/` contains the OAuth/OIDC protocol flow (grant handling, scope and tenant checks, error responses). This is intentional for now; do not move it without asking.
- **Expired tokens are physically deleted:** authorization codes, refresh tokens and password tokens are short-lived security records, not business data. `ExpiredTokenCleanupService` (run nightly by `ExpiredTokenCleanupJob`) deletes them with the repositories' `DeleteExpiredAsync` once they have been expired for 7 days (`ExpiredTokenCleanupService.ExpiredTokenRetention`). The deletes use `IgnoreQueryFilters()` so that tokens of soft-deleted clients are deleted too.
- **Signing keys have no domain aggregate:** `Infrastructure/OAuth/SigningKeyService` implements `ITokenSigner` and `ISigningKeyManager` directly on `SigningKeyEntity`, because the keys are pure infrastructure (RSA, Data Protection).
- **Data Protection keys are in the main database:** `AuthDbContext.DataProtectionKeys` (ASP.NET's own `DataProtectionKey` type, no `Entity` suffix, no repository) is used by `PersistKeysToDbContext` in `Program.cs`. They must stay in the same database as the signing keys they protect, so that one backup holds both. Never delete them: anything protected with a deleted key, including the signing keys, becomes unreadable.

## Current state

- The `/serilog-ui` log viewer (`SerilogSetup.UseLogViewer`) and the `/hangfire` dashboard (`HangfireSetup.UseBackgroundJobs`) require the `Admin` policy (`AuthorizationPolicies`), i.e. the `AuthServerAdmin` role.
- Every feature is in the layered structure: Roles, ResourceServers, Scopes, Clients, Tenants, Users, OAuth.
- Token lifetimes are stored per client (`Client.AccessTokenLifetime`, `IdTokenLifetime`, `RefreshTokenLifetime`, `interval` columns). ID and refresh token lifetimes are null for clients that do not sign in users (machine clients); `Client.ValidateTokenLifetimes` enforces this. They cannot be edited yet: `ClientService.CreateAsync` and the seeder set the `Client.DefaultXxxLifetime` values. `ClientDto` exposes them in seconds and the client details page shows them read-only. `AuthTokenOptions` (section `Tokens`) holds only the `Issuer`.
- Self-registration (`Register` page, `UserAuthenticationService.RegisterAsync`): opened from the login page ("Luo tunnus") or by a product with `prompt=create` (`AuthorizeController`). It creates an invited user and sends a link to the SetPassword page, like an invitation; setting the password proves the email. The page always shows the same message (no account enumeration): an existing active user gets a "sign in instead" email, an invited user a new link, a locked user nothing. Rate limited per IP like "forgot password". The link carries the `returnUrl` (the product's sign-in request); after setting the password the user is signed in and continues there.
- Users are also created by invitation (`UserService.InviteAsync`, `POST api/admin/user/invite` and `POST api/user/invite`): the user sets their password with the invitation link. An invitation may name the client the user signs in with (`ClientId`, also in `POST api/tenant`); the link carries it, and the PasswordSet page then shows a button to the client's `InitiateLoginUri` (like Okta's "Initiate login URI": the product's address that starts the sign-in, set in the admin UI). Without it the page only tells the user to go back to the app. `User.Status` (`UserStatuses.Invited` / `Active`) is computed from `HasPassword`: setting the first password accepts the invitation. Locking is separate from the status. Inviting an existing user only adds the roles. An admin can send a new invitation link to a user without a password (`UserService.ResendInviteAsync`, `POST api/admin/user/{id}/resend-invite`, "Lähetä uudelleen" on the user details page); the external API does the same by calling `invite` again with the same email. The external API must never give auth server roles: it resolves role names among the products' roles only (`RoleService.GetProductRoleIdsByNamesAsync`), it always gives the roles with a tenant, and the validator rejects an auth server role with a tenant. Keep it that way if the external API changes. Otherwise it accepts any product's roles: giving the right role is the calling product's responsibility.
- Domain verification (`TenantDomain`, in the Tenants folder; docs/domainin-vahvistus.md): a tenant adds a domain and gets a TXT record (`TenantDomain.TxtRecord`); the domain is verified when its DNS has the record (`IDnsTxtLookup`, `Infrastructure/Tenants/DnsTxtLookup` with the DnsClient package). A domain is verified for one tenant at a time (unique partial index `ix_tenant_domains_verified_name`). Tenants do it through their product: the external API `api/tenant/{tenantId}/domain` (list, add, `verify`, delete), because only the product knows its tenant admins. The admin UI can also list, add, verify ("Tarkista") and delete a tenant's domains (`api/admin/tenant/...domain`); verification still requires the DNS record. `TenantDomainRecheckJob` re-reads verified domains weekly and removes the verification when the record is gone; a failed lookup changes nothing. Verification gives no rights yet: managing the users of a verified domain is a later step.
- Sign-in without an organization: when the user has no tenant in the client's target (and the client is not tied to a tenant), `AuthorizeController` signs them in without one. `AuthorizationCode.TenantId` and `RefreshToken.TenantId` are then null, and the ID and access tokens have no `tenant_id` and no `role`. The product decides what to do, e.g. lets the user create an organization with `POST api/tenant` and signs them in again (`demos/RazorPagesDemo`).
- A client's resource server is its **target** (`Client.TargetResourceServerId`: the product users sign in to, or the API the client calls), and a client has only its target's scopes (plus global scopes for clients that sign in users), so an access token has one audience. Creating a resource server also creates its backend client `<audience>-backend` (`ResourceServer.BackendClientId`): a machine client whose target is the auth server, with the scope `auth_server:api` (`SystemScopes.AuthServerApi`), for calling the external API. Nothing links that client to the resource server afterwards, and deleting the resource server does not delete it.
- The pages that send emails (`Register`, `ForgotPassword`) are protected by Cloudflare Turnstile as well as the IP rate limit: the `_Turnstile` partial shows the widget and `Captcha/TurnstileVerifier` checks the token before the email is sent. The keys are in section `Turnstile` and required at startup (`TurnstileSetup`); development uses Cloudflare's test keys (`appsettings.Development.json`, always pass). There is no per-email limit.
- Password links: `PasswordToken` (type `Invitation`, valid 7 days, or `Reset`, valid 1 hour) is a single-use link to the SetPassword page; only its hash is stored, and a new token invalidates the user's earlier unused ones. "Forgot password" (`ForgotPassword` page, `UserAuthenticationService.RequestPasswordResetAsync`) always shows the same message (no account enumeration), sends nothing to an admin-locked user and is rate limited per IP. Setting a password ends the automatic lockout and revokes refresh tokens. It also signs the user in at the auth server (SetPassword page), replacing any earlier sign-in in that browser, e.g. the admin who sent the invitation. `PasswordToken` has no DTO and is never returned through the API. Emails are only logged for now (`LoggingUserEmailSender`).
- There is no user name: the email is the login identifier and the `name`/`preferred_username` claim. The email is unique among current users only: when it is changed, the old address is free for another user at once (e.g. a company gives a leaver's address to a new employee). So an email does not identify a person over time: logs and products identify users by `Id` (`sub`), never by email.
- Users are never deleted (`User` is not soft-deletable): access is removed by removing roles, which also revokes the user's refresh tokens. A person whose roles were removed can be invited again.
- Locking: `User.Locked` is set by an admin and lasts until unlocked; `User.LockedUntil` is the automatic lockout after failed logins and ends by itself. Unlocking also ends the automatic lockout. `User.CanSignIn(now)` is checked at login, at the authorize and token endpoints and when the sign-in cookie is validated.
- The sign-in cookie (login page, authorize endpoint, admin UI) holds the auth server roles from sign-in. `Web/Authentication/UserSession` re-checks the user (lock, roles, email) at most once per `Authentication:SessionValidationInterval` (default 5 minutes), so locking a user or removing a role takes effect within that interval.
