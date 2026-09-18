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

- **Windows**: a portable zip, an MSI, and an EXE installer
- **macOS**: a `.dmg` and a portable zip, for both `osx-x64` and `osx-arm64`. The app is
  **not code-signed or notarized**, so on first launch (or first mount of the dmg) Gatekeeper
  will report it as "damaged" or unable to be opened — this is not a corrupted download, just
  macOS refusing to run software from an unidentified developer. Fix it with either:
  - Terminal: `xattr -cr /path/to/AzureKeyVaultDesktop.app` (run against the `.app` after
    unzipping, or against the mounted volume's `.app` before dragging it to Applications), or
  - **System Settings → Privacy & Security**, scroll down, and click **Open Anyway** next to
    the app's name (you may need to attempt to open it once first for that button to appear;
    on macOS Sequoia the old right-click → Open "Open Anyway" prompt is often skipped in favor
    of this Settings-panel flow)
- **Linux**: a self-contained AppImage (`chmod +x` it, then run)

## Security notes

- Only Key Vault `user_impersonation` and Azure Service Management `user_impersonation`
  delegated scopes are ever requested — nothing broader.
- Secret values are only fetched when you explicitly click Reveal/Copy — the secret list never
  bulk-fetches values.
- No secrets or tokens are ever written to the app's local `settings.json`; auth tokens live
  exclusively in your OS's native secure credential store.
