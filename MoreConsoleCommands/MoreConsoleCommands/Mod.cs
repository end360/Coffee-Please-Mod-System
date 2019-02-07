using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using DevConsole;
namespace MoreConsoleCommands
{
    class Mod
    {
        private static List<string> registeredACommands;
        
        [Command("more", "slay", "Kills whatever customer you're looking at.")]
        public static void KillAimedNPC()
        {
            RaycastHit hit;
            Ray ray = Camera.current.ScreenPointToRay(Input.mousePosition);

            if(Physics.Raycast(ray, out hit))
            {
                GameObject hitObj = hit.transform.gameObject;

                
                if(hitObj != null)
                {
                    Customer cust = hitObj.GetComponent<Customer>();
                    if(cust != null)
                    {
                        try
                        {
                            cust.PickupOrder();
                            cust.ConsumeProducts();
                            cust.FinishCustomer();
                            cust.LeaveLine();
                        }
                        catch (System.Exception) { }
                        cust.Despawn();
                        Object.Destroy(hitObj);
                    }
                }
            }
        }

        [Command("more", "slayall", "Kills all customers and pedestrians.")]
        public static void KillAllNPCS()
        {

            foreach(Customer cust in Object.FindObjectsOfType<Customer>())
            {
                try
                {
                    cust.PickupOrder();
                    cust.ConsumeProducts();
                    cust.FinishCustomer();
                    cust.LeaveLine();
                }
                catch (System.Exception) { }
                cust.Despawn();
                Object.Destroy(cust.gameObject);
            }
        }

        [Command("more", "standinline", "Forces the customer you're looking at to stand in line.")]
        public static void StandInLine()
        {
            RaycastHit hit;
            Ray ray = Camera.current.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObj = hit.transform.gameObject;


                if (hitObj != null)
                {
                    Customer cust = hitObj.GetComponent<Customer>();
                    if (cust != null)
                    {
                        cust.StandInLine();
                    }
                }
            }
        }

        [Command("more", "saveas", "Saves to the specified slot.")]
        public static void SaveToSlot( int slot = 1 )
        {
            SaveSystem.get.Save(slot);
            DevConsole.Console.Log("Saved to slot " + slot);
        }

        [Command("more", "loadsave", "Loads the save from the specified slot.")]
        public static void LoadFromSlot( int slot = 1)
        {
            if (SaveSystem.get.doesSaveExist(slot))
            {
                DevConsole.Console.Log("Started loading " + slot);
                SaveSystem.get.StartLoad(slot);
            }
            else
            {
                DevConsole.Console.LogError("There is not a save for slot " + slot);
            }
        }

        static void AddActionCommand(Action method, string group, string alias, string help)
        {
            if (!registeredACommands.Contains(group + "." + alias))
            {
                registeredACommands.Add(group + "." + alias);
                DevConsole.Console.AddCommand(new DevConsole.ActionCommand(method, group, alias, help));
            }
        }

        static void AddActionCommand<T>(Action<T> method, string group, string alias, string help)
        {
            if (!registeredACommands.Contains(group + "." + alias))
            {
                registeredACommands.Add(group + "." + alias);
                DevConsole.Console.AddCommand(new DevConsole.ActionCommand<T>(method, group, alias, help));
            }
        }
        
        public static void Load()
        {
            /*if (registeredACommands == null)
            {
                registeredACommands = new List<string>();
            }

            AddActionCommand( KillAimedNPC, "more", "slay",        "Kills whatever customer you're looking at.");
            AddActionCommand( KillAllNPCS,  "more", "slayall",     "Kills all customers and pedestrians.");
            AddActionCommand( StandInLine,  "more", "standinline", "Forces the customer you're looking at to stand in line.");
            AddActionCommand( new Action<int>(SaveToSlot), "more", "saveas", "Saves to the specified slot.");
            AddActionCommand( new Action<int>(LoadFromSlot), "more", "loadsave", "Loads the save from the specified slot.");*/
        }

        public static void UnLoad()
        {
        }
    }
}
