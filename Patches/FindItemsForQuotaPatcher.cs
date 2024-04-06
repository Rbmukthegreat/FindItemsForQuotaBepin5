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

        [HarmonyPatch(typeof(HUDManager), "AddTextToChatOnServer")]
        [HarmonyPostfix]
        private static void GetText(string chatMessage)
        {
            Log.LogMessage(chatMessage);
            string[] words = chatMessage.Split(' ');
            if (words.Length < 2) return;
            if (words[0] == ".find" && int.TryParse(words[1], out int target))
            {
                FindItems(null, target);
            } else if (words[0] == ".find" && (words[1].ToLower() == "rend" || words[1].ToLower() == "art"))
            {
                FindItems(words[1], 0);
            }
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

        private static void FindItems(string moon, int target)
        {
            if (!CurrentLevel) { Log.LogMessage("Scene hasn't loaded yet!"); return; }
            if (!ShipLanded) { Log.LogMessage("Ship hasn't landed yet!"); return; }
            if (CurrentLevel.levelID != 3) { Log.LogMessage("Not at company building!"); return; }

            int moneyNeeded = CalculateMoneyNeeded(moon, target);
            Log.LogMessage($"Money needed: {moneyNeeded}");

            var loot = GetLoot(moneyNeeded);

            Vector3 companyShelfLocation = new(-27.95f, -2.62f, -31.36f);
            Player.transform.position = companyShelfLocation;

            List<int> subset = PrzydatekFast(loot.Select(loot => loot.scrapValue).ToList(), moneyNeeded);
            Log.LogMessage($"Total items being sold: {subset.Sum()}");
            loot = loot.Where((loot, index) => subset[index] == 1).ToList();
            TeleportObjects(loot);
        }

        private static List<GrabbableObject> GetLoot(int moneyNeeded)
        {
            if (!_ship) _ship = GameObject.Find("/Environment/HangarShip");
            string Clone = "(Clone)";
            var loot = _ship.GetComponentsInChildren<GrabbableObject>()
                .Where(obj => obj.itemProperties.isScrap && obj is not RagdollGrabbableObject 
                       && obj.name.Substring(0,obj.name.Count() - Clone.Count()) != "GiftBox")
                .ToList();
            var filteredLoot = loot.Where(loot => IsItemAllowed(loot.name.Substring(0, loot.name.Count() - Clone.Count()), Plugin.ConfigInstance.Filter)).ToList();
            if (filteredLoot.Select(loot => loot.scrapValue).Sum() > moneyNeeded)
                loot = filteredLoot;
            return loot;
        }

        private static int CalculateMoneyNeeded(string moon, int totalNeeded)
        {
            if (moon != null)
            {
                int moonValue = (moon == "rend") ? 550 : 1500;
                return Mathf.Max(NeedToSell(moonValue - Credits), ProfitQuota - QuotaFulfilled);
            } else
            {
                return (NeedToSell(totalNeeded) < totalNeeded) ? NeedToSell(totalNeeded) : totalNeeded;
            }
        }

        private static int NeedToSell(int target)
        {
            int overtime = 15;
            // Sometimes the game gives you the + 15, other times it doesn't. Not sure why.
            return Mathf.CeilToInt(1f/6*(5*(target + overtime) + ProfitQuota - QuotaFulfilled));
        }

        private static bool IsItemAllowed(string item, Dictionary<string, ConfigEntry<bool>> Filter)
        {
            bool exists = Filter.TryGetValue(item, out ConfigEntry<bool> ret);
            return !exists || !ret.Value;
        }

        private static void TeleportObjects(List<GrabbableObject> loot)
        {
            foreach (var grab in loot)
            {
                DropItemAt(grab, Player.transform.position);
            }
            int totalSum = loot.Select(loot => loot.scrapValue).Sum();
            Log.LogMessage($"Total sold: {totalSum}");
        }

        private static void DropItemAt(GrabbableObject item, Vector3 position)
        {
            item.transform.position = position;
            item.FallToGround();
        }
    }
}