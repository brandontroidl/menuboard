# Menu Board - Technical Writeup

Architecture and implementation reference for developers maintaining or extending the app.

## Stack

| Layer | Choice | Notes |
|---|---|---|
| Runtime | .NET 8 (`net8.0-windows`) | WPF requires Windows; `UseWindowsForms` is also enabled for monitor enumeration |
| UI | WPF, MVVM | `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`) |
| Data | SQLite via EF Core (`Microsoft.EntityFrameworkCore.Sqlite`) | Single file `menuboard.db` next to the exe |
| App config | JSON (`settings.json` next to the exe) | Monitor layout + display swap; deliberately outside the DB (per-PC hardware config) |
| Auto-start | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Registry value written/removed by the app |
| Tests | MSTest, EF Core InMemory | `MenuBoard.Tests` project |

## Process model

One process, three windows, created in `App.OnStartup`:

- **AdminWindow** (`MainWindow`, `ShutdownMode.OnMainWindowClose`) - the editor. Closing it prompts; declining minimizes instead so a kiosk can't be killed by accident.
- **DisplayWindow x2** - one per TV, `ScreenNumber` 1 (hot food) and 2 (drinks/snacks). Borderless fullscreen when assigned a monitor; falls back to a resizable "preview" window when there aren't enough monitors (e.g., developing on a laptop).

Because everything is in-process, change propagation is a plain C# event - no polling, no IPC:

```
AdminWindow edit → MenuDataService.SaveChanges() → DataChanged event
    → DisplayViewModel.Refresh() (marshalled to its Dispatcher) → UI rebinds
```

Refresh must hand the ViewModel **new object instances** each time - the entities have no `INotifyPropertyChanged`, so change notification rides entirely on the ViewModel's property setters detecting a different reference. Categories get a new `ObservableCollection`; settings come from `GetDisplaySettingsSnapshot` (`AsNoTracking`), because the tracked `GetDisplaySettings` returns the *same* instance every call and would silently suppress theme updates.

## Solution layout

```
MenuBoard/
  MenuBoard.sln
  publish.ps1                    Deployment script (framework-dependent or -SelfContained)
  src/MenuBoard/
    App.xaml(.cs)                Startup, DB bootstrap, monitor layout, ActivateAdmin()
    Models/                      Category, MenuItem, DisplaySettings (EF entities)
    Data/MenuDbContext.cs        DbContext + Seed() of default DisplaySettings
    Services/
      MenuDataService.cs         All CRUD + DataChanged event (single choke point for writes)
      ImageService.cs            Copies picked images to Images/ with GUID filenames
      MonitorService.cs          Screen.AllScreens → WPF-unit bounds (DPI-adjusted)
      AppSettingsService.cs      settings.json load/save (MonitorLayout, SwapDisplays)
      StartupService.cs          HKCU Run key register/unregister
    ViewModels/
      AdminViewModel.cs          Editor state + commands; raises MonitorSettingsChanged
      DisplayViewModel.cs        Read-only per-screen view; subscribes to DataChanged
    Views/
      AdminWindow.xaml(.cs)      Sidebar (screens/categories) + item grid + settings expanders
      DisplayWindow.xaml(.cs)    Menu rendering; Esc/double-click → App.ActivateAdmin()
    Converters/                  CentsToDollars, ImagePath, NullToCollapsed
  tests/MenuBoard.Tests/         MSTest unit tests
```

## Data model

Prices are stored as **integer cents** and rendered through `CentsToDollarsConverter` ("$X.XX"). `ConvertBack` strips `$`/commas; unparseable or negative input returns `Binding.DoNothing` so a typo never overwrites a stored price.

- **Category**: `Id`, `Name`, `DisplayOrder`, `ScreenNumber` (1|2), `Items` (cascade delete)
- **MenuItem**: `Id`, `CategoryId`, `Name`, `Price` (cents), `Description?`, `ImagePath?` (relative to `Images/`), `DisplayOrder`, `IsAvailable`
- **DisplaySettings**: per screen - `BackgroundColor` (hex), `HeaderText`, `FontScale` (0.5-2.0, applied as a `LayoutTransform` on the display root)

`GetCategoriesForScreen` (used by displays) filters `IsAvailable` and is `AsNoTracking`; `GetAllCategoriesForScreen` (used by the admin) returns everything tracked, so admin edits to bound entities are picked up by `SaveChanges`.

The schema is created with `EnsureCreated()` - **there are no EF migrations**. See "Known limitations" before changing entity shapes.

## Monitor layout

