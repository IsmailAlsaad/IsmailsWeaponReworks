# Ismail's Weapon Reworks: Roadmap

Standalone tModLoader mod (internal name `IsmailsWeaponReworks`) with reworks of vanilla Terraria weapons. It depends on no other mod. Factorraria lists it in `modReferences`. Each weapon gets its own section; one chat = one patch (or a few small patches of the same weapon). Vanilla items are reworked through a `GlobalItem` (recipe, drops and base stats stay vanilla). Vanilla dust and sounds only; Ismail provides all art (every sprite has a vanilla fallback until his file exists). Multiplayer first: the owner's client decides, everyone else mirrors.

Design preferences: lots of particles and visuals with trails and sound effects and animations and all the satisfying game juice.

Layout: one folder per weapon, `Content/Weapons/<Weapon>/` (GlobalItem, projectiles, Tuning, Fx, art). Namespace always equals the folder path. Shared code goes into `Common/` only when a second weapon needs it.

## Progress

- [x] Migration: Ice Boomerang imported from Factorraria commit `183e77e` (tested alone and together with Factorraria, all checks passed)
- [x] Migration: old copy removed from Factorraria (`patch_remove_weapons.ps1`); Factorraria lists this mod in `modReferences` (hard dependency); tested, disabling this mod while Factorraria is enabled is refused
- [ ] R1: synced server config with one toggle per rework (checked in each GlobalItem's `AppliesToEntity`)
- [~] Ice Blade: tap chop, 3-tier click charge, slash wave, freeze lockout (patch 2 applied, untested: release swing = tap swing, wave passes through blocks, connected trail)
- [~] R2: boomerang and blast hit sparks visible to other players (applied, untested)

## 1. Ice Boomerang (vanilla `ItemID.IceBoomerang`) [DONE]

Throw it and it becomes a crystal; hit a crystal and it chains through all of them, exploding each. Only the one-line stub of the original design text survived in Factorraria's repo; restore the design details here if you want them kept.

Multiplayer review (2026-11-01, code review only, not yet tested with two players): the owner's client decides everything (throw, stop point, crystal spawn and eviction, chain targets, blast, chill); other machines run the same motion and the visuals from synced ai values and netUpdate. NPC chill goes through vanilla `NPC.AddBuff`, which syncs unless `quiet` is set. No functional desync found. R2: hit sparks (`OnHitNPC` only runs on the owner's client) are now also shown to the other players: every other machine runs the same swept hit test on its own copy of the enemies and spawns the sparks itself (visual only, no packets, once per enemy per segment). Known cosmetic gap, left as it is: a player who joins late sees each existing crystal with a fresh 30 s timer.

## 2. Ice Blade (vanilla `ItemID.IceBlade`) [patch 2 applied, untested]

Design: tap the attack button = plain custom melee chop (no wave, no cooldown). Hold = charge: ice-crack clicks that rise in pitch, one per tier (ticks 18 / 36 / 54), then a 4th low click at tick 68. Releasing after click 1, 2 or 3 = slash wave of that tier; releasing between click 3 and click 4 is the sweet spot. Still holding at click 4 = vanilla `BuffID.Frozen` for 3 s (roots the player and blocks every item); no wave fires. Click timing is fixed ticks, not scaled by melee speed.

Look: `IceBladeSwingProjectile` is a held projectile that draws the vanilla blade sprite. The arm lifts with a cubic ease-out (blade tilt in `IceBladeTuning`) and reaches full height on click 3; the blade trembles before click 4. Every click flashes and glows the blade and bursts dust. Release = the exact same chop as the tap swing (one shared timeline), with a connected freezing trail behind the blade (fainter for lower tiers); only the wave flies toward the cursor. `IceBladeWaveProjectile` is a crescent drawn from pixel squares (thick middle, tapering tips, afterimages, halo, blue Ice-dust fog). It expands, slows down and fades but its damage stays constant; it passes through blocks and hits each enemy once. Tier damage multipliers 0.55 / 1.0 / 1.8. All numbers are in `IceBladeTuning`, all dust and sound in `IceBladeFx`. Shared dust helpers were moved to `Common/IceFx.cs` (`IceBoomerangFx` forwards to them).

Multiplayer design: the owner's client decides release, tier, aim, freeze and the wave spawn, and syncs them through the held projectile's ai[0] phase, ai[1] tier, ai[2] aim angle (netUpdate). Every machine runs the same timers for the arm pose, click sounds, flash, dust and trail. The wave is a normal spawned projectile (synced by tModLoader, hit by the owner's client). The Frozen buff is added by the owner's client only. No custom packets.

Art: none needed (vanilla blade sprite and the 1x1 magic pixel).

Test checklist: tap swings and damages; holding plays 3 rising clicks then a low 4th; release before click 1 = chop only; release after click 1/2/3 = three clearly different wave sizes and damage; release between click 3 and 4 = biggest; holding past click 4 freezes the player about 3 s with no wave; swapping items mid-charge ends cleanly; waves stop at walls; the arm pose is right facing left and with the cursor on both sides; two players: the other player sees the arm lift, clicks, flash, slash, trail and wave, and the wave's hits register for the owner.