using System;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public static class ObjectDBPatches
{
	[HarmonyPatch(typeof(ObjectDB), "Awake")]
	public static class ObjectDBAwake
	{
		public static void Postfix(ObjectDB __instance)
		{
			AddStatusEffect(__instance);
			AddCooldownStatusEffect(__instance);
		}
	}

	[HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
	public static class ObjectDBCopyOtherDB
	{
		public static void Postfix(ObjectDB __instance)
		{
			AddStatusEffect(__instance);
			AddCooldownStatusEffect(__instance);
		}
	}

	[HarmonyPatch(typeof(ObjectDB), "GetStatusEffect", new Type[] { typeof(int) })]
	public static class ObjectDBGetStatusEffect
	{
		public static void Postfix(ObjectDB __instance, int nameHash, StatusEffect __result)
		{
			if (__result != null)
			{
				if (nameHash == "SE_Regeneration".GetStableHashCode() || nameHash == "SE_VL_Regeneration".GetStableHashCode())
				{
					(__result as SE_Regeneration).m_icon = SE_Regeneration.AbilityIcon;
				}
				else if (nameHash == "SE_Bulwark".GetStableHashCode() || nameHash == "SE_VL_Bulwark".GetStableHashCode())
				{
					(__result as SE_Bulwark).m_icon = SE_Bulwark.AbilityIcon;
				}
				else if (nameHash == "SE_Enrage".GetStableHashCode() || nameHash == "SE_VL_Enrage".GetStableHashCode())
				{
					(__result as SE_Enrage).m_icon = SE_Enrage.AbilityIcon;
				}
				else if (nameHash == "SE_Shell".GetStableHashCode() || nameHash == "SE_VL_Shell".GetStableHashCode())
				{
					(__result as SE_Shell).m_icon = SE_Shell.AbilityIcon;
				}
				else if (nameHash == "SE_SpiritDrain".GetStableHashCode() || nameHash == "SE_VL_SpiritDrain".GetStableHashCode())
				{
					(__result as SE_SpiritDrain).m_icon = SE_SpiritDrain.AbilityIcon;
				}
				else if (nameHash == "SE_Berserk".GetStableHashCode() || nameHash == "SE_VL_Berserk".GetStableHashCode())
				{
					(__result as SE_Berserk).m_icon = SE_Berserk.AbilityIcon;
				}
				else if (nameHash == "SE_Execute".GetStableHashCode() || nameHash == "SE_VL_Execute".GetStableHashCode())
				{
					(__result as SE_Execute).m_icon = SE_Execute.AbilityIcon;
				}
				else if (nameHash == "SE_Slow".GetStableHashCode() || nameHash == "SE_VL_Slow".GetStableHashCode())
				{
					(__result as SE_Slow).m_icon = SE_Slow.AbilityIcon;
				}
				else if (nameHash == "SE_PowerShot".GetStableHashCode() || nameHash == "SE_VL_PowerShot".GetStableHashCode())
				{
					(__result as SE_PowerShot).m_icon = SE_PowerShot.AbilityIcon;
				}
				else if (nameHash == "SE_ShadowStalk".GetStableHashCode() || nameHash == "SE_VL_ShadowStalk".GetStableHashCode())
				{
					(__result as SE_ShadowStalk).m_icon = SE_ShadowStalk.AbilityIcon;
				}
				else if (nameHash == "SE_Companion".GetStableHashCode() || nameHash == "SE_VL_Companion".GetStableHashCode())
				{
					(__result as SE_Companion).m_icon = SE_Companion.AbilityIcon;
				}
				else if (nameHash == "SE_RootsBuff".GetStableHashCode() || nameHash == "SE_VL_RootsBuff".GetStableHashCode())
				{
					(__result as SE_RootsBuff).m_icon = SE_RootsBuff.AbilityIcon;
				}
				else if (nameHash == "SE_Riposte".GetStableHashCode() || nameHash == "SE_VL_Riposte".GetStableHashCode())
				{
					(__result as SE_Riposte).m_icon = SE_Riposte.AbilityIcon;
				}
				else if (nameHash == "SE_Rogue".GetStableHashCode() || nameHash == "SE_VL_Rogue".GetStableHashCode())
				{
					(__result as SE_Rogue).m_icon = SE_Rogue.AbilityIcon;
				}
				else if (nameHash == "SE_Monk".GetStableHashCode() || nameHash == "SE_VL_Monk".GetStableHashCode())
				{
					(__result as SE_Monk).m_icon = SE_Monk.AbilityIcon;
				}
				else if (nameHash == "SE_Ranger".GetStableHashCode() || nameHash == "SE_VL_Ranger".GetStableHashCode())
				{
					(__result as SE_Ranger).m_icon = SE_Ranger.AbilityIcon;
				}
				else if (nameHash == "SE_Valkyrie".GetStableHashCode() || nameHash == "SE_VL_Valkyrie".GetStableHashCode())
				{
					(__result as SE_Valkyrie).m_icon = SE_Valkyrie.AbilityIcon;
				}
				else if (nameHash == "SE_Weaken".GetStableHashCode() || nameHash == "SE_VL_Weaken".GetStableHashCode())
				{
					(__result as SE_Weaken).m_icon = SE_Weaken.AbilityIcon;
				}
				if (nameHash == "SE_Ability1_CD".GetStableHashCode() || nameHash == "SE_VL_Ability1_CD".GetStableHashCode())
				{
					(__result as SE_Ability1_CD).m_icon = SE_Ability1_CD.AbilityIcon;
				}
				else if (nameHash == "SE_Ability2_CD".GetStableHashCode() || nameHash == "SE_VL_Ability2_CD".GetStableHashCode())
				{
					(__result as SE_Ability2_CD).m_icon = SE_Ability2_CD.AbilityIcon;
				}
				else if (nameHash == "SE_Ability3_CD".GetStableHashCode() || nameHash == "SE_VL_Ability3_CD".GetStableHashCode())
				{
					(__result as SE_Ability3_CD).m_icon = SE_Ability3_CD.AbilityIcon;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Hud), "Awake")]
	public static class HudAwake
	{
		public static void Postfix(Hud __instance)
		{
			VLGameAssets.ResolveAllRuntimeIcons();
		}
	}

	private static void AddCooldownStatusEffect(ObjectDB odb)
	{
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Ability1_CD"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Ability1_CD>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Ability2_CD"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Ability2_CD>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Ability3_CD"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Ability3_CD>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Windfury_CD"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Windfury_CD>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_DyingLight_CD"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_DyingLight_CD>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_CDReactivearmor"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_CDReactivearmor>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Shapeshift_CD"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Shapeshift_CD>());
		}
	}

	private static void AddStatusEffect(ObjectDB odb)
	{
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Regeneration"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Regeneration>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Bulwark"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Bulwark>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Enrage"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Enrage>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Shell"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Shell>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_SpiritDrain"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_SpiritDrain>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Berserk"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Berserk>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Execute"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Execute>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_PowerShot"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_PowerShot>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_ShadowStalk"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_ShadowStalk>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Companion"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Companion>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_RootsBuff"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_RootsBuff>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Slow"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Slow>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Riposte"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Riposte>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Rogue"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Rogue>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Monk"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Monk>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Ranger"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Ranger>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Valkyrie"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Valkyrie>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Weaken"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Weaken>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_BiomeMeadows"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_BiomeMeadows>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_BiomeBlackForest"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_BiomeBlackForest>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_BiomeMountain"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_BiomeMountain>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_BiomeSwamp"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_BiomeSwamp>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_BiomePlains"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_BiomePlains>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_BiomeOcean"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_BiomeOcean>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_BiomeMist"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_BiomeMist>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_BiomeAsh"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_BiomeAsh>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_DruidFenringForm"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_DruidFenringForm>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_DruidCultistForm"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_DruidCultistForm>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_FlameArmor"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_FlameArmor>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_FlameWeapon"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_FlameWeapon>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_IceArmor"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_IceArmor>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_IceWeapon"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_IceWeapon>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_ThunderArmor"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_ThunderArmor>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_ThunderWeapon"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_ThunderWeapon>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_MageFireAffinity"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Fireaffinity>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_MageFrostAffinity"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Frostaffinity>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_MageLightningAffinity"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Lightningaffinity>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_ManaShield"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_ManaShield>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_ArcaneIntellect"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_ArcaneIntellect>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_ElementalMastery"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_ElementalMastery>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Reactivearmor"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Reactivearmor>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Charm"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Charm>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Charmcontrol"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Charmcontrol>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_CharmImmunity"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_CharmImmunity>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_Frozen"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_Frozen>());
		}
		if (!odb.m_StatusEffects.Find((StatusEffect se) => se.name == "SE_VL_SeedRegeneration"))
		{
			odb.m_StatusEffects.Add(ScriptableObject.CreateInstance<SE_SeedRegeneration>());
		}

		if (ZNetScene.instance != null)
		{
			VLGameAssets.ResolveAllRuntimeIcons();
		}
	}
}
