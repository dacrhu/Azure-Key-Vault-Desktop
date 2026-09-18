# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A cross-platform (Windows/macOS/Linux) desktop app for browsing and managing **secrets** in
Azure Key Vault, built with .NET 8 + Avalonia. Signs in with the user's Microsoft (Entra ID)
account. Hard constraint baked into the design: the app must never create a Key Vault, and must
never reference certificate or key APIs — secrets only, everywhere (code, requested OAuth
scopes, RBAC guidance in `docs/SETUP.md`).

## Commands

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/AzureKeyVaultDesktop.App

# Single test / test class
dotnet test --filter "FullyQualifiedName~ClipboardAutoClearServiceTests"
dotnet test --filter "Name=CopySecretAsync_SetsClipboardImmediately"

# Self-contained publish for one platform (RID: win-x64, osx-x64, osx-arm64, linux-x64)
dotnet publish src/AzureKeyVaultDesktop.App -c Release -r <RID> --self-contained true -p:PublishSingleFile=true -p:Version=1.2.3
```

Requires the .NET 8 SDK (pinned via `global.json`: 8.0.100, `rollForward: latestFeature`).

## Architecture

**Core/App split.** `src/AzureKeyVaultDesktop.Core` has zero Avalonia dependency — auth, Key
Vault access, settings persistence, and the clipboard auto-clear *logic* all live there and are
unit-tested directly (`tests/AzureKeyVaultDesktop.Core.Tests`). `src/AzureKeyVaultDesktop.App`
is the Avalonia MVVM shell (CommunityToolkit.Mvvm source generators: `[ObservableProperty]`,
`[RelayCommand]`). The only clipboard piece that needs Avalonia (`TopLevel.Clipboard`) is a thin
`IClipboardTextAccessor` implementation injected into Core's `ClipboardAutoClearService`.

**DI and navigation (`App.axaml.cs`).** Page ViewModels (Login/VaultList/SecretList/
SecretDetail/NewSecret/Settings) are registered `Transient` — each navigation gets a fresh
instance, so a constructor-driven initial load or an explicit `Initialize(...)` call reruns
every time the page is shown again. `INavigationService.NavigateTo<T>(configure)` resolves `T`
from the container and lets the caller pass state into it (e.g.
`NavigateTo<SecretListViewModel>(vm => vm.Initialize(vault))`) before it's displayed — this is
how state flows between pages instead of constructor parameters.

**`IKeyVaultManagementService`/`IKeyVaultSecretsService` take `IAuthService`, never a captured
`TokenCredential`.** This is load-bearing: `IAuthService.Credential` changes identity on every
sign-in (a fresh `InteractiveBrowserCredential` per session — see
`AzureIdentityAuthService.BuildCredential`). Both services compare `_authService.Credential` by
reference on every call and rebuild their internal `ArmClient`/`SecretClient`s (and drop their
caches) when it has changed, so signing out and back in as someone else can't keep serving the
previous identity's vaults/secrets. Do not "simplify" these back to capturing `TokenCredential`
in the constructor — that was a real bug here (DI singletons froze the first user's credential).

**Two-token auth, but transparent to callers.** `ArmClient` (vault discovery) and `SecretClient`
(secrets data-plane) each negotiate their own scope from the same shared `TokenCredential` — ARM
via `.default`, Key Vault via its challenge-based auth policy reading the `WWW-Authenticate`
response. No manual scope-juggling needed anywhere in this codebase.

**Vault listing is filtered by actual access, not just ARM visibility.**
`KeyVaultManagementService.ListAccessibleVaultsAsync` first asks ARM for every vault in every
subscription the user can see (that only requires subscription-level Reader — says nothing
about the vault's own RBAC/access policy), then probes each candidate with a real
`GetPropertiesOfSecretsAsync` call and drops anything that fails. This is intentional — the app
must show only vaults the user can actually read secrets from — and the probing is what the
per-identity cache (`forceRefresh` parameter, bypassed by the UI's "Refresh" buttons) exists to
amortize.

**Streaming, not batch.** Both vault listing and per-subscription vault fetches use
`System.Threading.Channels` to yield results as they arrive (bounded concurrency via
`SemaphoreSlim`) rather than waiting for the slowest subscription/vault before showing anything
— a deliberate perf fix. Don't reintroduce `Task.WhenAll(...)` before the `yield return` loop.

**Clipboard auto-clear guard.** `ClipboardAutoClearService.CopySecretAsync` remembers exactly
the value it wrote and only clears the clipboard if it still holds that exact value when the
timer fires, so it never clobbers something the user copied from elsewhere in the meantime.
`CountdownTick` fires once per second for the UI countdown. See
`ClipboardAutoClearServiceTests` for the guard behavior contract.

**Localization.** `LocalizationService` (`App/Services/Localization`) is a single instance
(`LocalizationService.Instance`) registered in DI *and* bound directly from XAML via
`{Binding [SomeKey], Source={x:Static loc:LocalizationService.Instance}}` — both paths share the
same state, so a language switch (the `CurrentLanguage` setter) live-updates every bound string
across the app by raising `PropertyChanged("Item[]")`. Translations live in `Translations.cs`;
add a key there for all three languages (en/hu/de) before referencing it anywhere. Dynamic/
interpolated messages go through `ILocalizationService.Translate(key, args)`, injected into the
owning ViewModel — don't hardcode user-facing strings in `Views/` or `ViewModels/`.

**Startup deadlock trap.** `App.axaml.cs`'s `OnFrameworkInitializationCompleted` blocks
synchronously on settings load before the window is created. It's wrapped in
`Task.Run(...).GetAwaiter().GetResult()` specifically because Avalonia's `SynchronizationContext`
is already installed by that point — a plain `.GetAwaiter().GetResult()` on an awaited call
deadlocks (the continuation tries to post back to the UI thread that's blocked waiting for it).
Keep this pattern for any future startup-time blocking call.

## CI/CD and releases

- `.github/workflows/build.yml`: build+test matrix (ubuntu/windows/macos) on PRs and on tag
  pushes matching `*.*.*` — deliberately **not** on every push to `main`.
- `.github/workflows/release.yml`: triggers on tags matching `*.*.*` (no `v` prefix — tags are
  plain semver like `1.0.0`). Publishes self-contained per-RID builds with `-p:Version=<tag>`,
  then packages, per OS:
  - **Windows** (`win-x64`): a portable zip, an MSI (`packaging/windows/product.wxs`, built with
    the cross-platform `wix` dotnet tool pinned to v5 — v6+ requires accepting WiX's paid "Open
    Source Maintenance Fee" EULA, not worth it here — files are listed explicitly in the .wxs
    rather than glob-harvested, so a future Avalonia/SkiaSharp upgrade that changes the native
    DLLs dropped next to the exe needs that file updated by hand), and an EXE installer
    (`packaging/windows/installer.iss`, built with Inno Setup via `ISCC.exe`, installed through
    Chocolatey since it isn't guaranteed preinstalled on `windows-latest`).
  - **macOS** (`osx-x64`/`osx-arm64`): a `.dmg` (`hdiutil create`) and a portable zip, both
    wrapping the unsigned `.app` bundle from `packaging/macos/build-app-bundle.sh` — users still
    need to right-click > Open the first time, or `xattr -cr`, since the app isn't
    code-signed/notarized.
  - **Linux** (`linux-x64`): an AppImage (`APPIMAGE_EXTRACT_AND_RUN=1` when invoking
    `appimagetool` — it's itself an AppImage and needs that to run without FUSE, which
    `ubuntu-latest` runners don't have).

  Then creates a GitHub Release from all of the above.

## Security constraints (don't relax these)

- No code path may reference `Azure.Security.KeyVault.Certificates` or
  `Azure.Security.KeyVault.Keys`, and no ARM call may create a Key Vault — see
  `docs/SETUP.md` for the exact (minimal) API permissions the app requests (Key Vault and Azure
  Service Management `user_impersonation` only).
- Secret values are fetched only in direct response to an explicit user action (Reveal/Copy) —
  never to bulk-populate a list, and never automatically on navigating to a secret. This was
  tried once (auto-reveal on open) and reverted after internal security review flagged it.
- `settings.json` never stores tokens — those live exclusively in the OS-native
  MSAL/Azure.Identity token cache (`TokenCachePersistenceOptions`, with an unencrypted-storage
  fallback documented for Linux systems without a keyring).
