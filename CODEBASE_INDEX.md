# CODEBASE INDEX - IsmailsWeaponReworks
generated 2026-11-03 | 72 .cs files | regenerate: `python index_codebase.py` (project root)

Legend: `+` public `#` protected `~` internal | s=static ab=abstract v=virtual ov=override c=const | `Name(Types)` method, `Name:Type` field/prop | `uses:` other project types used in file | `ns!=X` namespace differs from folder path (matters for `using`) | no line numbers: grep the name | private members and SetDefaults/SetStaticDefaults omitted

## Most-referenced types (files referencing them)
RemoteHitTracker(16) DetachedTrails(14) AfterimageTrail(13) IceFx(9) BoomerangFlight(8) FlamerangTuning(7) StarfuryTuning(6) ShroomerangTuning(5) FlamerangFx(5) WoodenBoomerangTuning(4) ThornChakramTuning(4) SunfuryTuning(4) PixelTrail(4) PixelLine(4) IceBoomerangTuning(4) TrimarangTuning(3) SweepTrail(3) StarfuryFx(3) ShroomerangFx(3) IceBoomerangFx(3)

## Inheritance
Project bases -> derived:
- EnchantedBoomerangBase: EnchantedBoomerangProjectile, EtherealBoomerangProjectile
- PixelTrailStyle: MoltenTrailStyle, SunfuryTrailStyle
- SweepTrailStyle: IceBladeTrailStyle, StarfurySweepTrailStyle
tModLoader/other bases -> project types:
- ModProjectile (25): EnchantedBoomerangBase, FlamerangBlastProjectile, FlamerangProjectile, IceBladeSwingProjectile, IceBladeWaveProjectile, IceBoomerangProjectile, IceCrystalBlastProjectile, IceCrystalProjectile, MoltenBoomerangProjectile, ObsidianShardProjectile, ShroomerangBurstProjectile, ShroomerangMushroomProjectile, ShroomerangProjectile, StardustBurstProjectile, StarfuryBlastProjectile, StarfuryStarProjectile, StarfurySwingProjectile, SunfuryBlastProjectile, SunfuryFlailProjectile, ThornChakramProjectile, ThornChakramStingerProjectile, TrimarangEtherealProjectile, TrimarangProjectile, WoodenBoomerangProjectile, WoodenBoomerangShardProjectile
- GlobalItem (10): EnchantedBoomerangGlobalItem, FlamerangGlobalItem, IceBladeGlobalItem, IceBoomerangGlobalItem, ShroomerangGlobalItem, StarfuryGlobalItem, SunfuryGlobalItem, ThornChakramGlobalItem, TrimarangGlobalItem, WoodenBoomerangGlobalItem
- Mod (1): IsmailsWeaponReworks
- ModPlayer (1): StarfuryPlayer
- ModSystem (1): DetachedTrails

## Files

### (root)/
**IsmailsWeaponReworks.cs**
  class IsmailsWeaponReworks : Mod

### Common/
**AfterimageTrail.cs**
  struct Afterimage
    +Position:Vector2 +Rotation:float +Scale:float +Width:float +Color:Color +Birth:int
  class AfterimageTrail
    +Sprite(Texture2D,int,float)[s] +IsEmpty:bool +Add(Vector2,float,float,Color,float) +Tick() +Draw()
**BoomerangFlight.cs**
  static class BoomerangFlight
    +StepReturn(Projectile,Player,float,float,float,float)[s] +StepForceReturn(Projectile,Player,float,float,float,float)[s] +StepOutbound(Projectile,float,float,float,float)[s]
**DetachedTrails.cs** uses: AfterimageTrail, PixelTrail, SweepTrail
  class DetachedTrails : ModSystem
    +Hand(PixelTrail)[s] +Hand(SweepTrail)[s] +Hand(AfterimageTrail)[s] +PostUpdateEverything()[ov] +PostDrawTiles()[ov] +OnWorldUnload()[ov] +Unload()[ov]
**HeldSwordPose.cs**
  static class HeldSwordPose
    +ToWorld(int,float)[s] +Hand(Player,int,float)[s] +BladeDirection(Player,int,float,float)[s] +HandWorld(Player,float)[s] +BladeDirectionWorld(Player,float)[s] +Sprite(Texture2D,int,out Vector2,out float,out SpriteEffects)[s]
**IceFx.cs**
  static class IceFx
    +Burst(Vector2,int,float,float)[s] +Ring(Vector2,int,float,float)[s] +Gather(Vector2,float)[s] +DrawPixel(Vector2,float,Color,float)[s]
**PixelLine.cs**
  static class PixelLine
    +Draw(Vector2,float,int,float,Func<float, float>,Func<float, Color>)[s]
    struct Span
      +Y:int +X0:int +X1:int +Order:int +Color:Color
