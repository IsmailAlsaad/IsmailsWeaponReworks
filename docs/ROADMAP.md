# Ismail's Weapon Reworks: Roadmap

Standalone tModLoader mod (internal name `IsmailsWeaponReworks`) with reworks of vanilla Terraria weapons. It depends on no other mod. Factorraria lists it in `modReferences`. Each weapon gets its own section; one chat = one patch (or a few small patches of the same weapon). Vanilla items are reworked through a `GlobalItem` (recipe, drops and base stats stay vanilla). Vanilla dust and sounds only; Ismail provides all art (every sprite has a vanilla fallback until his file exists). Multiplayer first: the owner's client decides, everyone else mirrors.

Design preferences: lots of particles and visuals with trails and sound effects and animations and all the satisfying game juice.

Layout: one folder per weapon, `Content/Weapons/<Weapon>/` (GlobalItem, projectiles, Tuning, Fx, art). Namespace always equals the folder path. Shared code goes into `Common/` only when a second weapon needs it.

## Progress

- [x] Migration: Ice Boomerang imported from Factorraria commit `183e77e` (tested alone and together with Factorraria, all checks passed)
- [x] Migration: old copy removed from Factorraria (`patch_remove_weapons.ps1`); Factorraria lists this mod in `modReferences` (hard dependency); tested, disabling this mod while Factorraria is enabled is refused
- [ ] R1: synced server config with one toggle per rework (checked in each GlobalItem's `AppliesToEntity`)
- [~] Ice Blade: tap chop, 3-tier click charge, slash wave, freeze lockout (patch 2 applied, untested: release swing = tap swing, wave passes through blocks)
- [~] R2: boomerang and blast hit sparks visible to other players (applied, untested)
- [~] Enchanted Boomerang: left = normal throw, right = ethereal throw, collision = stardust burst + bounce (patch 1 applied, untested)
- [~] Trimarang: hold to spin 20 tiles out, ethereal forms on every 5th hit (patch 1 applied, untested)
- [~] Flamerang: fire explosion at the end of range, molten boomerangs with burning trails, obsidian shards on contact (patch 1 applied, untested)

## 1. Ice Boomerang (vanilla `ItemID.IceBoomerang`) [DONE]

Throw it and it becomes a crystal; hit a crystal and it chains through all of them, exploding each. Only the one-line stub of the original design text survived in Factorraria's repo; restore the design details here if you want them kept.

Multiplayer review (2026-11-01, code review only, not yet tested with two players): the owner's client decides everything (throw, stop point, crystal spawn and eviction, chain targets, blast, chill); other machines run the same motion and the visuals from synced ai values and netUpdate. NPC chill goes through vanilla `NPC.AddBuff`, which syncs unless `quiet` is set. No functional desync found. R2: hit sparks (`OnHitNPC` only runs on the owner's client) are now also shown to the other players: every other machine runs the same swept hit test on its own copy of the enemies and spawns the sparks itself (visual only, no packets, once per enemy per segment). Known cosmetic gap, left as it is: a player who joins late sees each existing crystal with a fresh 30 s timer.

## 2. Ice Blade (vanilla `ItemID.IceBlade`) [patch 2 applied, untested]

Design: tap the attack button = plain custom melee chop (no wave, no cooldown). Hold = charge: ice-crack clicks that rise in pitch, one per tier (ticks 18 / 36 / 54), then a 4th low click at tick 68. Releasing after click 1, 2 or 3 = slash wave of that tier; releasing between click 3 and click 4 is the sweet spot. Still holding at click 4 = vanilla `BuffID.Frozen` for 3 s (roots the player and blocks every item); no wave fires. Click timing is fixed ticks, not scaled by melee speed.

Look: `IceBladeSwingProjectile` is a held projectile that draws the vanilla blade sprite. The arm lifts with a cubic ease-out (blade tilt in `IceBladeTuning`) and reaches full height on click 3; the blade trembles before click 4. Every click flashes and glows the blade and bursts dust. Release = the exact same chop as the tap swing (one shared timeline), with a freezing fan trail behind the blade (fainter for lower tiers); only the wave flies toward the cursor, starting in front of the player halfway out along the blade's reach. The trail tapers: short at the start of the swing, the full blade length at the end. `IceBladeWaveProjectile` is a crescent drawn from pixel squares (thick middle, tapering tips, afterimages, halo, blue Ice-dust fog). It expands, slows down and fades but its damage stays constant; it passes through blocks and hits each enemy once. Tier damage multipliers 0.55 / 1.0 / 1.8. All numbers are in `IceBladeTuning`, all dust and sound in `IceBladeFx`. Shared dust helpers were moved to `Common/IceFx.cs` (`IceBoomerangFx` forwards to them).

