## Release 1.0.0

- Jumping no longer cancels auto-run
- Strafing no longer cancels auto-run, and now leans your line sideways instead of being ignored
- Crouching no longer cancels auto-run; it turns the sprint toggle off and you continue at sneak speed
- Holding block to steer no longer raises your shield when you jump, strafe or crouch at the same time; vanilla already kept it down while you were only steering
- Dodging is suppressed while auto-running, so a jump with the shield up or while crouched is a jump instead of a dodge roll
- The sprint toggle now stays on when you stop moving, until you turn it off or run out of stamina
- Jump presses that arrive a fraction too early are remembered briefly and retried, instead of being dropped
- While flying with the console `fly` command, the crouch key only descends and no longer leaves a crouch toggled on