**PixelTrail.cs** uses: PixelLine
  abstract class PixelTrailStyle
    +Grid:float[v] +LifeTicks:int[v] +Layers:int[v] +Jitter:float[v] +GetWidth(int,float)[ab] +GetColor(int,float)[ab] +Update(PixelTrail)[v]
  class PixelTrail
    +Style:PixelTrailStyle +IsOwner:bool +Count:int +IsEmpty:bool +Clock:int +PointAt(int) +AgeAt(int) +Add(Vector2) +Tick() +Draw()
**RemoteHitTracker.cs**
  class RemoteHitTracker
    +Update(Projectile,int,Action<NPC>) +TouchOnce(Projectile,Action<NPC>)[s]
**SweepTrail.cs** uses: PixelLine
  abstract class SweepTrailStyle
    +Grid:float[v] +LifeTicks:int[v] +Rails:int[v] +GetColor(float,float)[ab]
  class SweepTrail
    +Style:SweepTrailStyle +IsOwner:bool +Count:int +IsEmpty:bool +Add(Vector2,Vector2,float) +Tick() +Draw()

### Content/Weapons/EnchantedBoomerang/
**EnchantedBoomerangBase.cs** uses: AfterimageTrail, BoomerangFlight, DetachedTrails, EnchantedBoomerangFx, EnchantedBoomerangTuning, EtherealBoomerangProjectile, RemoteHitTracker, StardustBurstProjectile
  abstract class EnchantedBoomerangBase : ModProjectile
    +Texture:string[ov] #Ethereal:bool[ab] +Load()[ov] +Unload()[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +Colliding(Rectangle,Rectangle)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Returning, Bounced}
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
    +ThrowSpeed:float[s] +ThrowRange:float[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +ReturnAcceleration:float[s] +ReturnDrag:float[s] +ReturnMaxSpeed:float[s] +CatchDistance:float[s] +FlightMaxTicks:int[s] +HitCooldownTicks:int[s] +SpinSpeed:float[s] +CollideRadius:float[s] +BounceSpeed:float[s] +BurstRadius:float[s] +BurstDamageMultiplier:float[s] +BurstLifeTicks:int[s] +NormalColor:Color[s] +EtherealColor:Color[s] +EtherealOpacity:float[s] +PixelSize:float[s]
**EtherealBoomerangProjectile.cs** uses: EnchantedBoomerangBase
  class EtherealBoomerangProjectile : EnchantedBoomerangBase
    #Ethereal:bool[ov]
**StardustBurstProjectile.cs** uses: EnchantedBoomerangFx, EnchantedBoomerangTuning, IceFx, RemoteHitTracker
  class StardustBurstProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]

### Content/Weapons/Flamerang/
**FlamerangBlastProjectile.cs** uses: FlamerangBurn, FlamerangFx, FlamerangTuning, IceFx, RemoteHitTracker
  class FlamerangBlastProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
**FlamerangFx.cs** uses: FlamerangTuning
  static class FlamerangFx
    +FlightDust(Vector2,Vector2,float)[s] +Gather(Vector2,float)[s] +HitSparks(Vector2)[s] +Catch(Vector2)[s] +Blast(Vector2,float)[s] +MoltenBorn(Vector2,Vector2)[s] +TrailEmber(Vector2)[s] +ShardLand(Vector2)[s] +ShardShatter(Vector2)[s] +ShardGlow(Vector2,float)[s]
**FlamerangGlobalItem.cs** uses: FlamerangProjectile, FlamerangTuning
  class FlamerangGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**FlamerangProjectile.cs** uses: AfterimageTrail, BoomerangFlight, DetachedTrails, FlamerangBlastProjectile, FlamerangFx, FlamerangTuning, MoltenBoomerangProjectile, ObsidianShardProjectile, RemoteHitTracker
  class FlamerangProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Settling, Spent}
