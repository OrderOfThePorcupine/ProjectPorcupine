#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;
using ProjectPorcupine.Entities;
using ProjectPorcupine.Pathfinding;
using UnityEngine;

[MoonSharpUserData]
public class InventoryManager
{
    private static readonly string InventoryManagerLogChanel = "InventoryManager";

    private Dictionary<string, List<InventoryOfTypeCreated>> inventoryTypeCreated = new Dictionary<string, List<InventoryOfTypeCreated>>();

    public InventoryManager()
    {
        Inventories = new Dictionary<string, List<Inventory>>();
    }

    public delegate bool InventoryOfTypeCreated(Inventory inventory);

    public event Action<Inventory> InventoryCreated;

    public Dictionary<string, List<Inventory>> Inventories { get; private set; }

    public static bool CanBePickedUp(Inventory inventory, bool canTakeFromStockpile)
    {
        // You can't pick up stuff that isn't on a tile or if it's locked
        if (inventory == null || inventory.Tile == null || inventory.Locked)
        {
            return false;
        }

        Furniture furniture = inventory.Tile.Furniture;
        return furniture == null || canTakeFromStockpile == true || furniture.HasTypeTag("Storage") == false;
    }

    public void RegisterInventoryTypeCreated(InventoryOfTypeCreated func, string type)
    {
        List<InventoryOfTypeCreated> inventories;
        if (inventoryTypeCreated.TryGetValue(type, out inventories) == false)
        {
            inventories = new List<InventoryOfTypeCreated>();
            inventoryTypeCreated[type] = inventories;
        }

        inventories.Add(func);
    }

    public void UnregisterInventoryTypeCreated(InventoryOfTypeCreated func, string type)
    {
        inventoryTypeCreated[type].Remove(func);
    }

    public Tile GetFirstTileWithValidInventoryPlacement(int maxOffset, Tile inTile, Inventory inv)
    {
        for (int offset = 0; offset <= maxOffset; offset++)
        {
            int offsetX = 0;
            int offsetY = 0;
            Tile tile;

            // searching top & bottom line of the square
            for (offsetX = -offset; offsetX <= offset; offsetX++)
            {
                offsetY = offset;
                tile = World.Current.GetTileAt(inTile.X + offsetX, inTile.Y + offsetY, inTile.Z);
                if (CanPlaceInventoryAt(tile, inv))
                {
                    return tile;
                }

                offsetY = -offset;
                tile = World.Current.GetTileAt(inTile.X + offsetX, inTile.Y + offsetY, inTile.Z);
                if (CanPlaceInventoryAt(tile, inv))
                {
                    return tile;
                }
            }

            // searching left & right line of the square
            for (offsetY = -offset; offsetY <= offset; offsetY++)
            {
                offsetX = offset;
                tile = World.Current.GetTileAt(inTile.X + offsetX, inTile.Y + offsetY, inTile.Z);
                if (CanPlaceInventoryAt(tile, inv))
                {
                    return tile;
                }

                offsetX = -offset;
                tile = World.Current.GetTileAt(inTile.X + offsetX, inTile.Y + offsetY, inTile.Z);
                if (CanPlaceInventoryAt(tile, inv))
                {
                    return tile;
                }
            }
        }

        return null;
    }

    public bool PlaceInventoryAround(Tile tile, Inventory inventory, int radius = 3)
    {
        tile = GetFirstTileWithValidInventoryPlacement(radius, tile, inventory);
        if (tile == null)
        {
            return false;
        }

        return PlaceInventory(tile, inventory);
    }

    public bool PlaceInventory(Tile tile, Inventory inventory)
    {
        bool tileWasEmpty = tile.Inventory == null;

        if (tile.PlaceInventory(inventory) == false)
        {
            // The tile did not accept the inventory for whatever reason, therefore stop.
            return false;

            // TODO: Geoffrotism. Is this where we would hook in to handle inventory not being able to be placed in a tile.
        }

        CleanupInventory(inventory);

        // We may also have to create a new stack on the tile, if the startTile was previously empty.
        if (tileWasEmpty)
        {
            List<Inventory> inventories;
            if (Inventories.TryGetValue(tile.Inventory.Type, out inventories) == false)
            {
                inventories = new List<Inventory>();
                Inventories[tile.Inventory.Type] = inventories;
            }

            inventories.Add(tile.Inventory);
            InvokeInventoryCreated(tile.Inventory);
        }

        return true;
    }

