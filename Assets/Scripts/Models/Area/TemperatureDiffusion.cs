#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;
using MoonSharp.Interpreter;
using ProjectPorcupine.Rooms;
using UnityEngine;

[MoonSharpUserData]
public class TemperatureDiffusion
{
    private float[,] diffusion;
    private HashSet<Furniture> sinksAndSources;
    private bool recomputeOnNextUpdate;
    private World world;

    /// <summary>
    /// Create and Initialize arrays with default values.
    /// </summary>
    public TemperatureDiffusion(World world)
    {
        sinksAndSources = new HashSet<Furniture>();

        this.world = world;
        world.FurnitureManager.Created += OnFurnitureCreated;
        foreach (Furniture furn in world.FurnitureManager)
        {
            if (furn.RoomEnclosure)
            {
                furn.Removed += OnFurnitureRemoved;
            }
        }

        world.OnTileTypeChanged += OnTileTypeChanged;

        RecomputeDiffusion();
        TimeManager.Instance.FixedFrequency += FixedFrequency;
    }

    /// <summary>
    /// If needed, progress physics.
    /// </summary>
    public void FixedFrequency(float deltaTime)
    {
        UpdateTemperature(deltaTime);
    }

    /// <summary>
    /// Register a sink or a source.
    /// </summary>
    /// <param name="provider"> The provider of this change. </param>
    public void RegisterSinkOrSource(Furniture provider)
    {
        if (sinksAndSources.Contains(provider) == false)
        {
            sinksAndSources.Add(provider);
        }
    }

    /// <summary>
    /// Deregister a sink or a source.
    /// </summary>
    /// <param name="provider"> The provider of this change. </param>
    public void DeregisterSinkOrSource(Furniture provider)
    {
        if (sinksAndSources.Contains(provider))
        {
            sinksAndSources.Remove(provider);
        }
    }

    /// <summary>
    /// Public interface to temperature model, returns temperature at x, y.
    /// </summary>
    /// <param name="x">X coordinates.</param>
    /// <param name="y">Y coordinates.</param>
    /// <param name="z">Z coordinates.</param>
    /// <returns>Temperature at x,y,z.</returns>
    public TemperatureValue GetTemperature(int x, int y, int z)
    {
        Room room = world.GetTileAt(x, y, z).Room;
        return room == null ? TemperatureValue.AbsoluteZero : room.Atmosphere.GetTemperature();
    }

    /// <summary>
    /// Resize Map.
    /// </summary>
    public void Resize()
    {
        RecomputeDiffusion();
        sinksAndSources = new HashSet<Furniture>();
    }

    /// <summary>
    /// On tile type change update.
    /// </summary>
    /// <param name="tile"> The tile in question. </param>
    private void OnTileTypeChanged(Tile tile)
    {
        recomputeOnNextUpdate = true;
    }

    /// <summary>
    /// On furniture creation, recompute.
    /// </summary>
    /// <param name="furn"></param>
    private void OnFurnitureCreated(Furniture furn)
    {
        if (furn.RoomEnclosure)
        {
            furn.Removed += OnFurnitureRemoved;
            recomputeOnNextUpdate = true;
        }
    }

    /// <summary>
    /// On furniture removal recompute.
    /// </summary>
    /// <param name="furn"></param>
    private void OnFurnitureRemoved(Furniture furn)
    {
        recomputeOnNextUpdate = true;
    }

    /// <summary>
    /// Recompute diffusion graph.
    /// </summary>
    private void RecomputeDiffusion()
    {
        recomputeOnNextUpdate = false;

        InitDiffusionMap();

        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                for (int z = 0; z < world.Depth; z++)
                {
                    Tile tile = world.GetTileAt(x, y, z);

                    if (tile.Furniture != null && tile.Furniture.RoomEnclosure)
                    {
                        Tile[] neighbours = tile.GetNeighbours(true, false, true);

                        AddDiffusionFromSource(tile.Furniture, tile.North(), neighbours[5], tile.South(), neighbours[6]);
                        AddDiffusionFromSource(tile.Furniture, tile.East(), neighbours[6], tile.West(), neighbours[7]);
                        AddDiffusionFromSource(tile.Furniture, tile.South(), neighbours[7], tile.North(), neighbours[4]);
                        AddDiffusionFromSource(tile.Furniture, tile.West(), neighbours[4], tile.East(), neighbours[5]);
                    }
                }
            }
        }
    }

    private void InitDiffusionMap()
    {
        int roomCount = world.RoomManager.Count;
        diffusion = new float[roomCount, roomCount];
    }

    private void AddDiffusionFromSource(Furniture wall, Tile source, Tile left, Tile middle, Tile right)
    {
        float diffusivity = wall.Parameters["thermal_diffusivity"].ToFloat(0);
        if (AreTilesInDifferentRooms(source, left))
        {
            AddDiffusionFromTo(source.Room, left.Room, (left.Room.IsOutsideRoom() ? 0.01f : 0.25f) * diffusivity);
        }

        if (AreTilesInDifferentRooms(source, middle))
        {
            AddDiffusionFromTo(source.Room, middle.Room, (middle.Room.IsOutsideRoom() ? 0.02f : 0.5f) * diffusivity);
        }

        if (AreTilesInDifferentRooms(source, right))
        {
            AddDiffusionFromTo(source.Room, right.Room, (right.Room.IsOutsideRoom() ? 0.01f : 0.25f) * diffusivity);
        }
    }

    private bool AreTilesInDifferentRooms(Tile t1, Tile t2)
    {
        return t1 != null && t2 != null &&
            t1.Room != null && t2.Room != null &&
            t1.Room.Equals(t2.Room) == false;
    }

    private void AddDiffusionFromTo(Room r1, Room r2, float value)
    {
        diffusion[r1.ID, r2.ID] += value;
    }

    private void UpdateTemperature(float deltaTime)
    {
        if (recomputeOnNextUpdate)
        {
            RecomputeDiffusion();
        }

        foreach (var furn in sinksAndSources)
        {
            GenerateHeatFromFurniture(furn, deltaTime);
        }

        int roomCount = world.RoomManager.Count;
        Room r1, r2;
        for (int i = 0; i < roomCount; i++)
        {
            r1 = world.RoomManager[i];
            for (int j = 0; j < roomCount; j++)
            {
                if (diffusion[i, j] != 0)
                {
                    r2 = world.RoomManager[j];
                    float temperatureDifference = r1.Atmosphere.GetTemperature().InKelvin - r2.Atmosphere.GetTemperature().InKelvin;
                    if (temperatureDifference > 0)
                    {
                        float energyTransfer = diffusion[i, j] * temperatureDifference * Mathf.Sqrt(r1.GetGasPressure()) * Mathf.Sqrt(r2.GetGasPressure()) * deltaTime;
                        r1.Atmosphere.ChangeEnergy(-energyTransfer);
                        r2.Atmosphere.ChangeEnergy(energyTransfer);
                    }
                }
            }
        }
    }

    private void GenerateHeatFromFurniture(Furniture furniture, float deltaTime)
    {
        if (furniture.Tile.Room.IsOutsideRoom() == true)
        {
            return;
        }

        Tile tile = furniture.Tile;
        float pressure = tile.Room.GetGasPressure();
        float efficiency = ModUtils.Clamp01(pressure / furniture.Parameters["pressure_threshold"].ToFloat());
        float energyChangePerSecond = furniture.Parameters["base_heating"].ToFloat() * efficiency;
        float energyChange = energyChangePerSecond * deltaTime;

        tile.Room.Atmosphere.ChangeEnergy(energyChange);
    }
}
