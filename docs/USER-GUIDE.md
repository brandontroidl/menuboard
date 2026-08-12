# Menu Board - User Guide (How-To)

This guide is for the person running the store. No technical knowledge needed. It covers everyday use: changing prices, adding items, adding pictures, and what to do when something looks wrong.

## The basics

Menu Board runs on the store PC and puts a menu on each TV:

- **TV 1** - Hot Food / Meals
- **TV 2** - Drinks / Snacks

The **admin editor** is where you make changes. Every change you make shows up on the TVs **immediately** and is **saved automatically** - there is no Save button, and everything survives shutdowns and reboots.

## Getting to the admin editor

- If you can see the admin window (titled "Menu Board Admin"), just click it.
- If the menus cover every screen: **press Esc** or **double-click on either TV's menu** and the admin window comes to the front.
- If the app isn't running at all, start `MenuBoard.exe` (or reboot the PC if auto-start is on).

## Everyday tasks

### Change a price
1. In the admin editor, pick the right TV using the radio buttons in the left sidebar (TV 1 or TV 2).
2. Click the category in the sidebar that holds the item.
3. Click into the item's price box, type the new price (e.g. `5.99` or `$5.99`), and click anywhere else. Done - the TV updates instantly.

If you type something that isn't a price, the old price stays - nothing gets wiped.

### Add an item
1. Pick the TV and category.
2. Click **+ Add Item**. A "New Item" row appears.
3. Click into the name box and type the item name, then set the price. An optional short description goes in the third box.

### Mark an item sold out (without deleting it)
Uncheck the **Available** box on the item's row. The item disappears from the TV but stays in your list. Check it again when it's back.

### Add or change a picture
1. Click the **Image** button on the item's row.
2. Pick a JPG or PNG file. The picture is copied into the app's own folder, so you can delete the original afterward.

Use pictures sparingly - a few featured items with photos looks better than a photo on everything.

### Add, rename, reorder, or delete a category
- **Add:** click **+ Add Category** in the sidebar, then type the name in the "Category:" box on the right.
- **Rename:** select the category, edit the "Category:" box.
- **Reorder:** select it and use the **Up** / **Down** buttons at the bottom of the sidebar.
- **Delete:** select it and click **Delete**. This also deletes all items inside it, so double-check first.

### Reorder items within a category
Use the **^** and **v** buttons on each item row.

## Changing how a TV looks

Expand **Display Settings** at the top of the right-hand panel (it applies to whichever TV is selected in the sidebar):

- **Header Text** - the big title at the top of that TV (e.g. "HOT FOOD & MEALS").
- **Background Color** - a hex color code like `#000000` (black) or `#1a1a2e` (dark navy).
- **Font Scale** - drag to make everything on that TV bigger or smaller. 1.0-1.5 works for most TVs.

## App Settings (one-time setup)

Expand **App Settings**:

- **Start Menu Board automatically when Windows starts** - keep this checked on the store PC so the menus come back by themselves after a power cut or reboot.
- **Monitor layout** - leave on "Automatic (recommended)". It puts the admin editor on the PC's own screen and a menu on each TV.
- **Swap which TV shows Hot Food vs Drinks/Snacks** - use this if the menus come up on the wrong TVs.

## Closing vs. minimizing

Clicking the X on the admin window asks **"Exit Menu Board?"**:

- **No** (the safe choice) - the admin window minimizes and the TVs keep showing menus.
- **Yes** - the whole app closes and **both TVs go blank**. Only do this on purpose.

## Backing up your menu

Your entire menu lives in two things, both in the same folder as `MenuBoard.exe`:

1. `menuboard.db` - all items, prices, categories, and settings
2. the `Images` folder - all item photos

Copy both to a USB stick every so often. To restore, copy them back into the folder next to `MenuBoard.exe` (with the app closed) and start the app.

## Quick troubleshooting

| Problem | Fix |
|---|---|
| TVs are blank | Is the PC on? Is `MenuBoard.exe` running? Reboot the PC - if auto-start is on, everything comes back by itself. |
| Menus are on the wrong TVs | App Settings → check "Swap which TV shows Hot Food vs Drinks/Snacks". |
| One TV shows the desktop instead of a menu | Right-click desktop → Display settings → make sure the TV is in **Extend** mode (not Duplicate), then restart the app. |
| Can't find the admin window | Press Esc or double-click on a TV menu. |
| Text too small/large on a TV | Display Settings → drag Font Scale. |
| A picture doesn't show | Re-add it with the Image button; use a JPG or PNG. |
| Everything is gone / database broken | Restore `menuboard.db` and `Images` from your backup (see above). |

For anything deeper, see `INSTALL.md` (setup) and `docs/HANDOFF.md` (full runbooks).