Multiplayer design: the owner's client decides release, tier, aim, freeze and the wave spawn, and syncs them through the held projectile's ai[0] phase, ai[1] tier, ai[2] aim angle (netUpdate). Every machine runs the same timers for the arm pose, click sounds, flash, dust and trail. The wave is a normal spawned projectile (synced by tModLoader, hit by the owner's client). The Frozen buff is added by the owner's client only. No custom packets.

Art: none needed (vanilla blade sprite and the 1x1 magic pixel).

Test checklist: tap swings and damages; holding plays 3 rising clicks then a low 4th; release before click 1 = chop only; release after click 1/2/3 = three clearly different wave sizes and damage; release between click 3 and 4 = biggest; holding past click 4 freezes the player about 3 s with no wave; swapping items mid-charge ends cleanly; waves stop at walls; the arm pose is right facing left and with the cursor on both sides; two players: the other player sees the arm lift, clicks, flash, slash, trail and wave, and the wave's hits register for the owner.

## 3. Enchanted Boomerang (vanilla `ItemID.EnchantedBoomerang`) [patch 1 applied, untested]

Design: left click throws the normal boomerang, right click (`AltFunctionUse`) throws an ethereal one drawn see-through with a ghostly trail. One of each kind can be out at the same time (`EnchantedBoomerangProjectile` and `EtherealBoomerangProjectile` are two types sharing `EnchantedBoomerangBase`, so `ownedProjectileCounts` limits each kind separately). Both fly a fixed range (18 tiles, tunable) and return to the player; a block ends the way out and turns them back (both kinds, the ethereal one does NOT pass blocks); they pierce infinitely (`penetrate = -1`, an enemy can be hit again after 12 ticks, and again on the way back). Same damage for both.

Collision: when a normal and an ethereal one touch (swept test on their relative motion, 20 px radius each, forgiving on purpose; once per throw), `StardustBurstProjectile` explodes at the contact point (damages every enemy in 72 px once, 1.5x boomerang damage, gold and blue sparkle dust plus two pixel rings), and each boomerang flies off 90 degrees to its own travel direction, on the side that leads away from the other one, then returns. Head-on throws (right click along the line the left boomerang is coming back on) are the easy way to trigger it.

Multiplayer design: both boomerangs belong to the same player, so the owner's client sees both and decides everything (turn, wall hit, collision, bounce, burst spawn); it syncs through ai[0] phase, velocity and netUpdate. Other machines simulate the same motion and visuals, and the burst is a normal spawned projectile that every machine plays on its first tick. Other players' hit sparks come from `Common/RemoteHitTracker` (extracted from the Ice Boomerang's R2 code, which now uses it too). No custom packets.

Art: optional. Drop `EnchantedBoomerangProjectile.png` and `EtherealBoomerangProjectile.png` into `Content/Weapons/EnchantedBoomerang/` (same size as the vanilla boomerang sprite, single frame) and they replace the vanilla texture automatically.

Test checklist: left click throws and returns, right click throws the ghost version, both can be out together; each is limited to one in flight; both turn back at walls; piercing hits an enemy going out and coming back; head-on collision bursts in stardust at the meeting point and sends the boomerangs off sideways (apart from each other), then both return and are caught; a pair collides only once per throw; the burst damages nearby enemies once; Ice Boomerang still behaves the same (chain, crystals, chill) and still shows hit sparks to the other player; two players: the other player sees both boomerangs, the bounce, the burst and the hit sparks.

## 4. Trimarang (vanilla `ItemID.Trimarang`) [patch 1 applied, untested]

Design: hold left click. The boomerang flies toward the cursor and glides to a stop exactly 20 tiles away (the speed is proportional to the distance left), or stops at the first block, and then spins there until the button is released (it also comes back if the item is swapped, or the player gets 45 tiles away). It spins slowly at first and speeds up (ease-in) to a cap over 3 s, and the faster it spins the shorter its per-enemy hit cooldown (14 to 5 ticks), so it shreds. Knockback is cut to 30% while holding so enemies stay in the blades. Release at any time (even before it reaches 20 tiles) and it accelerates back and is caught.

Ethereal forms: every 5th hit of the main boomerang (ethereal hits do not count) it sheds a `TrimarangEtherealProjectile`: a see-through copy that spins at the same speed, is shot outward (consecutive ones fan out by the golden angle), and after 10 ticks homes on the nearest enemy that is NOT the one that triggered it (it never targets and never damages that enemy, even if it is the only one around; then it just drifts and fizzles after 6 s). It passes through tiles, pierces, deals 0.7x damage, and after 8 hits it explodes as a visual only (no damage). At most 8 are alive at once (the oldest fizzles).

Multiplayer design: the owner's client decides everything (stop, wall, release, hit counting, ethereal spawns, target choice, the 8-hit explosion). The main boomerang syncs its phase with ai[0] and netUpdate; its spin is a pure function of its age, so every machine spins it the same. An ethereal form syncs its target and its "dying" state through ai[1] (-2 = dying), so every machine plays the explosion without a kill packet. Other players' hit sparks come from `Common/RemoteHitTracker`. No custom packets. The return and catch step moved to `Common/BoomerangFlight.cs` (the Enchanted Boomerang uses it too).

Needs the Enchanted Boomerang patch applied first.

Art: optional. Drop `TrimarangProjectile.png` and `TrimarangEtherealProjectile.png` into `Content/Weapons/Trimarang/` (same size as the vanilla Trimarang sprite, single frame).

Test checklist: hold left click: it glides to a stop 20 tiles out and spins, getting faster, with a rising whoosh; releasing at any time (also before it arrives) sends it back and it is caught; it stops at walls; it shreds enemies without knocking them away; every 5th hit sheds a ghost copy that flies out and hunts another enemy, never the one that triggered it; ghosts pass through walls and explode harmlessly after 8 hits or when they have nothing to hunt; no more than 8 ghosts at once; the Enchanted Boomerang still returns and is caught as before; two players: the other player sees the spin, the ghosts, their explosions and the hit sparks.

## 5. Flamerang (vanilla `ItemID.Flamarang`) [patch 1 applied, untested]

Design: it behaves like the Ice Boomerang. Left click throws it at the cursor (fixed 20-tile range, brakes at the end, only one thrown at a time) and it pierces everything, but it never returns as itself.
- Nothing hit: at the end of its range it spins down, then explodes in fire (`FlamerangBlastProjectile`: 96 px circle, boomerang damage, sets enemies on fire) and two `MoltenBoomerangProjectile`s fly off sideways from the explosion, bend around in an arc and then home back to the player.
- Touching an enemy or a tile first: the same fire explosion happens at the contact point and also throws 6 `ObsidianShardProjectile`s (fan out upward, fall, land on the first tile and linger 8 s, cutting enemies that touch them; they glow like lava and cool to dark obsidian). The boomerang is replaced by one molten boomerang that returns straight to the player.
- Molten boomerangs pierce infinitely (an enemy can be hit again after 12 ticks and on the way back) and leave a trail of fire for 40 ticks; any enemy touching the trail is set on fire (vanilla On Fire, 4 s, refreshed below 1.5 s left).

Multiplayer design: the owner's client decides everything (stop, contact, explosion and every spawn) and flips the main boomerang's phase with ai[0] and netUpdate. The blast, the shards and the molten boomerangs are normal spawned projectiles, so tModLoader syncs them; every machine plays the blast visuals on its first tick. The molten arc is deterministic from its spawn velocity and turn direction (ai[1]); the owner flips arc to return with netUpdate and applies the burning from the trail (`NPC.AddBuff` syncs itself). Every machine draws its own copy of the trail from where its copy has been. A shard's landing is decided by the owner (ai[0] = 1, netUpdate); its lifetime is a plain countdown each machine runs. Other players' hit sparks come from `Common/RemoteHitTracker`. No custom packets.

Needs the Enchanted Boomerang and Trimarang patches applied first (it uses `Common/RemoteHitTracker` and `Common/BoomerangFlight`).

Art: optional. Drop `FlamerangProjectile.png`, `MoltenBoomerangProjectile.png` and `ObsidianShardProjectile.png` into `Content/Weapons/Flamerang/` (the boomerang ones the size of the vanilla Flamarang sprite, single frame) and they replace the vanilla textures.

Test checklist: left click throws at the cursor and only one can be out; thrown at open space it brakes, spins down and explodes at 20 tiles and two molten boomerangs arc out sideways and come back and are caught; thrown at an enemy or a wall it explodes at once, 6 obsidian shards fly up, fall and linger and hurt enemies that touch them, and one molten boomerang flies straight back; the explosion burns enemies; molten boomerangs leave a fiery trail that sets enemies on fire; you can throw again while the molten ones are still flying; shards are gone after about 8 s; Ice Boomerang, Enchanted Boomerang and Trimarang still work as before; two players: the other player sees the flight, the explosions, the shards, the molten arcs and trails and the hit sparks.