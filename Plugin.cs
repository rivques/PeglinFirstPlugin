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
        public static RelicEffect bombRelicEffect;
        public static RelicEffect slimeRelicEffect;
        public static PegManager pegManager = null;
        public static Random rnd;
        public static new ManualLogSource Log;
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        internal bool isPatched;
        internal const int RUBBER_PEG_CONVERT = 5;

        private void OnEnable()
        {
            rnd = new Random();
            if (!isPatched)
            {
                // Plugin startup logic
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
                Log = base.Logger;
                Plugin.Log.LogInfo("Global logging works!");

                RelicDataModel bombRelic = new RelicDataModel("io.github.rivques.bombRelic")
                {
                    Rarity = RelicRarity.BOSS,
                    BundlePath = "testbundle",
                    SpriteName = "bombster",
                    LocalKey = "angryFace"
                };
                bombRelic.SetAssemblyPath(this);

                RelicDataModel slimeRelic = new RelicDataModel("io.github.rivques.slimeRelic")
                {
                    Rarity = RelicRarity.RARE,
                    BundlePath = "testbundle",
                    SpriteName = "slimed peg",
                    LocalKey = "slimeBall"
                };
                slimeRelic.SetAssemblyPath(this);



                RelicRegister.RegisterRelic(bombRelic, out RelicEffect bombEffect);
                bombRelicEffect = bombEffect;

                RelicRegister.RegisterRelic(slimeRelic, out RelicEffect slimeEffect);
                slimeRelicEffect = slimeEffect;

                LocalizationHelper.ImportTerm(
                    new TermDataModel(bombRelic.NameTerm)
                    {
                        English = "Bombster"
                    },
                    new TermDataModel(bombRelic.DescriptionTerm)
                    {
                        English = "Prime an unprimed <sprite name=BOMB> every time you are <style=damage>damaged.</style>"
                    },
                    new TermDataModel(slimeRelic.NameTerm)
                    {
                        English = "Bouncy Slimeball"
                    },
                    new TermDataModel(slimeRelic.DescriptionTerm)
                    {
                        English = $"On reload {Plugin.RUBBER_PEG_CONVERT} <sprite name=PEG> become <color=#FF69B4>bouncy</color> and <style=durable>durable</style>"
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
            if (RelicRegister.TryGetCustomRelicEffect("io.github.rivques.bombRelic", out RelicEffect effect) && ____relicManager.RelicEffectActive(effect))
            {
                Plugin.Log.LogInfo("Player took damage with bomb relic active!");
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
                Plugin.Log.LogInfo("Player took damage, but bomb relic not active!");
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

            RelicRegister.TryGetCustomRelicEffect("io.github.rivques.bombRelic", out RelicEffect bombEffect);
            Relic bombRelic = allRelics.Find(r => r.effect == bombEffect);
            __instance.AddRelic(bombRelic);

            RelicRegister.TryGetCustomRelicEffect("io.github.rivques.slimeRelic", out RelicEffect slimeEffect);
            Relic slimeRelic = allRelics.Find(r => r.effect == slimeEffect);
            __instance.AddRelic(slimeRelic);
        }
    }

    [HarmonyPatch(typeof(BattleController),"StartReloading")]

    public class ReloadPatch
    {
        public static void Prefix(PegManager ____pegManager, RelicManager ____relicManager)
        {
            if (RelicRegister.TryGetCustomRelicEffect("io.github.rivques.slimeRelic", out RelicEffect effect) && ____relicManager.RelicEffectActive(effect))
            {
                Plugin.Log.LogInfo("Player reloaded with slime relic active!");
                if (Plugin.pegManager != null)
                {
                    Plugin.pegManager.ApplyEnemySlimeToPegs(Peg.SlimeType.BouncySlime, Plugin.RUBBER_PEG_CONVERT);
                }
            }
            else
            {
                Plugin.Log.LogInfo("Player reloaded, but slime relic not active!");
            }
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
