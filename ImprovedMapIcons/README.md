# ImprovedMapIcons

##### by Pip

A show/hide toggle for the question-mark quest pins, and a map that opens with the icons you
actually want switched on.

Client-side only. Works on a server without the server or anyone else having it, and nobody else
sees a difference.

## The quest pin toggle

Hildir's three quest sites and the Ashlands Mysterious Locations all drop a `?` pin on
your map. Those are the only pins the game gives no toggle for — you can cross one off or delete
it, but you cannot simply hide the category the way you can with houses or portals.

This adds the button the legend never had, directly below the boss icon, and the shaded panel
behind that group grows to hold it. It behaves exactly like the death and boss buttons above it:

- **Right-click** shows or hides every quest pin at once. The button greys out while they are
  hidden.
- **Left-click** does nothing on a single press, and toggles on a double-tap.

There is no left-click-to-select, because nothing can place a `?` pin by hand.

Discovering a new quest site while they are hidden leaves them hidden. The game normally switches a
category back on whenever it adds a pin to it, which would undo the toggle every time Hildir's
table revealed somewhere new.

## Icons that stay switched off

Every time Valheim loads a world it switches all of your map icons back on. The toggles work
perfectly well — they just are not saved anywhere, so the same handful has to be switched off by
hand at the start of every session.

`DefaultVisiblePins` lists the icons you want on. Everything else starts off. The default is the
house and the portal.

Only icons the legend has a button for are touched. Your bed, other players, pings, shouts and
event markers are left alone, since nothing in the UI could switch those back on again.

## Icon names

`DefaultVisiblePins` and `QuestPinTypes` both take these. **Use the names, not the numbers** — the
numbers are not in legend order, and the two traps below are easy to fall into.

| Name                          | Number   | Icon                       |
| ----------------------------- | -------- | -------------------------- |
| `Icon0`                       | 0        | Campfire                   |
| `Icon1`                       | 1        | House                      |
| `Icon2`                       | 2        | Hammer                     |
| `Icon3`                       | 3        | Plain dot                  |
| `Icon4`                       | **6**    | Portal                     |
| `Death`                       | **4**    | Death marker (upper group) |
| `Boss`                        | 9        | Boss marker (upper group)  |
| `Hildir1` `Hildir2` `Hildir3` | 14 15 16 | The `?` quest markers      |

The two traps: **`Icon4` is 6, not 4**, and **4 is `Death`**. The campfire through portal icons read
top to bottom down the lower group of the legend, but the underlying numbers do not.

If you would rather read it off your own game than trust a table, turn `VerboseLogging` on, open the
map and click an icon. Every toggle logs its name, its number and its sprite:

```
Map icon Icon4 (6, mapicon_portal) is now hidden
```

## Settings

`Pip.ImprovedMapIcons.cfg`, written on first run. Read live, so a config manager can change
anything mid-game and the map updates on the next frame.

| Setting              | Default                 |                                                                                 |
| -------------------- | ----------------------- | ------------------------------------------------------------------------------- |
| `AddQuestIcon`       | true                    | Add the `?` button to the map legend                                            |
| `QuestPinTypes`      | Hildir1,Hildir2,Hildir3 | Which pin types that button governs                                             |
| `LegendNudge`        | 0                       | Extra pixels to lift the top icon group, if the spacing is off on your UI scale |
| `DefaultVisiblePins` | Icon1,Icon4             | Icons that start switched on. Empty leaves the game alone                       |
| `VerboseLogging`     | false                   | Diagnostic: log every icon toggle, by name, number and sprite                   |

Changing `DefaultVisiblePins` re-applies it immediately, so it doubles as a one-click way to put
the map back to your preferred set after a session of switching things on and off.

Source: [Github](https://github.com/michaelpipkin/PipValheimMods/tree/main/ImprovedMapIcons)

Install with [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)

If not using the Thunderstore mod manager, copy ImprovedMapIcons.dll into the BepInEx/plugins folder