**FlamerangTuning.cs**
  static class FlamerangTuning
    +ThrowSpeed:float[s] +MinThrowRange:float[s] +MaxThrowRange:float[s] +SettleTicks:int[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +FlightMaxTicks:int[s] +BlastRadius:float[s] +BlastDamageMultiplier:float[s] +BlastLifeTicks:int[s] +MoltenDamageMultiplier:float[s] +MoltenArcSpeed:float[s] +ReturnStartSpeed:float[s] +ReturnAcceleration:float[s] +ReturnDrag:float[s] +ReturnMaxSpeed:float[s] +CatchDistance:float[s] +MoltenHitCooldownTicks:int[s] +SpinSpeed:float[s] +TrailLifeTicks:int[s] +TrailRadius:float[s] +BurnCheckInterval:int[s] +BurnTicks:int[s] +BurnRefreshBelowTicks:int[s] +TrailWidth:float[s] +TrailJitter:float[s] +TrailBurnFade:float[s] +ShardCount:int[s] +ShardDamageMultiplier:float[s] +ShardMinSpeed:float[s] +ShardMaxSpeed:float[s] +ShardSpreadDeg:float[s] +ShardGravity:float[s] +ShardMaxFallSpeed:float[s] +ShardLingerTicks:int[s] +ShardMaxHits:int[s] +ShardMaxAlive:int[s] +ShardCoolTicks:int[s] +ShardHitCooldownTicks:int[s] +PixelSize:float[s] +HotColor:Color[s] +FireColor:Color[s] +EmberColor:Color[s] +ObsidianColor:Color[s]
  static class FlamerangBurn
    +Apply(NPC)[s]
**MoltenBoomerangProjectile.cs** uses: AfterimageTrail, BoomerangFlight, DetachedTrails, FlamerangBurn, FlamerangFx, FlamerangTuning, MoltenTrailStyle, PixelTrail, RemoteHitTracker
  class MoltenBoomerangProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnKill(int)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PostAI()[ov] +PreDraw(ref Color)[ov]
**MoltenTrail.cs** uses: FlamerangBurn, FlamerangFx, FlamerangTuning, PixelTrail, PixelTrailStyle
  class MoltenTrailStyle : PixelTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Layers:int[ov] +Jitter:float[ov] +GetWidth(int,float)[ov] +GetColor(int,float)[ov] +Update(PixelTrail)[ov] +Create(bool)[s]
**ObsidianShardProjectile.cs** uses: FlamerangFx, FlamerangTuning, RemoteHitTracker
  class ObsidianShardProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +Break() +MakeRoom(int,int)[s] +CanHitNPC(NPC)[ov] +OnKill(int)[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]

### Content/Weapons/IceBlade/
**IceBladeFx.cs** uses: IceFx
  static class IceBladeFx
    +PlayClick(Vector2,int)[s] +PlayFreezeClick(Vector2)[s] +PlayFreeze(Vector2)[s] +PlayTapSwing(Vector2)[s] +PlaySlash(Vector2,int)[s] +PlayWaveShatter(Vector2)[s] +ClickBurst(Vector2,int)[s] +FreezeClickBurst(Vector2)[s] +ChargeGather(Vector2,int)[s] +BladeSparkle(Vector2,Vector2,int)[s] +FrostOnHand(Vector2,float)[s] +FreezeShatter(Vector2)[s] +SlashDust(Vector2,Vector2,int)[s] +WaveBorn(Vector2,Vector2,int)[s] +WaveFog(Vector2,Vector2,float)[s] +WaveSpark(Vector2,Vector2,float)[s]
**IceBladeGlobalItem.cs** uses: IceBladeSwingProjectile
  class IceBladeGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +OnHitNPC(Item,Player,NPC,HitInfo,int)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**IceBladeSwingProjectile.cs** uses: DetachedTrails, HeldSwordPose, IceBladeFx, IceBladeTrailStyle, IceBladeTuning, IceBladeWaveProjectile, IceFx, SweepTrail
  class IceBladeSwingProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Charge, Slash, Tap, Frozen}
**IceBladeTrail.cs** uses: IceBladeTuning, SweepTrailStyle
  class IceBladeTrailStyle : SweepTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Rails:int[ov] +GetColor(float,float)[ov]
**IceBladeTuning.cs**
  static class IceBladeTuning
    +Click1Ticks:int[s] +Click2Ticks:int[s] +Click3Ticks:int[s] +FreezeClickTicks:int[s] +ReleaseGraceTicks:int[s] +ClickTicks(int)[s] +FreezeDebuffTicks:int[s] +FrozenAnimTicks:int[s] +ArmStartDeg:float[s] +ArmEndDeg:float[s] +LiftTicks:int[s] +BladeTiltDeg:float[s] +BladeLength:float[s] +SwordScale:float[s] +ShakeStartTicks:int[s] +ShakeDeg:float[s] +ClickFlashTicks:int[s] +TapWindupTicks:int[s] +TapSwingTicks:int[s] +TapRecoverTicks:int[s] +TapStartDeg:float[s] +TapEndDeg:float[s] +SlashWaveTick:int[s] +TrailLifeTicks:int[s] +WaveSpawnFraction:float[s] +TrailOpacity(int)[s] +TrailInnerFraction(int)[s] +TrailExpandBy:float[s] +TrailRails:int[s] +WaveDamageMultiplier(int)[s] +WaveKnockbackMultiplier(int)[s] +WaveSpeed(int)[s] +WaveLifeTicks(int)[s] +WaveRadius(int)[s] +WaveThickness(int)[s] +WaveArcHalfDeg:float[s] +WaveDrag:float[s] +WaveGrowth:float[s] +PixelSize:float[s] +AfterimageCount:int[s] +HaloColor:Color[s] +MidColor:Color[s] +CoreColor:Color[s]
