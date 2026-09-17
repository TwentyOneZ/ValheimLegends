using System.Collections.Generic;

namespace ValheimLegends
{
    /// <summary>
    /// Utilitário de alta performance para busca e consulta de skills customizadas do Valheim Legends.
    /// Substitui mais de 120 ocorrências de LINQ (GetSkillList().FirstOrDefault(...)) eliminando
    /// a alocação de closures/delegates e o overhead de iteração genérica por frame/hit.
    /// Mantém cache de referência estável para o Player local com invalidação automática em caso de morte/troca de player.
    /// </summary>
    public static class VL_SkillHelper
    {
        private static Player _cachedPlayer;
        private static Skills.Skill _cachedDiscipline;
        private static Skills.Skill _cachedAbjuration;
        private static Skills.Skill _cachedAlteration;
        private static Skills.Skill _cachedConjuration;
        private static Skills.Skill _cachedEvocation;
        private static Skills.Skill _cachedIllusion;

        public static void InvalidateCache()
        {
            _cachedPlayer = null;
            _cachedDiscipline = null;
            _cachedAbjuration = null;
            _cachedAlteration = null;
            _cachedConjuration = null;
            _cachedEvocation = null;
            _cachedIllusion = null;
        }

        private static void RefreshLocalPlayerCache(Player player)
        {
            _cachedPlayer = player;
            _cachedDiscipline = null;
            _cachedAbjuration = null;
            _cachedAlteration = null;
            _cachedConjuration = null;
            _cachedEvocation = null;
            _cachedIllusion = null;

            if (player == null) return;
            var skills = player.GetSkills();
            if (skills == null) return;
            var list = skills.GetSkillList();
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s == null || s.m_info == null) continue;

                if (s.m_info == ValheimLegends.DisciplineSkillDef) _cachedDiscipline = s;
                else if (s.m_info == ValheimLegends.AbjurationSkillDef) _cachedAbjuration = s;
                else if (s.m_info == ValheimLegends.AlterationSkillDef) _cachedAlteration = s;
                else if (s.m_info == ValheimLegends.ConjurationSkillDef) _cachedConjuration = s;
                else if (s.m_info == ValheimLegends.EvocationSkillDef) _cachedEvocation = s;
                else if (s.m_info == ValheimLegends.IllusionSkillDef) _cachedIllusion = s;
            }
        }

        public static Skills.Skill GetSkill(Player player, Skills.SkillDef def)
        {
            if (player == null || def == null) return null;

            // Fast path para o jogador local (99% das chamadas em input, buffs e combate)
            if (player == Player.m_localPlayer)
            {
                if (_cachedPlayer != player || player.IsDead())
                {
                    RefreshLocalPlayerCache(player);
                }

                if (def == ValheimLegends.DisciplineSkillDef && _cachedDiscipline != null) return _cachedDiscipline;
                if (def == ValheimLegends.AbjurationSkillDef && _cachedAbjuration != null) return _cachedAbjuration;
                if (def == ValheimLegends.AlterationSkillDef && _cachedAlteration != null) return _cachedAlteration;
                if (def == ValheimLegends.ConjurationSkillDef && _cachedConjuration != null) return _cachedConjuration;
                if (def == ValheimLegends.EvocationSkillDef && _cachedEvocation != null) return _cachedEvocation;
                if (def == ValheimLegends.IllusionSkillDef && _cachedIllusion != null) return _cachedIllusion;
            }

            // Fallback sem alocação (loop for indexado na lista interna)
            var playerSkills = player.GetSkills();
            if (playerSkills == null) return null;
            var skillList = playerSkills.GetSkillList();
            if (skillList == null) return null;

            for (int i = 0; i < skillList.Count; i++)
            {
                var s = skillList[i];
                if (s != null && s.m_info == def)
                {
                    return s;
                }
            }

            return null;
        }

        public static float GetSkillLevel(Player player, Skills.SkillDef def)
        {
            var skill = GetSkill(player, def);
            return skill != null ? skill.m_level : 0f;
        }

        public static Skills.Skill GetSkill(Character character, Skills.SkillDef def)
        {
            return character is Player p ? GetSkill(p, def) : null;
        }

        public static float GetSkillLevel(Character character, Skills.SkillDef def)
        {
            return character is Player p ? GetSkillLevel(p, def) : 0f;
        }
    }
}
