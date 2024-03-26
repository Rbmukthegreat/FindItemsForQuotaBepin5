using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using static FindItemsForQuotaBepin5.Przydatek;

namespace FindItemsForQuotaBepin5.Patches
{
    [HarmonyPatch]
    internal class FindItemsForQuotaPatcher
    {
        private static ManualLogSource Log = Plugin.Log;
        private static InputAction FindItemsForQuotaAction;
        private static int ProfitQuota { get { return TimeOfDay.Instance.profitQuota; } }
        private static SelectableLevel CurrentLevel { get { return StartOfRound.Instance?.currentLevel; } }
        private static PlayerControllerB Player { get { return StartOfRound.Instance?.localPlayerController; } }
        private static GameObject _ship;
        private static bool ShipLanded { get { return StartOfRound.Instance.shipHasLanded; } }
        private static bool CompletedThisRound = false;

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        private static void OnLoad()
        {
            CompletedThisRound = false;
        }

        [HarmonyPatch(typeof(StartOfRound), "openingDoorsSequence")]
        [HarmonyPostfix]
        private static void LogShipLanded()
        {
            GameNetworkManager.Instance.StartCoroutine(LogShipLandedCoroutine());
        }

        private static IEnumerator LogShipLandedCoroutine()
        {
            yield return new WaitForSeconds(1f + 0.2f + 0.05f + 0.8f + 5f + 10f + 4f);
            Log.LogMessage("Ship has landed!");
        }

        [HarmonyPatch(typeof(StartOfRound), "OnEnable")]
        [HarmonyPostfix]
        private static void OnEnable()
        {
            FindItemsForQuotaAction = new InputAction("FindItemsForQuota.FindItems", binding: "<Keyboard>/backslash");
            FindItemsForQuotaAction.Enable();
            FindItemsForQuotaAction.started += OnFindItemsAction;
        }

        [HarmonyPatch(typeof(StartOfRound), "OnDisable")]
        [HarmonyPostfix]
        private static void OnDisable()
        {
            FindItemsForQuotaAction.started -= OnFindItemsAction;
            FindItemsForQuotaAction.Disable();
        }

        private static void OnFindItemsAction(InputAction.CallbackContext context)
        {
            if (!CurrentLevel) { Log.LogMessage("Scene hasn't loaded yet!"); return; }
            if (!ShipLanded) { Log.LogMessage("Ship hasn't landed yet!"); return; }
            if (CurrentLevel.levelID != 3) { Log.LogMessage("Not at company building!"); return; }
            if (CompletedThisRound) { Log.LogMessage("Already found items this quota!"); }
            CompletedThisRound = true;
            Log.LogMessage($"Current moon: {CurrentLevel.name}");
            Log.LogMessage($"Current quota: {ProfitQuota}");
            // Taken from shiploot source code
            if (!_ship) _ship = GameObject.Find("/Environment/HangarShip");
            var loot = _ship.GetComponentsInChildren<GrabbableObject>()
                .Where(obj => obj.itemProperties.isScrap && obj is not RagdollGrabbableObject)
                .ToList();
            Player.transform.position = new Vector3(-25.75f, -2.62f, -31.33f);
            Log.LogMessage($"Player position: {Player.transform.position}");
            GameNetworkManager.Instance.StartCoroutine(TeleportObjects(loot));
        }

        private static IEnumerator TeleportObjects(List<GrabbableObject> loot)
        {
            // Items don't teleport if teleported right away
            yield return new WaitForSeconds(1f);
            List<int> subset = PrzydatekFast(loot.Select(loot => loot.scrapValue).ToList(), ProfitQuota);
            loot = loot.Where((loot, index) => subset[index] == 1).ToList();
            foreach (var grab in loot)
            {
                Log.LogMessage($"Object name: {grab.name} with value {grab.scrapValue}");
                float oldCarryWeight = Player.carryWeight;
                Player.currentlyHeldObject = grab;
                Player.currentlyHeldObjectServer = grab;
                // Silly fix, but it works.
                try { Player.DiscardHeldObject(); }
                catch { Log.LogInfo("Shotgun"); }
                Player.carryWeight = oldCarryWeight;
            }
            int totalSum = loot.Select(loot => loot.scrapValue).Sum();
            Log.LogMessage($"Total sold: {totalSum}, remaining: {ProfitQuota - totalSum}");
        }
    }
}