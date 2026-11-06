## Release 1.0.0

- Added a question-mark button to the map legend, below the boss icon, for quest pins.
- Discovering a new quest site no longer switches the category back on while it is hidden
- Added `DefaultVisiblePins`, which sets the icons you want switched on at the start of every
  session. Valheim switches all of them back on each time it loads a world and never saves the
  toggles, so without this the same handful has to be switched off by hand every time
- Loading a world no longer switches icons back on behind the startup set. Re-adding a saved pin
  normally un-hides its whole category, so a single saved campfire was enough to light the campfire
  icon back up
- Only icons the legend can toggle are ever touched. Beds, other players, pings, shouts and event
  markers are left alone, since nothing in the UI could switch those back on
- `VerboseLogging` names every icon switched on or off, by name, number and sprite, by you or by
  the game
