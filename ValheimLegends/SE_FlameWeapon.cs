using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_FlameWeapon : StatusEffect
{
    public static Sprite AbilityIcon =
        ZNetScene.instance.GetPrefab("StaffFireball")
            .GetComponent<ItemDrop>().m_itemData.GetIcon();

    public static GameObject GO_SEFX;

    public bool doOnce = true;

    // -----------------------------
    // CHARGES (stacks)
    // -----------------------------
    public const int MaxCharges = 10;

    private string _baseName = "Flame Weapon";

    public int Charges { get; private set; } = 0;

    public SE_FlameWeapon()
    {
        base.name = "SE_VL_FlameWeapon";
        m_icon = ZNetScene.instance.GetPrefab("StaffFireball")
            .GetComponent<ItemDrop>().m_itemData.GetIcon();
        m_tooltip = "Attacks are imbued with fire.";
        m_name = "Flame Weapon: 0";

        _baseName = m_name;
        UpdateName();

        doOnce = true;
    }

    /// <summary>
    /// Adiciona cargas e atualiza o m_name no formato "Flame Weapon: X".
    /// Chame isso a cada hit no patch de dano.
    /// </summary>
    public void AddCharge(int amount = 1)
    {
        Charges = Mathf.Clamp(Charges + amount, 0, MaxCharges);
        UpdateName();
    }

    public void ResetCharges()
    {
        Charges = 0;
        UpdateName();
    }

    private void UpdateName()
    {
        m_name = $"{_baseName}: {Charges}";
    }

    public override void UpdateStatusEffect(float dt)
    {
        // Garante que o nome base nunca fique poluído
        if (string.IsNullOrEmpty(_baseName))
        {
            _baseName = "Flame Weapon";
        }
        else if (_baseName.Contains(":"))
        {
            _baseName = _baseName.Split(':')[0].Trim();
        }

        // Reaplica nome com cargas
        UpdateName();

        float level = m_character.GetSkills().GetSkillList()
            .FirstOrDefault(x => x.m_info == ValheimLegends.EvocationSkillDef)
            .m_level * (1f + Mathf.Clamp(
                (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) +
                (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f),
                0f, 0.5f));
        if (doOnce)
        {
            doOnce = false;

            m_tooltip =
                "Attacks are imbued with Fire " +
                "\nHits do extra " +
                Mathf.Max(
                    0.5f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 6f) *
                    (1f + (level / 150f)),
                    0.1f
                ).ToString("#.#") +
                "-" +
                Mathf.Max(
                    1.3f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 6f) *
                    (1f + (level / 150f)),
                    0.1f
                ).ToString("#.#") +
                " average Fire damage";
        }

        // Keep the name updated with current stacks (0-10)
        if (m_character is Player p)
        {
            int stacks = ValheimLegends.GetEnchanterWeaponCharges(p, ValheimLegends.EnchanterWeaponElement.Flame);
            float percent = Mathf.Clamp(stacks, 0, 100) * (1f + (level / 100f));
            if (percent <= 0f)
            {
                m_name = "Flame Weapon: 0%";
            }
            else
            {
                m_name = $"Flame Weapon: {percent:#.#}%";
            }
        }

        base.UpdateStatusEffect(dt);
    }

    public override bool CanAdd(Character character)
    {
        return character.IsPlayer();
    }
}
