using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ForgeSmartToggle
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger = new(MyPluginInfo.PLUGIN_GUID);

        // Plugin startup logic. This function is automatically called when your plugin initializes
        public void Awake()
        {
            Logger = base.Logger;

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            Patches.HybridForgePatch.Init(Config);

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }
    }
}
