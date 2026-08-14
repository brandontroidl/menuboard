# Installation Guide

## Option 1: Build from Source

### Prerequisites

- Windows 10 or later
- .NET 8 SDK (download from https://dotnet.microsoft.com/download/dotnet/8.0)

### Steps

1. Clone or download the repository.

2. Build the solution:

   ```
   cd MenuBoard
   dotnet build
   ```

3. Run the tests:

   ```
   dotnet test
   ```

4. Publish a standalone executable (from the `MenuBoard` folder in PowerShell):

   ```
   .\publish.ps1                  # store PC needs the .NET 8 Desktop Runtime
   .\publish.ps1 -SelfContained   # bigger, but nothing to install on the store PC
   ```

5. The published application is in the `MenuBoard/publish/` directory. Run `MenuBoard.exe` from there, or copy the folder to a user-writable location on the target PC (e.g. `C:\MenuBoard`, not `C:\Program Files` - the app writes its database, images, and settings next to the exe).

   > Updating an existing install? Your menu data lives next to the exe (`menuboard.db`, `Images\`, `settings.json`). Back those up first and copy them into the new publish folder.

## Option 2: Run from Build Output

After building (step 3 above), run directly:

```
cd MenuBoard
dotnet run --project src/MenuBoard
```

## Hardware Setup

### Monitor Configuration

The application supports both common setups (chosen automatically):

- **PC + two TVs only (typical store setup)** - Both TVs show fullscreen menus. The admin editor runs behind them; press Esc or double-click a menu display to bring it to the front.
- **PC with its own monitor + two TVs** - The admin editor stays on the primary monitor and the TVs each show a fullscreen menu.

You can override this under **App Settings → Monitor layout** in the admin editor, and use **Swap** if Hot Food and Drinks/Snacks come up on the wrong TVs.

Configure all monitors in Windows Display Settings (right-click desktop, Display settings). Ensure each TV is set to "Extend" mode, not "Duplicate."

### Recommended TV Settings

- Resolution: 1920x1080 (1080p) for best text clarity
- Orientation: Landscape (standard horizontal position)
- Overscan: Disabled (check TV settings for "PC mode" or "Game mode" to avoid edge cropping)

## First Run

On first launch, the application:
1. Creates a `menuboard.db` SQLite database next to the executable
2. Creates an `Images/` folder for menu item photos
3. Seeds default display settings for both screens
4. Opens the admin window
5. Opens fullscreen display windows on the detected monitors (see Monitor Configuration above)

With only one monitor connected (e.g., testing on a laptop), the menu displays open as regular resizable preview windows instead. The admin editor always works regardless of how many monitors are connected.

## Updating Menu Content

All changes are made through the admin editor:

1. **Switch screens** - Use the radio buttons in the sidebar to toggle between TV1 and TV2.
2. **Add a category** - Click "+ Add Category" in the sidebar. Edit the name in the right panel.
3. **Reorder categories** - Select a category and use the Up/Down buttons.
4. **Add an item** - Select a category, then click "+ Add Item" in the right panel.
5. **Edit an item** - Click into the name, price, or description fields. Changes save when you click away.
6. **Add an image** - Click the "Image" button on an item row and select a JPG or PNG file.
7. **Toggle availability** - Uncheck "Available" to temporarily hide an item without deleting it.
8. **Delete** - Use the X button for items or the Delete button for categories.

All changes appear on the TV displays instantly.

## Display Settings

Expand "Display Settings" in the admin editor to configure each TV:

- **Header Text** - The title shown at the top of the display (e.g., "Hot Food & Meals")
- **Background Color** - Hex color code (e.g., #000000 for black, #1a1a2e for dark blue)
- **Font Scale** - Adjust from 0.5x to 2.0x to fit your TV size and viewing distance

## Auto-Start on Boot

In the admin editor, expand **App Settings** and check **"Start Menu Board automatically when Windows starts."** That's it - the app registers itself in the current user's startup entries.

(Manual alternative: press `Win+R`, type `shell:startup`, press Enter, and create a shortcut to `MenuBoard.exe` there.)

For unattended kiosk operation, also consider:
- Setting Windows to auto-login (netplwiz)
- Disabling Windows Update restart notifications
- Setting the PC power plan to "Never sleep" when plugged in

## Troubleshooting

**TVs show nothing / display windows don't appear**
- Check Windows Display Settings - TVs must be in "Extend" mode, not "Duplicate"
- Restart the application after connecting monitors

**Menus are on the wrong TVs (Hot Food where Drinks should be)**
- In the admin editor, expand App Settings and check "Swap which TV shows Hot Food vs Drinks/Snacks"

**Can't get back to the admin editor (menus cover every screen)**
- Press Esc or double-click on either menu display

**Text is too small / too large on the TVs**
- Adjust the Font Scale slider in Display Settings (expand the section in the admin editor)
- Values between 1.0 and 1.5 work well for most standard TVs at typical viewing distances

**Images don't appear on the display**
- Verify the image file is a supported format (JPG, PNG, BMP, GIF)
- Images are copied to the `Images/` folder next to the executable - check that folder has write permissions

**Database issues**
- The database file `menuboard.db` is next to the executable
- To reset completely, delete `menuboard.db` and restart - it will be recreated with defaults
- Back up `menuboard.db` and the `Images/` folder to preserve your menu data
