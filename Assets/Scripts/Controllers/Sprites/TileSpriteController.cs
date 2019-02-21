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
using UnityEngine.Tilemaps;

public class TileSpriteController : BaseSpriteController<Tile>
{
    public Tilemap[] tilemaps;
    public TilemapRenderer[] tilemapRenderers;
    public UnityEngine.Tilemaps.Tile errorTile;

    public Dictionary<string, TileBase> TileLookup;

    // Use this for initialization
    public TileSpriteController(World world) : base(world, "Tiles", world.Volume)
    {
        world.OnTileChanged += OnChanged;
        world.OnTileTypeChanged += OnChanged;

        TileLookup = new Dictionary<string, TileBase>();
        foreach (var tiletype in PrototypeManager.TileType.Values)
        {
            UnityEngine.Tilemaps.Tile tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
            Sprite sprite = SpriteManager.GetSprite("Tile", tiletype.Type);
            tile.sprite = sprite;
            tile.name = tiletype.Type;
            TileLookup[tiletype.Type] = tile;
        }

        TileLookup[TileType.Empty.Type] = null;

        objectParent.AddComponent<Grid>();

        Sprite floor = SpriteManager.GetSprite("Tile", "floor");
        errorTile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        errorTile.sprite = floor;
        errorTile.name = "ErrorTile";
        errorTile.color = Color.red;

        tilemaps = new Tilemap[world.Depth];
        tilemapRenderers = new TilemapRenderer[world.Depth];
        for (int z = 0; z < world.Depth; z++)
        {
            GameObject go = new GameObject("Tile layer " + (z + 1));
            go.transform.SetParent(objectParent.transform);
            go.transform.position -= new Vector3(.5f, .5f, -z);

            tilemaps[z] = go.AddComponent<Tilemap>();
            tilemaps[z].orientation = Tilemap.Orientation.XY;
            tilemapRenderers[z] = go.AddComponent<TilemapRenderer>();
            tilemapRenderers[z].sortingLayerID = SortingLayer.NameToID("Tiles");
            tilemapRenderers[z].sortingOrder = -z;

            TileBase[] tiles = new TileBase[world.Width * world.Height];
            BoundsInt bounds = new BoundsInt(0, 0, 0, world.Width, world.Height, 1);

            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    Tile worldTile = world.GetTileAt(x, y, z);

                    TileBase tilemapTile;
                    if (TileLookup.TryGetValue(worldTile.Type.Type, out tilemapTile) == false)
                    {
                        tilemapTile = errorTile;
                        UnityDebugger.Debugger.LogWarningFormat("TileSpriteController", "Could not find graphics tile for type {0}", worldTile.Type.Type);
                    }

                    tiles[x + (y * world.Width)] = tilemapTile;
                }
            }

            tilemaps[z].SetTilesBlock(bounds, tiles);
        }
    }

    public override void RemoveAll()
    {
        world.OnTileChanged -= OnChanged;

        base.RemoveAll();
    }

    protected override void OnCreated(Tile tile)
    {
        OnChanged(tile);
    }

    // This function should be called automatically whenever a tile's data gets changed.
    protected override void OnChanged(Tile tile)
    {
        if (tile.Type == TileType.Empty)
        {
            tilemaps[tile.Z].SetTile(new Vector3Int(tile.X, tile.Y, 0), null);
            return;
        }

        TileBase tilemapTile;
        if (TileLookup.TryGetValue(tile.Type.Type, out tilemapTile) == false)
        {
            tilemapTile = errorTile;
            UnityDebugger.Debugger.LogWarningFormat("TileSpriteController", "Could not find graphics tile for type {0}", tile.Type.Type);
        }

        tilemaps[tile.Z].SetTile(new Vector3Int(tile.X, tile.Y, 0), tilemapTile);
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