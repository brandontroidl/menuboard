# Menu Board - Handoff Binder

Everything needed to own, operate, deploy, and maintain this project. If you are picking this up cold, read this file first; it points to the rest.

## 1. What this is

A Windows desktop app (.NET 10 / WPF) that turns a PC and two TVs into digital menu boards for a convenience store. TV 1 shows Hot Food / Meals, TV 2 shows Drinks / Snacks. A built-in admin editor on the same PC manages all content. Fully offline; all data is local.

**Repository:** `menuboard` (git). Application code is under `MenuBoard/`.

## 2. Document index

| Document | Audience | Contents |
|---|---|---|
| `README.md` | Everyone | Overview, features, quick start |
| `INSTALL.md` | Installer / IT | Build, publish, hardware and monitor setup, first run, auto-start, troubleshooting |
| `docs/OWNER-QUICKSTART.md` | Store owner (non-technical) | One-page card: daily use, change a price, sold out, TVs blank |
| `docs/USER-GUIDE.md` | Store staff | Day-to-day how-to: prices, items, images, sold-out, look-and-feel, backups |
| `docs/TECHNICAL.md` | Developers | Architecture, data model, monitor layout logic, build/test, known limitations |
| `docs/HANDOFF.md` | Next owner | This file - runbooks, inventory, maintenance |
| `LICENSE` | Everyone | MIT |

## 3. System at a glance

