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
using UnityEngine;

public class TileSpriteController : BaseSpriteController<Tile>
{
    private Sprite emptySprite;

    // Use this for initialization
    public TileSpriteController(World world) : base(world, "Tiles", world.Volume)
    {
        world.OnTileChanged += OnChanged;
        world.OnTileTypeChanged += OnChanged;

        emptySprite = SpriteManager.GetSprite("Tile", "empty");

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                for (int z = 0; z < world.Depth; z++)
                {
                    Tile tile = world.GetTileAt(x, y, z);
                    OnCreated(tile);
                }
            }
        }
    }

    public override void RemoveAll()
    {
        world.OnTileChanged -= OnChanged;

        base.RemoveAll();
    }

    protected override void OnCreated(Tile tile)
    {
        // This creates a new GameObject and adds it to our scene.
        GameObject tile_go = new GameObject("Tile", new[] { typeof(SpriteRenderer) });

        // Add our tile/GO pair to the dictionary.
        objectGameObjectMap.Add(tile, tile_go);

        // tile_go.name = "Tile_" + tile.X + "_" + tile.Y + "_" + tile.Z;
        tile_go.transform.position = new Vector3(tile.X, tile.Y, tile.Z);
        tile_go.transform.SetParent(objectParent.transform, true);

        // Add a Sprite Renderer
        // Add a default sprite for empty tiles.
        SpriteRenderer sr = tile_go.GetComponent<SpriteRenderer>();
        sr.sprite = emptySprite;
        sr.sortingLayerName = "Tiles";
        
        OnChanged(tile);
    }

    // This function should be called automatically whenever a tile's data gets changed.
    protected override void OnChanged(Tile tile)
    {
        if (objectGameObjectMap.ContainsKey(tile) == false)
        {
            UnityDebugger.Debugger.LogError("TileSpriteController", "tileGameObjectMap doesn't contain the tile_data -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }

        GameObject tile_go = objectGameObjectMap[tile];

        if (tile_go == null)
        {
            UnityDebugger.Debugger.LogError("TileSpriteController", "tileGameObjectMap's returned GameObject is null -- did you forget to add the tile to the dictionary? Or maybe forget to unregister a callback?");
            return;
        }

        // TODO Evaluate this criteria and naming schema!
        if ((tile.WalkCount >= 30) && DoesTileSpriteExist(tile.Type.Type + "_heavy"))
        {
            if (tile.ForceTileUpdate || tile.WalkCount == 30)
            {
                ChangeTileSprite(tile_go, tile.Type.Type + "_heavy");
            }
        }
        else if ((tile.WalkCount >= 10) && DoesTileSpriteExist(tile.Type.Type + "_low"))
        {
            if (tile.ForceTileUpdate || tile.WalkCount == 10)
            {
                ChangeTileSprite(tile_go, tile.Type.Type + "_low");
            }
        }
        else
        {
            ChangeTileSprite(tile_go, tile.Type.Type);
        }

        // NOTE: TileType.Empty does a lookup and is not fast!
        if (tile.Type == TileType.Empty)
        {
            tile_go.SetActive(false);
        }
        else
        {
            tile_go.SetActive(true);
        }
    }

    protected override void OnRemoved(Tile tile)
    {
    }

    private void ChangeTileSprite(GameObject tile_go, string name)
    {
        // TODO How to manage if not all of the names are present?
        tile_go.GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite("Tile", name);
    }

    private bool DoesTileSpriteExist(string name)
    {
        return SpriteManager.HasSprite("Tile", name);
    }
}