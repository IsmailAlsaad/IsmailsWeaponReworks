# Ismail's Weapon Reworks: Roadmap

Standalone tModLoader mod (internal name `IsmailsWeaponReworks`) with reworks of vanilla Terraria weapons. It depends on no other mod. Factorraria lists it in `modReferences`. Each weapon gets its own section; one chat = one patch (or a few small patches of the same weapon). Vanilla items are reworked through a `GlobalItem` (recipe, drops and base stats stay vanilla). Vanilla dust and sounds only; Ismail provides all art (every sprite has a vanilla fallback until his file exists). Multiplayer first: the owner's client decides, everyone else mirrors.

Design preferences: lots of particles and visuals with trails and sound effects and animations and all the satisfying game juice.

Layout: one folder per weapon, `Content/Weapons/<Weapon>/` (GlobalItem, projectiles, Tuning, Fx, art). Namespace always equals the folder path. Shared code goes into `Common/` only when a second weapon needs it.

## Progress

- [~] Migration: Ice Boomerang imported from Factorraria commit `183e77e` (applied, untested)
- [ ] Migration: removal of the old copy from Factorraria
- [ ] R1: synced server config with one toggle per rework (checked in each GlobalItem's `AppliesToEntity`)

## 1. Ice Boomerang (vanilla `ItemID.IceBoomerang`) [DONE]

Throw it and it becomes a crystal; hit a crystal and it chains through all of them, exploding each. Only the one-line stub of the original design text survived in Factorraria's repo; restore the design details here if you want them kept.