using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace FindItemsForQuotaBepin5;

[BepInPlugin(modGUID, modName, modVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string modGUID = "Rbmukthegreat.FindItemsForQuotaMod";
    private const string modName = "Find Items For Quota Mod";
    private const string modVersion = "1.2.2";
    internal static FindItemsForQuotaBepin5Config ConfigInstance;

    private readonly Harmony harmony = new(modGUID);

    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;
        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Logger.LogInfo($"Current version: {MyPluginInfo.PLUGIN_VERSION}");

        ConfigInstance = new FindItemsForQuotaBepin5Config(base.Config);
        ConfigInstance.RegisterOptions();

        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
