# CODEBASE INDEX - IsmailsWeaponReworks
generated 2026-11-01 | 21 .cs files | regenerate: `python index_codebase.py` (project root)

Legend: `+` public `#` protected `~` internal | s=static ab=abstract v=virtual ov=override c=const | `Name(Types)` method, `Name:Type` field/prop | `uses:` other project types used in file | `ns!=X` namespace differs from folder path (matters for `using`) | no line numbers: grep the name | private members and SetDefaults/SetStaticDefaults omitted

## Most-referenced types (files referencing them)
IceFx(5) RemoteHitTracker(4) IceBoomerangTuning(4) IceBoomerangFx(3) EnchantedBoomerangTuning(3) IceCrystalProjectile(2) IceBoomerangChill(2) IceBladeTuning(2) IceBladeFx(2) EtherealBoomerangProjectile(2) EnchantedBoomerangFx(2) EnchantedBoomerangBase(2) StardustBurstProjectile(1) IceCrystalBlastProjectile(1) IceBoomerangProjectile(1) IceBladeWaveProjectile(1) IceBladeSwingProjectile(1) EnchantedBoomerangProjectile(1)

## Inheritance
Project bases -> derived:
- EnchantedBoomerangBase: EnchantedBoomerangProjectile, EtherealBoomerangProjectile
tModLoader/other bases -> project types:
- ModProjectile (7): EnchantedBoomerangBase, IceBladeSwingProjectile, IceBladeWaveProjectile, IceBoomerangProjectile, IceCrystalBlastProjectile, IceCrystalProjectile, StardustBurstProjectile
- GlobalItem (3): EnchantedBoomerangGlobalItem, IceBladeGlobalItem, IceBoomerangGlobalItem
- Mod (1): IsmailsWeaponReworks

## Files

### (root)/
**IsmailsWeaponReworks.cs**
  class IsmailsWeaponReworks : Mod

### Common/
**IceFx.cs**
  static class IceFx
    +Burst(Vector2,int,float,float)[s] +Ring(Vector2,int,float,float)[s] +Gather(Vector2,float)[s] +DrawPixel(Vector2,float,Color,float)[s]
**RemoteHitTracker.cs**
  class RemoteHitTracker
    +Update(Projectile,int,Action<NPC>) +TouchOnce(Projectile,Action<NPC>)[s]

### Content/Weapons/EnchantedBoomerang/
**EnchantedBoomerangBase.cs** uses: EnchantedBoomerangFx, EnchantedBoomerangTuning, EtherealBoomerangProjectile, RemoteHitTracker, StardustBurstProjectile
  abstract class EnchantedBoomerangBase : ModProjectile
    +Texture:string[ov] #Ethereal:bool[ab] +Load()[ov] +Unload()[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +Colliding(Rectangle,Rectangle)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Returning, Bounce}
**EnchantedBoomerangFx.cs**
  static class EnchantedBoomerangFx
    +FlightDust(Vector2,Vector2,bool)[s] +HitSparks(Vector2,bool)[s] +Catch(Vector2,bool)[s] +BounceSparks(Vector2)[s] +Burst(Vector2,float)[s]
**EnchantedBoomerangGlobalItem.cs** uses: EnchantedBoomerangProjectile, EnchantedBoomerangTuning, EtherealBoomerangProjectile
  class EnchantedBoomerangGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +AltFunctionUse(Item,Player)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**EnchantedBoomerangProjectile.cs** uses: EnchantedBoomerangBase
  class EnchantedBoomerangProjectile : EnchantedBoomerangBase
    #Ethereal:bool[ov]
**EnchantedBoomerangTuning.cs**
  static class EnchantedBoomerangTuning
    +ThrowSpeed:float[s] +ThrowRange:float[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +ReturnStartSpeed:float[s] +ReturnAcceleration:float[s] +ReturnMaxSpeed:float[s] +CatchDistance:float[s] +FlightMaxTicks:int[s] +HitCooldownTicks:int[s] +SpinSpeed:float[s] +CollideRadius:float[s] +BounceSpeed:float[s] +BounceDecay:float[s] +BounceTicks:int[s] +BurstRadius:float[s] +BurstDamageMultiplier:float[s] +BurstLifeTicks:int[s] +NormalColor:Color[s] +EtherealColor:Color[s] +EtherealOpacity:float[s] +PixelSize:float[s]
**EtherealBoomerangProjectile.cs** uses: EnchantedBoomerangBase
  class EtherealBoomerangProjectile : EnchantedBoomerangBase
    #Ethereal:bool[ov]
**StardustBurstProjectile.cs** uses: EnchantedBoomerangFx, EnchantedBoomerangTuning, IceFx, RemoteHitTracker
  class StardustBurstProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]

