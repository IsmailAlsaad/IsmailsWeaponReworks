# CODEBASE INDEX - IsmailsWeaponReworks
generated 2026-11-05 | 148 .cs files | regenerate: `python index_codebase.py` (project root)

Legend: `+` public `#` protected `~` internal | s=static ab=abstract v=virtual ov=override c=const | `Name(Types)` method, `Name:Type` field/prop | `uses:` other project types used in file | `ns!=X` namespace differs from folder path (matters for `using`) | no line numbers: grep the name | private members and SetDefaults/SetStaticDefaults omitted

## Most-referenced types (files referencing them)
ThrustPalette(24) DetachedTrails(23) RemoteHitTracker(22) AfterimageTrail(14) CameraShake(13) ThrustPlayer(12) ThrustChain(11) IceFx(11) HeldAim(11) HeldSwordPose(10) SweepTrail(9) MuramasaTuning(9) DarkLanceTuning(9) BoomerangFlight(8) ThrustTimeline(7) PixelTrail(7) PixelLine(7) FlamerangTuning(7) BloodButchererTuning(7) StarfuryTuning(6)

## Inheritance
Project bases -> derived:
- EnchantedBoomerangBase: EnchantedBoomerangProjectile, EtherealBoomerangProjectile
- HeldSpearProjectile: DarkLanceThrustProjectile, SpearThrustProjectile, StormSpearThrustProjectile, SwordfishThrustProjectile, TridentThrustProjectile
- MetaballStyle: DarkLanceBlobStyle
- PixelTrailStyle: ArcBoltStyle, BloodButchererJawTrailStyle, DarkLanceOrbTrailStyle, MoltenTrailStyle, SunfuryTrailStyle
- SweepTrailStyle: BloodButchererSpinTrailStyle, IceBladeTrailStyle, MuramasaSweepTrailStyle, StarfurySweepTrailStyle, ThrustTrailStyle
tModLoader/other bases -> project types:
- ModProjectile (37): BloodButchererSwingProjectile, DarkLanceBlastProjectile, DarkLanceChargeProjectile, DarkLanceOrbProjectile, DarkLanceSweepProjectile, EnchantedBoomerangBase, FlamerangBlastProjectile, FlamerangProjectile, HeldSpearProjectile, IceBladeSwingProjectile, IceBladeWaveProjectile, IceBoomerangProjectile, IceCrystalBlastProjectile, IceCrystalProjectile, MoltenBoomerangProjectile, MuramasaIaidoProjectile, MuramasaSlashProjectile, MuramasaStanceProjectile, MuramasaSwingProjectile, ObsidianShardProjectile, RottedForkJavelinProjectile, RottedForkSpinProjectile, ShroomerangBurstProjectile, ShroomerangMushroomProjectile, ShroomerangProjectile, StardustBurstProjectile, StarfuryBlastProjectile, StarfuryStarProjectile, StarfurySwingProjectile, SunfuryBlastProjectile, SunfuryFlailProjectile, ThornChakramProjectile, ThornChakramStingerProjectile, TrimarangEtherealProjectile, TrimarangProjectile, WoodenBoomerangProjectile, WoodenBoomerangShardProjectile
- GlobalItem (18): BloodButchererGlobalItem, DarkLanceGlobalItem, EnchantedBoomerangGlobalItem, FlamerangGlobalItem, IceBladeGlobalItem, IceBoomerangGlobalItem, MuramasaGlobalItem, RottedForkGlobalItem, ShroomerangGlobalItem, SpearGlobalItem, StarfuryGlobalItem, StormSpearGlobalItem, SunfuryGlobalItem, SwordfishGlobalItem, ThornChakramGlobalItem, TridentGlobalItem, TrimarangGlobalItem, WoodenBoomerangGlobalItem
- ModPlayer (8): DarkLancePlayer, DashPlayer, ImmunityPlayer, MuramasaPlayer, RottedForkPlayer, StarfuryPlayer, ThrustPlayer, TridentPlayer
- GlobalNPC (2): BloodButchererGripNPC, CarriedNPC
- ModSystem (2): DetachedTrails, MetaballLayers
- Mod (1): IsmailsWeaponReworks

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
**AimSpread.cs**
  static class AimSpread
    +Offset(int,int,float)[s]
**ArcBolt.cs** uses: DetachedTrails, PixelTrail, PixelTrailStyle
  class ArcBoltStyle : PixelTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Layers:int[ov] +GetWidth(int,float)[ov] +GetColor(int,float)[ov]
  static class ArcBolt
    +Spawn(Vector2,Vector2,ArcBoltStyle,float,int)[s]
**AreaEffect.cs**
  static class AreaEffect
    +ForEachNpc(Vector2,float,Action<NPC>)[s] +ApplyBuff(Vector2,float,int,int)[s]
**BoomerangFlight.cs**
  static class BoomerangFlight
    +StepReturn(Projectile,Player,float,float,float,float)[s] +StepForceReturn(Projectile,Player,float,float,float,float)[s] +StepOutbound(Projectile,float,float,float,float)[s]
**BounceMath.cs**
  static class BounceMath
    +Reflect(Vector2,Vector2)[s] +PushAway(Vector2,Vector2,float)[s] +Slide(Vector2,Vector2)[s] +NpcNormal(Vector2,NPC,Vector2)[s] +TileNormal(Player,Vector2,out Vector2)[s] +TileAt(Player,Vector2,out Point)[s]
**CameraShake.cs**
  static class CameraShake
    +Punch(Vector2,float,int,string)[s]
**CarriedNPC.cs**
  class CarriedNPC : GlobalNPC
    +InstancePerEntity:bool[ov] +HoldTick:long +Held:bool +CanCarry(NPC,float)[s] +Hold(NPC,Vector2)[s] +Release(NPC)[s] +ClampToOpenAir(Vector2,Vector2,NPC)[s] +PreAI(NPC)[ov] +CanHitPlayer(NPC,Player,ref int)[ov]
**DashPlayer.cs** uses: BounceMath, DashSettings, Immunity, Phase
  enum DashPhase
    {None, Dash, HitStop, Bounce}
  class DashPlayer : ModPlayer
    +Phase:DashPhase +Direction:Vector2 +Settings:DashSettings +Bounced:bool +Begin(Vector2,DashSettings,Action<Vector2, Vector2, bool>) +PreUpdateMovement()[ov] +PostUpdate()[ov] +Bounce(Vector2,bool) +Finish(bool)
**DetachedTrails.cs** uses: AfterimageTrail, PixelTrail, SweepTrail
  class DetachedTrails : ModSystem
    +Hand(PixelTrail)[s] +Hand(SweepTrail)[s] +Hand(AfterimageTrail)[s] +PostUpdateEverything()[ov] +PostDrawTiles()[ov] +OnWorldUnload()[ov] +Unload()[ov]
**HeldAim.cs**
  static class HeldAim
    +Capture(Player)[s] +ToWorld(Player,float)[s] +ToWorldDirection(Player,float)[s] +Facing(float)[s]
