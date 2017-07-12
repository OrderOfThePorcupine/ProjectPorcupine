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
using MoonSharp.Interpreter;
using ProjectPorcupine.Rooms;
using UnityEngine;

[MoonSharpUserData]
public class TemperatureDiffusion
{
    private float[,] diffusion;
    private HashSet<Furniture> sinksAndSources;
    private World world;

    /// <summary>
    /// Create and Initialize arrays with default values.
    /// </summary>
    public TemperatureDiffusion(World world)
    {
        sinksAndSources = new HashSet<Furniture>();
        this.world = world;

        world.RoomManager.FloodFillEnded += FloodFillEnded;
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
        sinksAndSources = new HashSet<Furniture>();
        TimeManager.Instance.RunNextFrame(RebuildMap);
    }

    /// <summary>
    /// Flood Fill Began.
    /// </summary>
    private void FloodFillEnded()
    {
        TimeManager.Instance.RunNextFrame(RebuildMap);
    }

    private void RebuildMap()
    {
        diffusion = new float[world.RoomManager.Count, world.RoomManager.Count];

        foreach (Room room in world.RoomManager)
        {
            foreach (Tile tile in room.GetBoundaryTiles())
            {
                AddDiffusions(tile);
            }
        }
    }

    private void AddDiffusions(Tile tile)
    {
        Tile[] neighbours = tile.GetNeighbours(true, false, true);
        float diffusivity = tile.Furniture.Parameters["thermal_diffusivity"].ToFloat(0);

        // N => SE, S, SW => 5, 2, 6
        // E => SW, W, SE => 6, 3, 7
        // S => NW, N, NE => 7, 0, 4
        // W => NE, E, SE => 4, 1, 5
        for (int i = 0; i < 4; i++)
        {
            Tile source = neighbours[0];
            Tile left = neighbours[i < 3 ? i + 5 : 4];
            Tile middle = neighbours[i < 2 ? i + 2 : i - 2];
            Tile right = neighbours[i < 2 ? i + 6 : i + 2];

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
        if (diffusion == null || world.RoomManager.Count != diffusion.Length)
        {
            RebuildMap();
        }

        int roomCount = world.RoomManager.Count;

        foreach (var furn in sinksAndSources)
        {
            GenerateHeatFromFurniture(furn, deltaTime);
        }

        Room r1, r2;
        for (int i = 0; i < roomCount; i++)
        {
            r1 = world.RoomManager[i];
            for (int j = 0; j < roomCount; j++)
            {
                if (diffusion[i, j] == 0)
                {
                    continue;
                }

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
