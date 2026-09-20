using System.Runtime.CompilerServices;
using HarmonyLib;

namespace dj3dwFollowerPatch
{
    /// <summary>
    /// Heals tamed creatures over time.
    /// MonsterAI.UpdateAI only runs on the client that owns the creature, so the heal is applied
    /// exactly once per creature and the game's own Heal RPC keeps everyone else in sync.
    /// </summary>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class FollowerHealPatch
    {
        private sealed class HealState
        {
            public float Accumulated;
        }

        // Per-creature timer without touching the game's own fields. Entries die with the AI component.
        private static readonly ConditionalWeakTable<MonsterAI, HealState> States = new ConditionalWeakTable<MonsterAI, HealState>();

        // ___m_character asks Harmony to hand us BaseAI's protected m_character field.
        private static void Postfix(MonsterAI __instance, float dt, bool __result, Character ___m_character)
        {
            // UpdateAI returns false when this client does not own the creature (or it is not ready).
            if (!__result || !Plugin.Enabled.Value) return;

            Character character = ___m_character;
            if (character == null || !character.IsTamed() || character.IsDead()) return;
            if (Plugin.OnlyWhenFollowing.Value && __instance.GetFollowTarget() == null) return;
            if (Plugin.OnlyOutOfCombat.Value && __instance.IsAlerted()) return;

            float maxHealth = character.GetMaxHealth();
            if (maxHealth <= 0f || character.GetHealth() >= maxHealth) return;

            HealState state = States.GetOrCreateValue(__instance);
            state.Accumulated += dt;

            float tick = Plugin.TickSeconds.Value;
            if (state.Accumulated < tick) return;

            float amount = maxHealth * (Plugin.HealPercentPerSecond.Value / 100f) * state.Accumulated;
            state.Accumulated = 0f;

            if (amount > 0f)
            {
                character.Heal(amount, false);
            }
        }
    }
}