| Piece | Value |
|---|---|
| Store hardware | 1 Windows PC (built-in/primary screen for admin) + 2 TVs via HDMI/DisplayPort, both landscape, Windows display mode "Extend" |
| App executable | `MenuBoard.exe` (from `MenuBoard\publish\` after running `publish.ps1`) |
| Menu data | `menuboard.db` (SQLite) next to the exe |
| Item photos | `Images\` folder next to the exe |
| App config | `settings.json` next to the exe (monitor layout, TV swap) |
| Auto-start | Registry `HKCU\...\CurrentVersion\Run`, value `MenuBoard` (managed by the in-app checkbox) |
| Dev requirements | Windows 10/11, .NET 10 SDK |
| Store PC requirements | Windows 10/11; nothing else with the default self-contained publish (.NET 10 Desktop Runtime x64 only if published with `-FrameworkDependent`) |

## 4. Runbooks

### 4.1 Fresh install on a store PC

Preferred: build `MenuBoardSetup.exe` once (`cd MenuBoard` then `.\build-installer.ps1`, requires NSIS) and just run it on the store PC - it installs to the user profile, sets start-on-boot, and adds a Start Menu entry. Then do steps 3, 5, 7, 8 below. Manual alternative:

1. On a dev machine: `cd MenuBoard` then `.\publish.ps1` (self-contained by default - nothing to install on the store PC).
2. Copy the whole `publish\` folder to the store PC (e.g., `C:\MenuBoard\`).
3. Connect both TVs, set Windows display mode to **Extend**, TVs at 1920x1080, landscape.
4. Run `MenuBoard.exe`. Admin opens on the PC screen; a fullscreen menu appears on each TV.
5. If the menus are on the wrong TVs or not fullscreen where expected: App Settings → "Swap which TV shows Hot Food vs Drinks/Snacks", or pin each menu with the "TV 1 monitor" / "TV 2 monitor" pickers.
6. App Settings → check **"Start Menu Board automatically when Windows starts"**.
7. Kiosk hardening (recommended): auto-login (`netplwiz`), power plan set to never sleep, Windows Update active hours set to store hours.
8. Reboot once to confirm everything comes back by itself.

### 4.2 Deploy an update to an existing store PC

1. Build a new `publish\` folder (`.\publish.ps1 ...`).
2. On the store PC, exit the app (X on the admin window → Yes).
3. **Back up `menuboard.db`, `Images\`, and `settings.json`** from the install folder.
4. Replace the install folder's contents with the new publish output.
5. Copy the three backed-up items back in.
6. Start `MenuBoard.exe`, spot-check both TVs.

Schema caution: the database schema is created once and never auto-upgraded (no EF migrations - see `docs/TECHNICAL.md` § Known limitations). If a code change altered the entity models, plan a manual migration step before deploying.

### 4.3 Backup and restore

- **Backup** (do this on a schedule - it's the entire menu): copy `menuboard.db` + `Images\` (+ `settings.json` for completeness) to a USB stick or cloud folder. Close the app first - a copy of the database taken mid-write can be inconsistent.
- **Restore:** close the app, copy the files back next to `MenuBoard.exe`, start the app.
- **Factory reset:** close the app, delete `menuboard.db`, start the app - it recreates an empty database with default display settings.

### 4.4 Move to a replacement PC

Follow 4.1 on the new PC, then copy `menuboard.db`, `Images\`, and `settings.json` from the old install (or the latest backup) into the new install folder before first launch. Re-tick the auto-start checkbox (it's a per-machine registry entry).

### 4.5 Emergency: TVs blank during business hours

1. Is the PC on and logged in? (Auto-login recommended - see 4.1 step 7.)
2. Is `MenuBoard.exe` running? If not, start it (or reboot; auto-start brings it back).
3. Windows Display settings: both TVs detected and in Extend mode?
4. Still stuck: reboot the PC. Total recovery time is under two minutes and the menu data is safe - it's all on disk.

## 5. Routine maintenance

- **Weekly-ish:** back up `menuboard.db` + `Images\` (see 4.3).
- **Occasionally:** confirm Windows Update hasn't left the PC waiting on a restart; reboot after hours if so.
- **Rarely:** `Images\` accumulates files from replaced photos (the app never deletes images). If it ever bothers you, clean out files not referenced by any item - or ignore it; they're small.

## 6. Development handoff

- Prereqs: Windows + .NET 10 SDK. Any editor works; no other tooling required.
- Build `dotnet build`, test `dotnet test`, run `dotnet run --project src/MenuBoard` (from `MenuBoard/`), deploy `.\publish.ps1`.
- On a single-monitor dev machine the two TV windows open as resizable preview windows - you can develop everything without TVs.
- Read `docs/TECHNICAL.md` before touching code; especially the data-change event flow, the monitor layout algorithm, and the no-migrations caveat.
- Tests live in `tests/MenuBoard.Tests` (MSTest). CI note: compiles on Linux with `-p:EnableWindowsTargeting=true`, but tests execute on Windows only.

## 7. Design decisions worth knowing

- **Prices are integer cents** end-to-end; formatting happens only in the UI converter. Never store floats.
- **One process, event-driven refresh** - the TVs update through an in-process `DataChanged` event, which is why there's no refresh button and no polling.
- **Hardware config (`settings.json`) is separate from menu data (`menuboard.db`)** so a database restore onto different hardware doesn't drag the wrong monitor layout with it.
- **Closing the admin prompts and defaults to minimize** - a kiosk must not be killable by a stray click.
- **Auto layout treats a 2-monitor PC as "both are TVs"** - that's the typical store box; a 3rd (primary) monitor is assumed to be the admin screen.

## 8. Known limitations

Summarized here; details in `docs/TECHNICAL.md`: no schema auto-migration; primary-monitor DPI assumption (keep TVs at 100% scaling); no undo in the editor; orphaned image files are left on disk by design. Monitor hot-plug and late-arriving USB-adapter displays are handled automatically, but a USB display adapter (e.g. j5create) still needs its driver installed on the store PC.

## 9. Support quick reference

| Symptom | First move | Doc |
|---|---|---|
| Staff can't edit / find admin | Esc or double-click a TV menu | USER-GUIDE |
| Wrong menu on wrong TV | App Settings → Swap | USER-GUIDE |
| TVs blank | Runbook 4.5 | HANDOFF |
| New store PC | Runbook 4.1 | HANDOFF |
| App update | Runbook 4.2 | HANDOFF |
| Code question | TECHNICAL.md | TECHNICAL |
