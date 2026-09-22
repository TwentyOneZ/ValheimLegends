using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace ValheimLegends;

public static class VL_Utility
{
	public static string ModID;

	public static string Folder;

	private static int m_interactMask = LayerMask.GetMask("item", "piece", "piece_nonsolid", "Default", "static_solid", "Default_small", "character", "character_net", "terrain", "vehicle");

	private static int m_LOSMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid", "Default_small", "terrain", "vehicle");

	private static float vl_timer;

	public static float GetReferencePower(float characterLevel) => VL_BalanceMath.ReferencePower(characterLevel);

	public static float GetProgressionModifier(float value) => VL_BalanceMath.Progression(value);

	public static float GetMagicAbilityDamage(float characterLevel, float magicDamagePercent, float school, float coefficient) =>
		VL_BalanceMath.Magic(characterLevel, magicDamagePercent, school, coefficient);

	public static float GetMagicAbilityDamage(float characterLevel, float magicDamagePercent, float school, float secondary, float coefficient) =>
		VL_BalanceMath.Magic(characterLevel, magicDamagePercent, school, secondary, coefficient);

	public static float GetPhysicalAbilityMultiplier(float school, float coefficient) => VL_BalanceMath.Physical(school, coefficient);

	public static float GetPhysicalAbilityMultiplier(float school, float secondary, float coefficient) =>
		VL_BalanceMath.Physical(school, secondary, coefficient);

	public static HitData.DamageTypes GetPhysicalAbilityDamage(Player player, float school, float secondary, float coefficient, float config)
	{
		ItemDrop.ItemData weapon = player.GetCurrentWeapon();
		HitData.DamageTypes damage = weapon.GetDamage();
		// EpicMMO's GetDamage hook only scales items in the local inventory, not bare hands.
		if (!player.GetInventory().ContainsItem(weapon))
			damage.Modify(1f + EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 100f);
		damage.Modify(GetPhysicalAbilityMultiplier(school, secondary, coefficient)
			* VL_GlobalConfigs.g_DamageModifer * config);
		return damage;
	}

	public static float GetHealingPower(float characterLevel, float endurance, float healingSkill, float baseHeal) =>
		GetHealingPower(characterLevel, endurance, healingSkill, baseHeal, VL_GlobalConfigs.g_DamageModifer);

	public static float GetHealingPower(float characterLevel, float endurance, float healingSkill, float baseHeal, float globalModifier) =>
		VL_BalanceMath.Heal(characterLevel, endurance, healingSkill, baseHeal, globalModifier);

	public static float GetCooldown(float baseCooldown, float globalModifier, float intelligence) =>
		VL_BalanceMath.Cooldown(baseCooldown, globalModifier, intelligence);

	public static float GetAbilityCost(float baseCost, float globalModifier, float agility) =>
		VL_BalanceMath.Cost(baseCost, globalModifier, agility);

	public static void SetSummonDamage(GameObject summon, Player caster, float school, float secondary, float coefficient, float config)
	{
		ZNetView view = summon.GetComponent<ZNetView>();
		if (view == null || !view.IsValid() || !view.IsOwner()) return;
		ZDO zdo = view.GetZDO();
		zdo.Set("VL_SummonOwner", caster.GetZDOID());
		zdo.Set("VL_SummonDamage", GetMagicAbilityDamage(EpicMMOSystem.LevelSystem.Instance.getLevel(),
			EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage(), school, secondary, coefficient)
			* VL_GlobalConfigs.g_DamageModifer * config);
	}

	public static bool ApplySummonDamage(Character attacker, HitData hit)
	{
		ZNetView view = attacker.GetComponent<ZNetView>();
		ZDO zdo = view != null && view.IsValid() ? view.GetZDO() : null;
		if (zdo == null || zdo.GetZDOID("VL_SummonOwner") == ZDOID.None) return false;
		float total = hit.m_damage.GetTotalDamage();
		if (total > 0f) hit.m_damage.Modify(zdo.GetFloat("VL_SummonDamage", 0f) / total);
		return true;
	}

