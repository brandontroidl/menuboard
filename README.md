# Menu Board

A Windows desktop application that drives two TVs as digital menu boards for a convenience store. One PC runs the app, which opens an admin editor on the primary monitor and two fullscreen menu displays on the secondary and tertiary monitors.

## Features

- **Two independent displays** - TV1 shows hot food/meals, TV2 shows drinks/snacks. Each has its own categories and items.
- **Built-in admin editor** - Add, edit, remove, and reorder categories and menu items. Upload images for featured items. All changes save instantly.
- **List-style layout** - Clean, high-contrast design with dot leaders between item names and prices. Readable from 10+ feet.
- **Configurable per-screen** - Set header text, background color, and font scale independently for each TV.
- **Automatic monitor detection** - Places display windows on the correct monitors at startup. Works gracefully if fewer than 3 monitors are connected.
- **Local and offline** - Everything runs on the local machine with a SQLite database. No internet required.

## Requirements

- Windows 10 or later
- .NET 8 Runtime (Desktop)
- Two additional monitors/TVs connected to the PC

## Quick Start

1. Download the latest release or build from source (see INSTALL.md)
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

- .NET 8 / WPF
- SQLite via Entity Framework Core
- CommunityToolkit.Mvvm
- MSTest

## License

MIT - see LICENSE file.
