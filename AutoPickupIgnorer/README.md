# AutoPickupIgnorer

##### by Pip

This project is a recreation of an old mod that I used to love, but which sadly has not been maintained in some time.
The mod allows the player to specify which items will and won't be auto-picked-up.

Once Valheim is run with the mod installed, a config file will be created (PipMod.AutoPickupIgnorer.cfg).
In order to ignore an item for auto-pickup, open the config file remove the # in front of the item.

There is also a configurable hotkey (' by default) that will toggle the pickup behavior between three states:

1. Ignore items as specified by the config file
2. Ignore all items
3. Default Valheim behavior (pickup items unless you have previously dropped them)

Source: [Github](https://github.com/michaelpipkin/PipValheimMods/tree/main/AutoPickupIgnorer)

Install with [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)

Copy AutoPickupIgnorer.dll into the BepInEx/plugins folder