### Content/Weapons/IceBlade/
**IceBladeFx.cs** uses: IceFx
  static class IceBladeFx
    +PlayClick(Vector2,int)[s] +PlayFreezeClick(Vector2)[s] +PlayFreeze(Vector2)[s] +PlayTapSwing(Vector2)[s] +PlaySlash(Vector2,int)[s] +PlayWaveShatter(Vector2)[s] +ClickBurst(Vector2,int)[s] +FreezeClickBurst(Vector2)[s] +ChargeGather(Vector2,int)[s] +BladeSparkle(Vector2,Vector2,int)[s] +FrostOnHand(Vector2,float)[s] +FreezeShatter(Vector2)[s] +SlashDust(Vector2,Vector2,int)[s] +WaveBorn(Vector2,Vector2,int)[s] +WaveFog(Vector2,Vector2,float)[s] +WaveSpark(Vector2,Vector2,float)[s]
**IceBladeGlobalItem.cs** uses: IceBladeSwingProjectile
  class IceBladeGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +OnHitNPC(Item,Player,NPC,HitInfo,int)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**IceBladeSwingProjectile.cs** uses: IceBladeFx, IceBladeTuning, IceBladeWaveProjectile, IceFx
  class IceBladeSwingProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Charge, Slash, Tap, Frozen}
    struct TrailSample
      +Inner:Vector2 +Tip:Vector2 +Born:int +Tier:int
**IceBladeTuning.cs**
  static class IceBladeTuning
    +Click1Ticks:int[s] +Click2Ticks:int[s] +Click3Ticks:int[s] +FreezeClickTicks:int[s] +ReleaseGraceTicks:int[s] +ClickTicks(int)[s] +FreezeDebuffTicks:int[s] +FrozenAnimTicks:int[s] +ArmStartDeg:float[s] +ArmEndDeg:float[s] +LiftTicks:int[s] +BladeTiltDeg:float[s] +BladeLength:float[s] +SwordScale:float[s] +ShakeStartTicks:int[s] +ShakeDeg:float[s] +ClickFlashTicks:int[s] +TapWindupTicks:int[s] +TapSwingTicks:int[s] +TapRecoverTicks:int[s] +TapStartDeg:float[s] +TapEndDeg:float[s] +SlashWaveTick:int[s] +TrailLifeTicks:int[s] +TrailMaxSamples:int[s] +TrailInnerAtSwingStart:float[s] +TrailInnerAtSwingEnd:float[s] +WaveSpawnFraction:float[s] +TrailOpacity(int)[s] +TrailThickness(int)[s] +WaveDamageMultiplier(int)[s] +WaveKnockbackMultiplier(int)[s] +WaveSpeed(int)[s] +WaveLifeTicks(int)[s] +WaveRadius(int)[s] +WaveThickness(int)[s] +WaveArcHalfDeg:float[s] +WaveDrag:float[s] +WaveGrowth:float[s] +PixelSize:float[s] +AfterimageCount:int[s] +HaloColor:Color[s] +MidColor:Color[s] +CoreColor:Color[s]
**IceBladeWaveProjectile.cs** uses: IceBladeFx, IceBladeTuning, IceFx
  class IceBladeWaveProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]

### Content/Weapons/IceBoomerang/
**IceBoomerangFx.cs** uses: IceFx
  static class IceBoomerangFx
    +Burst(Vector2,int,float,float)[s] +Ring(Vector2,int,float,float)[s] +Gather(Vector2,float)[s] +PlayFormed(Vector2)[s] +PlayBlast(Vector2,int)[s] +PlayCatch(Vector2)[s] +PlayShatter(Vector2)[s]
**IceBoomerangGlobalItem.cs** uses: IceBoomerangProjectile, IceBoomerangTuning
  class IceBoomerangGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**IceBoomerangProjectile.cs** uses: IceBoomerangFx, IceBoomerangTuning, IceCrystalBlastProjectile, IceCrystalProjectile, RemoteHitTracker
  class IceBoomerangProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Settling, Chain, Returning, Spent}
**IceBoomerangTuning.cs**
  static class IceBoomerangTuning
    +ThrowSpeed:float[s] +MinThrowRange:float[s] +MaxThrowRange:float[s] +SettleTicks:int[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +ChainSpeedGainPerLevel:float[s] +ChainMaxSpeed:float[s] +ChainMaxTargetDistance:float[s] +DamageGainPerLevel:float[s] +BlastRadius:float[s] +BlastDamageMultiplier:float[s] +BlastLifeTicks:int[s] +BlastFlashTicks:int[s] +FlightMaxTicks:int[s] +MaxCrystals:int[s] +CrystalLifeTicks:int[s] +CrystalPopTicks:int[s] +ExpiryWarnTicks:int[s] +CrystalSheetFrames:int[s] +CrystalBounceLifeLossTicks:int[s] +HaloIntervalTicks:int[s] +HaloLifeTicks:int[s] +HaloMaxScale:float[s] +HaloAlpha:float[s] +ChillTicks:int[s] +ChillRefreshBelowTicks:int[s] +ChillCheckInterval:int[s]
  static class IceBoomerangChill
    +Apply(NPC)[s]
**IceCrystalBlastProjectile.cs** uses: IceBoomerangChill, IceBoomerangFx, IceBoomerangTuning, IceCrystalProjectile, RemoteHitTracker
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
