# Game Design: New Features Overview

This document describes the new gameplay systems from a **game design** perspective: what the player experiences, how systems interact, and what you can tune.

---

## 1. Combo System

**What it does:** Rewards sustained aggression. Hitting and killing enemies builds a **combo** that increases a **multiplier** and earns **points**. Letting the combo drop (by not hitting anything before a countdown expires) resets the combo but not your total points for the level.

### Player experience

- **First hit** starts the combo (count = 1, multiplier = 1).
- **Each hit** increases the combo count and **resets a countdown timer**. If the timer reaches zero before the next hit, the combo **breaks** (count and multiplier reset; total points stay).
- **Kills** give bonus points and can increase the multiplier.
- **Rapid hits** (several hits within a short time window) grant extra points and can **raise the multiplier** (up to a cap). This encourages short, aggressive bursts.
- **Total points** accumulate for the whole level/round and are used for:
  - **Shop quality** – better performance → higher tier augments offered.
  - **Ultimate meter** – the same points feed into the ultimate charge.

### Design levers (ComboSystem)

- **Combo duration** – How long the player has between hits before the combo breaks. Can be extended by augments (Combo Duration).
- **Hit / kill / rapid-streak points** – Base points per hit, bonus per kill, and bonus when hitting rapidly. All are scaled by the current multiplier.
- **Rapid window & hits required** – Time window and number of hits needed to trigger the rapid-streak bonus and multiplier bump.
- **Max multiplier** – Cap on the combo multiplier.
- **Quality thresholds** – Total points needed (per level) to unlock Medium / High / Elite augment tiers in the shop.

### Interaction with other systems

- **Element switch** – Switching element (e.g. fire → ice) **resets the combo timer** but does not break the combo or change total points. Gives a small window to keep the combo alive after a swap.
- **Stat augments** – “Combo Duration” augments add extra seconds to the countdown, making it easier to maintain combos.

---

## 2. Ultimate Meter

**What it does:** A **super meter** that fills when the player earns combo points. When full, the player can use an **ultimate ability** (currently a placeholder; you can hook in a real ability later).

### Player experience

- **Filling the meter** – Every point awarded by the combo system (hits, kills, rapid streaks) also adds to the ultimate meter. The HUD shows a bar filling up.
- **Per-element tracking** – The system tracks how much each element (Physical, Fire, Ice, Lightning) contributed. Useful if you later add element-specific ultimates or visuals.
- **Ready state** – When the meter is full, the player can trigger the ultimate (e.g. a big attack or buff). After use, the meter resets and can be filled again.

### Design levers (UltimateMeter)

- **Points required for full charge** – How many combo points are needed to fill the bar once.
- **Stat augments** – “Ultimate Charge” augments make the meter fill faster (more points per combo point).

---

## 3. Combat HUD

**What it does:** Shows the player their combo status and ultimate charge in real time.

### Elements

- **Combo count** – Current hit count in the combo (resets when the combo breaks).
- **Multiplier** – Current combo multiplier (e.g. 2x, 3x). Often shown as “2x” next to the count.
- **Combo timer bar** – Visual countdown; depletes over time and refills on each hit. When it empties, the combo breaks.
- **Total points** – Points accumulated this level/round (used for shop quality and ultimate).
- **Ultimate bar** – Fills as the player earns combo points; indicates when the ultimate is ready.

Combo UI can fade in when a combo is active and fade out when it breaks, so the screen stays readable when the player isn’t in combat.

---

## 4. Augment Shop & Quality Tiers

**What it does:** Between levels (or when you open the augment shop), the player chooses one of several **augments**. The **quality** of the choices depends on how well they played (combo performance / total points).

### Player experience

- After a level (or at a designated moment), the game shows a small set of **augment choices** (e.g. three).
- The player picks one; it’s added to their “inventory” and **stats apply immediately** for the rest of the run.
- **Playing well** (high total points / strong combos) unlocks **better tiers** of augments (e.g. stronger or rarer effects).

### Quality tiers

- **Low** – Baseline augments (e.g. small stat boosts).
- **Medium** – Unlocked at a moderate points threshold.
- **High** – Unlocked at a higher threshold.
- **Elite** – Unlocked at the highest threshold.

Exact thresholds are set in **ComboSystem** (e.g. medium at 20 points, high at 50, elite at 100). You can mix stat-boost augments and other types (e.g. element upgrades) in the same catalog and filter by tier when building the choice set.

---

## 5. Stat Boost Augments

**What it does:** Augments that **permanently** change the player’s stats for the rest of the run: damage, speed, health, lifesteal, regen, combo duration, ultimate charge rate, etc. Effects apply as soon as the player picks the augment and can stack if they pick similar augments more than once.

### Stat types (from a design perspective)

| Stat | Effect on gameplay |
|------|--------------------|
| **Move Speed** | Faster or slower movement. |
| **Base Damage** | More (or less) melee/ranged damage per hit. |
| **Max HP** | Larger or smaller health pool. |
| **Ranged Damage** | Stronger or weaker ranged attacks only. |
| **Projectile Speed** | Projectiles travel faster or slower. |
| **Combo Duration** | More (or less) time between hits before the combo breaks. |
| **Ultimate Charge** | Ultimate meter fills faster or slower from combo points. |
| **Lifesteal** | Heal a % of damage dealt. |
| **Regen** | Heal a % of max HP per second over time. |

### Multi-stat and trade-offs

- A single augment can change **multiple stats** (e.g. +damage and +move speed).
- Values can be **negative** to create trade-offs (e.g. “+damage, -move speed” for a heavier, slower build).

This lets you design items that push different playstyles (glass cannon, tank, sustain, combo-focused, etc.) without new code.

### Flow

1. Player picks an augment from the shop.
2. The augment is added to their inventory (by ID).
3. **PlayerStatModifiers** reads the inventory and **re-applies** all stat changes whenever inventory changes (e.g. after each pick).
4. **Damage, movement, HP, lifesteal, regen, combo duration, and ultimate charge** all read from these modifiers, so the effect is immediate and consistent for the rest of the run.

---

## Summary: How It Fits Together

1. **Combat** → Hits and kills feed the **combo** (count, multiplier, timer) and **points**.
2. **Points** → Drive **shop quality** (which tier of augments appears) and **ultimate charge**.
3. **Augments** → Modify **stats** (damage, speed, HP, lifesteal, regen, combo time, ultimate gain).
4. **Stats** → Make the next level easier and support different builds (aggressive, defensive, combo-focused).
5. **HUD** → Surfaces combo state and ultimate charge so the player can play around them.

Together, this creates a loop: **play well → better augments → stronger stats → easier to play well again**, while the combo and ultimate add short-term goals (keep the combo, fill the ultimate) on top of survival and clearing levels.