**HeldSpearProjectile.cs** uses: AimSpread, DetachedTrails, HeldAim, Mode, RemoteHitTracker, SegmentHit, SpearPose, SweepTrail, ThrustChain, ThrustFx, ThrustPalette, ThrustPlayer, ThrustTimeline, ThrustTrailStyle
  abstract class HeldSpearProjectile : ModProjectile
    #ItemType:int[ab] #Chain:ThrustChain[ab] #Reach:float[ab] #Palette:ThrustPalette[ab] #SpearScale:float[v] #GripFraction:float[v] #HitWidth:float[v] #RetargetEachThrust:bool[v] #TrailLifeTicks:int[v] #TrailRails:int[v] #TrailOpacity:float[v] #PixelSize:float[v] #TrailTaper:float[v] #OnThrustStart(int)[v] #OnStrikeStart(int)[v] #OnTipStep(Vector2,Vector2,float)[v] #OnTipHit(NPC,HitInfo,int)[v] #ShouldContinue(int)[v] #ThrustPitch(int)[v] #SpriteRotation:float[v] #SpritePivot:Vector2[v] #ShaftLength()[v] +Texture:string[ov] #Owner:Player #IsOwner:bool #BaseAim:float #Seed:int #Mode:int #EndAge:int #Age:int #ThrustIndex:int #Extension:float #Striking:bool #Butt:Vector2 #Tip:Vector2 #Direction:Vector2 +AI()[ov] +Colliding(Rectangle,Rectangle)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**HeldSwordPose.cs**
  static class HeldSwordPose
    +ToWorld(int,float)[s] +Hand(Player,int,float)[s] +BladeDirection(Player,int,float,float)[s] +HandWorld(Player,float)[s] +BladeDirectionWorld(Player,float)[s] +Sprite(Texture2D,int,out Vector2,out float,out SpriteEffects)[s]
**IceFx.cs**
  static class IceFx
    +Burst(Vector2,int,float,float)[s] +Ring(Vector2,int,float,float)[s] +Gather(Vector2,float)[s] +DrawPixel(Vector2,float,Color,float)[s]
**Immunity.cs**
  static class Immunity
    +Grant(Player,int)[s] +Active(Player)[s] +Clear(Player)[s]
  class ImmunityPlayer : ModPlayer
    +TicksLeft:int +ImmuneTo(PlayerDeathReason,int,bool)[ov] +PostUpdate()[ov]
**LiquidState.cs**
  enum LiquidKind
    {None, Water, Lava, Honey, Shimmer}
  static class LiquidState
    +Touching(Player)[s] +HeadUnder(Player)[s] +Kind(Player)[s]
**MetaballLayer.cs** uses: MetaballParticle, MetaballStyle
  class MetaballLayer
    +Style:MetaballStyle +IsEmpty:bool +Spawn(Vector2,Vector2,int,float) +AddBlob(Vector2,float,float) +Tick(int) +Clear() +Draw()
    struct Blob
      +Position:Vector2 +Radius:float +Heat:float +Stamp:int
**MetaballLayers.cs** uses: MetaballLayer, MetaballStyle
  class MetaballLayers : ModSystem
    +Of<TStyle>()[s] +PostUpdateEverything()[ov] +PostDrawTiles()[ov] +OnWorldUnload()[ov] +Unload()[ov]
**MetaballStyle.cs**
  class MetaballParticle
    +Position:Vector2 +Velocity:Vector2 +Age:int +Life:int +Size:float +Wobble:float
  abstract class MetaballStyle
    +Grid:float[v] +Merge:float[v] +OutlineWidth:float[v] +Bands:int[v] +GlowStrength:float[v] +MaxParticles:int[v] +Rise:float[v] +Drag:float[v] +Wobble:float[v] +SizeOverLife(float)[v] +HeatOverLife(float)[v] +GetFill(float)[ab] +GetOutline()[ab] +GetGlow()[v] +Step(MetaballParticle)[v]
**OrbitRing.cs**
  static class OrbitRing
    +Angle(float,int,int,float)[s] +Position(Vector2,float,float)[s] +Rank(Projectile,out int,Func<Projectile, bool>)[s]
**PixelLine.cs**
  static class PixelLine
    +Draw(Vector2,float,int,float,Func<float, float>,Func<float, Color>)[s]
    struct Span
      +Y:int +X0:int +X1:int +Order:int +Color:Color
**PixelShaderFx.cs**
  class PixelShaderParams
    +Grid:float +Time:float +Seed:float +Progress:float +Intensity:float +Bands:float +Ellipse:bool +ColorA:Color +ColorB:Color +ColorC:Color +Params:Vector4 +Params2:Vector4 +Ramp:Texture2D +Blend:BlendState +FallbackColor:Color +Configure:Action<Effect>
  static class PixelShaderFx
    +Draw(string,Vector2,int,int,PixelShaderParams)[s] +Draw(string,Vector2,int,PixelShaderParams)[s] +Set(Effect,string,float)[s] +Set(Effect,string,Vector2)[s] +Set(Effect,string,Vector3)[s] +Set(Effect,string,Vector4)[s]
**PixelTrail.cs** uses: PixelLine
  abstract class PixelTrailStyle
    +Grid:float[v] +LifeTicks:int[v] +Layers:int[v] +Jitter:float[v] +GetWidth(int,float)[ab] +GetColor(int,float)[ab] +Update(PixelTrail)[v]
  class PixelTrail
    +Style:PixelTrailStyle +IsOwner:bool +Count:int +IsEmpty:bool +Clock:int +PointAt(int) +AgeAt(int) +Add(Vector2) +FadeWithin(int) +Tick() +Draw()
**PlayerDash.cs** uses: Afterimage, DashPhase, DashPlayer, Phase
  struct DashSettings
    +Speed:float +Ticks:int +BounceOnTile:bool +BounceSpeed:float +HitStopTicks:int +BounceTicks:int +ImmuneTicks:int +GrazeCos:float +BounceDrag:float +ImmuneTailTicks:int
  static class PlayerDash
    +Begin(Player,Vector2,DashSettings,Action<Vector2, Vector2, bool>)[s] +Bounce(Player,Vector2)[s] +End(Player)[s] +IsDashing(Player)[s] +Afterimage(Player)[s]
**RemoteHitTracker.cs**
  class RemoteHitTracker
    +Update(Projectile,int,Action<NPC>) +TouchOnce(Projectile,Action<NPC>)[s]
**SegmentHit.cs**
  static class SegmentHit
    +Overlaps(Rectangle,Vector2,Vector2,float)[s] +Sweep(Rectangle,Vector2,Vector2,Vector2,Vector2,float,int)[s] +ClosestPoint(Vector2,Vector2,Vector2)[s] +ClearLocalImmunity(Projectile)[s]
**SpearPose.cs** uses: HeldSwordPose
  static class SpearPose
    +Arm(Player,float)[s] +Hand(Player,float)[s] +DrawSprite(Texture2D,Vector2,float,int,float,Color,float,Vector2,float)[s]
**SweepTrail.cs** uses: PixelLine
  abstract class SweepTrailStyle
    +Grid:float[v] +LifeTicks:int[v] +Rails:int[v] +TaperFraction:float[v] +Fade:SweepFade[v] +TailLifeScale:float[v] +GetColor(float,float)[ab] #Falloff(float)[s] #ThreeStop(float,Color,Color,Color)[s]
  enum SweepFade
    {Uniform, TailFirst}
  class SweepTrail
    +Style:SweepTrailStyle +IsOwner:bool +Count:int +IsEmpty:bool +Add(Vector2,Vector2,float) +Tick() +Draw()