**IceBladeWaveProjectile.cs** uses: Afterimage, AfterimageTrail, DetachedTrails, IceBladeFx, IceBladeTuning, IceFx
  class IceBladeWaveProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]

### Content/Weapons/IceBoomerang/
**IceBoomerangFx.cs** uses: IceFx
  static class IceBoomerangFx
    +Burst(Vector2,int,float,float)[s] +Ring(Vector2,int,float,float)[s] +Gather(Vector2,float)[s] +PlayFormed(Vector2)[s] +PlayBlast(Vector2,int)[s] +PlayCatch(Vector2)[s] +PlayShatter(Vector2)[s]
**IceBoomerangGlobalItem.cs** uses: IceBoomerangProjectile, IceBoomerangTuning
  class IceBoomerangGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**IceBoomerangProjectile.cs** uses: AfterimageTrail, DetachedTrails, IceBoomerangFx, IceBoomerangTuning, IceCrystalBlastProjectile, IceCrystalProjectile, RemoteHitTracker
  class IceBoomerangProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Settling, Chain, Returning, Spent}
**IceBoomerangTuning.cs**
  static class IceBoomerangTuning
    +ThrowSpeed:float[s] +MinThrowRange:float[s] +MaxThrowRange:float[s] +SettleTicks:int[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +ChainSpeedGainPerLevel:float[s] +ChainMaxSpeed:float[s] +ChainMaxTargetDistance:float[s] +DamageGainPerLevel:float[s] +BlastRadius:float[s] +BlastDamageMultiplier:float[s] +BlastLifeTicks:int[s] +BlastFlashTicks:int[s] +FlightMaxTicks:int[s] +HitCooldownTicks:int[s] +MaxCrystals:int[s] +CrystalLifeTicks:int[s] +CrystalPopTicks:int[s] +ExpiryWarnTicks:int[s] +CrystalSheetFrames:int[s] +CrystalBounceLifeLossTicks:int[s] +HaloIntervalTicks:int[s] +HaloLifeTicks:int[s] +HaloMaxScale:float[s] +HaloAlpha:float[s] +ChillTicks:int[s] +ChillRefreshBelowTicks:int[s] +ChillCheckInterval:int[s]
  static class IceBoomerangChill
    +Apply(NPC)[s]
**IceCrystalBlastProjectile.cs** uses: IceBoomerangChill, IceBoomerangFx, IceBoomerangTuning, IceCrystalProjectile, RemoteHitTracker
  class IceCrystalBlastProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
**IceCrystalProjectile.cs** uses: IceBoomerangChill, IceBoomerangFx, IceBoomerangTuning
  class IceCrystalProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +RandomFrame()[s] +RegisterBounce(Projectile)[s] +EvictForNewCrystal(int)[s] +AI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]

### Content/Weapons/Shroomerang/
**ShroomerangBurstProjectile.cs** uses: IceFx, RemoteHitTracker, ShroomerangFx, ShroomerangTuning
  class ShroomerangBurstProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
**ShroomerangFx.cs** uses: ShroomerangTuning
  static class ShroomerangFx
    +FlightDust(Vector2,Vector2)[s] +SporePuff(Vector2,int,float,float)[s] +HitSparks(Vector2)[s] +SporeRing(Vector2,int,float,float)[s] +SporeCloud(Vector2,float,int)[s] +Trail(Vector2,Vector2,float)[s] +Catch(Vector2)[s] +PlayPop(Vector2)[s] +CloudPuff(Vector2,float)[s] +PlayPuff(Vector2)[s]
**ShroomerangGlobalItem.cs** uses: ShroomerangProjectile, ShroomerangTuning
  class ShroomerangGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**ShroomerangMushroomProjectile.cs** uses: ShroomerangBurstProjectile, ShroomerangFx, ShroomerangTuning
  class ShroomerangMushroomProjectile : ModProjectile
    +Texture:string[ov] +EvictForNewMushroom(int)[s] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**ShroomerangProjectile.cs** uses: AfterimageTrail, BoomerangFlight, DetachedTrails, RemoteHitTracker, ShroomerangFx, ShroomerangMushroomProjectile, ShroomerangTuning
  class ShroomerangProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +Colliding(Rectangle,Rectangle)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Returning}
