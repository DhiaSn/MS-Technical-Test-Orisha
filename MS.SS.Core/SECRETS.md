# Backend secrets and required settings

Values below are never committed. Locally they live in user-secrets (project `MS.SS.Core.API`,
`UserSecretsId` `ms-ss-core-api`); the Docker Compose stack sets the same keys as environment variables
instead (`:` becomes `__`, e.g. `TokenOptions__Secret`), with throwaway local defaults already in
`docker-compose.yml` — nothing to set up there.

**Keep this file in sync:** any new secret or required setting added to the backend must be added here
in the same change.

## Secrets

| Key | Required | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Always (outside Docker) | PostgreSQL connection string. Also read by the EF design-time factory for migrations. |
| `TokenOptions:Secret` | Always (outside Docker) | JWT signing key, at least 32 bytes. The boot fails when missing or too short. |

## Local setup (running `dotnet run` or `dotnet ef` outside Docker)

Start PostgreSQL alone first: `docker compose up -d postgres` (from `src/`), then:

```bash
cd MS.SS.Core/MS.SS.Core.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=reception;Username=reception;Password=reception_local_dev"
dotnet user-secrets set "TokenOptions:Secret" "<any string of at least 32 bytes>"
```

`dotnet ef migrations add` / `dotnet ef database update` read the same user-secrets store (see
`MS.SS.Core.Infrastructure/Database/Context/AppDbContextFactory.cs`) as long as you run them from
`MS.SS.Core.Infrastructure` with `MS.SS.Core.API` as a sibling directory (the default solution layout),
or pass `--project MS.SS.Core.Infrastructure --startup-project MS.SS.Core.API` from elsewhere.

## Where the secrets store lives

`dotnet user-secrets` writes to a per-user, per-project file outside the repo:
`%APPDATA%\Microsoft\UserSecrets\ms-ss-core-api\secrets.json` on Windows
(`~/.microsoft/usersecrets/ms-ss-core-api/secrets.json` on Linux/macOS). `dotnet user-secrets list`
(run from `MS.SS.Core.API`) shows what is currently set; `dotnet user-secrets remove <key>` clears one.

## Why not `appsettings.Development.json`

Earlier in the build these two values were throwaway strings committed in
`appsettings.Development.json`. They are now only ever supplied through user-secrets (local, outside
Docker) or the Compose environment (containers) — matching the pattern used on the reference project this
one's architecture was copied from. `appsettings.json` keeps both keys empty; the app refuses to start
without them, which is the point (fail fast on missing configuration, never a silent default).
