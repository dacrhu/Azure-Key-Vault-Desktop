# Azure Key Vault Desktop

A cross-platform (Windows/macOS/Linux) desktop app for browsing and managing **secrets** in
Azure Key Vault. It signs in with your own Microsoft (Entra ID) work/school account and only
ever touches secrets — it cannot create a Key Vault, and has no code path for certificates or
cryptographic keys.

## Features

- Sign in with your Microsoft (Entra ID) work/school account
- List the Key Vaults you have access to, and the secrets inside them
- Reveal a secret's value on demand and copy it to the clipboard, which auto-clears itself
  after a configurable timeout (default 30s) — without overwriting anything you've copied
  since
- Create new secrets

## Getting started

You'll need your own Entra ID App Registration first — see [docs/SETUP.md](docs/SETUP.md)
for a step-by-step walkthrough (about 5 minutes, one-time per organization).

### Build and run from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet restore
dotnet build
dotnet run --project src/AzureKeyVaultDesktop.App
```

### Run the tests

```bash
dotnet test
```

## Project structure

```
src/AzureKeyVaultDesktop.Core/   Auth, Key Vault, settings, and clipboard logic (no UI dependency)
src/AzureKeyVaultDesktop.App/    Avalonia UI (MVVM, CommunityToolkit.Mvvm)
tests/AzureKeyVaultDesktop.Core.Tests/  Unit tests for Core
packaging/                       Linux AppImage and macOS .app bundle packaging scripts
.github/workflows/                CI (build+test) and release (multi-OS packaging) pipelines
```

## Packaged releases

Tagged releases (plain semver, e.g. `1.0.0`) are built by [.github/workflows/release.yml](.github/workflows/release.yml)
into self-contained, no-install-required packages for each OS — no separate .NET runtime
needed on the target machine:

- **Windows**: zip of a self-contained single-file executable
- **macOS**: temporarily unavailable — this GHE org has no GitHub-hosted macOS runner
  capacity provisioned, so those release jobs are disabled for now (commented out in
  `release.yml`/`build.yml` with instructions to re-enable). The packaging script
  (`packaging/macos/build-app-bundle.sh`) still produces an unsigned `.app` bundle once
  macOS runners are available again; unsigned means Gatekeeper will warn on first launch —
  right-click the app and choose **Open**, or run `xattr -cr AzureKeyVaultDesktop.app`
- **Linux**: a self-contained AppImage (`chmod +x` it, then run)

## Security notes

- Only Key Vault `user_impersonation` and Azure Service Management `user_impersonation`
  delegated scopes are ever requested — nothing broader.
- Secret values are only fetched when you explicitly click Reveal/Copy — the secret list never
  bulk-fetches values.
- No secrets or tokens are ever written to the app's local `settings.json`; auth tokens live
  exclusively in your OS's native secure credential store.
