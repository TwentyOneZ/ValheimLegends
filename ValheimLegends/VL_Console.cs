using UnityEngine;

namespace ValheimLegends;

public static class VL_Console
{
	public static bool CheatRaiseSkill(Skills skill_instance, string name, float value, Player player)
	{
		foreach (Skills.SkillDef legendsSkill in ValheimLegends.legendsSkills)
		{
			if (legendsSkill.m_skill.ToString() == "781" && name.ToLower() == "discipline")
			{
				player.RaiseSkill(legendsSkill.m_skill, value);
				return true;
			}
			if (legendsSkill.m_skill.ToString() == "791" && name.ToLower() == "abjuration")
			{
				player.RaiseSkill(legendsSkill.m_skill, value);
				return true;
			}
			if (legendsSkill.m_skill.ToString() == "792" && name.ToLower() == "alteration")
			{
				player.RaiseSkill(legendsSkill.m_skill, value);
				return true;
			}
			if (legendsSkill.m_skill.ToString() == "793" && name.ToLower() == "conjuration")
			{
				player.RaiseSkill(legendsSkill.m_skill, value);
				return true;
			}
			if (legendsSkill.m_skill.ToString() == "794" && name.ToLower() == "evocation")
			{
				player.RaiseSkill(legendsSkill.m_skill, value);
				return true;
			}
			if (legendsSkill.m_skill.ToString() == "795" && name.ToLower() == "illusion")
			{
				player.RaiseSkill(legendsSkill.m_skill, value);
				return true;
			}
		}
		return false;
	}

	public static void CheatChangeClass(string className)
	{
		bool flag = false;
		if (className.ToLower() == "berserker")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Berserker;
			flag = true;
		}
		else if (className.ToLower() == "druid")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Druid;
			flag = true;
		}
		else if (className.ToLower() == "mage")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Mage;
			flag = true;
		}
		else if (className.ToLower() == "priest")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Priest;
			flag = true;
		}
		else if (className.ToLower() == "monk")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Monk;
			flag = true;
		}
		else if (className.ToLower() == "duelist")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Duelist;
			flag = true;
		}
		else if (className.ToLower() == "enchanter")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Enchanter;
			flag = true;
		}
		else if (className.ToLower() == "rogue")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Rogue;
			flag = true;
		}
		else if (className.ToLower() == "ranger")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Ranger;
			flag = true;
		}
		else if (className.ToLower() == "shaman")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Shaman;
			flag = true;
		}
		else if (className.ToLower() == "valkyrie")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Valkyrie;
			flag = true;
		}
		else if (className.ToLower() == "metavoker")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.Metavoker;
			flag = true;
		}
		else if (className.ToLower() == "none")
		{
			ValheimLegends.vl_player.vl_class = ValheimLegends.PlayerClass.None;
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		Console.instance.Print("Class changed to " + className);
		ValheimLegends.UpdateVLPlayer(Player.m_localPlayer);
		ValheimLegends.NameCooldowns();
		if (ValheimLegends.abilitiesStatus == null)
		{
			return;
		}
		foreach (RectTransform item in ValheimLegends.abilitiesStatus)
		{
			if (item.gameObject != null)
			{
				Object.Destroy(item.gameObject);
			}
		}
		ValheimLegends.abilitiesStatus.Clear();
	}
}
