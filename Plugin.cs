using BepInEx;
using BepInEx.Logging;
using PeglinRelicLib.Model;
using PeglinRelicLib.Register;
using Relics;
using HarmonyLib;
using Battle;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MyFirstPlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("io.github.crazyjackel.RelicLib")]
    public class Plugin : BaseUnityPlugin
    {
        public static RelicEffect myRelicEffect;
        public static new ManualLogSource Log;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Log = base.Logger;
            Plugin.Log.LogInfo("Global logging works!");
            RelicDataModel model = new RelicDataModel("io.github.rivques.testRelic")
            {
                Rarity = RelicRarity.COMMON,
                BundlePath = "relic",
                SpriteName = "knife",
                LocalKey = "knifeCrit",
            };
            model.SetAssemblyPath(this);

            bool success = RelicRegister.RegisterRelic(model, out RelicEffect myEffect);
            myRelicEffect = myEffect;


            harmony.PatchAll();
        }
    }
    [HarmonyPatch(typeof(PlayerHealthController), "Damage")]
    class DamagePatch
    {
        
        static void Prefix(PlayerHealthController __instance, ref float damage, RelicManager ____relicManager, FloatVariable ____playerHealth)
        {
            damage = 0;
            if (RelicRegister.TryGetCustomRelicEffect("io.github.rivques.testRelic", out RelicEffect effect) && ____relicManager.RelicEffectActive(effect))
            {
                Plugin.Log.LogInfo("Player took damage with relic active!");
            } else
            {
                Plugin.Log.LogInfo("Player took damage, but relic not active!");
            }
        }
    }
    [HarmonyPatch(typeof(RelicManager), "Reset")]
    public class RelicManagerPatch
    {
        public static void Postfix(RelicManager __instance)
        {
            Plugin.Log.LogInfo("Relic manager reset!");
            List<Relic> allRelics = __instance._commonRelicPool._relics
                .Union(__instance._rareRelicPool._relics)
                .Union(__instance._rareScenarioRelics._relics)
                .Union(__instance._bossRelicPool._relics)
                .ToList();
            RelicRegister.TryGetCustomRelicEffect("io.github.rivques.testRelic", out RelicEffect relicEffect);
            Relic relic = allRelics.Find(r => r.effect == relicEffect);
            __instance.AddRelic(relic);

        }
    }
}