**ThrustChain.cs** uses: ThrustTimeline
  struct ThrustChain
    +Timeline:ThrustTimeline +Count:int +GapTicks:int +SpreadDeg:float +CooldownTicks:int +Endless:bool +PeriodTicks:int +FiniteTotalTicks:int +IsFinished(int) +Locate(int,out int,out int) +EndOfThrust(int)
**ThrustFx.cs**
  struct ThrustPalette
    +Core:Color +Mid:Color +Halo:Color +DustA:int +DustB:int
  static class ThrustFx
    +PlayThrust(Vector2,float)[s] +TipStreak(Vector2,Vector2,ThrustPalette)[s] +HitSpark(Vector2,ThrustPalette)[s]
**ThrustPlayer.cs**
  class ThrustPlayer : ModPlayer
    +CooldownUntil:uint +CooldownItem:int +StartCooldown(int,int) +OnCooldown(int) +CanUse(Player,int,int)[s]
**ThrustTimeline.cs**
  struct ThrustTimeline
    +WindupTicks:int +ThrustTicks:int +HoldTicks:int +RecoverTicks:int +PullBack:float +EasePower:float +TotalTicks:int +StrikeStart:int +StrikeEnd:int +IsStriking(int) +Extension(int)
**ThrustTrailStyle.cs** uses: SweepTrailStyle, ThrustPalette
  class ThrustTrailStyle : SweepTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Rails:int[ov] +TaperFraction:float[ov] +GetColor(float,float)[ov]
**TileSound.cs**
  static class TileSound
    +PlayHit(int,int)[s]

### Content/Weapons/BloodButcherer/
**BloodButchererFx.cs** uses: BloodButchererTuning, IceFx
  static class BloodButchererFx
    +PlayLunge(Vector2)[s] +PlayOpen(Vector2)[s] +PlayBite(Vector2)[s] +PlayClamp(Vector2)[s] +PlaySpin(Vector2,float)[s] +PlayThrow(Vector2)[s] +ThrowBurst(Vector2,Vector2)[s] +GripDrip(Vector2)[s] +SpinFling(Vector2,Vector2)[s] +TetherDrip(Vector2)[s] +MouthMist(Vector2,Vector2)[s] +Drool(Vector2)[s] +BiteSnap(Vector2)[s] +ClampBurst(Vector2)[s] +ChompSnap(Vector2)[s] +PlayChomp(Vector2)[s] +ImpactBurst(Vector2,Vector2,float)[s] +PlayImpact(Vector2,float)[s] +HitBlood(Vector2)[s] +DrawTeeth(Vector2,Vector2,float,Vector2,float,float,Color)[s]
**BloodButchererGlobalItem.cs** uses: BloodButchererSwingProjectile, BloodButchererTuning
  class BloodButchererGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**BloodButchererGripNPC.cs** uses: BloodButchererFx, BloodButchererTuning, CameraShake
  class BloodButchererGripNPC : GlobalNPC
    +InstancePerEntity:bool[ov] +HoldTick:long +LaunchTick:long +ImpactOwner:int +ImpactBaseDamage:int +Held:bool +Launched:bool +BeginImpact(int,int) +PreAI(NPC)[ov] +CanHitPlayer(NPC,Player,ref int)[ov]
**BloodButchererSpinTrailStyle.cs** uses: BloodButchererTuning, SweepFade, SweepTrailStyle
  class BloodButchererSpinTrailStyle : SweepTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Rails:int[ov] +Fade:SweepFade[ov] +GetColor(float,float)[ov]