**ShroomerangTuning.cs**
  static class ShroomerangTuning
    +ThrowSpeed:float[s] +ThrowRange:float[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +ReturnAcceleration:float[s] +ReturnDrag:float[s] +ReturnMaxSpeed:float[s] +CatchDistance:float[s] +FlightMaxTicks:int[s] +HitCooldownTicks:int[s] +SpinSpeed:float[s] +AfterimageTicks:int[s] +FirstSpawnDelayTicks:int[s] +SpawnIntervalTicks:int[s] +MaxMushrooms:int[s] +MushroomLifeTicks:int[s] +PopTicks:int[s] +WarnTicks:int[s] +InheritVelocity:float[s] +ScatterSpeed:float[s] +Drag:float[s] +MushroomSpinSpeed:float[s] +ArmTicks:int[s] +TriggerPadding:float[s] +TriggerCheckInterval:int[s] +BurstRadius:float[s] +BurstDamageMultiplier:float[s] +BurstLifeTicks:int[s] +GlowColor:Color[s] +CloudColor:Color[s]

### Content/Weapons/Starfury/
**StarfuryBlastProjectile.cs** uses: StarfuryFx, StarfuryTuning
  class StarfuryBlastProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +PreDraw(ref Color)[ov]
**StarfuryFx.cs**
  static class StarfuryFx
    +PlaySwing(Vector2,bool)[s] +PlayCall(Vector2)[s] +PlayStarImpact(Vector2)[s] +PlayBat(Vector2,int)[s] +PlayBounce(Vector2)[s] +CallGather(Vector2)[s] +CallBurst(Vector2)[s] +SlashDust(Vector2,Vector2)[s] +BladeSparkle(Vector2,Vector2)[s] +HitSpark(Vector2)[s] +StarTrail(Vector2,Vector2,int)[s] +StarImpact(Vector2,int)[s] +BatBurst(Vector2,int)[s] +BounceBurst(Vector2,int)[s] +ChargeHit(Vector2,int)[s] +ChargeUp(Vector2,int)[s] +PlayChargeUp(Vector2,int)[s] +ChargeRelease(Vector2,int)[s] +ChargeSparkle(Vector2,Vector2,int)[s] +BlastBurst(Vector2)[s] +PlayBlast(Vector2)[s] +Shake(Vector2,float)[s]
**StarfuryGlobalItem.cs** uses: HeldSwordPose, Phase, StarfuryPlayer, StarfuryStarProjectile, StarfurySwingProjectile, StarfuryTuning
  class StarfuryGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +AltFunctionUse(Item,Player)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**StarfuryPlayer.cs** uses: StarfuryTuning
  class StarfuryPlayer : ModPlayer
    +LastPhase:int +LastEndArm:float +LastEndTick:uint +LastCallTick:uint +LastEndWorldArm:float +ChargeHits:int +AddHit() +PostUpdate()[ov]
**StarfuryStarProjectile.cs** uses: AfterimageTrail, DetachedTrails, StarfuryBlastProjectile, StarfuryFx, StarfuryTuning
  class StarfuryStarProjectile : ModProjectile
    +Texture:string[ov] +Generation:int +CurrentMode:Mode +CanBeBatted:bool +WeaponDamage:int +WeaponKnockback:float +Spawn(IEntitySource,Vector2,Vector2,int,Mode,float,int,float,int)[s] +SolveToss(Vector2,Player,int)[s] +CountOwned(int)[s] +Launch(Vector2,int,int,float) +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Mode
      {Straight, Launched, Parabolic, Arc, Toss}
**StarfurySwingProjectile.cs** uses: DetachedTrails, HeldSwordPose, Mode, StarfuryFx, StarfuryPlayer, StarfuryStarProjectile, StarfurySweepTrailStyle, StarfuryTuning, SweepTrail
  class StarfurySwingProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {SwingDown, SwingUp, Call}
**StarfuryTrail.cs** uses: StarfuryTuning, SweepTrailStyle
  class StarfurySweepTrailStyle : SweepTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Rails:int[ov] +GetColor(float,float)[ov]
**StarfuryTuning.cs**
  static class StarfuryTuning
    +BladeLength:float[s] +SwordScale:float[s] +AnticipateTicks:int[s] +AnticipateDeg:float[s] +AnticipateTiltDeg:float[s] +SwingTicks:int[s] +SwingEasePower:float[s] +OvershootDeg:float[s] +RecoverTicks:int[s] +SweepHitSteps:int[s] +RestArmDeg:float[s] +SwingHighDeg:float[s] +SwingLowDeg:float[s] +TiltHighDeg:float[s] +TiltLowDeg:float[s] +ComboTicks:int[s] +CallLiftTicks:int[s] +CallSpawnTick:int[s] +CallTicks:int[s] +CallArmDeg:float[s] +CallTiltDeg:float[s] +CallKickDeg:float[s] +CallCooldownTicks:int[s] +CallFrontDistance:float[s] +CallTargetHeight:float[s] +CallRestArmDeg:float[s] +FlashTicks:int[s] +CallHopHeight:float[s] +CallBlockGraceTicks:int[s] +CalledStarDamageMultiplier:float[s] +CalledStarKnockbackMultiplier:float[s] +StarLifeTicks:int[s] +StarSize:int[s] +StarSpin:float[s] +AfterimageCount:int[s] +GenerationGrowth:float[s] +MaxBats:int[s] +MaxStars:int[s] +BatPadding:float[s] +BatGraceTicks:int[s] +BatTargetRange:float[s] +LaunchSpeed(int)[s] +LaunchDamageMultiplier(int)[s] +LaunchKnockbackMultiplier:float[s] +LaunchLifeTicks:int[s] +BounceFrontDistance:float[s] +BounceTargetHeight:float[s] +BounceSpeedTarget:float[s] +BounceMinTicks:int[s] +BounceMaxTicks:int[s] +BounceLead:float[s] +ArcMaxFallSpeed:float[s] +Gravity:float[s] +MaxFallSpeed:float[s] +BounceImmunityTicks:int[s] +BouncedLifeTicks:int[s] +BlastSize:int[s] +BlastTicks:int[s] +BlastDamageMultiplier:float[s] +BlastKnockbackMultiplier:float[s] +BlastShake:float[s] +HitsPerLevel:int[s] +MaxLevel:int[s] +MaxHits:int[s] +LevelOf(int)[s] +PixelSize:float[s] +TrailLifeTicks:int[s] +TrailOpacityLow:float[s] +TrailOpacityMid:float[s] +TrailOpacityHigh:float[s] +TrailOpacityAt(float)[s] +TailLifeScale:float[s] +TrailRails:int[s] +TrailExpandBy:float[s] +TrailInnerLow:float[s] +TrailInnerMid:float[s] +TrailInnerHigh:float[s] +TrailInnerAt(float)[s] +TrailSamplesPerTick:int[s] +GoldColor:Color[s] +PinkColor:Color[s] +CoreColor:Color[s] +GenerationColor(int)[s]

### Content/Weapons/Sunfury/
**SunfuryBlastProjectile.cs** uses: IceFx, PixelLine, RemoteHitTracker, SunfuryFx, SunfuryTuning
  class SunfuryBlastProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
**SunfuryFlailProjectile.cs** uses: AfterimageTrail, DetachedTrails, IceFx, PixelLine, PixelTrail, RemoteHitTracker, SunfuryBlastProjectile, SunfuryFx, SunfuryTrailStyle, SunfuryTuning
  class SunfuryFlailProjectile : ModProjectile
    +Texture:string[ov] +Encode(Phase,int)[s] +Load()[ov] +Unload()[ov] +AI()[ov] +PostAI()[ov] +CanDamage()[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Charge, Throw, Caught, Special, Retract, Fail}
    enum FailReason
      {Undercharged, LockedChain, Mistimed}
**SunfuryFx.cs** uses: SunfuryTuning
  static class SunfuryFx
    +ChargeGather(Vector2,int,float)[s] +SpinEmbers(Vector2,Vector2,int,float)[s] +TierComplete(Vector2,int)[s] +ThrowWhoosh(Vector2,int)[s] +ApexJingle(Vector2,int)[s] +Caught(Vector2,int)[s] +Fizzle(Vector2)[s] +LockedChain(Vector2)[s] +Mistimed(Vector2)[s] +CatchHead(Vector2)[s] +FlightDust(Vector2,Vector2,int)[s] +SpecialLaunch(Vector2)[s] +HitSparks(Vector2,int)[s] +SunBlast(Vector2,float)[s] +Shake(Vector2,float)[s] +DrawDisc(Vector2,float,Color,float)[s]
**SunfuryGlobalItem.cs** uses: Phase, SunfuryFlailProjectile
  class SunfuryGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**SunfuryTrail.cs** uses: PixelTrailStyle, SunfuryTuning
  class SunfuryTrailStyle : PixelTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Layers:int[ov] +Jitter:float[ov] +GetWidth(int,float)[ov] +GetColor(int,float)[ov]
**SunfuryTuning.cs**
  static class SunfuryTuning
    +ChargeTicks(int)[s] +WindowEarlyGraceTicks:int[s] +WindowLateGraceTicks:int[s] +MinReach:float[s] +MaxReach:float[s] +EllipseWidthRatio:float[s] +ThrowSpeed(int)[s] +ReleaseBlendTicks:int[s] +MinHalfTicks:int[s] +TierDamageMultiplier(int)[s] +HitCooldownTicks:int[s] +HeadSize:float[s] +BurnTicks:int[s] +SpinRadiusMin:float[s] +SpinRadiusMax:float[s] +SpinRadiusSmoothing:float[s] +SpinOmegaStart:float[s] +SpinOmegaEnd:float[s] +SpecialSpeed:float[s] +SpecialMinReach:float[s] +SpecialMaxReach:float[s] +SpecialStopsOnTiles:bool[s] +SpecialPathDamageMultiplier:float[s] +SpecialHangTicks:int[s] +RetractSpeed:float[s] +CatchDistance:float[s] +FailRetractSpeed:float[s] +BlastRadius:float[s] +BlastDamageMultiplier:float[s] +BlastKnockbackMultiplier:float[s] +BlastLifeTicks:int[s] +BlastBurnTicks:int[s] +ShakeStrength:float[s] +PixelSize:float[s] +TrailLifeTicks:int[s] +TrailWidth(int)[s] +TrailJitter:float[s] +AfterimageTicks:int[s] +FlashTicks:int[s] +GlowColor(int)[s] +HotColor(int)[s] +ChainColor:Color[s] +ChainLightColor:Color[s]

### Content/Weapons/ThornChakram/
**ThornChakramFx.cs** uses: ThornChakramTuning
  static class ThornChakramFx
    +Puff(Vector2,int,float,float)[s] +HitSparks(Vector2)[s] +Ring(Vector2,int,float,float)[s] +Trail(Vector2,Vector2,float)[s] +Shred(Vector2,int)[s] +Catch(Vector2)[s] +PlayBurst(Vector2)[s] +PlayLatch(Vector2)[s] +PlayShred(Vector2,float)[s] +PlayExplosion(Vector2)[s]
**ThornChakramGlobalItem.cs** uses: ThornChakramProjectile, ThornChakramTuning
  class ThornChakramGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**ThornChakramProjectile.cs** uses: AfterimageTrail, BoomerangFlight, DetachedTrails, RemoteHitTracker, ThornChakramFx, ThornChakramStingerProjectile, ThornChakramTuning
  class ThornChakramProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +CanDamage()[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Returning, Latched, Spent}
**ThornChakramStingerProjectile.cs** uses: ThornChakramFx, ThornChakramTuning
  class ThornChakramStingerProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +CanHitNPC(NPC)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov]
