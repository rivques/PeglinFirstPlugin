using BepInEx;
using BepInEx.Logging;
using PeglinRelicLib.Model;
using PeglinRelicLib.Register;
using PeglinRelicLib.Utility;
using Relics;
using HarmonyLib;
using Battle;
using System.Collections.Generic;
using System.Linq;
using System;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public

namespace MyFirstPlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("io.github.crazyjackel.RelicLib")]
    public class Plugin : BaseUnityPlugin
    {
        public static RelicEffect myRelicEffect;
        public static PegManager pegManager = null;
        public static Random rnd;
        public static new ManualLogSource Log;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        internal bool isPatched;
        private void OnEnable()
        {
            rnd = new Random();
            if (!isPatched)
            {
                // Plugin startup logic
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
                Log = base.Logger;
                Plugin.Log.LogInfo("Global logging works!");
                RelicDataModel model = new RelicDataModel("io.github.rivques.testRelic")
                {
                    Rarity = RelicRarity.COMMON,
                    BundlePath = "testbundle",
                    SpriteName = "relic1",
                    LocalKey = "angryFace"
                };
                model.SetAssemblyPath(this);

                bool success = RelicRegister.RegisterRelic(model, out RelicEffect myEffect);
                myRelicEffect = myEffect;

                LocalizationHelper.ImportTerm(
                    new TermDataModel(model.NameTerm)
                    {
                        English = "Red Angry Dude"
                    },
                    new TermDataModel(model.DescriptionTerm)
                    {
                        English = "<sprite name=\"BOMB\"><sprite name=\"BOMB\"><sprite name=\"BOMB\"><sprite name=\"BOMB\"><sprite name=\"BOMB\">"
                    }
                    );
                harmony.PatchAll();
                isPatched = true;
            }
        }
    }
    [HarmonyPatch(typeof(PlayerHealthController), "Damage")]
    class DamagePatch
    {
        
        static void Prefix(PlayerHealthController __instance, ref float damage, RelicManager ____relicManager, FloatVariable ____playerHealth)
        {
            if (RelicRegister.TryGetCustomRelicEffect("io.github.rivques.testRelic", out RelicEffect effect) && ____relicManager.RelicEffectActive(effect))
            {
                Plugin.Log.LogInfo("Player took damage with relic active!");
                if(Plugin.pegManager != null)
                {
                    var unHitBombs = Plugin.pegManager._bombs.Where(x => x.HitCount == 0).ToList();
                    Plugin.Log.LogInfo("damage taken while pegManager exists!");
                    Plugin.Log.LogInfo("Found " + unHitBombs.Count.ToString() + " unhit bombs");
                    if (unHitBombs.Count != 0)
                    {
                        unHitBombs[Plugin.rnd.Next(0, unHitBombs.Count)].PegActivated(true);
                    }
                }

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
    [HarmonyPatch(typeof(BattleController), "Awake") ]
    public class BattleControllerPatch
    {
        public static void Postfix(PegManager ____pegManager)
        {
            Plugin.Log.LogInfo("PegManager instantiated");
            Plugin.pegManager = ____pegManager;
        }
    }
}