`App.ApplyMonitorLayout()` (safe to re-run at runtime; the admin's settings raise `MonitorSettingsChanged` to trigger it):

1. Enumerate monitors (`MonitorService`, WinForms `Screen.AllScreens`, divided by the primary window's DPI scale to get WPF units), sorted left-to-right then top-to-bottom - this ordering defines "Monitor 1..N" everywhere in the UI and in `settings.json`.
2. **Explicit picks win:** `Tv1Monitor` / `Tv2Monitor` in `settings.json` (set via the App Settings pickers; -1 = automatic) pin a TV to a monitor index. Out-of-range indices (monitor unplugged) fall back to automatic.
3. For any TV still unassigned, build the automatic pool per `MonitorLayout`:
   - `Auto` (default): 2 monitors total → **both** are TVs (store PC hooked straight to the TVs); 3+ → non-primary monitors are TVs, primary keeps the admin.
   - `AdminPlusTvs`: non-primary only.
   - `AllMonitors`: every monitor, primary included.
   `SwapDisplays` reverses the pool; monitors already claimed by explicit picks are removed from it.
4. Assign TV 1 → first available, TV 2 → next. A TV without a monitor becomes a preview window.
5. `MoveAdminOffTvMonitors`: if some monitor is free of TVs but the admin window's center sits on a TV-covered monitor, the admin window is repositioned to the free monitor (the "console" screen).

Escape hatch when menus cover every screen: `DisplayWindow` handles Esc and double-click by calling `App.ActivateAdmin()`, which restores/activates the admin and briefly toggles `Topmost` to hop above the borderless fullscreen windows.

## Persistence summary

| What | Where | When written |
|---|---|---|
| Menu content + display settings | `menuboard.db` | Immediately on every edit (`SaveChanges`) |
| Item photos | `Images/` (GUID filenames) | On image pick (copied; originals untouched; never deleted by the app) |
| Monitor layout / swap / per-TV monitor picks | `settings.json` | On change in App Settings |
| Auto-start | HKCU Run key `MenuBoard` → quoted `Environment.ProcessPath` | On checkbox toggle |

All of it lives next to the exe, so an install is fully portable: copy the folder, and the menu goes with it.

## Build, test, publish

```powershell
cd MenuBoard
dotnet build
dotnet test                      # MSTest; runs on Windows only (WPF-dependent TFM)
.\publish.ps1                    # framework-dependent → publish\
.\publish.ps1 -SelfContained     # bundles the runtime (no .NET install needed on target)
```

On a non-Windows CI/build agent, compilation works with `-p:EnableWindowsTargeting=true`; tests still need Windows to execute.

## Tests

- `MenuDataServiceTests` - CRUD, screen filtering, availability filtering, `DataChanged` firing (EF InMemory).
- `CentsToDollarsConverterTests` - round-trips plus garbage/negative input protection.
- `AppSettingsServiceTests` - defaults, save/reload round-trip, corrupt-file fallback.
- `MenuItemTests` - model/relationship basics.

Not covered (would need UI automation): window placement, XAML bindings, the exit and category-delete confirmation flows.

## Known limitations & gotchas

- **No EF migrations.** `EnsureCreated()` never alters an existing database. Adding a column to an entity will crash on stores with an existing `menuboard.db` unless you add a manual `ALTER TABLE` upgrade step (or accept deleting the DB). Prefer `settings.json` for new app-level config.
- **Mixed DPI:** monitor bounds are scaled by the *primary* monitor's DPI factor. Fine when everything is 100% (typical for TVs); windows can land slightly off if the admin monitor uses display scaling and the TVs don't. Fix would be per-monitor DPI via `GetDpiForMonitor`.
- **Orphaned images:** deleting an item doesn't delete its image file (intentional - files are cheap, and referencing bugs are not). `Images/` grows slowly; safe to clean manually against DB references.
- **Monitor hot-plug:** layout is applied at startup and on settings changes, not on Windows display-change events. Plugging in a TV after launch → toggle a layout setting or restart the app. (Hook `SystemEvents.DisplaySettingsChanged` → `ApplyMonitorLayout()` if this becomes annoying.)
- **Single edit surface:** the admin edits tracked entities directly; there's no undo. The confirmation on category delete is the item count at risk, not a soft-delete.

## Sensible extension points

Scheduled menus (day-parting) would be a `TimeWindow` on Category plus a timer-driven `Refresh`; remote editing would mean swapping the in-process `DataChanged` event for file/DB watching or a small local web server; a third TV means generalizing `ScreenNumber` beyond 1|2 and the two hard-coded display windows into a list.
