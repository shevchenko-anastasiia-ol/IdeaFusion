# IdeaFusion Keycloak realm

`IdeaFusion-realm.json` is imported automatically at Keycloak startup by `AppHost.cs`
(`.WithRealmImport("../infrastructure")`). It is the single source of truth for the
`IdeaFusion` realm: clients, roles, identity provider, SMTP, password/OTP policy and
the seeded users.

The file is committed on purpose, so it must never contain real credentials. Every secret
is a `${PLACEHOLDER}` that Keycloak substitutes from its own container environment.

## One-time setup

AppHost user-secrets id: `70be0edd-1fed-4a44-b694-42a2bf7382e0`.

```bash
# Gmail SMTP (App Password, not the account password)
dotnet user-secrets set "smtp-user"           "you@gmail.com"            --project AppHost
dotnet user-secrets set "smtp-app-password"  "<16-char app password>"    --project AppHost

# Google OAuth client
dotnet user-secrets set "google-client-id"      "<client-id>.apps.googleusercontent.com" --project AppHost
dotnet user-secrets set "google-client-secret"  "<client-secret>"        --project AppHost

# Secret for the ideafusion-admin service account
dotnet user-secrets set "keycloak-admin-client-secret" "$(openssl rand -hex 24)" --project AppHost
```

`smtp-host` (default `smtp.gmail.com`) and `smtp-port` (default `587`) are plain
`AddParameter` values, so they can be overridden the same way if a different relay is needed.

## Google Cloud console

OAuth 2.0 client of type **Web application**, with authorised redirect URI:

```
http://localhost:8080/realms/IdeaFusion/broker/google/endpoint
```

## Gmail

1. Enable 2-Step Verification on the Google account.
2. Create an App Password for "Mail".
3. Put the account address in `smtp-user` and the 16-character app password in
   `smtp-app-password`. The regular Google password will not work against `smtp.gmail.com:587`.

## Re-importing after an edit

Keycloak only imports a realm that does not exist yet, and `.WithDataVolume()` keeps the
existing one across restarts. After editing `IdeaFusion-realm.json` use the **Reset** action
in the Aspire dashboard (or delete the Keycloak volume) and start again, otherwise the old
realm is kept.

## File name is load-bearing

The import directory is read on every start, and Keycloak decides how to treat each file from
its name. It expects `<realm-name>-realm.json`, so this file is `IdeaFusion-realm.json` to
match the `IdeaFusion` realm. A name like `ideafusion-realm.json` or `realm.json` makes
Keycloak infer a different realm name, run secondary import passes against a realm that does
not exist, and abort startup with `ERROR: Session not bound to a realm`.

If you ever split the seeded users into their own files, the names must be
`IdeaFusion-users-1.json` (and `IdeaFusion-federated-users-1.json` for federated users).
`README.md` in this directory is ignored, since only `.json` files are read.

## Seeded accounts

All four IDs match `IdentityDAL/Data/IdentitySeeder.cs` so the existing profile, team and
post rows resolve to real Keycloak subjects.

| Username    | Email                  | Password     | Roles                     |
|-------------|------------------------|--------------|---------------------------|
| AnnoGray    | anna@ideafusion.com    | `User@1234`  | author                    |
| VladBibber  | vlad@ideafusion.com    | `User@1234`  | author                    |
| EmilyFox    | emily@ideafusion.com   | `User@1234`  | author                    |
| Admin       | admin@admin.com        | `Admin@1234` | author, administrator    |

These are development credentials. Replace them before any shared or deployed environment.

## Token shape

The `ideafusion-frontend` client requests two extra client scopes so that a token issued to
the SPA can be validated by the microservices:

- `ideafusion-roles` maps realm roles to a flat `role` claim.
- `ideafusion-audience` adds `ideafusion_api` to `aud`, which is the
  `Keycloak__Audience` value already injected into IdentityAPI, ContentAPI, Collaboration.API
  and ApiGateway.

Team-scoped roles (`Owner`, `Leader`, `Member`) are not Keycloak roles; they stay in the
Collaboration database and are checked per team.
