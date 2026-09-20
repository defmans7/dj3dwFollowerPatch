using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace dj3dwFollowerPatch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dj3dw.FollowerPatch";
        public const string PluginName = "dj3dwFollowerPatch";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> HealPercentPerSecond;
        internal static ConfigEntry<bool> OnlyWhenFollowing;
        internal static ConfigEntry<bool> OnlyOutOfCombat;
        internal static ConfigEntry<float> TickSeconds;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Turn the follower healing on or off.");
            HealPercentPerSecond = Config.Bind("Healing", "HealPercentPerSecond", 0.5f,
                new ConfigDescription("Health restored each second, as a percentage of the creature's max health. 0.5 means a full heal takes about 200 seconds.",
                    new AcceptableValueRange<float>(0f, 100f)));
            OnlyWhenFollowing = Config.Bind("Healing", "OnlyWhenFollowing", true,
                "Only heal tamed creatures that are currently following a player. When false, every tamed creature heals.");
            OnlyOutOfCombat = Config.Bind("Healing", "OnlyOutOfCombat", true,
                "Pause healing while the creature is alerted (fighting or chasing something).");
            TickSeconds = Config.Bind("Healing", "TickSeconds", 1f,
                new ConfigDescription("How often healing is applied, in seconds. Lower is smoother but sends more network updates.",
                    new AcceptableValueRange<float>(0.1f, 10f)));

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(FollowerHealPatch));

            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
