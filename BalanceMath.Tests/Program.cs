using System;
using ValheimLegends;

static void Near(float actual, float expected, float tolerance = 0.02f)
{
    if (MathF.Abs(actual - expected) > tolerance)
        throw new Exception($"Expected {expected}, got {actual}");
}

foreach (var (level, power) in new[] { (1f, 4.9f), (10f, 13f), (50f, 49f), (100f, 94f) })
    Near(VL_BalanceMath.ReferencePower(level), power);
foreach (var (value, modifier) in new[] { (0f, .75f), (50f, 1f), (100f, 1.25f) })
    Near(VL_BalanceMath.Progression(value), modifier);
Near(VL_BalanceMath.Progression(-10f), .75f);
Near(VL_BalanceMath.Progression(120f), 1.25f);
Near(VL_BalanceMath.Cooldown(30f, 1f, 0f), 30f);
Near(VL_BalanceMath.Cooldown(30f, 1f, 50f), 22.5f);
Near(VL_BalanceMath.Cooldown(30f, 1f, 100f), 15f);
Near(VL_BalanceMath.Cost(50f, 1f, 100f), 25f);
Near(VL_BalanceMath.Magic(100f, 50f, 50f, .8f), 112.8f);
Near(VL_BalanceMath.Magic(100f, 0f, 50f, 50f, .4f), 37.6f);
Near(100f * VL_BalanceMath.Physical(50f, 50f, 1.6f), 160f);
Near(VL_BalanceMath.Physical(50f, 50f, .3f * 5f), 1.5f);
Near(VL_BalanceMath.Magic(100f, 0f, 50f, 50f, .22f * 10f), 206.8f);
// Representative ability packets at L100, neutral school/secondary and no Epic INT bonus.
foreach (var (name, coefficient) in new[]
{
    ("Fireball", .8f), ("Meteor", 2f), ("Ice Shard", .4f),
    ("Sanctify Fire+Blunt", 1.1f), ("Sanctify Spirit", 1.1f),
    ("Spirit Shock", .75f), ("Spirit Drain tick", .1f),
    ("Blizzard shard", .1f), ("Root Defender", .4f),
    ("Drusquito", .22f), ("Replica", .25f), ("Shadow Wolf", .3f)
})
    Near(VL_BalanceMath.Magic(100f, 0f, 50f, 50f, coefficient), 94f * coefficient);
Near(VL_BalanceMath.Magic(100f, 0f, 50f, 50f, .4f) * 3f, 112.8f); // Frozen shard
foreach (var (name, coefficient) in new[]
{
    ("Dash", 1f), ("Backstab", 1.6f), ("Execute", 1.25f),
    ("Power Shot", 1.15f), ("Monk unarmed", 1.35f),
    ("Poison Bomb contact", .2f)
})
    Near(10f * VL_BalanceMath.Physical(50f, 50f, coefficient), 10f * coefficient);
Near(10f * VL_BalanceMath.Physical(50f, 50f, 1.25f) * 2f, 25f); // Execute under 20% HP
Near(VL_BalanceMath.Physical(50f, 50f, .3f), .3f); // one Chi
Near(VL_BalanceMath.Magic(100f, 0f, 50f, 50f, .22f * 5f), 103.4f); // Guard Charges

foreach (var (baseHeal, expected) in new[] { (5f, 48.48f), (15f, 145.43f), (12f, 116.34f), (2f, 19.39f), (1f, 9.70f) })
{
    Near(VL_BalanceMath.Heal(100f, 50f, 50f, baseHeal, 1f), expected);
    Near(VL_BalanceMath.Heal(100f, 0f, 0f, baseHeal, 1f), expected * .5625f);
    Near(VL_BalanceMath.Heal(100f, 100f, 100f, baseHeal, 1f), expected * 1.5625f);
}
Near(VL_BalanceMath.Heal(100f, 50f, 50f, 12f, 1f) * .7f * .7f, 57.01f);
Near(VL_BalanceMath.Heal(100f, 50f, 50f, 5f, 1f) * 8f, 387.81f);
Near(VL_BalanceMath.Heal(100f, 50f, 50f, 2f, 1f) * 20f, 387.81f);
Near(VL_BalanceMath.Heal(100f, 50f, 50f, 15f, 1f) * 4f, 581.72f);
Near(VL_BalanceMath.Heal(100f, 50f, 50f, 12f, 1f) * 4f, 465.38f);
Near(VL_BalanceMath.Heal(100f, 50f, 50f, 1f, 1f) * 30f, 290.86f);
Console.WriteLine("Balance formulas OK");
