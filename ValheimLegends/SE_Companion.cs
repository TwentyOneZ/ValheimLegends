using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class SE_Companion : SE_Stats
{
    public static Sprite AbilityIcon;
    public static GameObject GO_SEFX;

    [Header("SE_VL_Companion")]
    public static float m_baseTTL = 600f;

    public float speedModifier = 1.2f;
    public float healthRegen = 1f;
    public float damageModifier = 1f;

    private float m_timer = 0f;
    private float m_interval = 5f;

    private const float MAX_DISTANCE_FROM_SUMMONER = 60f;
    private float m_distanceCheckTimer = 0f;
    private const float DISTANCE_CHECK_INTERVAL = 0.5f;

    private const string ZDO_SUMMONER_KEY = "VL_Companion_Summoner";

    private const string ZDO_SCALE_KEY = "VL_Companion_Scale";
    private float m_appliedScale = -1f; // cache para evitar re-aplicar toda hora

    // cache (opcional)
    public Player summoner;

    public SE_Companion()
    {
        base.name = "SE_VL_Companion";
        m_icon = AbilityIcon;
        m_tooltip = "Companion";
        m_name = "Companion";
        m_ttl = m_baseTTL;
    }

    public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
    {
        speed *= speedModifier;
    }

    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);

        // aplica scale antes de qualquer coisa (para multiplayer)
        ApplyScaleFromZDO();

        // checagem de distância
        m_distanceCheckTimer -= dt;
        if (m_distanceCheckTimer <= 0f)
        {
            m_distanceCheckTimer = DISTANCE_CHECK_INTERVAL;
            TryAutoDismissByDistance();
        }

        // regen
        m_timer -= dt;
        if (m_timer <= 0f)
        {
            m_timer = m_interval;
            m_character.Heal(healthRegen);
        }
    }

    private void ApplyScaleFromZDO()
    {
        if (m_character == null) return;

        var nview = m_character.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid()) return;

        var zdo = nview.GetZDO();
        if (zdo == null) return;

        float scale = zdo.GetFloat(ZDO_SCALE_KEY, 1f);
        if (scale <= 0f) return;

        if (Mathf.Abs(m_appliedScale - scale) < 0.001f) return;

        m_character.transform.localScale = scale * Vector3.one;
        m_appliedScale = scale;
    }

    private void TryAutoDismissByDistance()
    {
        if (m_character == null) return;

        var owner = GetSummoner();
        if (owner == null || owner.IsDead()) return;

        float dist = Vector3.Distance(m_character.transform.position, owner.transform.position);
        if (dist <= MAX_DISTANCE_FROM_SUMMONER) return;

        m_character.transform.SetPositionAndRotation(owner.transform.position + Vector3.up * 2f, Quaternion.identity);

        //RefundSummonerAbility2CooldownByWolfHealth(owner);
        //DespawnWolfNetSafe();
    }

    /// <summary>
    /// Refund baseado no COOLDOWN RESTANTE:
    /// remaining = ttl - time
    /// refund = remaining * hpPct
    /// time += refund
    /// </summary>
    private void RefundSummonerAbility2CooldownByWolfHealth(Player owner)
    {
        if (owner == null || m_character == null) return;

        // IMPORTANTÍSSIMO: cooldown é "do jogador".
        // Só o cliente do próprio jogador deve mexer nisso (mesmo padrão do seu Block+Ability2).
        if (Player.m_localPlayer == null || owner != Player.m_localPlayer) return;

        var seMan = owner.GetSEMan();
        if (seMan == null) return;

        int cdHash = "SE_VL_Ability2_CD".GetStableHashCode();
        if (!seMan.HaveStatusEffect(cdHash)) return;

        var cd = seMan.GetStatusEffect(cdHash) as SE_Ability2_CD;
        if (cd == null) return;

        float hpPct = Mathf.Clamp01(m_character.GetHealthPercentage());

        // m_time é protected -> reflection via Harmony Traverse
        float time = Traverse.Create(cd).Field("m_time").GetValue<float>();
        float ttl = cd.m_ttl;

        float remaining = ttl - time;
        if (remaining <= 0f) return;

        float refund = remaining * hpPct;
        float newTime = Mathf.Min(ttl, time + refund);
        
        float previousttl = owner.GetSEMan().GetStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()).m_ttl;
        owner.GetSEMan().GetStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()).m_ttl = newTime; 


    }

    private void DespawnWolfNetSafe()
    {
        if (m_character == null) return;

        var nview = m_character.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid()) return;

        // só o owner deve "matar" o character; evita NullRef em SyncVelocity causado por Destroy no meio do FixedUpdate
        if (!nview.IsOwner())
        {
            // fallback: expira rápido e o owner vai executar IsDone e matar
            m_ttl = 0.01f;
            m_time = m_ttl + 1f;
            return;
        }

        // morte limpa
        HitData hit = new HitData();
        hit.m_damage.m_spirit = 99999f;

        // sem texto/efeitos
        m_character.ApplyDamage(hit, showDamageText: false, triggerEffects: false, HitData.DamageModifier.Normal);
    }

    public override bool IsDone()
    {
        if (m_ttl > 0f && m_time > m_ttl)
        {
            ZLog.Log("killing " + m_character.m_name);
            HitData hitData = new HitData();
            hitData.m_damage.m_spirit = 99999f;
            m_character.ApplyDamage(hitData, showDamageText: false, triggerEffects: false, HitData.DamageModifier.VeryWeak);
        }
        return base.IsDone();
    }

    public override bool CanAdd(Character character)
    {
        return !character.IsPlayer();
    }

    private Player GetSummoner()
    {
        if (summoner != null && !summoner.IsDead())
            return summoner;

        if (m_character == null) return null;

        var nview = m_character.GetComponent<ZNetView>();
        if (nview == null || !nview.IsValid()) return null;

        var zdo = nview.GetZDO();
        if (zdo == null) return null;

        ZDOID id = zdo.GetZDOID(ZDO_SUMMONER_KEY);
        if (id == ZDOID.None) return null;

        var go = ZNetScene.instance?.FindInstance(id);
        if (go == null) return null;

        summoner = go.GetComponent<Player>();
        return summoner;
    }
}
