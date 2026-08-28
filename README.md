# Menu Board

[![ci](https://github.com/brandontroidl/menuboard/actions/workflows/ci.yml/badge.svg)](https://github.com/brandontroidl/menuboard/actions/workflows/ci.yml)

A Windows desktop application that drives two TVs as digital menu boards for a convenience store. One PC runs the app, which opens an admin editor plus two fullscreen menu displays - one per TV.

## Features

- **Two independent displays** - TV1 shows hot food/meals, TV2 shows drinks/snacks. Each has its own categories and items.
- **Built-in admin editor** - Add, edit, remove, and reorder categories and menu items. Upload images for featured items. All changes save instantly.
- **List-style layout** - Clean, high-contrast design with dot leaders between item names and prices. Readable from 10+ feet.
- **Configurable per-screen** - Set header text, background color, and font scale independently for each TV.
- **Flexible monitor layout** - Works with a PC hooked up to just the two TVs, or with an extra admin monitor. Automatic detection, plus a swap toggle if the menus come up on the wrong TVs. Press Esc or double-click a menu display to bring the admin editor to the front.
- **Kiosk-friendly** - "Start with Windows" checkbox for auto-launch on boot, and closing the admin window asks for confirmation so the TVs don't go blank by accident.
- **Local and offline** - Everything runs on the local machine with a SQLite database. No internet required.

## Requirements

- Windows 10 or later
- .NET 10 Runtime (Desktop) - not needed if you publish self-contained (see INSTALL.md)
- Two TVs connected to the PC (an additional admin monitor is optional)

## Documentation

| Doc | For | What's in it |
|---|---|---|
| [INSTALL.md](INSTALL.md) | Installer / IT | One-click installer, build, publish, hardware setup, troubleshooting |
| [docs/OWNER-QUICKSTART.md](docs/OWNER-QUICKSTART.md) | Store owner | One-page plain-language card for daily use |
| [docs/USER-GUIDE.md](docs/USER-GUIDE.md) | Store staff | Everyday how-to: prices, items, pictures, sold-out, backups |
| [docs/TECHNICAL.md](docs/TECHNICAL.md) | Developers | Architecture, data model, build/test, known limitations |
| [docs/HANDOFF.md](docs/HANDOFF.md) | Next owner | Runbooks (install, update, backup/restore, emergencies), maintenance |

## Quick Start

1. Download the [latest release](https://github.com/brandontroidl/menuboard/releases) or build from source (see INSTALL.md)
2. Connect two TVs to your PC (in addition to your primary monitor)
3. Run `MenuBoard.exe`
4. The admin editor opens on your primary monitor
5. Use the sidebar to switch between TV1 and TV2 content
6. Add categories and items - changes appear on the TVs immediately

## Project Structure

```
MenuBoard/
  MenuBoard.sln
  src/MenuBoard/           Main application
    Models/                Category, MenuItem, DisplaySettings
    Data/                  EF Core DbContext with SQLite
    Services/              MenuDataService, ImageService, MonitorService
    ViewModels/            AdminViewModel, DisplayViewModel
    Views/                 AdminWindow, DisplayWindow
    Converters/            CentsToDollars, ImagePath, NullToCollapsed
    App.xaml               Application entry point
  tests/MenuBoard.Tests/   Unit tests (MSTest)
```

## Tech Stack

- .NET 10 (LTS, supported to Nov 2028) / WPF
- SQLite via Entity Framework Core
- CommunityToolkit.Mvvm
- MSTest

## License

MIT - see LICENSE file.
