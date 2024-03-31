using BepInEx.Configuration;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using BepInEx.Configuration;
using UnityEngine.Yoga;
using UnityEngine.Assertions;

namespace FindItemsForQuotaBepin5
{
    internal class FindItemsForQuotaBepin5Config(ConfigFile File)
    {
        private readonly ConfigFile Config = File;
        public Dictionary<string, ConfigEntry<bool>> Filter;
        private readonly List<string> GeneralItems = new([      "PerfumeBottle",
                                                                "LaserPointer",
                                                                "FancyLamp",
                                                                "RedSodaCan",
                                                                "FancyGlass",
                                                                "MagnifyingGlass",
                                                                "Dentures",
                                                                "Mug",
                                                                "Painting",
                                                                "TragedyMask",
                                                                "RobotToy",
                                                                "FancyRing",
                                                                "HandBell",
                                                                "Hairdryer",
                                                                "Magic7Ball",
                                                                "ComedyMask",
                                                                "OldPhone",
                                                                "Airhorn",
                                                                "ToyCube",
                                                                "BinFullOfBottles",
                                                                "Hairbrush"]);
        private readonly List<string> SpecialItems = new([      "PickleJar",
                                                                "LungApparatusTurnedOff",
                                                                "GoldBar",
                                                                "RedLocustHive",
                                                                "ShotgunItem",
                                                                "RubberDucky",
                                                                "CashRegisterItem"]);

        public void RegisterOptions()
        {
            Filter = [];
            foreach (string item in GeneralItems) {
                Filter.Add(item, Config.Bind("Filter", item, false, "Decides if the mod should filter (until no longer possible) this item."));
            }
            foreach (string item in SpecialItems)
            {
                Filter.Add(item, Config.Bind("Filter", item, true, "Decides if the mod should filter (until no longer possible) this item."));
            }
        }
    }
}
