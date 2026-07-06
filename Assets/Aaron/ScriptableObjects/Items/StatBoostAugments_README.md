# Stat Boost Augments

Create augments in Unity: **Right-click → Create → Scriptable Objects → Stat Boost**, or run **Henry → Generate Stat Boost Augments** to regenerate the default tiered set.

## Tier value targets (starting point — tune in Inspector)

| Tier | Typical % stats | Damage multiplier | Notes |
|------|-----------------|-------------------|-------|
| **Bronze (Low)** | ~10 | +10% damage | |
| **Silver (Medium)** | ~30 | +30% damage | |
| **Gold (High)** | ~50 | +50% damage | |
| **Diamond (Elite)** | Multi-stat / trade-offs | — | Element gear upgrades live here too |

Element gear upgrades (`up_fire_*`, etc.) are **Diamond** — special upgrade effects, not plain stat boosts.

Regenerate defaults: **Henry → Generate Stat Boost Augments** (also sets element upgrades to Diamond and reloads **AugmentCatalog**).

**Instant heal augments** use `InstantHealLoadableStoreItem` (one-shot % max HP on pick; not stored in inventory).

## Value meaning by kind

| Stat Boost Kind | Value means | Example (positive / negative) |
|-----------------|-------------|--------------------------------|
| MoveSpeed | Percent added | 10 = +10%, -3 = -3% |
| DamageMultiplier | Percent added to all base damage | 10 = +10%, -5 = -5% |
| MaxHp | Percent added | 10 = +10%, -5 = -5% |
| RangedDamage | Percent added to ranged multiplier | 5 = +5%, -10 = -10% |
| ProjectileSpeed | Percent added | 10 = +10% |
| UltimateCharge | Percent faster | 10 = 10% more points |
| Lifesteal | Percent of damage dealt as heal | 2 = 2% |
| Regen | Percent of max HP per second | 0.5 = 0.5% |

You can use **multiple stat entries per augment** (e.g. one item that gives +10% damage and -5% move speed). Change entries and display text on the ScriptableObject anytime; no code changes needed.

---

## Unity setup (required for stat boosts to work)

**1. Add PlayerStatModifiers to the scene**
- In the scene that runs at game start (the one with **GameInitializer**), create an empty GameObject (or use an existing manager object).
- Add the **PlayerStatModifiers** component to it (Add Component → search "Player Stat Modifiers").
- In the **GameInitializer** object, find the **Game Components** list and add this **PlayerStatModifiers** component to the list (drag the same GameObject, or use the object picker and select the component).

**2. Create Stat Boost assets**
- In Project, go to **Assets/Aaron/ScriptableObjects/Items** (or your items folder).
- Right‑click → **Create → Scriptable Objects → Stat Boost**.
- Name the asset (e.g. Executioner). Set **Stat Boosts** (e.g. one entry: Kind = DamageMultiplier, Value = 50), **Display Name**, **Description**, **Cost**, **Icon**, **Quality Tier**. The **Id** field updates automatically.

**3. Add stat boosts to the augments catalog**
- Select your augments catalog asset (**AugmentCatalog**).
- Set **Folder To Load From** to the folder that contains your Stat Boost assets (e.g. the same Items folder).
- Click **Load** so the catalog includes the new Stat Boost assets.

If you skip step 1, damage (and other stats) from purchased augments will not apply. Step 2 and 3 are needed for Executioner (and other stat boosts) to appear in the shop and apply when chosen.

---

## Default augment set

Run **Henry → Generate Stat Boost Augments** for the full tiered roster (8 Bronze / 8 Silver / 8 Gold / 10 Diamond stat boosts). Damage boosts use **DamageMultiplier** (+10% / +30% / +50% by tier). Combo duration augments are not used.

### Legacy reference (superseded by generator)

The tables below are outdated examples; prefer regenerating from `StatBoostAugmentGenerator.cs`.

### Single-stat (one entry each)

