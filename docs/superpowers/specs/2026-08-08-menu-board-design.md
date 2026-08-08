# Convenience Store Menu Board - Design Spec

## Overview

A Windows WPF application (.NET 8) that drives two TVs as menu boards in a convenience store. TV1 shows hot food/meals, TV2 shows drinks/snacks. A built-in admin interface on the same PC lets the store operator add, edit, and remove menu items and images. Clean list-style layout with select featured images.

## Architecture

Single WPF application, three windows:

- **AdminWindow** - standard windowed mode on the primary monitor. Sidebar switches between managing TV1 and TV2 content. CRUD forms for categories and items.
- **DisplayWindow1** - borderless fullscreen on secondary monitor. Hot food/meals.
- **DisplayWindow2** - borderless fullscreen on tertiary monitor. Drinks/snacks.

On startup the app detects available monitors and positions each display window accordingly. Admin stays on primary. When the user saves changes, display windows refresh immediately.

**Stack:** .NET 8, WPF, SQLite via Entity Framework Core, CommunityToolkit.Mvvm for MVVM bindings.

## Data Model

### Categories
| Column        | Type    | Notes                              |
|---------------|---------|-------------------------------------|
| Id            | int PK  | Auto-increment                     |
| Name          | string  | e.g., "Burgers", "Fountain Drinks" |
| DisplayOrder  | int     | Manual sort within a screen        |
| ScreenNumber  | int     | 1 or 2                             |

### MenuItems
| Column        | Type     | Notes                                    |
|---------------|----------|------------------------------------------|
| Id            | int PK   | Auto-increment                          |
| CategoryId    | int FK   | References Categories.Id                |
| Name          | string   | Item name                               |
| Price         | int      | Price in cents (displayed as dollars)   |
| Description   | string?  | Optional short text                     |
| ImagePath     | string?  | Relative path to Images/ folder, nullable - only for featured items |
| DisplayOrder  | int      | Manual sort within a category           |
| IsAvailable   | bool     | Toggle to hide without deleting         |

### DisplaySettings
| Column          | Type    | Notes                              |
|-----------------|---------|-------------------------------------|
| Id              | int PK  | Auto-increment                     |
| ScreenNumber    | int     | 1 or 2                             |
| BackgroundColor | string  | Hex color                          |
| HeaderText      | string  | e.g., "Hot Food & Meals"          |
| FontScale       | double  | Adjust text size per TV            |

Images stored as files in a local `Images/` directory next to the executable. Database stores relative paths.

## Display Layout (per TV)

List-style, clean and readable from a distance:

```
+--------------------------------------------------+
|              HOT FOOD & MEALS          [header]   |
+--------------------------------------------------+
|  BURGERS                               [category] |
|    Classic Burger ..................... $5.99      |
|    Cheeseburger ...................... $6.49      |
|    [featured image]  BBQ Bacon Burger . $8.99     |
|                                                   |
|  CHICKEN                               [category] |
|    Chicken Tenders (4pc) ............. $5.49      |
|    Chicken Sandwich .................. $6.99      |
+--------------------------------------------------+
```

- Header at top with configurable text
- Categories as bold section headers
- Items listed with dot leaders to price, right-aligned
- Featured items optionally show a small image to the left of the name
- High contrast colors, large font - readable from 10+ feet
- No animations, no scrolling - static content

## Admin Interface

Single window with two main areas:

**Left sidebar:**
- Two tabs/buttons: "TV 1 - Hot Food" and "TV 2 - Drinks/Snacks"
- Under each: list of categories with drag-to-reorder (or up/down buttons)
- "Add Category" button

**Right content area (when a category is selected):**
- Category name (editable text field)
- List of items in that category with reorder controls
- "Add Item" button
- Each item row: Name, Price, Description, Image (browse button), Available toggle, Delete button

**Display settings (accessible per screen):**
- Header text
- Background color picker
- Font scale slider

Changes save to SQLite immediately on edit (no separate save button). Display windows observe the data and refresh.

## Monitor Management

On startup:
1. Enumerate `System.Windows.Forms.Screen.AllScreens`
2. Place AdminWindow on the primary monitor
3. Place DisplayWindow1 on the second monitor (if present)
4. Place DisplayWindow2 on the third monitor (if present)
5. If fewer than 3 monitors, show a notice in admin but still allow editing (display windows just won't open for missing monitors)

Display windows are `WindowStyle=None, WindowState=Maximized, Topmost=True` on their assigned screen.

## Project Structure

```
MenuBoard/
  MenuBoard.sln
  src/
    MenuBoard/
      App.xaml / App.xaml.cs
      Models/
        Category.cs
        MenuItem.cs
        DisplaySettings.cs
      Data/
        MenuDbContext.cs
      ViewModels/
        AdminViewModel.cs
        CategoryViewModel.cs
        MenuItemViewModel.cs
        DisplayViewModel.cs
      Views/
        AdminWindow.xaml
        DisplayWindow.xaml
      Converters/
        CentsToDollarsConverter.cs
        ImagePathConverter.cs
      Services/
        MonitorService.cs
        ImageService.cs
  Images/
```

## Key Behaviors

- **Price handling:** Stored as integer cents, displayed as "$X.XX" via a value converter.
- **Image management:** Browse button opens a file dialog. Selected image is copied into the `Images/` folder. Deleting an item does not delete its image file (other items could reference it).
- **Data change propagation:** Admin ViewModel raises an in-process event when data changes. Display ViewModels subscribe and refresh immediately. No polling needed since everything runs in one process.
- **Startup:** App creates the SQLite database on first run with EF migrations. Seeds empty display settings for both screens with sensible defaults (black background, white text, scale 1.0).
- **Graceful degradation:** If only 1 or 2 monitors are connected, the app still runs - admin always works, display windows open only for detected monitors.