    public bool ConsumeInventory(Tile tile, int amount)
    {
        if (tile.Inventory == null)
        {
            return false;
        }
        else
        {
            tile.Inventory.StackSize -= amount;
            CleanupInventory(tile.Inventory);
            return true;
        }
    }

    public bool PlaceInventory(Job job, Character character)
    {
        Inventory sourceInventory = character.Inventory;

        // Check that it's wanted by the job
        if (job.RequestedItems.ContainsKey(sourceInventory.Type) == false)
        {
            UnityDebugger.Debugger.LogError(InventoryManagerLogChanel, "Trying to add inventory to a job that it doesn't want.");
            return false;
        }

        // Check that there is a target to transfer to
        Inventory targetInventory;
        if (job.DeliveredItems.TryGetValue(sourceInventory.Type, out targetInventory) == false)
        {
            targetInventory = new Inventory(sourceInventory.Type, 0, sourceInventory.MaxStackSize);
            job.DeliveredItems[sourceInventory.Type] = targetInventory;
        }

        int transferAmount = Mathf.Min(targetInventory.MaxStackSize - targetInventory.StackSize, sourceInventory.StackSize);

        sourceInventory.StackSize -= transferAmount;
        targetInventory.StackSize += transferAmount;

        CleanupInventory(character);

        return true;
    }

    public bool PlaceInventory(Character character, Inventory sourceInventory, int amount = -1)
    {
        amount = amount < 0 ? sourceInventory.StackSize : Math.Min(amount, sourceInventory.StackSize);
        sourceInventory.ReleaseClaim(character);
        if (character.Inventory == null)
        {
            character.Inventory = sourceInventory.Clone();
            character.Inventory.StackSize = 0;

            List<Inventory> inventories;
            if (Inventories.TryGetValue(character.Inventory.Type, out inventories) == false)
            {
                inventories = new List<Inventory>();
                Inventories[character.Inventory.Type] = inventories;
            }

            inventories.Add(character.Inventory);
        }
        else if (character.Inventory.Type != sourceInventory.Type)
        {
            UnityDebugger.Debugger.LogError(InventoryManagerLogChanel, "Character is trying to pick up a mismatched inventory object type.");
            return false;
        }

        character.Inventory.StackSize += amount;

        if (character.Inventory.MaxStackSize < character.Inventory.StackSize)
        {
            sourceInventory.StackSize = character.Inventory.StackSize - character.Inventory.MaxStackSize;
            character.Inventory.StackSize = character.Inventory.MaxStackSize;
        }
        else
        {
            sourceInventory.StackSize -= amount;
        }

        CleanupInventory(sourceInventory);

        return true;
    }

    /// <summary>
    /// Gets <see cref="Inventory"/> closest to <see cref="tile"/>.
    /// </summary>
    /// <returns>The closest inventory of type.</returns>
    public Inventory GetClosestInventoryOfType(string type, Tile tile, bool canTakeFromStockpile)
    {
        List<Tile> path = GetPathToClosestInventoryOfType(type, tile, canTakeFromStockpile);
        return path != null ? path.Last().Inventory : null;
    }

    public bool RemoveInventoryOfType(string type, int quantity, bool onlyFromStockpiles)
    {
        if (!HasInventoryOfType(type, true))
        {
            return quantity == 0;
        }

        foreach (Inventory inventory in Inventories[type].ToList())
        {
            if (onlyFromStockpiles)
            {
                if (inventory.Tile == null ||
                    inventory.Tile.Furniture == null ||
                    inventory.Tile.Furniture.Type != "Stockpile" ||
                    inventory.Tile.Furniture.HasTypeTag("Stockpile"))
                {
                    continue;
                }
            }

            if (quantity <= 0)
            {
                break;
            }

            int removedFromStack = Math.Min(inventory.StackSize, quantity);
            quantity -= removedFromStack;
            inventory.StackSize -= removedFromStack;
            CleanupInventory(inventory);
        }

        return quantity == 0;
    }

