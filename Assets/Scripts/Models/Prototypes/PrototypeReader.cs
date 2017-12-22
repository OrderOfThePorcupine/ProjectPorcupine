using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ProjectPorcupine.OrderActions;
using Animation;

public class PrototypeReader
{
    public static T ReadJson<T>(T value, JToken jtoken)
    {
        if (jtoken != null)
        {
            return (T)Convert.ChangeType(jtoken, typeof(T));
            //value = (T)jtoken;
        }
        else
        {
            return value;
        }
    }

    public static T[] ReadJsonArray<T>(JToken jtoken)
    {
        List<T> itemList = new List<T>();
        if (jtoken != null)
        {
            foreach (JToken item in jtoken)
            {
                itemList.Add((T)Convert.ChangeType(item, typeof(T)));
            }
        }

        return itemList.ToArray();
    }


    public static Dictionary<string, OrderAction> ReadOrderActions(JToken orderActionsToken)
    {
        Dictionary<string, OrderAction> orderActions = new Dictionary<string, OrderAction>();
        if (orderActionsToken != null)
        {
            foreach (JProperty orderAction in orderActionsToken)
            {
                orderActions.Add(orderAction.Name, OrderAction.FromJson(orderAction));
            }
        }
        return orderActions;
    }

    public static List<ContextMenuLuaAction> ReadContextMenuActions(JToken contextMenuActionsToken)
    {
        List<ContextMenuLuaAction> contextMenu = new List<ContextMenuLuaAction>();

        if (contextMenuActionsToken != null)
        {
            foreach (JToken action in contextMenuActionsToken)
            {
                ContextMenuLuaAction menuItem = new ContextMenuLuaAction();
                menuItem.LuaFunction = PrototypeReader.ReadJson(menuItem.LuaFunction, action["LuaFunction"]);
                menuItem.LocalizationKey = PrototypeReader.ReadJson(menuItem.LocalizationKey, action["LocalizationKey"]);
                menuItem.RequireCharacterSelected = PrototypeReader.ReadJson(menuItem.RequireCharacterSelected, action["RequireCharacterSelected"]);
                menuItem.DevModeOnly = PrototypeReader.ReadJson(menuItem.DevModeOnly, action["DevModeOnly"]);

                contextMenu.Add(menuItem);
            }
        }

        return contextMenu;
    }

    public static List<TraderPotentialInventory> ReadTraderPotentialInventory(JToken potentialInventoryToken)
    {
        List<TraderPotentialInventory> potentialInventory = new List<TraderPotentialInventory>();

        if (potentialInventoryToken != null)
        {
            foreach (JToken inventoryToken in potentialInventoryToken)
            {
                TraderPotentialInventory inventory = new TraderPotentialInventory();
                inventory.Type = PrototypeReader.ReadJson(inventory.Type, inventoryToken["Type"]);
                inventory.Category = PrototypeReader.ReadJson(inventory.Category, inventoryToken["Category"]);
                inventory.MinQuantity = PrototypeReader.ReadJson(inventory.MinQuantity, inventoryToken["MinQuantity"]);
                inventory.MaxQuantity = PrototypeReader.ReadJson(inventory.MaxQuantity, inventoryToken["MaxQuantity"]);
                inventory.Rarity = PrototypeReader.ReadJson(inventory.Rarity, inventoryToken["Rarity"]);

                potentialInventory.Add(inventory);
            }
        }

        return potentialInventory;
    }

    public static Dictionary<string, SpritenameAnimation> ReadAnimations(JToken animationsToken)
    {
        Dictionary<string, SpritenameAnimation> animations = new Dictionary<string, SpritenameAnimation>();
        if (animationsToken != null)
        {
            foreach (JProperty animationToken in animationsToken)
            {
                SpritenameAnimation animation = new SpritenameAnimation();
                animation.ReadJson(animationToken);
                animations.Add(animation.State, animation);
            }
        }
        return animations;
    }

    /// <summary>
    /// Reads the furniture animations. A shorthand for instantiating a new FurnitureAnimation from a
    /// Jtoken containing animations.
    /// </summary>
    /// <returns>The furniture animations.</returns>
    /// <param name="animationsToken">Animations token.</param>
    public static FurnitureAnimation ReadFurnitureAnimations(JToken animationsToken)
    {
        if(animationsToken == null)
        {
            // Current system is based on a nonexistent FurnitureAnimation being null, rather than a FurnitureAnimation based on a null json token
            return null;
        }

        return new FurnitureAnimation(ReadAnimations(animationsToken));
    }

}
