# Keycloak setup for VaghtNegar

## 1. Start Keycloak locally

From the repository root:

```powershell
Copy-Item .env.example .env
notepad .env
docker compose up -d
docker compose logs -f keycloak
```

Set unique passwords in `.env` before starting the containers. Never commit the real `.env` file.

Wait until Keycloak has completed its startup. Open:

- Admin console: http://localhost:8080/admin/
- Realm issuer: http://localhost:8080/realms/vaghtnegar
- OpenID discovery: http://localhost:8080/realms/vaghtnegar/.well-known/openid-configuration

Sign in to the admin console with the bootstrap admin credentials from `.env`, then select the `vaghtnegar` realm.

The realm import creates these realm roles:

- `Patient`
- `Doctor`
- `Receptionist`
- `ClinicAdmin`
- `SuperAdmin`

It also creates the public OIDC client `vaghtnegar-web` with Authorization Code flow and PKCE S256, plus the `vaghtnegar-api` audience.

## 2. Configure the API

The API reads these settings from `appsettings.json` and supports environment-variable overrides:

- `Keycloak__Authority`
- `Keycloak__Audience`

Local defaults:

- Authority: `http://localhost:8080/realms/vaghtnegar`
- Audience: `vaghtnegar-api`

For production, use the public HTTPS issuer URL and keep HTTPS metadata validation enabled. Do not expose Keycloak's development mode or its admin console publicly without a production deployment configuration.

The API validates signature, issuer, audience, and token lifetime using Keycloak's OpenID Connect discovery metadata. Realm roles are mapped to .NET role claims. The protected endpoint `GET /api/v1/auth/me` returns the authenticated subject, username, email, and roles.

## 3. Create a test user

In the Keycloak admin console:

1. Open the `vaghtnegar` realm.
2. Go to Users and create a test user.
3. Set a username and email; enable the account.
4. Set a temporary password from the Credentials tab.
5. Assign one realm role, such as `Patient`, from Role mapping.

Self-registration is deliberately disabled in this first setup. Do not enable public patient registration until a verified mobile-number flow and safe patient-record linking have been implemented. Keycloak does not provide a ready-to-use SMS OTP provider out of the box.

## 4. Patient identity link migration

The Patient entity now has a nullable `IdentityUserId` with a unique filtered index. Existing patients remain unlinked until their mobile number has been verified.

After pulling the changes, create and apply the EF Core migration from the repository root:

```powershell
dotnet ef migrations add AddPatientIdentityUserId --project src/VaghtNegar.Infrastructure --startup-project src/VaghtNegar.API --output-dir Persistence/Migrations
dotnet ef database update --project src/VaghtNegar.Infrastructure --startup-project src/VaghtNegar.API
```

The domain method `Patient.LinkIdentityAccount(identityUserId, mobileNumberVerified)` rejects linking unless the trusted application flow confirms mobile verification.

## 5. Important security boundary

This commit establishes JWT validation and the identity foundation. It does not yet change existing clinic, doctor, patient, scheduling, or appointment endpoint access rules. Do not treat those existing management endpoints as production-secure until role/clinic authorization is applied.

Before enabling authenticated appointment booking, the API must resolve the patient's record from the authenticated `sub` claim and the verified identity link. It must not trust a client-supplied PatientId for self-service booking.

For production, deploy Keycloak with HTTPS, a managed/persistent PostgreSQL database, backups, secrets management, and a non-development Keycloak start command.
