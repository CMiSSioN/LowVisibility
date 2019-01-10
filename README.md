# Low Visibility
This is a mod for the [HBS BattleTech](http://battletechgame.com/) game that introduces a new layer of depth into the detection mechanics of the game. These changes are largely modeled off the MaxTech guidelines, with adaptations to fit the video game format.

The mod splits detections into two types - __visual lock__ and __sensor lock__. _Visual Lock_ occurs when your unit can see a target with the naked eye, and is influenced by the map environment and equipment.  _Sensor Lock_ occurs when your unit can identify a target using electronic sensors, which is influenced by ECM, Active Probes, and the skill of the pilot.

Each type of lock has several levels indicating a stronger identification of the target. _Visual Locks_ offer the least information, providing little more than the target's chassis (Atlas, Catapult, Missile Carrier) at long ranges. Once a unit is close enough, rough approximations of armor values and possible weapon mounts can be identified, but it's all the pilot's guesswork.

_Sensor Locks_ offer more detailed information, depending on the type of sensors that are equipped. Basic sensors provides a information like the chassis name and rough weapon composition, and randomly determines one deeper inspection of the unit - the actual weapons equipped, the current armor values, or the heat and stability of the target. __Active Probes__ go further and provide full information about the target, as per the vanilla experience. Component locations are identified, as are target buffs and debuffs.

Unfortunately _Sensor Locks_ aren't reliable, since they depend on the pilot's skill and attention. At the start of each round, every unit makes a __Sensor Check__ which determines how well their sensors function. If the roll is failed, units have to rely upon _visual locks_ that round, or _sensor locks_ from allies. On a good roll, the range of the unit's sensors is increased for that round, allowing them to detect distant enemies.

ECM components generate interference in a bubble around the unit, which makes the _sensor check_ of enemy units within that bubble more difficult. This can shutdown enemy sensors entirely, though more advanced _active probes_ can ignore this effect.

Stealth armor makes the equipped unit harder to detect, but does not generate an ECM bubble. Units attempting to detect a target with stealth armor need an active probe of equal to higher technology, or they will be completely unable to detect their foe.

This mod was specifically designed to work with [RogueTech](http://roguetech.org). Running standalone should work, but has not been tested.

## Vanilla Behavior
Before talking about what this mod does, it's helpful to understand the vanilla behavior. In vanilla [HBS BattleTech](http://battletechgame.com/), the detection model allows for opposing units to be in one of four states:
  * Undetected when outside of sensor range
  * Detected as a blob (the down-pointing arrow)
  * Detected as a specific type (the mech, turret or vehicle silhouette)
  * Visual detection with full details displayed

The range of visual detection is determined by a base spotting distance, which defaults to 300 (set in SimGameConstants). This value is modified by the spotting unit's `SpottingVisibilityMultiplier` and `SpottingVisibilityAbsolute` values. This value is then modified by the targets's `SpottingVisibilityMultiplier` and `SpottingVisibilityAbsolute` values. If this modified spotting distance is less than the distance to the target, the spotter is considered to have _LineOfSight_ to the target. Once one spotter on a team has _LineOfSight_ to a target, all other team members share that visibility.

If a target is outside of direct _LineOfSight_, then sensors are checked. The detecting unit's `SensorDistanceMultiplier` is added to any terrain `DesignMask.sensorRangeMultiplier`, while `SensorDistanceAbsolute` is added to the result. The target's `SensorSignatureModifier` modifies this value, and the final result is compared against a sensor detection range of 500 (also set in SimGameConstants). If the target is within range, it's identified as a sensor blip. The type of blip is influenced by the tactics level of the spotter.

The __Sensor Lock__ ability provides _LineOfSight_ to the target model, even if it's only within sensor range.

## Mod Behavior

This mod re-uses the HBS concepts of __visibility__ and __detection__, but applies them in new ways. __Visibility__ is when the source unit has _visual lock_ to a target, while __detection__ occurs when the source has _sensor lock_.

### Visibility

_Visual Lock_ is heavily influenced by the environment of the map. Each map contains one or more _mood_ tags that are mapped to visibility ranges. Instead of the __TODO:FIXME__ value from SimGameConstants, every unit uses this visibility range when determining how far away it can visually spot a target. Flags related to the light level set a base visibility level, while flags related to obscurement provide a multiplier to the base visibility range.

Light Level | Base Visibility | Tags
-- | -- | --
bright light | 60 * 30m | `mood_timeMorning, mood_timeNoon, mood_timeAfternoon, mood_timeDay`
dim light | 16 * 30m | `mood_timeSunrise, mood_timeSunset, mood_timeTwilight`
darkness | 6 * 30m | `mood_timeNight`

Obscurement | Visibility Multiplier | Tags
-- | -- | --
Minor | x0.5 | `mood_fogLight, mood_weatherRain, mood_weatherSnow`
Major | x0.2 | `mood_fogHeavy`

A unit on a map with _dim light_ and _minor obscurement_ would have a map vision range limit of `16 * 30m = 480m * 0.5 = 240m`. Even if the unit's SpottingVisibilityMultiplier or SpottingVisibilityAbsolute modifiers increase it's base range beyond this value, the unit would be limited to visually detecting targets no further away that 240m.

### Detection

At the start of every combat round, every unit (player or AI) makes a sensor check. This check is a random roll between 0 to 36, but is modified by the source unit's tactics skill, as per the table below. (Skills 11-13 are for [RogueTech](http://roguetech.org) elite units).

| Skill                | 1    | 2    | 3    | 4    | 5    | 6    | 7    | 8    | 9    | 10   | 11   | 12   | 13   |
| -------------------- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| Modifier             | +0   | +1   | +1   | +2   | +2   | +3   | +3   | +4   | +4   | +5   | +6   | +7   | +8   |

The result of this check determines the effective range of the unit that round:

  * A check of 0-12 is a __Failure__
  * A check of 13-19 is __Short Range__
  * A check of 20-26 is __Medium Range__
  * A check of 27+ is __Long Range__

On a failure, the unit can only detect targets it has visibility to (see below). On a success, the unit's sensors range for that round is set to a __base value__ defined by the spotter's unit type:

Type | Short | Medium | Long
-- | -- | -- | --
Mech | 300 | 450 | 550
Vehicle | 270 | 350 | 450
Turret | 240 | 480 | 720
Others | 150 | 250 | 350

This base value replaces the base sensor distance value from SimGameConstants for that model, but otherwise sensor detection ranges occur normally.

#### Jamming Modifier

When the source unit begins it's activation within an ECM bubble, its sensors will be __Jammed__ by the enemy ECM. Units in this state will have a debuff icon in the HUD stating they are _Jammed_ but the source will not be identified. Units will also display a floating notification when they begin their phase or end their movement within an ECM bubble. _Jammed_ units reduce their sensor check result by the ECM modifier of the jamming unit.  This modifier is variable, but typically will be in the -12 to -24 range. Some common values are:

ECM Component | Modifier
-- | --
IS Guardian ECM | -15
Clan Guardian ECM | -?
Angel ECM | -?
TODO | TODO

A significant deviation from the _MaxTech_ rules is that multiple ECM bubbles DO NOT apply their full strength to jammed units. Instead, for each additional ECM jamming a source a flat -5 modifier is applied. This modifier is configurable in the settings section of the `LowVisibility/mod.json` file.

#### Active Probe Modifier

If a unit is _Jammed_ and has an __Active Probe__, it applies a positive modifier to the sensor check typically in the range of +1 to +8.

Active Probe Component | Modifier
-- | --
Beagle Active Probe | +?
Bloodhound Active Probe | +?

### Identification Level

In _Low Visibility_ details about enemy units are often hidden unless you have a good sensors check or are equipped with _Active Probes_. This mod defines five __ID Levels__ which reflect a progressively more detailed knowledge of a target:

| ID Level        | Type      | Name                | Weapons                       | Components | Evasion Pips | Armor / Structure               | Heat           | Stability      | Buffs/Debuffs |
| --------------- | --------- | ------------------- | ----------------------------- | ---------- | ------------ | ------------------------------- | -------------- | -------------- | ------------- |
| Silhouette ID   | Full View | Chassis only        | Hidden                        | Hidden     | Hidden       | Percentage Only                 | Hidden         | Hidden         | Hidden        |
| Visual ID       | Full View | Chassis Only        | Type only                     | Hidden     | Shown        | Percentage Only                 | Shown          | Shown          | Hidden        |
| Sensor ID       | Blip      | Chassis and Model   | Types Always / Names Randomly | Hidden     | Shown ?      | Percentage and Max Value        | Randomly Shown | Randomly Shown | Hidden        |
| Active Probe ID | Blip      | Chassis and Variant | Shown                         | Shown      | Shown        | Percentage, Max, Current Values | Shown          | Shown          | Shown         |
| No ID           | Hidden    | Hidden              | Hidden                        | Hidden     | Hidden       | Hidden                          | Hidden         | Hidden         | Hidden        |

_Silhouette ID_ and _VisualID_ require the source unit to have __visibility__ to the target. _VisualID_ only occurs when the source is within 90m of the target, or the map visibility limit, whichever is smaller.

_Sensor ID_ and _Active Probe ID_ require the source to have __detection__ to the target.

## Jamming Details
TODO: Clean this up

* ```lv-jammer_tX_rY_mZ``` creates an ECM bubble of tier X in a circle of Y hexes (\*30 meters in game) around the source unit. The Jammer imposes a penalty of Z to any sensor checks by jammed units.
* ```lv-probe-tX_rY_mZ``` is a probe that of tier X. It adds Y hexes (\*30 meters in game) to the source unit's sensors range. It adds a bonus of Z to sensor checks made by this unit.

Probes of an equal tier penetrate jammers, to a T1 probe will penetrate a T1 jammer. This means the jammer won't add it's penalty to the source unit.

The MaxTech rulebook provides guidelines for how much of an impact ECM has on sensors. Because LowVisibility increases the dice 'roll' by a factor of 3, these modifiers are correspondingly increased. The table below lists the recommended modifier values for these scenarios:

| Sensor | MaxTech (A./G.) | Angel ECM Mod | Guardian ECM Mod |
| -- | -- | -- | -- |
| Vehicle Sensor | 7 / 6 | -21 | -16 |
| Mech Sensor | 6 / 5 | -18 | -15 |
| Beagle | 5 / 4 | -15 | -12 |
| Bloodhound | 4 / 3 | -12 | -9 |
| Clan Active Probe | 3 / 2 | -9 | -6 |

Assuming the ECM values above are used, and _Mech Sensors_ form the baseline at -18/-15 modifiers, recommended values for the active probe modifiers are:

* Beagle: +3
* Bloodhound: +6
* Clan Active Probe: +9

Probe ranges are given as additional ranges from MaxTech, while ECM ranges come from the Master Rules. Those values are:

* Guardian ECM: 6 hexes
* Angel ECM: 6 hexes
* Beagle: +4 hexes
* Bloodhound: +8 hexes
* Clan Active Probe: +7 hexes

Pull this all together, recommended tags for this mod are:

| Component | Tag |
| -- | -- |
| Guardian ECM | lv-jammer_t2_r6_m15 |
| Angel ECM | lv-jammer_t3_r6_m18 |
| Beagle Active Probe | lv-probe-t1_r4_m3 |
| Clan Active Probe | lv-probe-t1_r7_m9 |
| Bloodhound Active Probe | lv-probe-t2_r8_m6 |

## Stealth

Stealth systems reduce the chance of the unit being targeted with a visual or sensor lock.

Component | Effect
-- | --
 __Chameleon Light Polarization Shield__ | TODO
__Stealth Armor__ | TODO
__Null-Signature System__ | TODO
__Void-Signature System__ | TODO

```lv-stealth_tX``` - applies ECM to the target unit instead of generating a bubble. Won't jam enemies around the unit, but automatically defeats the sensor suit of equal or lower tier X.

```lv-stealth-range-mod_sA_mB_lC_eD``` - applies A as a modifier to any attack against the target at short range, B at medium range, C at long range, and D at extreme range.

```lv-stealth-move-mod_mX_sZ``` - applies X as a modifier, and reduces it by -1 for each Y hexes the unit moves. m3_s2 would be a +3 modifier if the unit doesn't move, +2 if it moves 1-2 hexes, +1 for 3-4 hexes, +0 otherwise.

## Worklog

### Unorganized Thoughts

- Add tags for low/light & heat vision; improves visual range & sensor range in certain conditions

- narc_tX_rY_dZ; narc defeats ecm of same 'tier', Continues to emit for durationZ, Y is radius within which anybody can benefit from the Narc boost.
- tag differs from narc in that it's only during LOS? Others wants it tied to TAG effects and be for 1-2 activations.
- jammers/stealth reduce the tier of any probes/sensors they face, but are soft counters not hard ones.
- SensorLock becomes passive ability that doubles your tactics, boosts sensor detect level by +1 (for 'what you know')

### WIP Features

- [] BUG - TrySelectActor fires multiple times. *whimper* Change to just OnActivation, but maybe a prefix?

- [] BUG - Debuff icons don't update when the sensor lock check is made, they only update after movement. Force an update somehow?

- [] BUG - Tactics skill should influence chassis name, blip type (CombatNameHelper, LineOfSightPatches)

- [] BUG - Weapons summary shown when beyond sensors range

- [] BUG - Units disappear from view in some cases. Doesn't appear related to the previous behavior, but is related.

- [] BUG - Enemies and neutral share vision currently. Probably want to split that?

- [] Component damage should eliminate ECM, AP, Stealth bonuses

- [] ```lv_shared_spotter``` tag on pilots to share LOS

- [] Implement ```lv-mimetic_m``` which represents reduces visibility if you don't move

- [] Add multiple ECM penalty to sensor check

- [] Validate functionality works with saves - career, campaign, skirmish

- [] Move SensorCheck to start of unit activation, not start of round. Generate one at the start of combat to ensure visibility can be initialized at that time.

- [] SensorLock.SensorsID should randomly provide one piece of information about the target (armor, weapons, heat, ...?)

- [] Implement Narc Effect - check status on target mech, if Effect.EffectData.tagData.tagList contains ```lv_narc_effect```, show the target even if it's outside sensors/vision range. Apply no penalty?

- [] Implement rings for vision lock range, ECM range, similar to what you have with sensor range (blue/white ring around unit)

- [] Implement stealth multi-target prohibition

### Possible Additions

- [] Consider: Sensor info / penalty driven by range bands? You get more info at short range than long?

- [] Consider: _Possible Info_ elements are randomly selected each round / actor (simulate one question note)

- [] Consider: Chance for VisualID to fail based upon a random roll

- [] Consider: Should target debuffs/buffs be shown? Feels sorta cheaty to know what the target actually has in terms of equipment buffs. Though since you can see components, you should be able to infer that...

- [] Consider: Should stealth have a visibility modifier that changes as you move move? I.e. 0.1 visibility if you don't move, 0.5 if you do, etc. (Think Chameleon shield - should make it harder to see the less you move)

- [] **CONSIDER**: Experiment with AllowRearArcSpotting:false in CombatGameConstants.json

- [] **CONSIDER**: What to do with SensorLock... certainly remove forceVisRebuild

- [] Pilot tactics should provide a better guess of weapon types for _VisualID_

### Completed Tasks

- [x] Implement stealth movement mod through modifier like others (no need to get fancy)

- [x] VisionLock and VisualID ranges should be modified by equipment.

- [x] Implement```lv-stealth-move-mod_m``` Stealth, NSS, Void System evasion by movement semantics

- [x] Visibility for players is unit specific, unless models have ```share_sensor_lock``` tags

- [x] If you have visual + sensor lock, you share your vision with allies. If you have sensor lock, and have the ```lv_share_sensor_lock``` tag, you share your sensor lock with allies.

- [x] Distinction between visual lock and sensor lock; if you have visual lock, you can see the target's silhouette. If you have sensor lock, your electronics can target them. You need both to have normal targeting modifiers.

- [x] Visibility for enemies is unit specific, unless models have ```share_sensor_lock``` tags

- [x] Implement ```lv-stealth-range-mod_s``` Stealth, NSS, Void System evasion by movement semantics

- [x] Implement Stealth, NSS, Void System sensor detection reduction

    AbstractActor relevant statistics:

    this.StatCollection.AddStatistic<float>("ToHitIndirectModifier", 0f);
    this.StatCollection.AddStatistic<float>("AccuracyModifier", 0f);
    this.StatCollection.AddStatistic<float>("CalledShotBonusMultiplier", 1f);
    this.StatCollection.AddStatistic<float>("ToHitThisActor", 0f);
    this.StatCollection.AddStatistic<float>("ToHitThisActorDirectFire", 0f);
    this.StatCollection.AddStatistic<bool>("PrecisionStrike", false);
    this.StatCollection.AddStatistic<int>("MaxEvasivePips", 4);

    AbstractActor:
    ​		public int EvasivePipsCurrent { get; set; }
    ​		public float DistMovedThisRound { get; set; }		
    ​		
### ECM Bubbles

ECM Equipment = ecm_t0
Guardian ECM = ecm_t1
Angel ECM = ecm_t2
CEWS = ecm_t3

Active Probe = activeprobe_t1
Bloodhound Probe = activeprobe_t2
CEWS = activeprobe_t3



Notes: We don't follow the tech manual, we follow MaxTech. So Angel doesn't defeat bloodhound, it just makes it harder for those probes to find units. There is no 'completely blocking' - we're simulating that at a soft level with information hiding.


### Appendix

Information from various source books used in the creation of this mod is included here for reference purposes.

* MaxTech gives visual range as
  - 1800m for daylight
  - 450 for twilight
  - 300 for rain or smoke
  - 150 for darkness
* Converted to meters, MaxTech sensor ranges are:
  * Bloodhound Active Probe: 480 / 960 / 1440
  * Clan Active Probe: 450 / 900 / 1350
  * Beagle Active Probe: 360 / 720 / 1080
  * Mech Sensor range: 240 / 480 / 720
  * Vehicle Range: 180 / 360 / 540
* ECM Equipment:
  * ECM/Guardian ECM provides a 6 hex bubble
    * Defeats Artemis IV/V, C3/C3i, Narc locks through the bubble (Tactical Rules)
  * Angel ECM - as Guardian ECM, but blocks BAP and streak
  * Power Armor ECM (Gear_PA_ECM) - As Guardian ECM
* Active Probes:
  * Light Active Probe - 3 hexes detect range
  * Beagle Active - 4 hexes detect range
  * Active Probe - 5 hexes detect range
  * Bloodhound - 8 hexes detect range. Beats Guardian ECMs.
* Combo Equipment:
  * Prototype ECM (Raven):
    * Active Probe, 3 hexes
    * ECM suite, 3 hexes
  * Watchdog System (Gear_Watchdog_EWS)
    * Standard Clan ECM Suite
    * Standard Clan Active Probe
  * Nova CEWS (Gear_Nova_CEWS) - No ECM can block CEWS except another CEWS. AP beats all other ECMs
    * Active Probe / 3 hexes
    * ECM / 3 hexes
* Stealth Armor
  * cannot be a secondary target
  * adds flat +1 at medium range, +2 at long range
  * ECM does not function, but 'Mech suffers effects as if in the radius of an enemy ECM suite
  * Requires ECM
* Null-Signature System (TacticalOperations: p336)
  * Cannot be detected by BAP, only Bloodhound, CEWS
  * Doesn't require ECM
  * Any critical shuts down the system
  * adds flat +1 at medium range, +2 at long range
  * Can stack with Chameleon
* Void Signature System (TacticalOperations: P349)
  * Can only be detected by a Bloodhound, CEWS - hidden from BAP, below
  * Requires an ECM unit
  * Any critical shuts down the system, as does losing the ECM
  * 0 movement = +3 penalty to hit
  * 1-2 hexes = +2 penalty to hit
  * 3-5 hexes = +1 penalty to hit
  * 6+ hexes = no penalty to hit
* Chameleon Light Polarization Shield (TacticalOperations: p300)
  * medium range +1 to hit, long range +2 to hit
  * Reduces visibility based upon range as well?
