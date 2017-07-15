#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

// DONE put into prototype manager - copy all initialization that inventory has
// DONE - Loading
// TODO - add to character
// TODO - Saving
// TODO - Load from save
// TODO - Interaction
//
//
//
//
//PrototypeManager.Inventory.Get("<inventory>").Clone() <--- how ytou access invent items?
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearManager{

    private static readonly string InventoryManagerLogChanel = "GearManager";

    public Dictionary<string, Gear> gearLookup;

    public GearManager()
    {
        gearLookup = new Dictionary<string, Gear>();
    }

    public bool loadGear()
    {
        return false;
    }
}