**ThornChakramTuning.cs**
  static class ThornChakramTuning
    +ThrowSpeed:float[s] +ThrowRange:float[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +ReturnAcceleration:float[s] +ReturnDrag:float[s] +ReturnMaxSpeed:float[s] +CatchDistance:float[s] +FlightMaxTicks:int[s] +HitCooldownTicks:int[s] +SpinSpeed:float[s] +BurstFirstDelayTicks:int[s] +BurstIntervalTicks:int[s] +BurstCount:int[s] +BurstRadius:float[s] +BurstSpeed:float[s] +BurstStingerLifeTicks:int[s] +BurstDamageMultiplier:float[s] +LatchTicks:int[s] +LatchOffset:float[s] +ShredIntervalTicks:int[s] +ShredDamageMultiplier:float[s] +LatchSpinMaxMultiplier:float[s] +ExplosionCount:int[s] +ExplosionSpeed:float[s] +ExplosionStingerLifeTicks:int[s] +ExplosionDamageMultiplier:float[s] +ExplosionHitCooldownTicks:int[s] +ExplosionSecondDelayTicks:int[s] +AfterimageTicks:int[s] +GlowColor:Color[s] +StingerSpriteRotationOffset:float[s] +StingerSpriteId:int[s] +LeafDust:int[s] +PoisonDust:int[s] +BloodDust:int[s]

### Content/Weapons/Trimarang/
**TrimarangEtherealProjectile.cs** uses: AfterimageTrail, BoomerangFlight, DetachedTrails, RemoteHitTracker, TrimarangFx, TrimarangTuning
  class TrimarangEtherealProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +BeginDying() +AI()[ov] +FindTarget(Vector2,int,Projectile)[s] +CanHitNPC(NPC)[ov] +CanDamage()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**TrimarangFx.cs**
  static class TrimarangFx
    +Spinning(Vector2,float,int)[s] +HitSparks(Vector2)[s] +Catch(Vector2)[s] +EtherealSpawn(Vector2,Vector2)[s] +EtherealTrail(Vector2,Vector2)[s] +EtherealExplode(Vector2)[s]
**TrimarangGlobalItem.cs** uses: TrimarangProjectile, TrimarangTuning
  class TrimarangGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**TrimarangProjectile.cs** uses: AfterimageTrail, BoomerangFlight, DetachedTrails, RemoteHitTracker, TrimarangEtherealProjectile, TrimarangFx, TrimarangTuning
  class TrimarangProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +AI()[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +Colliding(Rectangle,Rectangle)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Hold, Returning}
**TrimarangTuning.cs**
  static class TrimarangTuning
    +ThrowSpeed:float[s] +HoldRange:float[s] +MinThrowSpeed:float[s] +StopSnapDistance:float[s] +LeashDistance:float[s] +FlightMaxTicks:int[s] +SteerTurn:float[s] +HoldSteerSpeed:float[s] +HoldSteerSmoothing:float[s] +HoldSteerDeadzone:float[s] +SteerSyncTicks:int[s] +SpinStart:float[s] +SpinCap:float[s] +SpinRampTicks:int[s] +HitCooldownSlow:int[s] +HitCooldownFast:int[s] +HitRadius:float[s] +HoldKnockbackMultiplier:float[s] +ReturnStartSpeed:float[s] +ReturnAcceleration:float[s] +ReturnMaxSpeed:float[s] +CatchDistance:float[s] +HitsPerEthereal:int[s] +EtherealDamageMultiplier:float[s] +EtherealMaxAlive:int[s] +EtherealHitsToExplode:int[s] +EtherealLaunchSpeed:float[s] +EtherealLaunchTicks:int[s] +EtherealHomingSpeed:float[s] +EtherealTurn:float[s] +EtherealSearchRange:float[s] +EtherealRetargetTicks:int[s] +EtherealReturnAcceleration:float[s] +EtherealReturnDrag:float[s] +EtherealReturnMaxSpeed:float[s] +EtherealHitCooldownTicks:int[s] +EtherealLifeTicks:int[s] +GoldenAngleDeg:float[s] +MainColor:Color[s] +EtherealColor:Color[s] +EtherealOpacity:float[s]

### Content/Weapons/WoodenBoomerang/
**WoodenBoomerangFx.cs** uses: WoodenBoomerangTuning
  static class WoodenBoomerangFx
    +Splinters(Vector2,int,float,float)[s] +HitSparks(Vector2)[s] +Trail(Vector2,Vector2,float)[s] +Catch(Vector2)[s] +PlayShardBreak(Vector2)[s] +PlayBreak(Vector2)[s]
**WoodenBoomerangGlobalItem.cs** uses: WoodenBoomerangProjectile, WoodenBoomerangTuning
  class WoodenBoomerangGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**WoodenBoomerangProjectile.cs** uses: AfterimageTrail, BoomerangFlight, DetachedTrails, WoodenBoomerangFx, WoodenBoomerangShardProjectile, WoodenBoomerangTuning
  class WoodenBoomerangProjectile : ModProjectile
    +Texture:string[ov] +Art()[s] +Load()[ov] +Unload()[ov] +CanDamage()[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +Colliding(Rectangle,Rectangle)[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Phase
      {Outbound, Returning, Broken}
**WoodenBoomerangShardProjectile.cs** uses: RemoteHitTracker, WoodenBoomerangFx, WoodenBoomerangProjectile, WoodenBoomerangTuning
  class WoodenBoomerangShardProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +CanDamage()[ov] +AI()[ov] +OnTileCollide(Vector2)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**WoodenBoomerangTuning.cs**
  static class WoodenBoomerangTuning
    +ThrowSpeed:float[s] +ThrowRange:float[s] +BrakeDistance:float[s] +BrakeMinSpeed:float[s] +ReturnAcceleration:float[s] +ReturnDrag:float[s] +ReturnMaxSpeed:float[s] +CatchDistance:float[s] +FlightMaxTicks:int[s] +SpinSpeed:float[s] +AfterimageTicks:int[s] +ShardDamageMultiplier:float[s] +ShardMaxHits:int[s] +ShardLifeTicks:int[s] +ShardHitCooldownTicks:int[s] +ShardSpeed:float[s] +ShardSpreadAngle:float[s] +ShardHop:float[s] +ShardGravity:float[s] +ShardMaxFallSpeed:float[s] +ShardDrag:float[s] +BreakOnTilesWhileReturning:bool[s] +GlowColor:Color[s] +WoodDust:int[s]

## Non-code files (png omitted; sprites follow <Name>_On/_Off/_On_FirstLiquid naming)
- (root)/: .gitattributes, .gitignore, LICENSE; IsmailsWeaponReworks.csproj; build.txt, description.txt, description_workshop.txt
- Localization/: en-US_Mods.IsmailsWeaponReworks.hjson
- Properties/: launchSettings.json
- docs/: ROADMAP.md
- tools/: patch_engine.ps1
