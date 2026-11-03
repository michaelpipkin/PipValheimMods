# ImprovedAutoRunAndJump

##### by Pip

Auto-run that survives the keys you actually press, a sprint toggle that stays on, and a jump buffer.

Client-side only. Works on a server without the server or anyone else having it, and nobody else
sees a difference.

## Auto-run

- **Jump**, **strafe** and **crouch** no longer cancel the run.
- **Forward/back** still stop the run, as do attacking, dodging and build mode.

## Sprint toggle

Stays on when you stop moving; only running out of stamina clears it. Needs the game's own
run-toggle setting under Gameplay — Valheim labels that mechanic "Run", but this mod calls it
sprint to keep it apart from auto-run. With hold-to-sprint there is nothing to keep.

## Jump

An early press — mid-stride, or a moment after stepping off a ledge — is remembered for
`JumpBufferSeconds` and retried rather than dropped. Anything that legitimately refuses a jump
still refuses it.

## Flying

With the console `fly` command, the crouch key only descends, and any crouch is dropped on takeoff.

## Settings

`Pip.ImprovedAutoRunAndJump.cfg`, written on first run. Everything is on by default and read live,
so a config manager can change it mid-game.

| Setting                | Default |                                                               |
| ---------------------- | ------- | ------------------------------------------------------------- |
| `AutoRunKeepOnJump`    | true    | Jumping does not cancel auto-run                              |
| `AutoRunKeepOnStrafe`  | true    | Strafing does not cancel auto-run                             |
| `AutoRunKeepOnCrouch`  | true    | Crouching does not cancel auto-run                            |
| `AutoRunSuppressDodge` | true    | A jump during auto-run is never read as a dodge               |
| `AutoRunSuppressBlock` | true    | Steering does not raise your shield                           |
| `NoCrouchWhileFlying`  | true    | While flying, the crouch key only descends                    |
| `StickySprintToggle`   | true    | The sprint toggle survives standing still                     |
| `JumpBufferSeconds`    | 0.3     | Seconds an early jump press is retried. 0 disables the buffer |
| `LogJumpBlocks`        | false   | Diagnostic: log what refused each jump                        |
| `VerboseLogging`       | false   | Diagnostic: log the moments this mod steps in                 |

Source: [Github](https://github.com/michaelpipkin/PipValheimMods/tree/main/ImprovedAutoRunAndJump)

Install with [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)

If not using the Thunderstore mod manager, copy ImprovedAutoRunAndJump.dll into the BepInEx/plugins folder
