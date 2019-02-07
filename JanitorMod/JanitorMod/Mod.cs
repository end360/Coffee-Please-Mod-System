using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

/*
This will most likely be implemented as a real feature in future builds.
I was going to use the store upgrade system however in its current state its not easy to mod so I just faked it with console commands.
You must buy the janitor with "upgrade.purchase janitor" in the console unless you modify the code to be otherwise.
*/

namespace JanitorMod
{
    class Mod
    {
        private static UnityAction CleanTrashAction;

        public static StoreUpgradeDetails JanitorUpgradeDetails;


        public static void CleanTrash()
        {

            foreach(ItemFood f in Object.FindObjectsOfType<ItemFood>())
            {
                if (f.isStale)
                {
                    Object.Destroy(f.gameObject);
                }
            }

            foreach(TrashBin b in Object.FindObjectsOfType<TrashBin>())
            {
                b.EmptyTrash();
            }

            foreach(TrashBag b in Object.FindObjectsOfType<TrashBag>())
            {
                Object.Destroy(b.gameObject);
            }

            foreach(Dirt d in Object.FindObjectsOfType<Dirt>())
            {
                d.RemoveDirt();
            }

            foreach(ItemCup c in Object.FindObjectsOfType<ItemCup>())
            {
                c.CleanCup();
            }
            foreach(ItemFoamJug j in Object.FindObjectsOfType<ItemFoamJug>())
            {
                j.isDirty = false;
                j.SetVisuals();
            }
        }

        public static void TryCleanTrash()
        {
            if (JanitorUpgradeDetails.purchased)
            {
                CleanTrash();
            }
        }
		
		[DevConsole.Command("upgrade", "purchase", "Purchases an upgrade (this doesn't force it).")]
        public static void PurchaseUpgrade(string upgrade)
        {
            foreach(KeyValuePair<StoreUpgradeSystem.Upgrade, StoreUpgradeDetails> kv in StoreUpgradeSystem.get.upgradeDictionary)
            {
                if (kv.Key == StoreUpgradeSystem.Upgrade.none)
                    continue;
                if (kv.Value.name == upgrade)
                {
                    bool success = StoreUpgradeSystem.get.PurchaseUpgrade(kv.Key);
                    DevConsole.Console.Log(success ? "Purchased upgrade " + upgrade : "Failed to purchase upgrade " + upgrade);
                    return;
                }
            }

            if(upgrade == "janitor")
            {
                if (JanitorUpgradeDetails.purchased)
                {
                    DevConsole.Console.Log("You already own the janitor upgrade silly.");
                    return;
                }
                if (GameState.get.money.Pay(JanitorUpgradeDetails.cost))
                {
                    JanitorUpgradeDetails.purchased = true;
                }
                DevConsole.Console.Log(JanitorUpgradeDetails.purchased ? "Purchased upgrade " + upgrade : "Failed to purchase upgrade " + upgrade);
            }

        }
		
		[DevConsole.Command("upgrade", "list", "Lists all upgrades registered in the system.")]
        public static void ListUpgrades()
        {
            foreach (KeyValuePair<StoreUpgradeSystem.Upgrade, StoreUpgradeDetails> kv in StoreUpgradeSystem.get.upgradeDictionary)
            {
                if (kv.Key == StoreUpgradeSystem.Upgrade.none)
                    continue;
                DevConsole.Console.Log(string.Format("{2}{0}({1}) ${3}: {4}", kv.Value.name, kv.Key, kv.Value.purchased ? "[PURCHASED] " : "", kv.Value.cost, kv.Value.description));
            }
            DevConsole.Console.Log(string.Format("{2}{0}({1}) ${3}: {4}", "janitor", 1000, JanitorUpgradeDetails.purchased ? "[PURCHASED] " : "", JanitorUpgradeDetails.cost, JanitorUpgradeDetails.description));
        }

        public static void Load()
        {
            CleanTrashAction = new UnityAction(TryCleanTrash);
            EventManager.StartListening("DayOver", CleanTrashAction);

            JanitorUpgradeDetails = new StoreUpgradeDetails();
            JanitorUpgradeDetails.cost = 250;
            JanitorUpgradeDetails.description = "Automatically cleans the store when the day ends";
            JanitorUpgradeDetails.name = "Janitor";

        }

        public static void UnLoad()
        {
            if(CleanTrashAction != null)
            {
                EventManager.StopListening("DayOver", CleanTrashAction);
            }
        }
    }
}
