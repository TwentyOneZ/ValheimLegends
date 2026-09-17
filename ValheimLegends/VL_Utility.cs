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

	public static float GetZoneChargeCost => 40f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetZoneChargeCooldownTime => 600f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetZoneChargeCostPerUpdate => 1f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetZoneChargeSkillGain => 8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetWeakenCost => 40f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetWeakenCooldownTime => 60f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetWeakenSkillGain => 1.4f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetCharmCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetCharmCooldownTime => 60f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetCharmSkillGain => 2.6f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetMeteorPunchCost => 3f;

	public static float GetMeteorPunchCooldownTime => 1f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetMeteorPunchSkillGain => 4.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetPsiBoltCost => 5f;

	public static float GetPsiBoltCooldownTime => 1f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetPsiBoltSkillGain => 5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetFlyingKickCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetFlyingKickCooldownTime => 6f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetFlyingKickSkillGain => 0.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetPoisonBombCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetPoisonBombCooldownTime => 30f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetPoisonBombSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetBackstabCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetBackstabCooldownTime => 20f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetBackstabSkillGain => 2.6f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetFadeCost => 10f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetFadeCooldownTime => 15f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetFadeSkillGain => 1.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetSanctifyCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetSanctifyCooldownTime => 45f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetSanctifySkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetHealCost => 40f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetHealCostPerUpdate => 0.75f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetHealCooldownTime => 30f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetHealSkillGain => 1.3f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetPurgeCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetPurgeCooldownTime => 15f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetPurgeSkillGain => 0.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetQuickShotCost => 25f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetQuickShotCooldownTime => 10f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetQuickShotSkillGain => 0.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetRiposteCost => 30f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetRiposteCooldownTime => 6f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetRiposteSkillGain => 0.2f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetBlinkStrikeCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetBlinkStrikeCooldownTime => 30f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetBlinkStrikeSkillGain => 1.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetLightCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetLightCooldownTime => 20f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetLightSkillGain => 1.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetWarpCost => 40f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetWarpCostPerUpdate => 1f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetWarpCooldownTime => 6f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetWarpSkillGain => 0.2f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetReplicaCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetReplicaCooldownTime => 30f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetReplicaSkillGain => 1.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetForceWaveCost => 30f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetForceWaveSkillGain => 1.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetForceWaveCooldown => 20f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetFireballCost => 30f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetFireballCooldownTime => 2f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetFireballSkillGain => 1.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetMeteorCost => 30f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetMeteorCostPerUpdate => 0.25f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetMeteorCooldownTime => 30f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetMeteorSkillGain => 2.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetFrostNovaCost => 40f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetFrostNovaCooldownTime => 20f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetFrostNovaSkillGain => 1.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetBulwarkCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetBulwarkCooldownTime => 60f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetBulwarkSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetLeapCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetLeapCooldownTime => 15f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetLeapSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetStaggerCost => 40f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetStaggerCooldownTime => 20f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetStaggerSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetHarpoonPullCost => 1f;

	public static float GetHarpoonPullSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetHarpoonPullCooldown => 10f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetShieldReleaseSkillGain => 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetVineHookCost => 40f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetRegenerationCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetRegenerationCooldownTime => 60f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetRegenerationSkillGain => 2.7f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetRootCost => 30f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetRootCostPerUpdate => 0.3f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetRootCooldownTime => 20f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetRootSkillGain => 1.4f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static float GetDefenderCost => 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));

	public static float GetDefenderCooldownTime => 120f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));

	public static float GetDefenderSkillGain => 2.7f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));

	public static bool ReadyTime => Time.time > 0.01f + vl_timer;

    private static string _cachedKey1, _cachedKey1Lower;
    private static string _cachedCombo1, _cachedCombo1Lower;
    private static string _cachedKey2, _cachedKey2Lower;
    private static string _cachedCombo2, _cachedCombo2Lower;
    private static string _cachedKey3, _cachedKey3Lower;
    private static string _cachedCombo3, _cachedCombo3Lower;

    private static string GetKey1Lower()
    {
        string v = ValheimLegends.Ability1_Hotkey.Value;
        if (v != _cachedKey1) { _cachedKey1 = v; _cachedKey1Lower = string.IsNullOrEmpty(v) ? "" : v.ToLower(); }
        return _cachedKey1Lower;
    }

    private static string GetCombo1Lower()
    {
        string v = ValheimLegends.Ability1_Hotkey_Combo.Value;
        if (v != _cachedCombo1) { _cachedCombo1 = v; _cachedCombo1Lower = string.IsNullOrEmpty(v) ? "" : v.ToLower(); }
        return _cachedCombo1Lower;
    }

    private static string GetKey2Lower()
    {
        string v = ValheimLegends.Ability2_Hotkey.Value;
        if (v != _cachedKey2) { _cachedKey2 = v; _cachedKey2Lower = string.IsNullOrEmpty(v) ? "" : v.ToLower(); }
        return _cachedKey2Lower;
    }

    private static string GetCombo2Lower()
    {
        string v = ValheimLegends.Ability2_Hotkey_Combo.Value;
        if (v != _cachedCombo2) { _cachedCombo2 = v; _cachedCombo2Lower = string.IsNullOrEmpty(v) ? "" : v.ToLower(); }
        return _cachedCombo2Lower;
    }

    private static string GetKey3Lower()
    {
        string v = ValheimLegends.Ability3_Hotkey.Value;
        if (v != _cachedKey3) { _cachedKey3 = v; _cachedKey3Lower = string.IsNullOrEmpty(v) ? "" : v.ToLower(); }
        return _cachedKey3Lower;
    }

    private static string GetCombo3Lower()
    {
        string v = ValheimLegends.Ability3_Hotkey_Combo.Value;
        if (v != _cachedCombo3) { _cachedCombo3 = v; _cachedCombo3Lower = string.IsNullOrEmpty(v) ? "" : v.ToLower(); }
        return _cachedCombo3Lower;
    }

	public static bool Ability1_Input_Down
	{
		get
		{
			string key = GetKey1Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo1Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKeyDown(key) || Input.GetButtonDown(key);
			}
			return (Input.GetKeyDown(key) && Input.GetKey(combo)) || (Input.GetKey(key) && Input.GetKeyDown(combo)) || (Input.GetButtonDown(key) && Input.GetButton(combo)) || (Input.GetButton(key) && Input.GetButtonDown(combo));
		}
	}

    public static bool Ability1_Input_Pressed
    {
        get
        {
			string key = GetKey1Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo1Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKey(key) || Input.GetButton(key);
			}
			return (Input.GetKey(key) && Input.GetKey(combo)) || (Input.GetButton(key) && Input.GetButton(combo));
        }
    }

    public static bool Ability1_Input_Up
    {
        get
        {
			string key = GetKey1Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo1Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKeyUp(key) || Input.GetButtonUp(key);
			}
			return Input.GetKeyUp(key) || Input.GetKeyUp(combo) || Input.GetButtonUp(key) || Input.GetButtonUp(combo);
        }
    }

    public static bool Ability2_Input_Down
	{
		get
		{
			string key = GetKey2Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo2Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKeyDown(key) || Input.GetButtonDown(key);
			}
			return (Input.GetKeyDown(key) && Input.GetKey(combo)) || (Input.GetKey(key) && Input.GetKeyDown(combo)) || (Input.GetButtonDown(key) && Input.GetButton(combo)) || (Input.GetButton(key) && Input.GetButtonDown(combo));
		}
	}

    public static bool Ability2_Input_Pressed
    {
        get
        {
			string key = GetKey2Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo2Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKey(key) || Input.GetButton(key);
			}
			return (Input.GetKey(key) && Input.GetKey(combo)) || (Input.GetButton(key) && Input.GetButton(combo));
        }
    }

    public static bool Ability2_Input_Up
    {
        get
        {
			string key = GetKey2Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo2Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKeyUp(key) || Input.GetButtonUp(key);
			}
			return Input.GetKeyUp(key) || Input.GetKeyUp(combo) || Input.GetButtonUp(key) || Input.GetButtonUp(combo);
        }
    }

    public static bool Ability3_Input_Down
	{
		get
		{
			string key = GetKey3Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo3Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKeyDown(key) || Input.GetButtonDown(key);
			}
			return (Input.GetKeyDown(key) && Input.GetKey(combo)) || (Input.GetKey(key) && Input.GetKeyDown(combo)) || (Input.GetButtonDown(key) && Input.GetButton(combo)) || (Input.GetButton(key) && Input.GetButtonDown(combo));
		}
	}

	public static bool Ability3_Input_Pressed
	{
		get
		{
			string key = GetKey3Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo3Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKey(key) || Input.GetButton(key);
			}
			return (Input.GetKey(key) && Input.GetKey(combo)) || (Input.GetButton(key) && Input.GetButton(combo));
		}
	}

	public static bool Ability3_Input_Up
	{
		get
		{
			string key = GetKey3Lower();
			if (string.IsNullOrEmpty(key)) return false;
			string combo = GetCombo3Lower();
			if (string.IsNullOrEmpty(combo))
			{
				return Input.GetKeyUp(key) || Input.GetButtonUp(key);
			}
			return Input.GetKeyUp(key) || Input.GetKeyUp(combo) || Input.GetButtonUp(key) || Input.GetButtonUp(combo);
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
            // Então pegamos o TMP_Text que NÃO é o TimeText.
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

            // Localize se possível, senão escreve raw
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
        //IL_001a: Unknown result type (might be due to invalid IL or missing references)
        //IL_0020: Expected O, but got Unknown
        //IL_0041: Unknown result type (might be due to invalid IL or missing references)
        //IL_0048: Expected O, but got Unknown
        try
        {
            byte[] array = File.ReadAllBytes(Path.Combine(Folder, "VLAssets", path));
            Texture2D val = new Texture2D(1, 1);
            ImageConversion.LoadImage(val, array);
            return val;
        }
        catch
        {
            byte[] array2 = File.ReadAllBytes(Path.Combine(Folder, path));
            Texture2D val2 = new Texture2D(1, 1);
            ImageConversion.LoadImage(val2, array2);
            return val2;
        }
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
			if (string.Equals(ValheimLegends.iconAlignment.Value, "vertical", System.StringComparison.OrdinalIgnoreCase))
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
		return 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetEnrageCooldown(Player p)
	{
		return 60f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
	}

	public static float GetEnrageSkillGain(Player p)
	{
		return 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetSpiritBombCost(Player p)
	{
		return 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetSpiritBombCooldown(Player p)
	{
		return 30f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
	}

	public static float GetSpiritBombSkillGain(Player p)
	{
		return 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetShellCost(Player p)
	{
		return 80f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetShellCooldown(Player p)
	{
		return 120f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
	}

	public static float GetShellSkillGain(Player p)
	{
		return 1.8f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetDashCost(Player p)
	{
		return 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetDashCooldown(Player p)
	{
		return 10f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
	}

	public static float GetDashSkillGain(Player p)
	{
		return 0.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetBerserkCost(Player p)
	{
		return 0f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetBerserkCooldown(Player p)
	{
		return 60f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
	}

	public static float GetBerserkSkillGain(Player p)
	{
		return 2.7f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetExecuteCost(Player p)
	{
		return 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetExecuteCooldown(Player p)
	{
		return 60f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
	}

	public static float GetExecuteSkillGain(Player p)
	{
		return 2.4f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetPowerShotCost(Player p)
	{
		return 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetPowerShotCooldown(Player p)
	{
		return 60f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
	}

	public static float GetPowerShotSkillGain(Player p)
	{
		return 1.5f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetShadowStalkCost(Player p)
	{
		return 40f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetShadowStalkCooldown(Player p)
	{
		return 45f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
	}

	public static float GetShadowStalkSkillGain(Player p)
	{
		return 3.0f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f));
	}

	public static float GetSummonWolfCost(Player p)
	{
		return 50f * VL_GlobalConfigs.g_EnergyCostModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
	}

	public static float GetSummonWolfCooldown(Player p)
	{
		return 600f * VL_GlobalConfigs.g_CooldownModifer * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f));
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
                    string.Equals(item.m_shared.m_name, lowerName, System.StringComparison.OrdinalIgnoreCase) &&
                    item.m_stack >= minStack)
                {
                    return item;
                }
            }
        }
        return null;
    }

    private static readonly int[] BiomeHashes = new int[]
    {
        VL_Hashes.BiomeAsh,
        VL_Hashes.BiomeBlackForest,
        VL_Hashes.BiomeMeadows,
        VL_Hashes.BiomeMist,
        VL_Hashes.BiomeMountain,
        VL_Hashes.BiomeOcean,
        VL_Hashes.BiomePlains,
        VL_Hashes.BiomeSwamp
    };

    public static void RemoveConflictingBiomes(SEMan seMan, Player caster, int currentBiomeHash)
    {
        if (seMan == null) return;
        for (int i = 0; i < BiomeHashes.Length; i++)
        {
            int h = BiomeHashes[i];
            if (h == currentBiomeHash) continue;
            var se = seMan.GetStatusEffect(h);
            if (se == null) continue;

            Player c = null;
            if (se is SE_BiomeMeadows b1) c = b1.caster;
            else if (se is SE_BiomeBlackForest b2) c = b2.caster;
            else if (se is SE_BiomeSwamp b3) c = b3.caster;
            else if (se is SE_BiomeMountain b4) c = b4.caster;
            else if (se is SE_BiomePlains b5) c = b5.caster;
            else if (se is SE_BiomeOcean b6) c = b6.caster;
            else if (se is SE_BiomeMist b7) c = b7.caster;
            else if (se is SE_BiomeAsh b8) c = b8.caster;

            if (c == caster)
            {
                seMan.RemoveStatusEffect(se);
            }
        }
    }
}