**BloodButchererSwingProjectile.cs** uses: BloodButchererFx, BloodButchererGripNPC, BloodButchererJawTrailStyle, BloodButchererSpinTrailStyle, BloodButchererTether, BloodButchererTuning, CameraShake, DetachedTrails, HeldSwordPose, PixelTrail, SweepTrail
  class BloodButchererSwingProjectile : ModProjectile
    +Texture:string[ov] +Load()[ov] +Unload()[ov] +AI()[ov] +CanDamage()[ov] +CanHitNPC(NPC)[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
    enum Branch
      {Undecided, Miss, Clamp, Grip}
**BloodButchererTether.cs** uses: BloodButchererTuning, PixelLine
  class BloodButchererTether
    +Draw(Vector2,Vector2,Vector2,float,float,int,Color)
**BloodButchererTrail.cs** uses: BloodButchererTuning, PixelTrailStyle
  class BloodButchererJawTrailStyle : PixelTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Layers:int[ov] +GetWidth(int,float)[ov] +GetColor(int,float)[ov]
**BloodButchererTuning.cs**
  static class BloodButchererTuning
    +BladeLength:float[s] +HandleLength:float[s] +SwordScale:float[s] +WindupTicks:int[s] +ThrustTicks:int[s] +OpenTicks:int[s] +GapeTicks:int[s] +BiteTicks:int[s] +ChompCount:int[s] +ChompTicks:int[s] +ChompOpenAmount:float[s] +ChompDamageMultiplier:float[s] +ChompShake:float[s] +RecoverTicks:int[s] +DecideWaitTicks:int[s] +AttackSpeedInfluence:float[s] +SpeedScaleMin:float[s] +SpeedScaleMax:float[s] +PullTicks:int[s] +SpinTicks:int[s] +LockTicks:int[s] +SpinTopSpeed:float[s] +ChompTurnSpeed:float[s] +WindDownTicks:int[s] +WindDownRecoverTicks:int[s] +ReleaseHoldTicks:int[s] +ReleaseOpenTicks:int[s] +SpinOffset:float[s] +PinRatio:float[s] +LaunchSpeed:float[s] +LaunchAiOffTicks:int[s] +LaunchGravity:float[s] +LaunchMaxFall:float[s] +GripTickDamageMultiplier:float[s] +GripTickInterval:int[s] +LaunchDamageMultiplier:float[s] +MaxGrabSize:float[s] +LaunchShake:float[s] +SpinWhooshPitchLow:float[s] +SpinWhooshPitchHigh:float[s] +JawTrailCutTicks:int[s] +SpinSweepDamageMultiplier:float[s] +SpinSweepHitInterval:int[s] +SpinSweepMinSpeed:float[s] +SpinSweepWidth:float[s] +ImpactMinSpeed:float[s] +ImpactDamageMultiplier:float[s] +ImpactMaxMultiplier:float[s] +ImpactShake:float[s] +ImpactCooldownTicks:int[s] +TrailFullSpeed:float[s] +TrailFillTo:float[s] +SpinTrailLifeTicks:int[s] +SpinTrailRails:int[s] +SpinTrailOpacity:float[s] +SpinTrailSamplesPerTick:int[s] +TetherRestLength:float[s] +TetherWidth:float[s] +TetherThinWidth:float[s] +TetherSag:float[s] +TetherMaxSag:float[s] +TetherTrailing:float[s] +TetherMaxTrailing:float[s] +TetherWobble:float[s] +TetherSegments:int[s] +TetherDarkColor:Color[s] +TetherMidColor:Color[s] +TetherGlowColor:Color[s] +WindupPullback:float[s] +ThrustDistance:float[s] +BiteLungeDistance:float[s] +JawOpenDeg:float[s] +CatchPadding:float[s] +BiteDamageMultiplier:float[s] +OtherDamageMultiplier:float[s] +ClampShake:float[s] +PixelSize:float[s] +TeethCount:int[s] +ToothLength:float[s] +FlashTicks:int[s] +TrailLifeTicks:int[s] +TrailWidth:float[s] +TrailSamplesPerTick:int[s] +CrimsonColor:Color[s] +BrightColor:Color[s] +BoneColor:Color[s]

### Content/Weapons/DarkLance/
**DarkLanceBlastProjectile.cs** uses: AreaEffect, DarkLanceFx, DarkLanceTuning, IceFx, RemoteHitTracker, ThrustFx, ThrustPalette
  class DarkLanceBlastProjectile : ModProjectile
    +Texture:string[ov] +Colliding(Rectangle,Rectangle)[ov] +AI()[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
**DarkLanceChargeProjectile.cs** uses: DarkLanceFx, DarkLanceOrbProjectile, DarkLancePlayer, DarkLanceTuning, HeldAim, SpearPose, ThrustPalette, ThrustPlayer
  class DarkLanceChargeProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**DarkLanceEmbers.cs** uses: DarkLanceTuning, MetaballLayer, MetaballLayers, MetaballStyle, ThrustPalette
  class DarkLanceBlobStyle : MetaballStyle
    +Grid:float[ov] +Merge:float[ov] +OutlineWidth:float[ov] +Bands:int[ov] +GlowStrength:float[ov] +MaxParticles:int[ov] +Rise:float[ov] +Drag:float[ov] +Wobble:float[ov] +HeatOverLife(float)[ov] +GetFill(float)[ov] +GetOutline()[ov] +GetGlow()[ov]
  static class DarkLanceEmbers
    +Ball(Vector2,float,float,float)[s] +Spawn(Vector2,Vector2)[s] +Shed(Vector2,Vector2)[s] +TipCharge(Vector2,int,float)[s] +Burst(Vector2,float,float)[s]
**DarkLanceFx.cs** uses: CameraShake, DarkLanceEmbers, DarkLanceTuning, OrbitRing, PixelShaderFx, PixelShaderParams, ThrustPalette
  static class DarkLanceFx
    +Glow(Color)[s] +DrawGlow(Vector2,float,float)[s] +SweepStart(Vector2)[s] +SweepWhoosh(Vector2)[s] +Flecks(Vector2,Vector2,float,ThrustPalette)[s] +CollectPing(Vector2,int)[s] +Absorb(Vector2)[s] +ThrustStart(Vector2,int)[s] +Blast(Vector2,float,float)[s] +ChargeStart(Vector2)[s] +Gather(Vector2,float)[s] +Ring(Vector2,int,float,float)[s] +OrbBorn(Vector2,int,int)[s] +Shed(Vector2)[s] +Pop(Vector2,float)[s] +Fizzle(Vector2)[s]
**DarkLanceGlobalItem.cs** uses: DarkLanceChargeProjectile, DarkLancePlayer, DarkLanceSweepProjectile, DarkLanceThrustProjectile, DarkLanceTuning, HeldAim, ThrustPlayer
  class DarkLanceGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +AltFunctionUse(Item,Player)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**DarkLanceOrbProjectile.cs** uses: AreaEffect, CameraShake, DarkLanceEmbers, DarkLanceFx, DarkLancePlayer, DarkLanceTuning, DetachedTrails, OrbitRing, PixelTrail
  class DarkLanceOrbProjectile : ModProjectile
    +Texture:string[ov] +PreviousCenter:Vector2 +OnSpawn(IEntitySource)[ov] +AI()[ov] +PostAI()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**DarkLanceOrbTrail.cs** uses: DarkLanceFx, DarkLanceTuning, PixelTrailStyle, ThrustPalette
  class DarkLanceOrbTrailStyle : PixelTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Layers:int[ov] +GetWidth(int,float)[ov] +GetColor(int,float)[ov]
**DarkLancePlayer.cs** uses: DarkLanceOrbProjectile
  class DarkLancePlayer : ModPlayer
    +OrbTurn:float +Serial:int +NextIsThrust:bool +Collected:int +LanceTip:Vector2 +NextPing() +PostUpdate()[ov]
**DarkLanceSweepProjectile.cs** uses: DarkLanceEmbers, DarkLanceFx, DarkLanceOrbProjectile, DarkLancePlayer, DarkLanceTuning, DetachedTrails, HeldAim, HeldSwordPose, RemoteHitTracker, SegmentHit, SweepTrail, ThrustFx, ThrustPalette, ThrustPlayer
  class DarkLanceSweepProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**DarkLanceThrustProjectile.cs** uses: CameraShake, DarkLanceBlastProjectile, DarkLanceEmbers, DarkLanceFx, DarkLancePlayer, DarkLanceTuning, HeldSpearProjectile, Mode, SegmentHit, ThrustChain, ThrustPalette
  class DarkLanceThrustProjectile : HeldSpearProjectile
    #ItemType:int[ov] #Chain:ThrustChain[ov] #Reach:float[ov] #Palette:ThrustPalette[ov] #SpearScale:float[ov] #GripFraction:float[ov] #HitWidth:float[ov] #RetargetEachThrust:bool[ov] #TrailLifeTicks:int[ov] #TrailRails:int[ov] #TrailOpacity:float[ov] #PixelSize:float[ov] #TrailTaper:float[ov] #SpritePivot:Vector2[ov] +Texture:string[ov] #ShaftLength()[ov] #SpriteRotation:float[ov] #ThrustPitch(int)[ov] +AI()[ov] #OnStrikeStart(int)[ov] #OnTipStep(Vector2,Vector2,float)[ov] #OnTipHit(NPC,HitInfo,int)[ov] +PreDraw(ref Color)[ov]
**DarkLanceTuning.cs** uses: ThrustChain, ThrustPalette, ThrustTimeline
  static class DarkLanceTuning
    +ChargeIntervalTicks:int[s] +MaxOrbs:int[s] +ChargeWindDownTicks:int[s] +ChargeCooldownTicks:int[s] +LanceScale:float[s] +LanceTilt:float[s] +ArmRaise:float[s] +HandOffsetX:float[s] +HandOffsetY:float[s] +TremblePerOrb:float[s] +SweepWindupTicks:int[s] +SweepTicks:int[s] +SweepRecoverTicks:int[s] +SweepPullBack:float[s] +SweepCooldownTicks:int[s] +SweepDamageMultiplier:float[s] +SweepScale:float[s] +SweepGrip:float[s] +SweepHitWidth:float[s] +SweepMinHitSpeed:float[s] +SweepSpriteRotation:float[s] +CollectMinSpeed:float[s] +CollectTicks:int[s] +SweepTrailLifeTicks:int[s] +SweepTrailRails:int[s] +SweepTrailOpacity:float[s] +SweepTrailInner:float[s] +SweepEmberChance:float[s] +ThrustWindupTicks:int[s] +ThrustTicks:int[s] +ThrustHoldTicks:int[s] +ThrustRecoverTicks:int[s] +ThrustPullBack:float[s] +ThrustEasePower:float[s] +ThrustCooldownTicks:int[s] +ThrustReach:float[s] +ThrustScale:float[s] +ThrustGrip:float[s] +ThrustHitWidth:float[s] +ThrustDamageMultiplier:float[s] +SizePerOrb:float[s] +DamagePerOrb:float[s] +ThrustTrailLifeTicks:int[s] +ThrustTrailRails:int[s] +ThrustTrailOpacity:float[s] +ThrustBurst:ThrustChain[s] +BlastRadius:float[s] +BlastMinScale:float[s] +BlastScale(int)[s] +BlastDamageMultiplier:float[s] +BlastLifeTicks:int[s] +BlastShake:float[s] +BlastBuffTicks:int[s] +OrbRadius:float[s] +OrbBob:float[s] +OrbSpin:float[s] +OrbEase:float[s] +OrbFollow:float[s] +OrbSnapDistance:float[s] +OrbSpawnTicks:int[s] +OrbLifeTicks:int[s] +OrbHitSize:float[s] +OrbPopGraceTicks:int[s] +PopRadius:float[s] +PopBuffTicks:int[s] +PopShake:float[s] +TrailLifeTicks:int[s] +TrailWidth:float[s] +PixelSize:float[s] +ColorBands:int[s] +OrbUnitPx:float[s] +HaloReach:float[s] +HaloStrength:float[s] +EmberSpawnTicks:int[s] +EmberLifeTicks:int[s] +EmberSize:float[s] +EmberMerge:float[s] +EmberOutline:float[s] +EmberRise:float[s] +EmberDrag:float[s] +EmberKick:float[s] +EmberInherit:float[s] +EmberWobble:float[s] +EmberGlow:float[s] +EmberMax:int[s] +EmberHeat:float[s] +BallRadius:float[s] +BallBodyHeat:float[s] +BallCoreHeat:float[s] +TipGlowMinRadius:float[s] +TipGlowMaxRadius:float[s] +TipGlowFalloff:float[s] +TipGlowStrength:float[s] +Palette:ThrustPalette[s]

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
**IceBladeTrail.cs** uses: IceBladeTuning, SweepFade, SweepTrailStyle
  class IceBladeTrailStyle : SweepTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Rails:int[ov] +Fade:SweepFade[ov] +GetColor(float,float)[ov]
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

### Content/Weapons/Muramasa/
**MuramasaDrawState.cs** uses: MuramasaFx, MuramasaTuning, PixelLine
  class MuramasaDrawState
    +Finished:bool +PathLength:float +Strokes:IList<Vector2> +Begin(Player) +Tick(Player,bool) +Draw()
**MuramasaFx.cs** uses: MuramasaTuning
  static class MuramasaFx
    +PlayScrape(Vector2)[s] +PlayClick(Vector2)[s] +PlaySwing(Vector2,bool)[s] +Click(Vector2,Vector2)[s] +BladeSparkle(Vector2,Vector2)[s] +DrawStart(Vector2)[s] +DrawDust(Vector2)[s] +DrawEnd(IList<Vector2>)[s] +PlaySlash(Vector2,int)[s] +SlashStart(Vector2,Vector2)[s] +SlashSweepDust(Vector2,Vector2)[s] +SlashEnd(Vector2,Vector2)[s] +IaidoDraw(Vector2,Vector2)[s] +SlashDust(Vector2,Vector2)[s] +HitSpark(Vector2)[s]
**MuramasaGlobalItem.cs** uses: HeldAim, MuramasaIaidoProjectile, MuramasaPlayer, MuramasaStanceProjectile, MuramasaSwingProjectile, MuramasaTuning
  class MuramasaGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +StartSwing(Player,IEntitySource,int,float)[s]
**MuramasaIaidoProjectile.cs** uses: CameraShake, HeldSwordPose, MuramasaFx, MuramasaPlayer, MuramasaSlashProjectile, MuramasaStanceProjectile, MuramasaTuning
  class MuramasaIaidoProjectile : ModProjectile
    +Texture:string[ov] +Launch(Player,IList<Vector2>)[s] +AI()[ov] +CanDamage()[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**MuramasaPlayer.cs** uses: MuramasaIaidoProjectile, MuramasaStanceProjectile, MuramasaSwingProjectile, MuramasaTuning
  class MuramasaPlayer : ModPlayer
    +Find(int,int)[s] +PostUpdate()[ov]
**MuramasaSlashProjectile.cs** uses: AfterimageTrail, CameraShake, DetachedTrails, HeldSwordPose, MuramasaFx, MuramasaTuning, PixelLine, RemoteHitTracker
  class MuramasaSlashProjectile : ModProjectile
    +Texture:string[ov] +TravelTicks(float)[s] +AI()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**MuramasaStanceProjectile.cs** uses: CameraShake, HeldSwordPose, MuramasaDrawState, MuramasaFx, MuramasaGlobalItem, MuramasaIaidoProjectile, MuramasaPlayer, MuramasaSwingProjectile, MuramasaTuning
  class MuramasaStanceProjectile : ModProjectile
    +Texture:string[ov] +IsDrawing:bool +ArmLocal:float +TiltRad:float +Progress:float +FullySheathed:bool +Spawn(IEntitySource,Player,float,float)[s] +AI()[ov] +CanDamage()[ov] +PreDraw(ref Color)[ov]
**MuramasaSwingProjectile.cs** uses: CameraShake, DetachedTrails, HeldSwordPose, MuramasaFx, MuramasaStanceProjectile, MuramasaSweepTrailStyle, MuramasaTuning, SweepTrail
  class MuramasaSwingProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +CanDamage()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**MuramasaTrail.cs** uses: MuramasaTuning, SweepFade, SweepTrailStyle
  class MuramasaSweepTrailStyle : SweepTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Rails:int[ov] +Fade:SweepFade[ov] +TailLifeScale:float[ov] +GetColor(float,float)[ov]
**MuramasaTuning.cs**
  static class MuramasaTuning
    +BladeLength:float[s] +SwordScale:float[s] +SheathTicks:int[s] +ReadyArmDeg:float[s] +ReadyTiltDeg:float[s] +SheathArmDeg:float[s] +SheathTiltDeg:float[s] +ScrapeAt:float[s] +ClickKickDeg:float[s] +ClickFlashTicks:int[s] +ClickDustCount:int[s] +ClickShake:float[s] +OutlineWidth:float[s] +OutlinePulseTicks:int[s] +OutlineMin:float[s] +OutlineMax:float[s] +OutlineFade:float[s] +GlowScale:float[s] +OutlineColor:Color[s] +OutlineCoreColor:Color[s] +AnticipateTicks:int[s] +AnticipateDeg:float[s] +AnticipateTiltDeg:float[s] +SwingTicks:int[s] +SwingEasePower:float[s] +OvershootDeg:float[s] +RecoverTicks:int[s] +SweepHitSteps:int[s] +SwingHighDeg:float[s] +SwingLowDeg:float[s] +TiltHighDeg:float[s] +TiltLowDeg:float[s] +HitShake:float[s] +FlashTicks:int[s] +PixelSize:float[s] +TrailLifeTicks:int[s] +TailLifeScale:float[s] +TrailRails:int[s] +TrailExpandBy:float[s] +TrailInner:float[s] +TrailOpacity:float[s] +TrailSamplesPerTick:int[s] +CoreColor:Color[s] +BlueColor:Color[s] +DeepColor:Color[s] +TrailLagTicks:float[s] +TrailLengthScale:float[s] +ShowBladeDebug:bool[s] +DrawWindowTicks:int[s] +DrawSampleSpacing:float[s] +DrawMinPathPixels:float[s] +DrawMaxStrokes:int[s] +DrawSimplifyTolerance:float[s] +DrawLineWidth:float[s] +DrawRingMax:float[s] +DrawRingMin:float[s] +DrawWarnFraction:float[s] +IaidoDrawTicks:int[s] +IaidoRecoverTicks:int[s] +SlashGapTicks:int[s] +SlashSpeed:float[s] +SlashMinTravelTicks:int[s] +SlashMaxTravelTicks:int[s] +SlashEasePower:float[s] +SlashDamageMultiplier:float[s] +SlashWidth:float[s] +SlashLineWidth:float[s] +SlashLingerTicks:int[s] +SlashAfterimageTicks:int[s] +SlashShake:float[s] +SlashHitShake:float[s] +SlashPitchLow:float[s] +SlashPitchHigh:float[s]

### Content/Weapons/RottedFork/
**RottedForkFx.cs** uses: CameraShake, OrbitRing, RottedForkTuning, ThrustPalette
  static class RottedForkFx
    +SpinUp(Vector2,float)[s] +Whoosh(Vector2,float)[s] +Throw(Vector2)[s] +Impale(Vector2,Vector2)[s] +Drip(Vector2)[s] +Explode(Vector2,float)[s] +Flecks(Vector2,Vector2,float,ThrustPalette)[s]
**RottedForkGlobalItem.cs** uses: HeldAim, RottedForkJavelinProjectile, RottedForkSpinProjectile, RottedForkTuning, ThrustPlayer
  class RottedForkGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +AltFunctionUse(Item,Player)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +Throw(Player,IEntitySource,int,float)[s] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**RottedForkJavelinProjectile.cs** uses: AreaEffect, CarriedNPC, DetachedTrails, RemoteHitTracker, RottedForkFx, RottedForkPlayer, RottedForkTuning, SegmentHit, SpearPose, SweepTrail, ThrustFx, ThrustPalette, ThrustPlayer, ThrustTrailStyle
  class RottedForkJavelinProjectile : ModProjectile
    +Texture:string[ov] +OnSpawn(IEntitySource)[ov] +AI()[ov] +Colliding(Rectangle,Rectangle)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +SendExtraAI(BinaryWriter)[ov] +ReceiveExtraAI(BinaryReader)[ov] +OnTileCollide(Vector2)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**RottedForkPlayer.cs** uses: RottedForkGlobalItem, RottedForkJavelinProjectile, RottedForkSpinProjectile, ThrustPlayer
  class RottedForkPlayer : ModPlayer
    +AutoThrow:bool +PreUpdate()[ov]
**RottedForkSpinProjectile.cs** uses: DetachedTrails, HeldAim, RemoteHitTracker, RottedForkFx, RottedForkTuning, SegmentHit, SpearPose, SweepTrail, ThrustFx, ThrustPalette, ThrustPlayer, ThrustTrailStyle
  class RottedForkSpinProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +OnHitNPC(NPC,HitInfo,int)[ov] +OnKill(int)[ov] +PreDraw(ref Color)[ov]
**RottedForkTuning.cs** uses: ThrustPalette
  static class RottedForkTuning
    +SpinSpeed:float[s] +RampTicks:int[s] +StartSpeedFraction:float[s] +WindDownTicks:int[s] +MinSpinTicks:int[s] +CooldownTicks:int[s] +DamageMultiplier:float[s] +HitCooldownTicks:int[s] +MinHitSpeed:float[s] +HitWidth:float[s] +SpearScale:float[s] +JointOffset:float[s] +JavelinSpeed:float[s] +JavelinLifeTicks:int[s] +JavelinDamageMultiplier:float[s] +JavelinHitWidth:float[s] +JavelinCooldownTicks:int[s] +MaxImpaled:int[s] +MaxCarrySize:float[s] +CarryFollow:float[s] +CarryFrontGap:float[s] +CarryGap:float[s] +BlastRadius:float[s] +BlastDamageMultiplier:float[s] +CarriedBlastMultiplier:float[s] +BlastKnockback:float[s] +CarriedKnockback:float[s] +BlastShake:float[s] +TrailLifeTicks:int[s] +TrailRails:int[s] +TrailOpacity:float[s] +TrailInnerFraction:float[s] +PixelSize:float[s] +JavelinTrailTaper:float[s] +Palette:ThrustPalette[s]

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

### Content/Weapons/Spear/
**SpearGlobalItem.cs** uses: HeldAim, SpearThrustProjectile, SpearTuning, ThrustPlayer
  class SpearGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**SpearThrustProjectile.cs** uses: HeldSpearProjectile, SpearTuning, ThrustChain, ThrustPalette
  class SpearThrustProjectile : HeldSpearProjectile
    #ItemType:int[ov] #Chain:ThrustChain[ov] #Reach:float[ov] #Palette:ThrustPalette[ov] #SpearScale:float[ov] #GripFraction:float[ov] #HitWidth:float[ov] #TrailLifeTicks:int[ov] #TrailRails:int[ov] #TrailOpacity:float[ov] #PixelSize:float[ov]
**SpearTuning.cs** uses: ThrustChain, ThrustPalette, ThrustTimeline
  static class SpearTuning
    +Thrusts:int[s] +WindupTicks:int[s] +ThrustTicks:int[s] +HoldTicks:int[s] +RecoverTicks:int[s] +PullBack:float[s] +EasePower:float[s] +GapTicks:int[s] +SpreadDeg:float[s] +CooldownTicks:int[s] +DamageMultiplier:float[s] +Chain:ThrustChain[s] +Reach:float[s] +SpearScale:float[s] +GripFraction:float[s] +HitWidth:float[s] +TrailLifeTicks:int[s] +TrailRails:int[s] +TrailOpacity:float[s] +PixelSize:float[s] +Palette:ThrustPalette[s]

### Content/Weapons/Starfury/
**StarfuryBlastProjectile.cs** uses: CameraShake, StarfuryFx, StarfuryTuning
  class StarfuryBlastProjectile : ModProjectile
    +Texture:string[ov] +AI()[ov] +Colliding(Rectangle,Rectangle)[ov] +ModifyHitNPC(NPC,ref HitModifiers)[ov] +PreDraw(ref Color)[ov]
**StarfuryFx.cs**
  static class StarfuryFx
    +PlaySwing(Vector2,bool)[s] +PlayCall(Vector2)[s] +PlayStarImpact(Vector2)[s] +PlayBat(Vector2,int)[s] +PlayBounce(Vector2)[s] +CallGather(Vector2)[s] +CallBurst(Vector2)[s] +SlashDust(Vector2,Vector2)[s] +BladeSparkle(Vector2,Vector2)[s] +HitSpark(Vector2)[s] +StarTrail(Vector2,Vector2,int)[s] +StarImpact(Vector2,int)[s] +BatBurst(Vector2,int)[s] +BounceBurst(Vector2,int)[s] +ChargeHit(Vector2,int)[s] +ChargeUp(Vector2,int)[s] +PlayChargeUp(Vector2,int)[s] +ChargeRelease(Vector2,int)[s] +ChargeSparkle(Vector2,Vector2,int)[s] +BlastBurst(Vector2)[s] +PlayBlast(Vector2)[s]
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
**StarfuryTrail.cs** uses: StarfuryTuning, SweepFade, SweepTrailStyle
  class StarfurySweepTrailStyle : SweepTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Rails:int[ov] +Fade:SweepFade[ov] +TailLifeScale:float[ov] +GetColor(float,float)[ov]
**StarfuryTuning.cs**
  static class StarfuryTuning
    +BladeLength:float[s] +SwordScale:float[s] +AnticipateTicks:int[s] +AnticipateDeg:float[s] +AnticipateTiltDeg:float[s] +SwingTicks:int[s] +SwingEasePower:float[s] +OvershootDeg:float[s] +RecoverTicks:int[s] +SweepHitSteps:int[s] +RestArmDeg:float[s] +SwingHighDeg:float[s] +SwingLowDeg:float[s] +TiltHighDeg:float[s] +TiltLowDeg:float[s] +ComboTicks:int[s] +CallLiftTicks:int[s] +CallSpawnTick:int[s] +CallTicks:int[s] +CallArmDeg:float[s] +CallTiltDeg:float[s] +CallKickDeg:float[s] +CallCooldownTicks:int[s] +CallFrontDistance:float[s] +CallTargetHeight:float[s] +CallRestArmDeg:float[s] +FlashTicks:int[s] +CallHopHeight:float[s] +CallBlockGraceTicks:int[s] +CalledStarDamageMultiplier:float[s] +CalledStarKnockbackMultiplier:float[s] +StarLifeTicks:int[s] +StarSize:int[s] +StarSpin:float[s] +AfterimageCount:int[s] +GenerationGrowth:float[s] +MaxBats:int[s] +MaxStars:int[s] +BatPadding:float[s] +BatGraceTicks:int[s] +BatTargetRange:float[s] +BatPrefersBosses:bool[s] +LaunchSpeed(int)[s] +LaunchDamageMultiplier(int)[s] +LaunchKnockbackMultiplier:float[s] +LaunchLifeTicks:int[s] +BounceFrontDistance:float[s] +BounceTargetHeight:float[s] +BounceSpeedTarget:float[s] +BounceMinTicks:int[s] +BounceMaxTicks:int[s] +BounceLead:float[s] +ArcMaxFallSpeed:float[s] +Gravity:float[s] +MaxFallSpeed:float[s] +BounceImmunityTicks:int[s] +BouncedLifeTicks:int[s] +BlastSize:int[s] +BlastTicks:int[s] +BlastDamageMultiplier:float[s] +BlastKnockbackMultiplier:float[s] +BlastShake:float[s] +HitsPerLevel:int[s] +MaxLevel:int[s] +MaxHits:int[s] +LevelOf(int)[s] +PixelSize:float[s] +TrailLifeTicks:int[s] +TrailOpacityLow:float[s] +TrailOpacityMid:float[s] +TrailOpacityHigh:float[s] +TrailOpacityAt(float)[s] +TailLifeScale:float[s] +TrailRails:int[s] +TrailExpandBy:float[s] +TrailInnerLow:float[s] +TrailInnerMid:float[s] +TrailInnerHigh:float[s] +TrailInnerAt(float)[s] +TrailSamplesPerTick:int[s] +GoldColor:Color[s] +PinkColor:Color[s] +CoreColor:Color[s] +GenerationColor(int)[s]

### Content/Weapons/StormSpear/
**StormSpearFx.cs** uses: ArcBolt, ArcBoltStyle, StormSpearTuning, ThrustPalette
  static class StormSpearFx
    +DashStart(Vector2,Vector2)[s] +BodyTrail(Vector2,Vector2,Vector2)[s] +Zap(Vector2,Vector2,bool)[s]
**StormSpearGlobalItem.cs** uses: HeldAim, StormSpearThrustProjectile, StormSpearTuning, ThrustPlayer
  class StormSpearGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**StormSpearThrustProjectile.cs** uses: Afterimage, HeldSpearProjectile, PlayerDash, StormSpearFx, StormSpearTuning, ThrustChain, ThrustPalette
  class StormSpearThrustProjectile : HeldSpearProjectile
    #ItemType:int[ov] #Chain:ThrustChain[ov] #Reach:float[ov] #Palette:ThrustPalette[ov] #SpearScale:float[ov] #GripFraction:float[ov] #HitWidth:float[ov] #TrailLifeTicks:int[ov] #TrailRails:int[ov] #TrailOpacity:float[ov] #PixelSize:float[ov] #ThrustPitch(int)[ov] #OnStrikeStart(int)[ov] #OnTipStep(Vector2,Vector2,float)[ov] #OnTipHit(NPC,HitInfo,int)[ov]
**StormSpearTuning.cs** uses: DashSettings, ThrustChain, ThrustPalette, ThrustTimeline
  static class StormSpearTuning
    +DashSpeed:float[s] +DashTicks:int[s] +GrazeCos:float[s] +ImmuneTicks:int[s] +ImmuneTailTicks:int[s] +DamageMultiplier:float[s] +ElectrifiedTicks:int[s] +WindupTicks:int[s] +HoldTicks:int[s] +RecoverTicks:int[s] +PullBack:float[s] +EasePower:float[s] +CooldownTicks:int[s] +Chain:ThrustChain[s] +Dash:DashSettings[s] +Reach:float[s] +SpearScale:float[s] +GripFraction:float[s] +HitWidth:float[s] +TrailLifeTicks:int[s] +TrailRails:int[s] +TrailOpacity:float[s] +PixelSize:float[s] +BoltLifeTicks:int[s] +BoltWidth:float[s] +BoltJag:float[s] +BoltCore:Color[s] +BoltGlow:Color[s] +Palette:ThrustPalette[s]

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
**SunfuryFx.cs** uses: CameraShake, SunfuryTuning
  static class SunfuryFx
    +ChargeGather(Vector2,int,float)[s] +SpinEmbers(Vector2,Vector2,int,float)[s] +TierComplete(Vector2,int)[s] +ThrowWhoosh(Vector2,int)[s] +ApexJingle(Vector2,int)[s] +Caught(Vector2,int)[s] +Fizzle(Vector2)[s] +LockedChain(Vector2)[s] +Mistimed(Vector2)[s] +CatchHead(Vector2)[s] +FlightDust(Vector2,Vector2,int)[s] +SpecialLaunch(Vector2)[s] +HitSparks(Vector2,int)[s] +SunBlast(Vector2,float)[s] +DrawDisc(Vector2,float,Color,float)[s]
**SunfuryGlobalItem.cs** uses: Phase, SunfuryFlailProjectile
  class SunfuryGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**SunfuryTrail.cs** uses: PixelTrailStyle, SunfuryTuning
  class SunfuryTrailStyle : PixelTrailStyle
    +Grid:float[ov] +LifeTicks:int[ov] +Layers:int[ov] +Jitter:float[ov] +GetWidth(int,float)[ov] +GetColor(int,float)[ov]
**SunfuryTuning.cs**
  static class SunfuryTuning
    +ChargeTicks(int)[s] +WindowEarlyGraceTicks:int[s] +WindowLateGraceTicks:int[s] +MinReach:float[s] +MaxReach:float[s] +EllipseWidthRatio:float[s] +ThrowSpeed(int)[s] +ReleaseBlendTicks:int[s] +MinHalfTicks:int[s] +TierDamageMultiplier(int)[s] +HitCooldownTicks:int[s] +HeadSize:float[s] +BurnTicks:int[s] +SpinRadiusMin:float[s] +SpinRadiusMax:float[s] +SpinRadiusSmoothing:float[s] +SpinOmegaStart:float[s] +SpinOmegaEnd:float[s] +SpecialSpeed:float[s] +SpecialMinReach:float[s] +SpecialMaxReach:float[s] +SpecialStopsOnTiles:bool[s] +SpecialPathDamageMultiplier:float[s] +SpecialHangTicks:int[s] +RetractSpeed:float[s] +CatchDistance:float[s] +FailRetractSpeed:float[s] +BlastRadius:float[s] +BlastDamageMultiplier:float[s] +BlastKnockbackMultiplier:float[s] +BlastLifeTicks:int[s] +BlastBurnTicks:int[s] +ShakeStrength:float[s] +PixelSize:float[s] +TrailLifeTicks:int[s] +TrailWidth(int)[s] +TrailJitter:float[s] +AfterimageTicks:int[s] +FlashTicks:int[s] +GlowColor(int)[s] +HotColor(int)[s] +ChainColor:Color[s] +ChainLightColor:Color[s]

### Content/Weapons/Swordfish/
**SwordfishFx.cs**
  static class SwordfishFx
    +RageBurst(Vector2)[s] +RageTip(Vector2,Vector2)[s]
**SwordfishGlobalItem.cs** uses: HeldAim, LiquidState, SwordfishThrustProjectile, SwordfishTuning, ThrustPlayer
  class SwordfishGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**SwordfishThrustProjectile.cs** uses: HeldSpearProjectile, LiquidState, Mode, SwordfishFx, SwordfishTuning, ThrustChain, ThrustPalette
  class SwordfishThrustProjectile : HeldSpearProjectile
    #ItemType:int[ov] #Chain:ThrustChain[ov] #Reach:float[ov] #Palette:ThrustPalette[ov] #SpearScale:float[ov] #GripFraction:float[ov] #HitWidth:float[ov] #TrailLifeTicks:int[ov] #TrailRails:int[ov] #TrailOpacity:float[ov] #PixelSize:float[ov] #ShouldContinue(int)[ov] #ThrustPitch(int)[ov] #OnThrustStart(int)[ov] #OnTipStep(Vector2,Vector2,float)[ov]
**SwordfishTuning.cs** uses: ThrustChain, ThrustPalette, ThrustTimeline
  static class SwordfishTuning
    +Thrusts:int[s] +WindupTicks:int[s] +ThrustTicks:int[s] +HoldTicks:int[s] +RecoverTicks:int[s] +PullBack:float[s] +EasePower:float[s] +GapTicks:int[s] +SpreadDeg:float[s] +CooldownTicks:int[s] +DamageMultiplier:float[s] +Chain:ThrustChain[s] +RageWindupTicks:int[s] +RageThrustTicks:int[s] +RageHoldTicks:int[s] +RageRecoverTicks:int[s] +RagePullBack:float[s] +RageSpreadDeg:float[s] +RageCooldownTicks:int[s] +RageDamageMultiplier:float[s] +RageChain:ThrustChain[s] +Reach:float[s] +SpearScale:float[s] +GripFraction:float[s] +HitWidth:float[s] +TrailLifeTicks:int[s] +TrailRails:int[s] +TrailOpacity:float[s] +PixelSize:float[s] +Palette:ThrustPalette[s] +RagePalette:ThrustPalette[s]

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

### Content/Weapons/Trident/
**TridentFx.cs** uses: BounceMath, CameraShake, ThrustPalette, TileSound, TridentTuning
  static class TridentFx
    +DashStart(Vector2,Vector2,bool)[s] +DashBody(Vector2,Vector2,bool)[s] +Bounce(Vector2,Vector2,bool)[s]
**TridentGlobalItem.cs** uses: HeldAim, LiquidState, ThrustPlayer, TridentThrustProjectile, TridentTuning
  class TridentGlobalItem : GlobalItem
    +AppliesToEntity(Item,bool)[ov] +CanUseItem(Item,Player)[ov] +Shoot(Item,Player,EntitySource_ItemUse_WithAmmo,Vector2,Vector2,int,int,float)[ov] +ModifyTooltips(Item,List<TooltipLine>)[ov]
**TridentPlayer.cs** uses: LiquidState, TridentTuning
  class TridentPlayer : ModPlayer
    +PostUpdateEquips()[ov]
**TridentThrustProjectile.cs** uses: Afterimage, BounceMath, HeldSpearProjectile, Mode, PlayerDash, ThrustChain, ThrustPalette, TridentFx, TridentTuning
  class TridentThrustProjectile : HeldSpearProjectile
    #ItemType:int[ov] #Chain:ThrustChain[ov] #Reach:float[ov] #Palette:ThrustPalette[ov] #SpearScale:float[ov] #GripFraction:float[ov] #HitWidth:float[ov] #TrailLifeTicks:int[ov] #TrailRails:int[ov] #TrailOpacity:float[ov] #PixelSize:float[ov] #TrailTaper:float[ov] #ThrustPitch(int)[ov] #OnStrikeStart(int)[ov] #OnTipStep(Vector2,Vector2,float)[ov] #OnTipHit(NPC,HitInfo,int)[ov]
**TridentTuning.cs** uses: DashSettings, ThrustChain, ThrustPalette, ThrustTimeline
  static class TridentTuning
    +DashSpeed:float[s] +DashTicks:int[s] +LiquidDashMultiplier:float[s] +LiquidDashTicks:int[s] +BounceSpeed:float[s] +HitStopTicks:int[s] +BounceTicks:int[s] +BounceDrag:float[s] +GrazeCos:float[s] +BounceShake:float[s] +DamageMultiplier:float[s] +WindupTicks:int[s] +HoldTicks:int[s] +RecoverTicks:int[s] +PullBack:float[s] +EasePower:float[s] +CooldownTicks:int[s] +Chain:ThrustChain[s] +LiquidChain:ThrustChain[s] +Dash:DashSettings[s] +LiquidDash:DashSettings[s] +BuffRefreshTicks:int[s] +Reach:float[s] +SpearScale:float[s] +GripFraction:float[s] +HitWidth:float[s] +TrailLifeTicks:int[s] +TrailRails:int[s] +TrailOpacity:float[s] +PixelSize:float[s] +TrailTaper:float[s] +Palette:ThrustPalette[s]

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
- Content/Sounds/: ElectricEel.wav
- Effects/: DarkFlameOrb.fx, PixelGlow.fx; PixelShared.fxh; DarkFlameOrb.xnb, PixelGlow.xnb
- Localization/: en-US_Mods.IsmailsWeaponReworks.hjson
- Properties/: launchSettings.json
- docs/: ROADMAP.md
- tools/: bisect_shader.ps1, check_shader.ps1, check_shader_dx9.ps1, compile_shader.ps1, flatten_shader.ps1, patch_engine.ps1
