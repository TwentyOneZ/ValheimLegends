using System;

namespace ValheimLegends;

public static class VL_BalanceMath
{
	public static float ReferencePower(float level) => 4f + 0.9f * level;
	public static float Progression(float value) => 0.75f + 0.005f * MathF.Max(0f, MathF.Min(100f, value));
	public static float Magic(float level, float magicPercent, float school, float coefficient) =>
		ReferencePower(level) * (1f + magicPercent / 100f) * Progression(school) * coefficient;
	public static float Magic(float level, float magicPercent, float school, float secondary, float coefficient) =>
		Magic(level, magicPercent, school, coefficient) * Progression(secondary);
	public static float Physical(float school, float coefficient) => Progression(school) * coefficient;
	public static float Physical(float school, float secondary, float coefficient) => Physical(school, coefficient) * Progression(secondary);
	public static float Heal(float level, float endurance, float skill, float baseHeal, float global) =>
		baseHeal * MathF.Sqrt(ReferencePower(level)) * Progression(endurance) * Progression(skill) * global;
	public static float Cooldown(float baseCooldown, float global, float intelligence) =>
		baseCooldown * global * (1f - 0.005f * MathF.Max(0f, MathF.Min(100f, intelligence)));
	public static float Cost(float baseCost, float global, float agility) =>
		baseCost * global * (1f - 0.005f * MathF.Max(0f, MathF.Min(100f, agility)));
}
