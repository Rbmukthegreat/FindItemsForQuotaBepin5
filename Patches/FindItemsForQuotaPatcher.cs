using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
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
        private static readonly ManualLogSource Log = Plugin.Log;
        private static InputAction FindItemsForQuotaAction;
        private static int ProfitQuota { get { return TimeOfDay.Instance.profitQuota; } }
        private static int QuotaFulfilled { get { return TimeOfDay.Instance.quotaFulfilled;  } }
        private static SelectableLevel CurrentLevel { get { return StartOfRound.Instance?.currentLevel; } }
        private static PlayerControllerB Player { get { return StartOfRound.Instance?.localPlayerController; } }
        private static GameObject _ship;
        private static bool ShipLanded { get { return StartOfRound.Instance.shipHasLanded; } }
        private static bool CompletedThisRound = false;
        private static int Credits;

        [HarmonyPatch(typeof(Terminal), "Start")]
        [HarmonyPostfix]
        private static void UpdateCreditsStart(int ___groupCredits)
        {
            Credits = ___groupCredits;
        }

        [HarmonyPatch(typeof(Terminal), "BuyItemsServerRpc")]
        [HarmonyPostfix]
        private static void UpdateCreditsAfterPurchase(int ___groupCredits)
        {
            Credits = ___groupCredits;
        }

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
            if (CompletedThisRound) { Log.LogMessage("Already found items this quota!"); return; }
            CompletedThisRound = true;

            var loot = GetLoot();

            Vector3 companyShelfLocation = new(-27.95f, -2.62f, -31.36f);
            Player.transform.position = companyShelfLocation;
            
            Log.LogMessage($"Money needed: {CalculateMoneyNeeded()}");
            List<int> subset = PrzydatekFast(loot.Select(loot => loot.scrapValue).ToList(), CalculateMoneyNeeded());
            Log.LogMessage($"Total items being sold: {subset.Sum()}");
            loot = loot.Where((loot, index) => subset[index] == 1).ToList();
            GameNetworkManager.Instance.StartCoroutine(TeleportObjects(loot));
        }

        private static List<GrabbableObject> GetLoot()
        {
            if (!_ship) _ship = GameObject.Find("/Environment/HangarShip");
            string Clone = "(Clone)";
            var loot = _ship.GetComponentsInChildren<GrabbableObject>()
                .Where(obj => obj.itemProperties.isScrap && obj is not RagdollGrabbableObject && obj.name.Substring(0,obj.name.Count() - Clone.Count()) != "GiftBox")
                .ToList();
            var filteredLoot = loot.Where(loot => IsItemAllowed(loot.name.Substring(0, loot.name.Count() - Clone.Count()), Plugin.ConfigInstance.Filter)).ToList();
            if (filteredLoot.Select(loot => loot.scrapValue).Sum() > CalculateMoneyNeeded())
                loot = filteredLoot;
            return loot;
        }

        private static int CalculateMoneyNeeded()
        {
            int soldSufficientlyBig = Mathf.CeilToInt(1f/6 * (-5 * (Credits + QuotaFulfilled) + ProfitQuota + 2825));
            if (soldSufficientlyBig - ProfitQuota >= 6 * 15) return Mathf.Max(soldSufficientlyBig, ProfitQuota - QuotaFulfilled);
            return Mathf.Max(ProfitQuota - QuotaFulfilled, 550 - Credits);
        }

        private static bool IsItemAllowed(string item, Dictionary<string, ConfigEntry<bool>> Filter)
        {
            bool exists = Filter.TryGetValue(item, out ConfigEntry<bool> ret);
            return !exists || !ret.Value;
        }

        private static IEnumerator TeleportObjects(List<GrabbableObject> loot)
        {
            // Items don't teleport if teleported right away
            yield return new WaitForSeconds(3f);
            // string items = string.Join("\n", loot.Select(loot => loot.name).Distinct());
            // File.WriteAllText(Directory.GetCurrentDirectory() + @"\items.txt", items);
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