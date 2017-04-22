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
    private bool recomputeOnNextUpdate = false;
    private World world;
    private List<Room> roomsToRemove = new List<Room>();
    private List<Room> roomsToAdd = new List<Room>();

    /// <summary>
    /// Create and Initialize arrays with default values.
    /// </summary>
    public TemperatureDiffusion(World world)
    {
        sinksAndSources = new HashSet<Furniture>();

        this.world = world;

        world.RoomManager.Added += RoomAdded;
        world.RoomManager.Split += RoomJoinedOrSplit;
        world.RoomManager.Joined += RoomJoinedOrSplit;
        world.RoomManager.Removing += RoomRemoving;

        InitDiffusionMap();

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
        RebuildMap();
        sinksAndSources = new HashSet<Furniture>();
    }

    /// <summary>
    /// Removes the rooms from the diffusion graph.
    /// </summary>
    /// <param name="room"> The room to remove. </param>
    private void RoomRemoving(Room room)
    {
        recomputeOnNextUpdate = true;
        roomsToRemove.Add(room);
    }

    /// <summary>
    /// Removes the old room and adds the new room.
    /// </summary>
    /// <param name="oldRoom"> The old room. </param>
    /// <param name="newRoom"> The new room. </param>
    private void RoomJoinedOrSplit(Room oldRoom, Room newRoom)
    {
        recomputeOnNextUpdate = true;
        roomsToRemove.Add(oldRoom);
        roomsToAdd.Add(newRoom);
    }

    /// <summary>
    /// Adds the rooms to the diffusion graph.
    /// </summary>
    /// <param name="room"> The room to add. </param>
    private void RoomAdded(Room room)
    {
        recomputeOnNextUpdate = true;
        roomsToAdd.Add(room);
    }

    /// <summary>
    /// Recompute diffusion graph.
    /// </summary>
    /*
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
                        AddDiffusions(tile, andBack: true);
                    }
                }
            }
        }
    }
    */

    private void RebuildMap()
    {
        recomputeOnNextUpdate = false;
        InitDiffusionMap();

        foreach (Room room in roomsToAdd)
        {
            foreach (Tile tile in room.GetBorderingTiles())
            {
                Debug.LogWarning(tile.GetName());

                AddDiffusions(tile, andBack: true);
            }
        }

        foreach (Room room in roomsToRemove)
        {
            foreach (Room key in room.GetNeighbours().Values)
            {
                diffusion[room.ID, key.ID] = 0;
                diffusion[key.ID, room.ID] = 0;
            }
        }
    }

    private void InitDiffusionMap()
    {
        int roomCount = world.RoomManager.Count;

        if (diffusion == null || diffusion.Length == 0)
        {
            diffusion = new float[roomCount, roomCount];
        }
        else
        {
            diffusion = ResizeArray(diffusion, roomCount, roomCount);
        }
    }

    private T[,] ResizeArray<T>(T[,] original, int x, int y)
    {
        T[,] newArray = new T[x, y];
        int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
        int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

        for (int i = 0; i < minY; ++i)
        {
            Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);
        }

        return newArray;
    }

    private void AddDiffusions(Tile tile, bool andBack)
    {
        Tile[] neighbours = tile.GetNeighbours(true, false, true);
        float diffusivity = tile.Furniture.Parameters["thermal_diffusivity"].ToFloat(0);

        for (int i = 0; i < 4; i++)
        {
            Tile source = neighbours[0];
            Tile left = neighbours[i < 3 ? i + 5 : 4];
            Tile middle = neighbours[i < 2 ? i + 2 : i - 2];
            Tile right = neighbours[i < 2 ? i + 6 : i + 2];

            Debug.LogWarning("RAN");

            if (AreTilesInDifferentRooms(source, left))
            {
                AddDiffusionFromTo(source.Room, left.Room, (left.Room.IsOutsideRoom() ? 0.01f : 0.25f) * diffusivity);

                if (andBack)
                {
                    AddDiffusionFromTo(left.Room, source.Room, (source.Room.IsOutsideRoom() ? 0.01f : 0.25f) * diffusivity);
                }
            }

            if (AreTilesInDifferentRooms(source, middle))
            {
                AddDiffusionFromTo(source.Room, middle.Room, (middle.Room.IsOutsideRoom() ? 0.02f : 0.5f) * diffusivity);

                if (andBack)
                {
                    AddDiffusionFromTo(middle.Room, source.Room, (source.Room.IsOutsideRoom() ? 0.01f : 0.25f) * diffusivity);
                }
            }

            if (AreTilesInDifferentRooms(source, right))
            {
                AddDiffusionFromTo(source.Room, right.Room, (right.Room.IsOutsideRoom() ? 0.01f : 0.25f) * diffusivity);

                if (andBack)
                {
                    AddDiffusionFromTo(right.Room, source.Room, (source.Room.IsOutsideRoom() ? 0.01f : 0.25f) * diffusivity);
                }
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
        if (recomputeOnNextUpdate)
        {
            RebuildMap();
        }

        int roomCount = world.RoomManager.Count;

        if (diffusion.GetLength(0) != roomCount || diffusion.GetLength(1) != roomCount)
        {
            InitDiffusionMap();
        }

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
