#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;
using ProjectPorcupine.Entities;

public class SelectionInfo
{
    private List<ISelectable> stuffInTile;
    private int selectedIndex = 0;

    public SelectionInfo(Tile t)
    {
        Tile = t;

        BuildStuffInTile();
        SelectFirstStuff();
    }

    /// <summary>
    /// Returns true if the <see cref="Tile.Type"/> isn't empty or if there is stuff other than just the empty tile.
    /// </summary>
    public bool StuffInTile
    {
        get
        {
            // If we have more than just one (since Tile will always exist in the list and more than one refers to having a character/whatever also in there) then return true
            // Else only return true if TileType isn't empty since otherwise its just a vacuum space tile thingy
            return stuffInTile.Count > 1 || Tile.Type != TileType.Empty;
        }
    }

    public Tile Tile { get; protected set; }

    public void BuildStuffInTile()
    {
        // Make sure stuffInTile is big enough to handle all the characters, plus the 3 extra values.
        stuffInTile = new List<ISelectable>();

        // Copy the character references.
        for (int i = 0; i < Tile.Characters.Count; i++)
        {
            stuffInTile.Add(Tile.Characters[i]);
        }

        // Now assign references to the other three sub-selections available.
        if (Tile.Furniture != null)
        {
            stuffInTile.Add(Tile.Furniture);
        }

        if (Tile.Inventory != null)
        {
            stuffInTile.Add(Tile.Inventory);
        }

        foreach (Job pendingBuildJob in Tile.PendingBuildJobs)
        {
            stuffInTile.Add(pendingBuildJob);
        }

        stuffInTile.Add(Tile);
    }

    public void SelectFirstStuff()
    {
        if (stuffInTile[selectedIndex] == null)
        {
            SelectNextStuff();
        }
    }

    public void SelectNextStuff()
    {
        do
        {
            selectedIndex = (selectedIndex + 1) % stuffInTile.Count;
        }
        while (stuffInTile[selectedIndex] == null);
    }

    public ISelectable GetSelectedStuff()
    {
        return stuffInTile[selectedIndex];
    }

    public bool IsCharacterSelected()
    {
        return stuffInTile[selectedIndex] is Character;
    }
}
