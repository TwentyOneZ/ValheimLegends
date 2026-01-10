using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_ThunderWeapon : StatusEffect
{
    public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("DragonTear").GetComponent<ItemDrop>().m_itemData.GetIcon();

    public static GameObject GO_SEFX;

    public bool doOnce = true;

    // -----------------------------
    // CHARGES (stacks)
    // -----------------------------
    public const int MaxCharges = 10;

    // Guarda o nome base ("Thunder Weapon") para não ir concatenando infinito
    private string _baseName = "Thunder Weapon";

    public int Charges { get; private set; } = 0;

    public SE_ThunderWeapon()
    {
        base.name = "SE_VL_ThunderWeapon";
        m_icon = ZNetScene.instance.GetPrefab("DragonTear").GetComponent<ItemDrop>().m_itemData.GetIcon();
        m_tooltip = "Attacks are imbued with Lightning, jumps to nearby targets";
        m_name = "Thunder Weapon: 0";

        _baseName = m_name;
        UpdateName();

        doOnce = true;
    }

    /// <summary>
    /// Adiciona cargas e atualiza o m_name no formato "Thunder Weapon: X".
    /// Chame isso a cada hit no seu patch de dano.
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
        // Ex.: "Thunder Weapon: 3"
        m_name = $"{_baseName}: {Charges}";
    }

    public override void UpdateStatusEffect(float dt)
    {
        // Garante que _baseName permanece o "nome puro", sem ": X"
        // (ex.: se algum outro lugar mexeu em m_name)
        if (_baseName == null || _baseName.Length == 0)
        {
            _baseName = "Thunder Weapon";
        }
        else if (_baseName.Contains(":"))
        {
            _baseName = _baseName.Split(':')[0].Trim();
        }

        // Reaplica o nome com charges (mantém sempre certo)
        UpdateName();

        float level = m_character.GetSkills().GetSkillList()
            .FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
            .m_level * (1f + Mathf.Clamp(
                (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) +
                (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f),
                0f, 0.5f));

        if (doOnce)
        {
            doOnce = false;

            m_tooltip =
                "Attacks are imbued with Lightning, jumps to nearby targets" +
                "\n" +
                ((0.1f + (level / 300f)) * 100f).ToString("#.#") +
                "% chance to hit for extra " +
                Mathf.Max((0.1f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 4f) * (1f + (level / 150f))), 0.1f).ToString("#.#") +
                "-" +
                Mathf.Max((1.9f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 4f) * (1f + (level / 150f))), 0.1f).ToString("#.#") +
                " Lightning damage that jumps for nearby targets";
        }

        // Keep the name updated with current stacks (0-10)
        if (m_character is Player p)
        {
            int stacks = ValheimLegends.GetEnchanterWeaponCharges(p, ValheimLegends.EnchanterWeaponElement.Thunder);
            float percent = Mathf.Clamp(stacks, 0, 100) * (1f + (level / 100f));
            if (percent <= 0f)
            {
                m_name = "Thunder Weapon: 0%";
            }
            else
            {
                m_name = $"Thunder Weapon: {percent:#.#}%";
            }
        }

        base.UpdateStatusEffect(dt);
    }

    public override bool CanAdd(Character character)
    {
        return character.IsPlayer();
    }
}