	private static float GetAttribute(EpicMMOSystem.Parameter parameter) => EpicMMOSystem.LevelSystem.Instance.getParameter(parameter);

	public static float GetZoneChargeCost => GetAbilityCost(40f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetZoneChargeCooldownTime => GetCooldown(600f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetZoneChargeCostPerUpdate => GetAbilityCost(1f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetZoneChargeSkillGain => 8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetWeakenCost => GetAbilityCost(40f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetWeakenCooldownTime => GetCooldown(60f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetWeakenSkillGain => 1.4f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetCharmCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetCharmCooldownTime => GetCooldown(60f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetCharmSkillGain => 2.6f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetMeteorPunchCost => 3f;

	public static float GetMeteorPunchCooldownTime => GetCooldown(1f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetMeteorPunchSkillGain => 4.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetPsiBoltCost => 5f;

	public static float GetPsiBoltCooldownTime => GetCooldown(1f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetPsiBoltSkillGain => 5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetFlyingKickCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetFlyingKickCooldownTime => GetCooldown(6f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetFlyingKickSkillGain => 0.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetPoisonBombCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetPoisonBombCooldownTime => GetCooldown(30f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetPoisonBombSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetBackstabCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetBackstabCooldownTime => GetCooldown(20f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetBackstabSkillGain => 2.6f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetFadeCost => GetAbilityCost(10f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetFadeCooldownTime => GetCooldown(15f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetFadeSkillGain => 1.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetSanctifyCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetSanctifyCooldownTime => GetCooldown(45f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetSanctifySkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetHealCost => GetAbilityCost(40f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetHealCostPerUpdate => GetAbilityCost(0.75f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetHealCooldownTime => GetCooldown(30f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetHealSkillGain => 1.3f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetPurgeCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetPurgeCooldownTime => GetCooldown(15f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetPurgeSkillGain => 0.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetQuickShotCost => GetAbilityCost(25f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetQuickShotCooldownTime => GetCooldown(10f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetQuickShotSkillGain => 0.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetRiposteCost => GetAbilityCost(30f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetRiposteCooldownTime => GetCooldown(6f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetRiposteSkillGain => 0.2f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetBlinkStrikeCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetBlinkStrikeCooldownTime => GetCooldown(30f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetBlinkStrikeSkillGain => 1.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetLightCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetLightCooldownTime => GetCooldown(20f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetLightSkillGain => 1.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetWarpCost => GetAbilityCost(40f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetWarpCostPerUpdate => GetAbilityCost(1f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetWarpCooldownTime => GetCooldown(6f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetWarpSkillGain => 0.2f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetReplicaCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetReplicaCooldownTime => GetCooldown(30f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetReplicaSkillGain => 1.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetForceWaveCost => GetAbilityCost(30f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetForceWaveSkillGain => 1.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetForceWaveCooldown => GetCooldown(20f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetFireballCost => GetAbilityCost(30f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetFireballCooldownTime => GetCooldown(2f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetFireballSkillGain => 1.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetMeteorCost => GetAbilityCost(30f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetMeteorCostPerUpdate => GetAbilityCost(0.25f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetMeteorCooldownTime => GetCooldown(30f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetMeteorSkillGain => 2.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetFrostNovaCost => GetAbilityCost(40f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetFrostNovaCooldownTime => GetCooldown(20f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetFrostNovaSkillGain => 1.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetBulwarkCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetBulwarkCooldownTime => GetCooldown(60f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetBulwarkSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetLeapCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetLeapCooldownTime => GetCooldown(15f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetLeapSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetStaggerCost => GetAbilityCost(40f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetStaggerCooldownTime => GetCooldown(20f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetStaggerSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetHarpoonPullCost => 1f;

	public static float GetHarpoonPullSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetHarpoonPullCooldown => GetCooldown(10f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetShieldReleaseSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetVineHookCost => GetAbilityCost(40f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetRegenerationCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetRegenerationCooldownTime => GetCooldown(60f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetRegenerationSkillGain => 2.7f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetRootCost => GetAbilityCost(30f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetRootCostPerUpdate => GetAbilityCost(0.3f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetRootCooldownTime => GetCooldown(20f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetRootSkillGain => 1.4f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetDefenderCost => GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));

	public static float GetDefenderCooldownTime => GetCooldown(120f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));

	public static float GetDefenderSkillGain => 2.7f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static bool ReadyTime => Time.time > 0.01f + vl_timer;

	public static bool Ability1_Input_Down
	{
		get
		{
			if (ValheimLegends.Ability1_Hotkey.Value == "")
			{
				return false;
			}
			if (ValheimLegends.Ability1_Hotkey_Combo.Value == "")
			{
				return Input.GetKeyDown(ValheimLegends.Ability1_Hotkey.Value.ToLower()) || Input.GetButtonDown(ValheimLegends.Ability1_Hotkey.Value.ToLower());
			}
			if ((Input.GetKeyDown(ValheimLegends.Ability1_Hotkey.Value.ToLower()) && Input.GetKey(ValheimLegends.Ability1_Hotkey_Combo.Value.ToLower())) || (Input.GetKey(ValheimLegends.Ability1_Hotkey.Value.ToLower()) && Input.GetKeyDown(ValheimLegends.Ability1_Hotkey_Combo.Value.ToLower())) || (Input.GetButtonDown(ValheimLegends.Ability1_Hotkey.Value.ToLower()) && Input.GetButton(ValheimLegends.Ability1_Hotkey_Combo.Value.ToLower())) || (Input.GetButton(ValheimLegends.Ability1_Hotkey.Value.ToLower()) && Input.GetButtonDown(ValheimLegends.Ability1_Hotkey_Combo.Value.ToLower())))
			{
				return true;
			}
			return false;
		}
	}

    public static bool Ability1_Input_Pressed
    {
        get
        {
            if (ValheimLegends.Ability1_Hotkey.Value == "")
            {
                return false;
            }
            if (ValheimLegends.Ability1_Hotkey_Combo.Value == "")
            {
                return Input.GetKey(ValheimLegends.Ability1_Hotkey.Value.ToLower()) || Input.GetButton(ValheimLegends.Ability1_Hotkey.Value.ToLower());
            }
            if ((Input.GetKey(ValheimLegends.Ability1_Hotkey.Value.ToLower()) && Input.GetKey(ValheimLegends.Ability1_Hotkey_Combo.Value.ToLower())) || (Input.GetButton(ValheimLegends.Ability1_Hotkey.Value.ToLower()) && Input.GetButton(ValheimLegends.Ability1_Hotkey_Combo.Value.ToLower())))
            {
                return true;
            }
            return false;
        }
    }

    public static bool Ability1_Input_Up
    {
        get
        {
            if (ValheimLegends.Ability1_Hotkey.Value == "")
            {
                return false;
            }
            if (ValheimLegends.Ability1_Hotkey_Combo.Value == "")
            {
                return Input.GetKeyUp(ValheimLegends.Ability1_Hotkey.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability1_Hotkey.Value.ToLower());
            }
            if (Input.GetKeyUp(ValheimLegends.Ability1_Hotkey.Value.ToLower()) || Input.GetKeyUp(ValheimLegends.Ability1_Hotkey_Combo.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability1_Hotkey.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability1_Hotkey_Combo.Value.ToLower()))
            {
                return true;
            }
            return false;
        }
    }

    public static bool Ability2_Input_Down
	{
		get
		{
			if (ValheimLegends.Ability2_Hotkey.Value == "")
			{
				return false;
			}
			if (ValheimLegends.Ability2_Hotkey_Combo.Value == "")
			{
				return Input.GetKeyDown(ValheimLegends.Ability2_Hotkey.Value.ToLower()) || Input.GetButtonDown(ValheimLegends.Ability2_Hotkey.Value.ToLower());
			}
			if ((Input.GetKeyDown(ValheimLegends.Ability2_Hotkey.Value.ToLower()) && Input.GetKey(ValheimLegends.Ability2_Hotkey_Combo.Value.ToLower())) || (Input.GetKey(ValheimLegends.Ability2_Hotkey.Value.ToLower()) && Input.GetKeyDown(ValheimLegends.Ability2_Hotkey_Combo.Value.ToLower())) || (Input.GetButtonDown(ValheimLegends.Ability2_Hotkey.Value.ToLower()) && Input.GetButton(ValheimLegends.Ability2_Hotkey_Combo.Value.ToLower())) || (Input.GetButton(ValheimLegends.Ability2_Hotkey.Value.ToLower()) && Input.GetButtonDown(ValheimLegends.Ability2_Hotkey_Combo.Value.ToLower())))
			{
				return true;
			}
			return false;
		}
	}

    public static bool Ability2_Input_Pressed
    {
        get
        {
            if (ValheimLegends.Ability2_Hotkey.Value == "")
            {
                return false;
            }
            if (ValheimLegends.Ability2_Hotkey_Combo.Value == "")
            {
                return Input.GetKey(ValheimLegends.Ability2_Hotkey.Value.ToLower()) || Input.GetButton(ValheimLegends.Ability2_Hotkey.Value.ToLower());
            }
            if ((Input.GetKey(ValheimLegends.Ability2_Hotkey.Value.ToLower()) && Input.GetKey(ValheimLegends.Ability2_Hotkey_Combo.Value.ToLower())) || (Input.GetButton(ValheimLegends.Ability2_Hotkey.Value.ToLower()) && Input.GetButton(ValheimLegends.Ability2_Hotkey_Combo.Value.ToLower())))
            {
                return true;
            }
            return false;
        }
    }

    public static bool Ability2_Input_Up
    {
        get
        {
            if (ValheimLegends.Ability2_Hotkey.Value == "")
            {
                return false;
            }
            if (ValheimLegends.Ability2_Hotkey_Combo.Value == "")
            {
                return Input.GetKeyUp(ValheimLegends.Ability2_Hotkey.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability2_Hotkey.Value.ToLower());
            }
            if (Input.GetKeyUp(ValheimLegends.Ability2_Hotkey.Value.ToLower()) || Input.GetKeyUp(ValheimLegends.Ability2_Hotkey_Combo.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability2_Hotkey.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability2_Hotkey_Combo.Value.ToLower()))
            {
                return true;
            }
            return false;
        }
    }

    public static bool Ability3_Input_Down
	{
		get
		{
			if (ValheimLegends.Ability3_Hotkey.Value == "")
			{
				return false;
			}
			if (ValheimLegends.Ability3_Hotkey_Combo.Value == "")
			{
				return Input.GetKeyDown(ValheimLegends.Ability3_Hotkey.Value.ToLower()) || Input.GetButtonDown(ValheimLegends.Ability3_Hotkey.Value.ToLower());
			}
			if ((Input.GetKeyDown(ValheimLegends.Ability3_Hotkey.Value.ToLower()) && Input.GetKey(ValheimLegends.Ability3_Hotkey_Combo.Value.ToLower())) || (Input.GetKey(ValheimLegends.Ability3_Hotkey.Value.ToLower()) && Input.GetKeyDown(ValheimLegends.Ability3_Hotkey_Combo.Value.ToLower())) || (Input.GetButtonDown(ValheimLegends.Ability3_Hotkey.Value.ToLower()) && Input.GetButton(ValheimLegends.Ability3_Hotkey_Combo.Value.ToLower())) || (Input.GetButton(ValheimLegends.Ability3_Hotkey.Value.ToLower()) && Input.GetButtonDown(ValheimLegends.Ability3_Hotkey_Combo.Value.ToLower())))
			{
				return true;
			}
			return false;
		}
	}

	public static bool Ability3_Input_Pressed
	{
		get
		{
			if (ValheimLegends.Ability3_Hotkey.Value == "")
			{
				return false;
			}
			if (ValheimLegends.Ability3_Hotkey_Combo.Value == "")
			{
				return Input.GetKey(ValheimLegends.Ability3_Hotkey.Value.ToLower()) || Input.GetButton(ValheimLegends.Ability3_Hotkey.Value.ToLower());
			}
			if ((Input.GetKey(ValheimLegends.Ability3_Hotkey.Value.ToLower()) && Input.GetKey(ValheimLegends.Ability3_Hotkey_Combo.Value.ToLower())) || (Input.GetButton(ValheimLegends.Ability3_Hotkey.Value.ToLower()) && Input.GetButton(ValheimLegends.Ability3_Hotkey_Combo.Value.ToLower())))
			{
				return true;
			}
			return false;
		}
	}

	public static bool Ability3_Input_Up
	{
		get
		{
			if (ValheimLegends.Ability3_Hotkey.Value == "")
			{
				return false;
			}
			if (ValheimLegends.Ability3_Hotkey_Combo.Value == "")
			{
				return Input.GetKeyUp(ValheimLegends.Ability3_Hotkey.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability3_Hotkey.Value.ToLower());
			}
			if (Input.GetKeyUp(ValheimLegends.Ability3_Hotkey.Value.ToLower()) || Input.GetKeyUp(ValheimLegends.Ability3_Hotkey_Combo.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability3_Hotkey.Value.ToLower()) || Input.GetButtonUp(ValheimLegends.Ability3_Hotkey_Combo.Value.ToLower()))
			{
				return true;
			}
			return false;
		}
	}

    public static void RefreshAbilityIconLabels()
    {
        var list = ValheimLegends.abilitiesStatus;
        if (list == null || list.Count < 3) return;

        string[] names =
        {
                ValheimLegends.Ability1_Name,
                ValheimLegends.Ability2_Name,
                ValheimLegends.Ability3_Name
            };

        for (int i = 0; i < 3; i++)
        {
            var rt = list[i];
            if (rt == null) continue;

            // No template existe "TimeText" (hotkey/cooldown) e existe o text do nome.
            // Ent�o pegamos o TMP_Text que N�O � o TimeText.
            TMP_Text label = null;
            var texts = rt.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                if (t == null) continue;
                if (t.gameObject.name == "TimeText") continue;
                label = t;
                break;
            }

            if (label == null) continue;

            // Localize se poss�vel, sen�o escreve raw
            if (Localization.instance != null)
                label.text = Localization.instance.Localize(names[i]);
            else
                label.text = names[i];
        }
    }

    public static string GetModDataPath(this PlayerProfile profile)
	{
		return Path.Combine(Utils.GetSaveDataPath(FileHelpers.FileSource.Local), "ModData", ModID, "char_" + profile.GetFilename());
	}

	public static TData LoadModData<TData>(this PlayerProfile profile) where TData : new()
	{
		if (!File.Exists(profile.GetModDataPath()))
		{
			return new TData();
		}
		string json = File.ReadAllText(profile.GetModDataPath());
		return JsonUtility.FromJson<TData>(json);
	}

	public static void SaveModData<TData>(this PlayerProfile profile, TData data)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(profile.GetModDataPath()));
		File.WriteAllText(profile.GetModDataPath(), JsonUtility.ToJson(data));
	}

    public static Texture2D LoadTextureFromAssets(string path)
    {
        string p1 = Path.Combine(Folder, "VLAssets", path);
        if (File.Exists(p1))
        {
            byte[] array = File.ReadAllBytes(p1);
            Texture2D val = new Texture2D(1, 1);
            ImageConversion.LoadImage(val, array);
            return val;
        }

        string p2 = Path.Combine(Folder, path);
        if (File.Exists(p2))
        {
            byte[] array2 = File.ReadAllBytes(p2);
            Texture2D val2 = new Texture2D(1, 1);
            ImageConversion.LoadImage(val2, array2);
            return val2;
        }

        ZLog.LogError("[ValheimLegends] Asset texture not found: " + path + " (checked " + p1 + " and " + p2 + ")");
        return new Texture2D(1, 1);
    }


    public static bool TakeInput(Player p)
	{
		bool result = (!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !TextInput.IsVisible() && !StoreGui.IsVisible() && !InventoryGui.IsVisible() && !Menu.IsVisible() && (!TextViewer.instance || !TextViewer.instance.IsVisible()) && !Minimap.IsOpen() && !GameCamera.InFreeFly();
		if (p.IsDead() || p.InCutscene() || p.IsTeleporting())
		{
			result = false;
		}
		return result;
	}

	public static void InitiateAbilityStatus(Hud hud)
	{
		if (ValheimLegends.ClassIsValid)
		{
			float num = (float)Screen.width / 1920f;
			float num2 = (float)Screen.height / 1080f;
			float num3 = 80f * num;
			float num4 = 0f;
			float num5 = 106f * num2 + ValheimLegends.icon_Y_Offset.Value;
			float num6 = 209f * num + ValheimLegends.icon_X_Offset.Value;
			if (ValheimLegends.iconAlignment.Value.ToLower() == "vertical")
			{
				num3 = 0f;
				num4 = 100f * num2;
			}
			ValheimLegends.abilitiesStatus = new List<RectTransform>();
			ValheimLegends.abilitiesStatus.Clear();
			UnityEngine.Vector3 position = new UnityEngine.Vector3(num6 + num3, num5 + num4, 0f);
			Quaternion rotation = new Quaternion(0f, 0f, 0f, 1f);
			Transform statusEffectListRoot = hud.m_statusEffectListRoot;
			RectTransform rectTransform = UnityEngine.Object.Instantiate(hud.m_statusEffectTemplate, position, rotation, statusEffectListRoot);
			rectTransform.gameObject.SetActive(value: true);
			rectTransform.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize(global::ValheimLegends.ValheimLegends.Ability1_Name.ToString());
			ValheimLegends.abilitiesStatus.Add(rectTransform);
			position.x += num3;
			position.y += num4;
			RectTransform rectTransform2 = UnityEngine.Object.Instantiate(hud.m_statusEffectTemplate, position, rotation, statusEffectListRoot);
			rectTransform2.gameObject.SetActive(value: true);
			rectTransform2.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize(global::ValheimLegends.ValheimLegends.Ability2_Name.ToString());
			ValheimLegends.abilitiesStatus.Add(rectTransform2);
			position.x += num3;
			position.y += num4;
			RectTransform rectTransform3 = UnityEngine.Object.Instantiate(hud.m_statusEffectTemplate, position, rotation, statusEffectListRoot);
			rectTransform3.gameObject.SetActive(value: true);
			rectTransform3.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize(global::ValheimLegends.ValheimLegends.Ability3_Name.ToString());
			ValheimLegends.abilitiesStatus.Add(rectTransform3);
		}
	}

	public static void RotatePlayerToTarget(Player p)
	{
		UnityEngine.Vector3 lookDir = p.GetLookDir();
		lookDir.y = 0f;
		p.transform.rotation = UnityEngine.Quaternion.LookRotation(lookDir);
	}

	public static bool LOS_IsValid(Character hit_char, UnityEngine.Vector3 splash_center, UnityEngine.Vector3 splash_alternate = default(Vector3))
	{
		bool flag = false;
		if (VL_GlobalConfigs.ConfigStrings["vl_svr_aoeRequiresLoS"] == 0f)
		{
			return true;
		}
		if (splash_alternate == default(Vector3))
		{
			splash_alternate = splash_center + new UnityEngine.Vector3(0f, 0.2f, 0f);
		}
		if (hit_char != null)
		{
			RaycastHit hitInfo = default(RaycastHit);
			UnityEngine.Vector3 direction = hit_char.GetCenterPoint() - splash_center;
			if (Physics.Raycast(splash_center, direction, out hitInfo))
			{
				if (CollidedWithTarget(hit_char, hit_char.GetCollider(), hitInfo))
				{
					flag = true;
				}
				else
				{
					for (int i = 0; i < 8; i++)
					{
						UnityEngine.Vector3 size = hit_char.GetCollider().bounds.size;
						UnityEngine.Vector3 direction2 = hit_char.GetCenterPoint() + new UnityEngine.Vector3(size.x * ((float)UnityEngine.Random.Range(-i, i) / 6f), size.y * ((float)UnityEngine.Random.Range(-i, i) / 4f), size.z * ((float)UnityEngine.Random.Range(-i, i) / 6f)) - splash_center;
						if (Physics.Raycast(splash_center, direction2, out hitInfo) && CollidedWithTarget(hit_char, hit_char.GetCollider(), hitInfo))
						{
							flag = true;
							break;
						}
					}
				}
			}
			if (!flag && splash_alternate != default(Vector3) && splash_alternate != splash_center)
			{
				UnityEngine.Vector3 direction3 = hit_char.GetCenterPoint() - splash_alternate;
				if (Physics.Raycast(splash_alternate, direction3, out hitInfo))
				{
					if (CollidedWithTarget(hit_char, hit_char.GetCollider(), hitInfo))
					{
						flag = true;
					}
					else
					{
						for (int j = 0; j < 8; j++)
						{
							UnityEngine.Vector3 size2 = hit_char.GetCollider().bounds.size;
							UnityEngine.Vector3 direction4 = hit_char.GetCenterPoint() + new UnityEngine.Vector3(size2.x * ((float)UnityEngine.Random.Range(-j, j) / 6f), size2.y * ((float)UnityEngine.Random.Range(-j, j) / 4f), size2.z * ((float)UnityEngine.Random.Range(-j, j) / 6f)) - splash_alternate;
							if (Physics.Raycast(splash_alternate, direction4, out hitInfo) && CollidedWithTarget(hit_char, hit_char.GetCollider(), hitInfo))
							{
								flag = true;
								break;
							}
						}
					}
				}
			}
		}
		return flag;
	}

	private static bool CollidedWithTarget(Character chr, Collider col, RaycastHit hit)
	{
		if (hit.collider == chr.GetCollider())
		{
			return true;
		}
		Character component = null;
		hit.collider.gameObject.TryGetComponent<Character>(out component);
		bool flag = component != null;
		List<Component> list = new List<Component>();
		list.Clear();
		hit.collider.gameObject.GetComponents(list);
		if (component == null)
		{
			component = (Character)hit.collider.GetComponentInParent(typeof(Character));
			flag = component != null;
			if (component == null)
			{
				component = hit.collider.GetComponentInChildren<Character>();
				flag = component != null;
			}
		}
		if (flag && component == chr)
		{
			return true;
		}
		return false;
	}

	public static void FindCrosshairObject(Player p, UnityEngine.Vector3 originEyePoint, float maxDistance, out GameObject hover, out Character hoverCreature)
	{
		hover = null;
		hoverCreature = null;
		RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, m_interactMask);
		Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
		RaycastHit[] array2 = array;
		int num = 0;
		RaycastHit raycastHit;
		while (true)
		{
			if (num >= array2.Length)
			{
				return;
			}
			raycastHit = array2[num];
			if (!raycastHit.collider.attachedRigidbody || !(raycastHit.collider.attachedRigidbody.gameObject == p.gameObject))
			{
				break;
			}
			num++;
		}
		if (hoverCreature == null)
		{
			Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());
			if (character != null)
			{
				hoverCreature = character;
			}
		}
		if (Vector3.Distance(originEyePoint, raycastHit.point) < maxDistance)
		{
			if (raycastHit.collider.GetComponent<Hoverable>() != null)
			{
				hover = raycastHit.collider.gameObject;
			}
			else if ((bool)raycastHit.collider.attachedRigidbody)
			{
				hover = raycastHit.collider.attachedRigidbody.gameObject;
			}
			else
			{
				hover = raycastHit.collider.gameObject;
			}
		}
	}

	public static float GetEnrageCost(Player p)
	{
		return GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetEnrageCooldown(Player p)
	{
		return GetCooldown(60f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetEnrageSkillGain(Player p)
	{
		return 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetSpiritBombCost(Player p)
	{
		return GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetSpiritBombCooldown(Player p)
	{
		return GetCooldown(30f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetSpiritBombSkillGain(Player p)
	{
		return 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetShellCost(Player p)
	{
		return GetAbilityCost(80f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetShellCooldown(Player p)
	{
		return GetCooldown(120f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetShellSkillGain(Player p)
	{
		return 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetDashCost(Player p)
	{
		return GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetDashCooldown(Player p)
	{
		return GetCooldown(10f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetDashSkillGain(Player p)
	{
		return 0.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetBerserkCost(Player p)
	{
		return GetAbilityCost(0f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetBerserkCooldown(Player p)
	{
		return GetCooldown(60f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetBerserkSkillGain(Player p)
	{
		return 2.7f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetExecuteCost(Player p)
	{
		return GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetExecuteCooldown(Player p)
	{
		return GetCooldown(60f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetExecuteSkillGain(Player p)
	{
		return 2.4f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetPowerShotCost(Player p)
	{
		return GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetPowerShotCooldown(Player p)
	{
		return GetCooldown(60f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetPowerShotSkillGain(Player p)
	{
		return 1.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetShadowStalkCost(Player p)
	{
		return GetAbilityCost(40f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetShadowStalkCooldown(Player p)
	{
		return GetCooldown(45f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetShadowStalkSkillGain(Player p)
	{
		return 3.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetSummonWolfCost(Player p)
	{
		return GetAbilityCost(50f, VL_GlobalConfigs.g_EnergyCostModifer, GetAttribute(EpicMMOSystem.Parameter.Agility));
	}

	public static float GetSummonWolfCooldown(Player p)
	{
		return GetCooldown(600f, VL_GlobalConfigs.g_CooldownModifer, GetAttribute(EpicMMOSystem.Parameter.Intellect));
	}

	public static float GetSummonWolfSkillGain(Player p)
	{
		return 27f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static void SetTimer()
	{
		vl_timer = Time.time;
	}

    public static ItemDrop.ItemData FindItemByPrefabName(
    Inventory inv,
    string prefabName,
    int requiredAmount = 1)
    {
        if (inv == null) return null;

        foreach (ItemDrop.ItemData item in inv.GetAllItems())
        {
            if (item?.m_dropPrefab == null) continue;

            if (item.m_dropPrefab.name == prefabName &&
                item.m_stack >= requiredAmount)
            {
                return item;
            }
        }

        return null;
    }

    public static ItemDrop.ItemData FindItemBySharedName(Inventory inv, string lowerName, int minStack)
    {
        for (int j = 0; j < inv.GetHeight(); j++)
        {
            for (int i = 0; i < inv.GetWidth(); i++)
            {
                var item = inv.GetItemAt(i, j);
                if (item == null) continue;

                if (item.m_shared?.m_name != null &&
                    item.m_shared.m_name.ToLower() == lowerName &&
                    item.m_stack >= minStack)
                {
                    return item;
                }
            }
        }
        return null;
    }

}