    public bool HasInventoryOfType(string type, bool canTakeFromStockpile)
    {
        List<Inventory> inventories;
        if (Inventories.TryGetValue(type, out inventories) == false || inventories.Count == 0)
        {
            return false;
        }

        return inventories.Find(inventory => inventory.CanBePickedUp(canTakeFromStockpile)) != null;
    }

    public bool HasInventoryOfType(string[] types, bool canTakeFromStockpile)
    {
        foreach (string objectType in types)
        {
            if (HasInventoryOfType(objectType, canTakeFromStockpile))
            {
                return true;
            }
        }

        return false;
    }

    public List<Tile> GetPathToClosestInventoryOfType(string type, Tile tile, bool canTakeFromStockpile)
    {
        if (HasInventoryOfType(type, canTakeFromStockpile) == false)
        {
            return null;
        }

        // We know the objects are out there, now find the closest.
        return Pathfinder.FindPathToInventory(tile, type, canTakeFromStockpile);
    }

    public List<Tile> GetPathToClosestInventoryOfType(string[] objectTypes, Tile tile, bool canTakeFromStockpile)
    {
        if (HasInventoryOfType(objectTypes, canTakeFromStockpile) == false)
        {
            return null;
        }

        // We know the objects are out there, now find the closest.
        return Pathfinder.FindPathToInventory(tile, objectTypes, canTakeFromStockpile);
    }

    public JToken ToJson()
    {
        JArray inventoriesJson = new JArray();
        foreach (Inventory inventory in Inventories.SelectMany(pair => pair.Value))
        {
            // Skip any inventory without a tile, these are inventories in a character or elsewhere that will handle it itself.
            if (inventory.Tile == null)
            {
                continue;
            }

            inventoriesJson.Add(inventory.ToJSon());
        }

        return inventoriesJson;
    }

    public void FromJson(JToken inventoriesToken)
    {
        JArray inventoriesJArray = (JArray)inventoriesToken;

        foreach (JToken inventoryToken in inventoriesJArray)
        {
            int x = (int)inventoryToken["X"];
            int y = (int)inventoryToken["Y"];
            int z = (int)inventoryToken["Z"];

            Inventory inventory = new Inventory();
            inventory.FromJson(inventoryToken);
            PlaceInventory(World.Current.GetTileAt(x, y, z), inventory);
        }
    }

    public void InventoryAvailable(Inventory inventory)
    {
        List<InventoryOfTypeCreated> inventories;
        if (inventoryTypeCreated.TryGetValue(inventory.Type, out inventories))
        {
            inventories.RemoveAll(func => func(inventory));
        }
    }

    private void CleanupInventory(Inventory inventory)
    {
        if (inventory.StackSize != 0)
        {
            return;
        }

        List<Inventory> inventories;
        if (Inventories.TryGetValue(inventory.Type, out inventories))
        {
            inventories.Remove(inventory);
        }

        if (inventory.Tile != null)
        {
            inventory.Tile.Inventory = null;
            inventory.Tile = null;
        }
    }

    private void CleanupInventory(Character character)
    {
        CleanupInventory(character.Inventory);

        if (character.Inventory.StackSize == 0)
        {
            character.Inventory = null;
        }
    }

    private void InvokeInventoryCreated(Inventory inventory)
    {
        Action<Inventory> handler = InventoryCreated;
        if (handler != null)
        {
            handler(inventory);

            InventoryAvailable(inventory);
        }
    }

    private bool CanPlaceInventoryAt(Tile tile, Inventory inv)
    {
        return (tile.Inventory == null && tile.Furniture == null && tile.IsEnterable() == Enterability.Yes) ||
                    (tile.Inventory != null && tile.Inventory.CanAccept(inv));
    }
}
