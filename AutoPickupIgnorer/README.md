# AutoPickupIgnorer

##### by Pip

This project is a recreation of an old mod that I used to love, but which sadly has not been maintained in some time.
The mod allows the player to specify which items will and won't be auto-picked-up.

Once Valheim is run with the mod installed, a config file will be created (Pip.AutoPickupIgnorer.cfg).
In order to ignore an item for auto-pickup, open the config file and remove the # in front of the item.

If you don't care to track items in the config file that you are not ignoring, you can remove all #-prefixed items from the list,
and simply include a comma-separated list of the items you want to ignore.

The automatically generated list of items in the config file is not exhaustive, especially as new items are added to the game,
and you may need to add items manually. Simply add new items to the list without a leading #, and they will be ignored.

The config file wants an item's prefab name, which is often not what the game calls it on screen -
Abyssal Razor is `KnifeChitin`, and Acorns is `Acorn`. The
[item list](https://github.com/michaelpipkin/PipValheimMods/blob/main/AutoPickupIgnorer/ItemList.md)
has every item in the game with both names side by side, sorted by the name you see in game.
It is read straight out of Valheim, and updated when the game adds items.

The item list is kept in alphabetical order (ignoring the leading #) so it's easy to check whether an item is already there.
New items can be added anywhere in the list, and will be sorted into place the next time the config is loaded.
Duplicate entries are removed automatically.

Changes to the config file can be applied without relaunching the game by pressing the reload hotkey (= by default).
This also sorts any newly added items into place, so save your changes in your editor before pressing it.

There is also a configurable hotkey (' [single quote] by default) that will toggle the pickup behavior between three states:

1. Ignore items as specified by the config file
2. Ignore all items
3. Default Valheim behavior (pickup items unless you have previously dropped them)

Hotkeys are ignored while you are typing in chat, on a sign, in the console, or in the build menu search box.

Source: [Github](https://github.com/michaelpipkin/PipValheimMods/tree/main/AutoPickupIgnorer)

Install with [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)

If not using the Thunderstore mod manager, copy AutoPickupIgnorer.dll into the BepInEx/plugins folder
