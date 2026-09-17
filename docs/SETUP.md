# Setup: Azure AD (Entra ID) App Registration

Azure Key Vault Desktop signs you in with your own Microsoft work/school account and needs
an Entra ID App Registration to do that. You only need to do this once (per organization).

1. Sign in to the [Azure Portal](https://portal.azure.com) with an account able to register
   applications in your Entra ID tenant.
2. Go to **Microsoft Entra ID** → **App registrations** → **New registration**.
3. Name it (e.g. "Azure Key Vault Desktop App"). Under "Supported account types" choose
   **Accounts in this organizational directory only** (or "any organizational directory" for
   multi-tenant) — do **not** choose personal Microsoft accounts; Key Vault and Azure Resource
   Manager aren't usable with a personal Microsoft account. Leave Redirect URI blank for now.
   Click **Register**.
4. On the Overview page, note the **Application (client) ID** and **Directory (tenant) ID** —
   you'll enter these into the app's Sign-in / Settings screen.
5. Go to **Authentication** → **Add a platform** → **Mobile and desktop applications** → check
   the built-in `http://localhost` redirect URI → **Configure**.
6. Still on the Authentication page, under "Advanced settings," set **Allow public client
   flows** to **Yes**, then **Save**.
7. Go to **API permissions** → **Add a permission** → **APIs my organization uses** → search
   **Azure Key Vault** (app ID `cfa8b339-82a2-471a-a3c9-0fc0be7a4093`) → **Delegated
   permissions** → check `user_impersonation` → **Add permissions**.
8. Repeat for **Azure Service Management** (app ID `797f4846-ba00-4fd7-ba43-dac1f8f63013`) →
   **Delegated permissions** → `user_impersonation` → **Add permissions**. (This is what lets
   the app list which Key Vaults you have access to across your subscriptions.)
9. If your organization requires it, click **Grant admin consent for `<tenant>`** (ask your
   Entra admin if you don't have the rights).
10. Do **not** create a client secret under "Certificates & secrets" — this is a public/native
    client app; no secret is used or should exist.
11. Grant yourself (and anyone else who'll use the app) the right Azure RBAC roles:
    - On each Key Vault (or its resource group/subscription), assign **Key Vault Secrets
      User** (read-only) or **Key Vault Secrets Officer** (read/write) via **Access control
      (IAM)**. If a vault still uses the legacy access-policy model instead of RBAC, add
      yourself under the vault's **Access policies** with Secret `Get`/`List`/`Set` instead.
    - Also assign yourself **Reader** at the subscription (or management group) level, so the
      app can discover which vaults exist via Azure Resource Manager.
12. Launch the app. On the Sign-in screen, enter the **Application (client) ID** and
    **Directory (tenant) ID** from step 4 (leave Tenant ID blank to sign in with any
    organizational tenant), then click **Sign in** — a system browser window opens for your
    Microsoft work/school account login.

## Notes

- The app only ever requests the Key Vault `user_impersonation` and Azure Service Management
  `user_impersonation` delegated scopes — nothing else (no Microsoft Graph, no admin scopes).
- The app can only ever read and create **secrets**. It has no code path for certificates or
  keys, and does not request permissions to create or manage Key Vaults themselves.
- Your session token is cached securely using your OS's native credential store (Windows
  Credential Manager / macOS Keychain / Linux Secret Service via libsecret). On Linux systems
  without a keyring/secret-service running, the cache falls back to unencrypted local storage —
  avoid that on a shared or untrusted machine.