| Display Name | Description | Stat Boosts (entries) | Cost |
|--------------|-------------|------------------------|------|
| **Swift Step** | Move 5% faster. | MoveSpeed: 5 | 80 |
| **Wind Runner** | +10% movement speed. | MoveSpeed: 10 | 150 |
| **Featherweight** | +15% move speed. | MoveSpeed: 15 | 220 |
| **Heavy Edge** | +2 base melee damage. | BaseDamage: 2 | 100 |
| **Tempered Steel** | +4 damage to your strikes. | BaseDamage: 4 | 180 |
| **Executioner** | +6 base damage. | BaseDamage: 6 | 260 |
| **Vitality** | +10% max HP. | MaxHp: 10 | 90 |
| **Stout Heart** | +20% max health. | MaxHp: 20 | 170 |
| **Titan's Blood** | +30% max HP. | MaxHp: 30 | 250 |
| **Ranged Edge** | Ranged attacks deal 5% more damage. | RangedDamage: 5 | 85 |
| **Sharpshooter** | +10% ranged damage. | RangedDamage: 10 | 160 |
| **Sniper's Mark** | +15% ranged damage. | RangedDamage: 15 | 240 |
| **Quick Release** | Projectiles fly 10% faster. | ProjectileSpeed: 10 | 70 |
| **Overcharged Shot** | +20% projectile speed. | ProjectileSpeed: 20 | 140 |
| **Lightning Cast** | +30% projectile speed. | ProjectileSpeed: 30 | 210 |
| **Combo Flow** | Combo window lasts 0.5s longer. | ComboDuration: 0.5 | 95 |
| **Rhythm Keeper** | +1s to combo countdown. | ComboDuration: 1 | 175 |
| **Unbroken Chain** | +1.5s combo duration. | ComboDuration: 1.5 | 255 |
| **Ultimate Spark** | Ultimate charges 10% faster. | UltimateCharge: 10 | 100 |
| **Fury Accumulation** | +20% ultimate charge rate. | UltimateCharge: 20 | 190 |
| **Overdrive** | +30% ultimate charge gain. | UltimateCharge: 30 | 280 |
| **Vampiric Touch** | Heal for 2% of damage dealt. | Lifesteal: 2 | 120 |
| **Blood Pact** | 4% of damage dealt returns as HP. | Lifesteal: 4 | 200 |
| **Soul Leech** | 6% lifesteal. | Lifesteal: 6 | 290 |
| **Passive Heal** | Regenerate 0.25% of max HP per second. | Regen: 0.25 | 75 |
| **Steady Recovery** | 0.5% max HP per second regen. | Regen: 0.5 | 145 |
| **Phoenix Ash** | 0.75% max HP per second. | Regen: 0.75 | 215 |

### Multi-stat & trade-offs (two or more entries; negative = downside)

| Display Name | Description | Stat Boosts (entries) | Cost |
|--------------|-------------|------------------------|------|
| **Berserker** | More damage, slower movement. | BaseDamage: 4, MoveSpeed: -5 | 140 |
| **Glass Cannon** | Big damage, less HP. | BaseDamage: 6, MaxHp: -15 | 200 |
| **Tank** | More HP, slower and less damage. | MaxHp: 25, MoveSpeed: -8, BaseDamage: -2 | 180 |
| **Swift Striker** | Faster and stronger melee. | MoveSpeed: 8, BaseDamage: 2 | 220 |
| **Ranged Specialist** | Ranged damage and projectile speed. | RangedDamage: 10, ProjectileSpeed: 15 | 250 |
| **Combo Rush** | Longer combo window, faster ultimate. | ComboDuration: 0.5, UltimateCharge: 15 | 230 |
| **Vampire** | Lifesteal and a bit of regen. | Lifesteal: 3, Regen: 0.25 | 240 |
| **Brittle Edge** | High damage, less max HP. | BaseDamage: 5, MaxHp: -10 | 160 |
| **Turtle** | Much more HP, less move speed. | MaxHp: 30, MoveSpeed: -10 | 220 |
| **Haste Penalty** | Very fast, less damage. | MoveSpeed: 20, BaseDamage: -3 | 200 |
