using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ImprovedAutoRunAndJump
{
    /// <summary>
    /// Auto-run that survives the keys you actually press, a sprint toggle that survives standing
    /// still, and a jump that is not thrown away for arriving a fraction too early.
    ///
    /// Everything here is client-side. The patches read and write the local player's own input and
    /// movement state, nothing is sent anywhere, and no other player sees a difference - so this
    /// works on a server without the server or anyone else having it installed.
    /// </summary>
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInProcess("valheim.exe")]
    public class ImprovedAutoRunAndJump : BaseUnityPlugin
    {
        // Module info
        private const string modGUID = "Pip.ImprovedAutoRunAndJump";
        private const string modName = "Pip's Improved Auto-run and Jump";
        private const string modVersion = "1.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);

        // Config file entries
        private static ConfigEntry<bool> AutoRunKeepOnJump;
        private static ConfigEntry<bool> AutoRunKeepOnStrafe;
        private static ConfigEntry<bool> AutoRunKeepOnCrouch;
        private static ConfigEntry<bool> AutoRunSuppressDodge;
        private static ConfigEntry<bool> AutoRunSuppressBlock;
        private static ConfigEntry<bool> NoCrouchWhileFlying;
        private static ConfigEntry<bool> StickySprintToggle;
        private static ConfigEntry<float> JumpBufferSeconds;
        private static ConfigEntry<bool> LogJumpBlocks;
        private static ConfigEntry<bool> VerboseLogging;

        private void Awake()
        {
            Game.isModded = true;

            AutoRunKeepOnJump = Config.Bind("Auto-run", "AutoRunKeepOnJump", true,
                "Let you jump without dropping out of auto-run. Vanilla cancels auto-run on any jump, which makes it useless for crossing anything but flat ground.");
            AutoRunKeepOnStrafe = Config.Bind("Auto-run", "AutoRunKeepOnStrafe", true,
                "Let you strafe without dropping out of auto-run, steering around a tree or a rock instead of stopping for it. Forward and back still cancel, and remain the way to stop. The strafe is applied as a sideways lean on your current heading rather than as a turn, so letting go of the key puts you back on the original line instead of leaving you running off at an angle.");
            AutoRunKeepOnCrouch = Config.Bind("Auto-run", "AutoRunKeepOnCrouch", true,
                "Let you crouch without dropping out of auto-run, so holding a line while sneaking works the same as it does while running. The crouch itself still happens, and because crouching and sprinting are mutually exclusive in this game it turns the sprint toggle off - so auto-run continues at sneak speed rather than stopping.");
            AutoRunSuppressDodge = Config.Bind("Auto-run", "AutoRunSuppressDodge", true,
                "Stop auto-run turning a jump into a dodge. Holding block is the only way to steer while auto-running, and with a shield up the game reads jump as a dodge roll - which ends the run and sends you somewhere you did not ask to go. Crouching does the same thing. With this on, a jump while auto-running is always a jump, and the dodge key does nothing until the run ends. Dodging is a combat move and auto-run is for covering ground, so the two never want each other.");
            AutoRunSuppressBlock = Config.Bind("Auto-run", "AutoRunSuppressBlock", true,
                "Keep the shield down while auto-running. Vanilla already does this much: while you are only steering, it clears the block every frame. What it does not survive is steering and doing something else at the same time - jump, strafe or crouch and the shield goes up, slowing you and cutting stamina and eitr regeneration for a guard you never asked for. With this on the rule holds whatever else you press. Releasing block, or ending the run, gives the shield back immediately.");
            NoCrouchWhileFlying = Config.Bind("Auto-run", "NoCrouchWhileFlying", true,
                "Take crouch out of the picture entirely while flying, so the crouch key only ever means descend. Only reachable with the console 'fly' command, and ignored entirely if you never use it. Sneaking is pointless in flight - nothing can target you, on the ground or otherwise - and a crouch left toggled on in the air is actively harmful: it halves nothing, hides nothing, and turns the climb key into a dodge roll. Any crouch already held is dropped as you take off.");
            StickySprintToggle = Config.Bind("Sprint", "StickySprintToggle", true,
                "Make the sprint toggle stay on when you stop moving, instead of being thrown away the moment you stand still. With this off, every pause - looting a body, opening a door, waiting for a boat - costs another press of the sprint key to get going again. Running out of stamina still turns it off, since that is the one case where the game needs you to notice. Requires the game's own toggle setting for it to be on, under Gameplay settings - Valheim labels that one \"run\" rather than sprint - because if you hold the key to sprint instead, there is no toggle to keep.");
            JumpBufferSeconds = Config.Bind("Jump", "JumpBufferSeconds", 0.3f, new ConfigDescription(
                "How long a jump press is remembered and retried when the game refuses it, in seconds. A press that lands a fraction too early - while still in the air from a stride or a step off a ledge - is otherwise dropped silently. Set to 0 to turn the buffer off.", new AcceptableValueRange<float>(0f, 1f)));
            LogJumpBlocks = Config.Bind("Jump", "LogJumpBlocks", false,
                "Diagnostic. On every jump press, log the state the game decides from: whether you are grounded, blocking, crouched, attacking and so on. Use it to find out what is actually swallowing a jump.");
            VerboseLogging = Config.Bind("Logging", "VerboseLogging", false,
                "Log the moments this mod steps in - an auto-run kept through a jump, a jump taken over from the dodge handler, a crouch dropped on takeoff. Off by default because none of it is actionable when things are working, and writing to the log is not free. Turn it on if a setting does not seem to be doing anything.");

            harmony.PatchAll();
        }

        private void Update()
        {
            UpdateJumpBuffer();
        }

        /// <summary>
        /// A line that is only worth writing when something is being investigated. Unity's logger
        /// is synchronous and these sit in input paths that run every frame, so they are off unless
        /// asked for.
        /// </summary>
        private static void LogInfo(string message)
        {
            if (VerboseLogging != null && VerboseLogging.Value)
            {
                Debug.Log(message);
            }
        }

        // Whether a text field has focus. The jump buffer uses the game's own TakeInput below,
        // which is broader; this is the fallback for when that cannot be resolved.
        private static bool IsTypingText()
        {
            if (Console.IsVisible() || TextInput.IsVisible() || Minimap.InTextInput()
                || PlayerCustomizaton.IsBarberGuiVisible())
            {
                return true;
            }
            if (Chat.instance != null && Chat.instance.HasFocus())
            {
                return true;
            }
            // The build menu's search box takes typed input without any of the above being true
            BuildUi buildUi = Hud.instance != null ? Hud.instance.m_buildUi : null;
            return buildUi != null && buildUi.SearchFieldFocused;
        }

        // The jump buffer uses the game's own aggregate instead, so a buffered press can never
        // fire somewhere vanilla would have refused the original - it also covers menus, cutscenes,
        // death and teleporting. Non-public in the shipped assembly, hence the handle.
        private static readonly MethodInfo TakeInputMethod = AccessTools.Method(typeof(Player), "TakeInput");

        private static bool PlayerTakingInput(Player player)
        {
            if (TakeInputMethod == null)
            {
                return !IsTypingText();
            }
            return TakeInputMethod.Invoke(player, null) is bool taking && taking;
        }

        // ---------------- Jump leniency ----------------
        // Player.SetControls routes the jump key three ways, and only the last one jumps:
        //
        //     if (m_blocking)                                     Dodge(...)
        //     else if (IsCrouching() || m_crouchToggled || dodge)  Dodge(...)
        //     else                                                Jump()
        //
        // and Character.Jump then drops the call entirely unless IsOnGround(), which is
        // m_lastGroundTouch < 0.2f. So a press can be swallowed either by being routed to a dodge
        // or by landing during the fraction of a second a running stride leaves the ground.
        //
        // The buffer addresses the second case without changing any of vanilla's rules: the press
        // is remembered and retried for a short window, so one that arrived a little early takes
        // effect the moment the character can actually jump. Nothing is forced - Jump still decides
        // - so stamina, dodges, attacks and staggers all still block it exactly as before.
        private static readonly FieldInfo LastGroundTouchField = AccessTools.Field(typeof(Character), "m_lastGroundTouch");
        private static readonly FieldInfo JumpTimerField = AccessTools.Field(typeof(Character), "m_jumpTimer");
        private static readonly FieldInfo CrouchToggledField = AccessTools.Field(typeof(Player), "m_crouchToggled");
        // PlayerController owns the sprint toggle, which vanilla calls m_run. Reached by reflection
        // rather than Harmony field injection because it has to be written from a patch on Player,
        // which is a different type. The handle keeps the field's own name so the mapping stays
        // obvious in the IL; everything addressed to a reader calls it the sprint toggle.
        private static readonly FieldInfo ControllerRunField = AccessTools.Field(typeof(PlayerController), "m_run");
        // Private in the shipped assembly. Cleared directly rather than by withholding the button,
        // because toggle-block players keep m_blocking across a frame where the button reads false.
        private static readonly FieldInfo BlockingField = AccessTools.Field(typeof(Character), "m_blocking");
        private static float _jumpPressedAt = float.NegativeInfinity;

        private static bool ReadBool(FieldInfo field, object target, bool fallback)
        {
            if (field == null || target == null)
            {
                return fallback;
            }
            object value = field.GetValue(target);
            return value is bool b ? b : fallback;
        }

        private static float ReadFloat(FieldInfo field, object target, float fallback)
        {
            if (field == null || target == null)
            {
                return fallback;
            }
            object value = field.GetValue(target);
            return value is float f ? f : fallback;
        }

        private static void UpdateJumpBuffer()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }
            // Vanilla only reads the jump button through a path gated on TakeInput, so without
            // this the buffer jumps while the player is typing
            if (!PlayerTakingInput(player))
            {
                _jumpPressedAt = float.NegativeInfinity;
                return;
            }
            bool pressed = ZInput.GetButtonDown("Jump");
            if (pressed)
            {
                _jumpPressedAt = Time.time;
                if (LogJumpBlocks.Value)
                {
                    LogJumpState(player);
                }
            }
            float buffer = JumpBufferSeconds.Value;
            if (buffer <= 0f || _jumpPressedAt == float.NegativeInfinity)
            {
                return;
            }
            if (Time.time - _jumpPressedAt > buffer)
            {
                _jumpPressedAt = float.NegativeInfinity;
                return;
            }
            // ForceJump zeroes m_jumpTimer, so a small value means a jump has just happened and the
            // buffered press has been satisfied - this is what stops it firing twice
            if (ReadFloat(JumpTimerField, player, 1f) < 0.1f)
            {
                _jumpPressedAt = float.NegativeInfinity;
                return;
            }
            // Retrying on the frame it was pressed would double up with vanilla's own handling
            if (pressed)
            {
                return;
            }
            if (!player.IsOnGround() || player.InDodge() || player.InAttack() || player.IsStaggering())
            {
                return;
            }
            player.Jump();
        }

        private static void LogJumpState(Player player)
        {
            float groundTouch = ReadFloat(LastGroundTouchField, player, -1f);
            object crouchToggled = CrouchToggledField?.GetValue(player);
            Debug.Log("Jump pressed: "
                + $"onGround={player.IsOnGround()} lastGroundTouch={groundTouch:0.###} "
                + $"blocking={player.IsBlocking()} crouching={player.IsCrouching()} crouchToggled={crouchToggled} "
                + $"inAttack={player.InAttack()} inDodge={player.InDodge()} staggering={player.IsStaggering()} "
                + $"knockedBack={player.IsKnockedBack()} encumbered={player.IsEncumbered()} "
                + $"stamina={player.HaveStamina(player.m_jumpStaminaUsage)}");
        }

        // ---------------- Auto-run ----------------
        //
        // Vanilla's auto-run cancels on jump and on any movement key, which leaves the mouse as the
        // only thing you can touch while it is running. Both of those are one expression in
        // Player.SetControls:
        //
        //     else if (m_autoRun)
        //     {
        //         if (attack || jump || dodge || crouch || movedir != Vector3.zero
        //             || InPlaceMode() || attackHold || secondaryAttackHold)
        //         { m_autoRun = false; }
        //         else if (autoRun || blockHold) { re-aim m_moveDir to m_lookDir }
        //     }
        //
        // Rather than transpile that condition, this lets the cancel happen and puts m_autoRun back
        // afterwards when the only thing that caused it was a jump or a strafe. That is safe because
        // nothing in the rest of SetControls reads m_autoRun - verified against the shipped IL, where
        // the field appears five times and the last is this store - so the false is never observed
        // by anything but us, and a condition the next patch reorders cannot silently defeat it.
        //
        // What makes the strafe actually do something is less obvious. The branch that derives
        // m_moveDir from the movement keys is guarded by !m_autoRun and runs *before* the cancel, so
        // on the frame A is pressed the key is simply not read: left to itself, re-arming would give
        // auto-run that ignores strafe rather than auto-run that strafes. So m_moveDir is composed
        // here instead.
        //
        // Q is deliberately left alone. One tap latches the run on - the start branch sets m_autoRun
        // and nothing clears it on release - which is what makes it auto-run rather than a second
        // forward key. Held, it also re-aims: ZInput.GetButton is a held read, so while the run is
        // going it keeps landing in the re-aim branch above and steers the heading toward the look
        // direction, exactly as right-click does. W and S end the run, which is vanilla behaviour
        // and wants no help.
        private struct AutoRunInput
        {
            public bool WasAutoRun;
            public Vector3 MoveDir;
            public bool Jump;
            public bool Crouch;
            public bool ReroutedJump;
            public bool Steering;
            public bool SuppressBlock;
            public bool Blocked;
        }

        // The heading auto-run is following, tracked separately from m_moveDir because m_moveDir is
        // what gets written with the strafe applied. Feeding a strafed vector back in as the next
        // frame's heading would curve the run - hold A and you would spiral rather than lean.
        private static Vector3 _autoRunHeading;

        [HarmonyPatch(typeof(Player), "SetControls")]
        class Player_SetControls_AutoRun_Patch
        {
            static void Prefix(Player __instance, Vector3 movedir, bool attack, bool attackHold,
                bool secondaryAttackHold, ref bool jump, ref bool crouch, ref bool dodge,
                ref bool block, ref bool blockHold, ref bool run, bool autoRun, out AutoRunInput __state)
            {
                __state = default(AutoRunInput);
                if (__instance == null || __instance != Player.m_localPlayer)
                {
                    return;
                }

                // Crouching in flight has nothing to offer. UpdateDebugFly reads the crouch key
                // straight off the keyboard for altitude and never consults the crouch state, so
                // the toggle is pure overhead - and worse than inert, because a crouch left on
                // turns the climb key into a dodge roll by way of the branch below.
                //
                // The state is cleared and not merely the press, because the press is the only way
                // back out: with crouch disabled in the air, taking off while already crouched
                // would strand the toggle on until landing. Clearing it every frame also catches
                // the console 'fly' command, which never passes through this mod's hotkey.
                //
                // Written through the field rather than SetCrouch, which is private in the shipped
                // assembly. Player.SetCrouch overrides it to assign this one field and calls no
                // base, so the two are the same thing.
                if (NoCrouchWhileFlying != null && NoCrouchWhileFlying.Value && __instance.IsDebugFlying())
                {
                    crouch = false;
                    if (ReadBool(CrouchToggledField, __instance, false))
                    {
                        CrouchToggledField?.SetValue(__instance, false);
                        LogInfo("Crouch dropped: it does nothing while flying");
                    }
                }

                __state.WasAutoRun = __instance.m_autoRun;
                __state.MoveDir = movedir;
                __state.Jump = jump;
                __state.Crouch = crouch;
                // Either of these means the heading should follow where you are looking. Captured
                // before the block suppression below takes blockHold away, because that is the only
                // signal right-click steering has.
                __state.Steering = autoRun || blockHold;

                // Right-click does double duty: it is the only way to steer an auto-run, and it is
                // the block button. Vanilla is alive to that and already resolves it in favour of
                // steering - the re-aim branch sets a local flag, and the tail of the method reads
                //
                //     else { m_blocking = blockHold; }
                //     if (flag) { m_blocking = false; }
                //
                // so holding right-click to steer does not raise the shield.
                //
                // What vanilla does not cover is steering while doing anything else. Jump, strafe
                // or crouch and the cancel branch runs instead of the re-aim branch, the flag is
                // never set, and m_blocking keeps the held button - so the shield goes up on
                // exactly the frames this patch exists to keep the run alive through. Reported
                // from play as strafing with right-click down raising the shield and slowing down.
                //
                // This continues vanilla's own rule rather than inventing one. The button is
                // withheld rather than the steering, which this patch now performs itself in
                // ApplyAutoRunHeading - vanilla's re-aim branch reads the same blockHold, so
                // taking it away would otherwise take steering with it.
                if (__state.WasAutoRun && AutoRunSuppressBlock != null && AutoRunSuppressBlock.Value)
                {
                    block = false;
                    blockHold = false;
                    __state.SuppressBlock = true;
                }

                // Dodging while auto-running is never wanted, and worse, it is hard to avoid: the
                // tail of SetControls reads a jump as a dodge whenever the shield is up or the
                // player is crouched -
                //
                //     if (!(jump || dodge)) { return; }
                //     if (m_blocking) { Dodge(...); }
                //     else if (IsCrouching() || m_crouchToggled || dodge) { Dodge(...); }
                //     else { Jump(); }
                //
                // - and right-click is the only way to steer, so steering and jumping at once rolls
                // you off your line. Rather than clear blockHold, which would drop the shield for a
                // frame and interrupt the steering this exists to protect, the jump is taken out of
                // vanilla's hands and performed from the Postfix. Jump(bool force = false) is public
                // and already how the jump-leniency code retries a swallowed press.
                if (__state.WasAutoRun && AutoRunSuppressDodge != null && AutoRunSuppressDodge.Value)
                {
                    dodge = false;
                    bool wouldDodge = blockHold || __instance.IsBlocking() || __instance.IsCrouching()
                        || ReadBool(CrouchToggledField, __instance, false);
                    if (jump && wouldDodge)
                    {
                        jump = false;
                        __state.ReroutedJump = true;
                    }
                }

                // Causes that are nothing to do with this feature, read after the suppression above
                // so a dodge we have just cancelled is not counted as one. InPlaceMode is the build
                // hammer, where a camera that keeps running would be actively unhelpful.
                __state.Blocked = attack || attackHold || secondaryAttackHold || dodge
                    || __instance.InPlaceMode();

                // Crouching and sprinting cannot coexist: Player.UpdateCrouch drops the crouch again
                // on any frame m_run is set. Vanilla gets away with never saying so, because the
                // sprint toggle is cleared the moment you stand still - stop, crouch, move off, and
                // it looks deliberate. StickySprintToggle removes that accident, so the exclusion has to
                // be honoured on purpose or crouch would be impossible while the toggle is on.
                //
                // Done on the press rather than from the crouched state, because reading the state
                // is a race: UpdateCrouch runs from Update and this from FixedUpdate, so whichever
                // lands first decides, and when it lands first the crouch is already gone.
                //
                // Both copies of the flag have to go, which is the part that is easy to get wrong.
                // The sprint toggle lives on PlayerController as m_run, but Character keeps its own
                // m_run that SetControls assigns from the run argument further down - and the
                // argument was read from the controller before this prefix ran, so clearing the
                // controller alone leaves the character's copy true for the rest of the call.
                // UpdateCrouch reads the character's copy. The symptom is precise and was reported
                // from play: the first press of the crouch key appears to do nothing but cancel
                // sprint, because the crouch is set and then dropped again inside the same frame,
                // and only a second press - by which time the cleared toggle has fed through into a
                // fresh run argument - actually crouches.
                //
                // Not while flying, where it would be a bug rather than a rule: UpdateDebugFly
                // reads crouch straight off the keyboard to descend and takes its speed from
                // m_run - "m_run ? m_debugFlySpeed * 2.5f : m_debugFlySpeed" - so dropping the
                // toggle here would cut flying speed to a third every time you lost altitude. The
                // exclusion it enforces is a ground rule; in the air nothing reads the crouch.
                if (crouch && !__instance.IsDebugFlying()
                    && !ReadBool(CrouchToggledField, __instance, false))
                {
                    run = false;
                    PlayerController controller = __instance.GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        ControllerRunField?.SetValue(controller, false);
                    }
                    LogInfo("Sprint dropped so the crouch can take effect on this frame");
                }
            }

            // m_moveDir and m_lookDir are private in the shipped assembly even though the publicized
            // reference copies show them public, so they arrive by Harmony field injection rather
            // than being touched directly. Reading __instance.m_moveDir here would compile and then
            // throw when this method was JIT'd.
            static void Postfix(Player __instance, AutoRunInput __state, ref Vector3 ___m_moveDir,
                ref Vector3 ___m_lookDir)
            {
                if (__instance == null || __instance != Player.m_localPlayer)
                {
                    return;
                }

                if (!__state.WasAutoRun)
                {
                    // Auto-run may have started on this very frame, in which case vanilla has just
                    // put the launch heading in m_moveDir and it is ours to follow from here
                    _autoRunHeading = __instance.m_autoRun ? ___m_moveDir : Vector3.zero;
                    return;
                }

                if (!__instance.m_autoRun)
                {
                    if (!ShouldKeepRunning(__state, out float forgivenStrafe))
                    {
                        _autoRunHeading = Vector3.zero;
                        return;
                    }
                    __instance.m_autoRun = true;
                    LogInfo($"Auto-run kept through {DescribeAutoRunCause(__state)}"
                        + (Mathf.Abs(forgivenStrafe) > 0.001f ? $" (strafe {forgivenStrafe:0.##})" : string.Empty));
                }

                // After SetControls has had its say: in hold-block mode it has just assigned
                // m_blocking from the blockHold we withheld, so this is already false, but a
                // toggle-block player's m_blocking survives a frame where the button reads false
                // and only clearing it settles both.
                if (__state.SuppressBlock)
                {
                    BlockingField?.SetValue(__instance, false);
                }

                ApplyAutoRunHeading(__state, ref ___m_moveDir, ref ___m_lookDir);

                // Last, so the jump leaves along the heading this call just settled rather than the
                // one it arrived with
                if (__state.ReroutedJump)
                {
                    LogInfo("Auto-run jump taken over from the dodge handler");
                    __instance.Jump();
                }
            }

            static string DescribeAutoRunCause(AutoRunInput state)
            {
                if (state.Jump)
                {
                    return "a jump";
                }
                return state.Crouch ? "a crouch" : "a strafe";
            }

            /// <summary>
            /// Whether a cancel that just happened was caused only by things this feature forgives.
            /// Anything else is left cancelled, including a cause we cannot see: if neither a jump
            /// nor a strafe was pressed then something we have not accounted for stopped the run,
            /// and re-arming would be overriding a decision we do not understand.
            /// </summary>
            static bool ShouldKeepRunning(AutoRunInput state, out float strafe)
            {
                strafe = 0f;
                if (state.Blocked)
                {
                    return false;
                }
                // Forward and back stop auto-run. This is vanilla's behaviour and the way it is
                // meant to be stopped, so it is not configurable.
                if (Mathf.Abs(state.MoveDir.z) > 0.001f)
                {
                    return false;
                }
                bool strafing = Mathf.Abs(state.MoveDir.x) > 0.001f;
                if (strafing && !AutoRunKeepOnStrafe.Value)
                {
                    return false;
                }
                if (state.Jump && !AutoRunKeepOnJump.Value)
                {
                    return false;
                }
                if (state.Crouch && !AutoRunKeepOnCrouch.Value)
                {
                    return false;
                }
                if (!state.Jump && !strafing && !state.Crouch)
                {
                    return false;
                }
                strafe = strafing ? state.MoveDir.x : 0f;
                return true;
            }

            /// <summary>
            /// Points m_moveDir along the tracked heading, with any strafe leaned into it.
            /// </summary>
            static void ApplyAutoRunHeading(AutoRunInput state, ref Vector3 moveDir, ref Vector3 lookDir)
            {
                float strafe = AutoRunKeepOnStrafe.Value ? state.MoveDir.x : 0f;
                bool strafing = Mathf.Abs(strafe) > 0.001f;

                // Holding Q or right-click points the heading where you are looking. Vanilla does
                // this itself in its re-aim branch, but it cannot be relied on for either of the
                // two reasons this patch exists: on a strafing frame it took the cancel branch and
                // never reached the re-aim, and once the block is suppressed the branch no longer
                // sees the blockHold that was asking for it. So steering is done here, for every
                // frame that asks for it, and vanilla's version becomes a duplicate that computes
                // the same heading from the same look direction.
                if (state.Steering)
                {
                    Vector3 aim = lookDir;
                    aim.y = 0f;
                    if (aim.sqrMagnitude > 0.0001f)
                    {
                        _autoRunHeading = aim.normalized;
                    }
                }

                // Only ever adopted from m_moveDir when nothing is tracked yet, which is the frame
                // the run starts. Reading it back on a later frame is what made the strafe behave
                // like a toggle: vanilla does not touch m_moveDir on a quiet auto-run frame, so
                // what is sitting there is the leaned vector this method wrote last frame. Taking
                // that as the new heading promoted the lean to the line, and releasing the key
                // left the character running off at the angle it had reached.
                //
                // So the heading is authoritative while the run lasts, and changes for exactly one
                // reason: steering, above. The strafe is re-applied to it from scratch every frame
                // and never folded back in, which is what makes A and D held keys - let go and the
                // next frame composes nothing and writes the bare heading.
                if (_autoRunHeading.sqrMagnitude < 0.0001f)
                {
                    _autoRunHeading = moveDir;
                }
                if (_autoRunHeading.sqrMagnitude < 0.0001f)
                {
                    return;
                }

                Vector3 heading = _autoRunHeading.normalized;
                _autoRunHeading = heading;

                // Normalised so a strafe leans the run rather than speeding it up: the composed
                // vector is longer than either part, and m_moveDir's magnitude is the speed the
                // movement code asks for.
                Vector3 composed = heading;
                if (strafing)
                {
                    composed = heading + strafe * Vector3.Cross(Vector3.up, heading);
                }
                composed.y = 0f;
                if (composed.sqrMagnitude < 0.0001f)
                {
                    return;
                }
                moveDir = composed.normalized;
            }
        }

        // ---------------- Sticky sprint ----------------
        //
        // With the game's sprint-toggle option on, the sprint key flips m_run - and then
        // PlayerController.FixedUpdate throws that away as soon as you stand still:
        //
        //     float magnitude = zero.magnitude;
        //     if (magnitude < 0.05f && m_lastMagnitude < 0.05f && !m_character.m_autoRun)
        //     { m_run = false; }
        //
        // Both the current and the previous frame have to be still, which is why it survives a
        // stumble but not a stop. Nothing else in the method clears the toggle: the early return
        // for suppressed input passes run: false without touching m_run, so an inventory visit
        // already survives in vanilla, and running the stamina out is handled a few lines above.
        //
        // Rather than transpile the statement out, this keeps m_lastMagnitude above the threshold
        // so the condition cannot be true. The field exists for precisely this test - it is
        // assigned at the end of the method and read nowhere else in the game - so pinning it is
        // confined to the behaviour being changed, and it leaves the real magnitude, the autoRun
        // term and the stamina check all working as written. The value is reasserted every frame
        // because the method overwrites it on the way out.
        //
        // Letting the toggle stick is safe for a reason worth recording: m_run is not what makes
        // you sprint. Character.CheckRun returns false when moveDir.magnitude is under 0.1, and
        // Player.CheckRun returns on that before it reaches UseStamina, so standing still with the
        // toggle on costs no stamina and plays no animation. The clear is a convenience, not a
        // guard - which is what makes it safe to remove and why it had to be checked first.
        [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
        class PlayerController_FixedUpdate_StickySprint_Patch
        {
            static void Prefix(ref float ___m_lastMagnitude, Player ___m_character)
            {
                // ToggleRun is the game's own setting. In hold-to-sprint, m_run is recomputed from the
                // key every frame and the clear below it changes nothing, so there would be no
                // point - and no toggle to keep.
                if (StickySprintToggle == null || !StickySprintToggle.Value || !ZInput.ToggleRun)
                {
                    return;
                }
                // While crouched the toggle is left alone, so that the clear this patch exists to
                // suppress can still do its other job: holding the sprint toggle off is what lets a
                // crouch survive UpdateCrouch, which drops it on any frame m_run is set. Pressing
                // the sprint key while crouched still works - the toggle below runs either way,
                // and standing up is UpdateCrouch's own response to m_run coming back.
                // Flying excluded for the same reason the crouch rule is: m_run is the fly speed
                // multiplier, and nothing in the air cares whether the player is crouched.
                if (___m_character != null && !___m_character.IsDebugFlying()
                    && ReadBool(CrouchToggledField, ___m_character, false))
                {
                    return;
                }
                // A full stick deflection: enough to fail "< 0.05f" with room to spare, and a value
                // the field could legitimately hold, so nothing downstream sees anything strange.
                ___m_lastMagnitude = 1f;
            }
        }
    }
}
