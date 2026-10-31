# CODEBASE INDEX - IsmailsWeaponReworks
generated 2026-11-01 | 7 .cs files | regenerate: `python index_codebase.py` (project root)

Legend: `+` public `#` protected `~` internal | s=static ab=abstract v=virtual ov=override c=const | `Name(Types)` method, `Name:Type` field/prop | `uses:` other project types used in file | `ns!=X` namespace differs from folder path (matters for `using`) | no line numbers: grep the name | private members and SetDefaults/SetStaticDefaults omitted

## Most-referenced types (files referencing them)
IceBoomerangTuning(4) IceBoomerangFx(3) IceCrystalProjectile(2) IceBoomerangChill(2) IceCrystalBlastProjectile(1) IceBoomerangProjectile(1)

## Inheritance
Project bases -> derived:
tModLoader/other bases -> project types:
- ModProjectile (3): IceBoomerangProjectile, IceCrystalBlastProjectile, IceCrystalProjectile
- GlobalItem (1): IceBoomerangGlobalItem
- Mod (1): IsmailsWeaponReworks

## Files

### (root)/
**IsmailsWeaponReworks.cs**
  class IsmailsWeaponReworks : Mod

### Content/Weapons/IceBoomerang/
**IceBoomerangFx.cs**
  static class IceBoomerangFx
    +Burst(Vector2,int,float,float)[s] +Ring(Vector2,int,float,float)[s] +Gather(Vector2,float)[s] +PlayFormed(Vector2)[s] +PlayBlast(Vector2,int)[s] +PlayCatch(Vector2)[s] +PlayShatter(Vector2)[s]
**IceBoomerangGlobalItem.cs** uses: IceBoomerangProjectile, IceBoomerangTuning
  class IceBoomerangGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**IceBoomerangProjectile.cs** uses: IceBoomerangFx, IceBoomerangTuning, IceCrystalBlastProjectile, IceCrystalProjectile
  class IceBoomerangProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Settling, Chain, Returning, Spent}
**IceBoomerangTuning.cs**
  static class IceBoomerangTuning
    +ThrowSpeed:float[s] +MinThrowRange:float[s] +MaxThrowRange:float[s] +SettleTicks:int[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +ChainSpeedGainPerLevel:float[s] +ChainMaxSpeed:float[s] +ChainMaxTargetDistance:float[s] +DamageGainPerLevel:float[s] +BlastRadius:float[s] +BlastDamageMultiplier:float[s] +BlastLifeTicks:int[s] +BlastFlashTicks:int[s] +FlightMaxTicks:int[s] +MaxCrystals:int[s] +CrystalLifeTicks:int[s] +CrystalPopTicks:int[s] +ExpiryWarnTicks:int[s] +CrystalSheetFrames:int[s] +CrystalBounceLifeLossTicks:int[s] +HaloIntervalTicks:int[s] +HaloLifeTicks:int[s] +HaloMaxScale:float[s] +HaloAlpha:float[s] +ChillTicks:int[s] +ChillRefreshBelowTicks:int[s] +ChillCheckInterval:int[s]
  static class IceBoomerangChill
    +Apply(NPC)[s]
**IceCrystalBlastProjectile.cs** uses: IceBoomerangChill, IceBoomerangFx, IceBoomerangTuning, IceCrystalProjectile
  class IceCrystalBlastProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
**IceCrystalProjectile.cs** uses: IceBoomerangChill, IceBoomerangFx, IceBoomerangTuning
  class IceCrystalProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +RandomFrame()[s] +RegisterBounce(Projectile)[s] +EvictForNewCrystal(int)[s] +AI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]

## Non-code files (png omitted; sprites follow <Name>_On/_Off/_On_FirstLiquid naming)
- (root)/: .gitattributes, .gitignore, LICENSE; IsmailsWeaponReworks.csproj; build.txt, description.txt, description_workshop.txt
- Localization/: en-US_Mods.IsmailsWeaponReworks.hjson
- Properties/: launchSettings.json
- docs/: ROADMAP.md
- tools/: patch_engine.ps1
