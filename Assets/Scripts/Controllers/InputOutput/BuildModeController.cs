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
using System.Text;
using ProjectPorcupine.Localization;
using ProjectPorcupine.OrderActions;
using ProjectPorcupine.Rooms;
using UnityEngine;

public enum BuildMode
{
    FLOOR,
    ROOMBEHAVIOR,
    FURNITURE,
    UTILITY,
    DECONSTRUCT,
    MINE
}

public class BuildModeController : IMouseHandler
{
    private MouseController mouseController;
    private GameObject furnitureParent;
    private TileType buildModeTile = TileType.Floor;

    private bool useCratedObject;

    public BuildModeController()
    {
        Instance = this;
        CurrentPreviewRotation = 0f;
        KeyboardManager.Instance.RegisterInputAction("RotateFurnitureLeft", KeyboardMappedInputType.KeyUp, RotateFurnitireLeft);
        KeyboardManager.Instance.RegisterInputAction("RotateFurnitureRight", KeyboardMappedInputType.KeyUp, RotateFurnitireRight);
        furnitureParent = new GameObject("Furniture Preview Sprites");
    }

    public static BuildModeController Instance { get; protected set; }

    public float CurrentPreviewRotation { get; private set; }

    public BuildMode BuildMode { get; private set; }

    public string BuildModeType { get; private set; }

    public bool Building { get; set; }

    public MouseHandlerCallbacks CallbacksEnabled
    {
        get
        {
            return MouseHandlerCallbacks.HANDLE_DRAG & MouseHandlerCallbacks.HANDLE_DRAG_VISUAL & MouseHandlerCallbacks.HANDLE_TOOLTIP & MouseHandlerCallbacks.HANDLE_PLACING_POSITION;
        }
    }

    public bool SinglePlacementDraggingEnabled
    {
        get
        {
            return IsObjectDraggable();
        }
    }

    public void SetMouseController(MouseController currentMouseController)
    {
        mouseController = currentMouseController;
    }

    public bool IsObjectDraggable()
    {
        if (BuildMode == BuildMode.FLOOR || BuildMode == BuildMode.DECONSTRUCT || BuildMode == BuildMode.UTILITY || BuildMode == BuildMode.MINE)
        {
            // floors are draggable
            return true;
        }

        if (BuildMode == BuildMode.ROOMBEHAVIOR)
        {
            // Room Behaviors are not draggable
            return false;
        }

        Furniture proto = PrototypeManager.Furniture.Get(BuildModeType);

        return proto.DragType != "single";
    }

    public string GetFloorTile()
    {
        return buildModeTile.ToString();
    }

    public void SetBuildMode(BuildMode newMode, string type = null, bool useCratedObject = false, bool startBuildMode = true)
    {
        Building = true;
        BuildMode = newMode;
        BuildModeType = type;

        if (newMode == BuildMode.FLOOR)
        {
            buildModeTile = PrototypeManager.TileType.Get(type);
        }
        else if (newMode == BuildMode.FURNITURE)
        {
            this.useCratedObject = useCratedObject;
            CurrentPreviewRotation = 0f;
        }

        if (startBuildMode)
        {
            mouseController.ChangeMouseMode(MouseController.MouseMode.BUILD);
        }
    }

    public void SetModeBuildTile(TileType type)
    {
        Building = true;
        BuildMode = BuildMode.FLOOR;
        buildModeTile = type;
        mouseController.ChangeMouseMode(MouseController.MouseMode.BUILD);
    }

    public void SetMode_DesignateRoomBehavior(string type)
    {
        Building = true;
        BuildMode = BuildMode.ROOMBEHAVIOR;
        BuildModeType = type;
        mouseController.ChangeMouseMode(MouseController.MouseMode.BUILD);
    }

    public void SetMode_BuildFurniture(string type, bool useCratedObject = false)
    {
        // Wall is not a Tile!  Wall is an "Furniture" that exists on TOP of a tile.
        Building = true;
        BuildMode = BuildMode.FURNITURE;
        BuildModeType = type;
        this.useCratedObject = useCratedObject;
        CurrentPreviewRotation = 0f;
        mouseController.ChangeMouseMode(MouseController.MouseMode.BUILD);
    }

    public void SetMode_BuildUtility(string type)
    {
        // Wall is not a Tile!  Wall is an "Furniture" that exists on TOP of a tile.
        Building = true;
        BuildMode = BuildMode.UTILITY;
        BuildModeType = type;
        mouseController.ChangeMouseMode(MouseController.MouseMode.BUILD);
    }

    public void SetMode_Deconstruct()
    {
        Building = true;
        BuildMode = BuildMode.DECONSTRUCT;
        mouseController.ChangeMouseMode(MouseController.MouseMode.BUILD);
    }

    public void SetMode_Mine()
    {
        Building = true;
        BuildMode = BuildMode.MINE;
        mouseController.ChangeMouseMode(MouseController.MouseMode.BUILD);
    }

    public void DoBuild(Tile tile)
    {
        if (BuildMode == BuildMode.ROOMBEHAVIOR)
        {
            string roomBehaviorType = BuildModeType;

            if (tile.Room != null && WorldController.Instance.World.IsRoomBehaviorValidForRoom(roomBehaviorType, tile.Room))
            {
                RoomBehavior proto = PrototypeManager.RoomBehavior.Get(roomBehaviorType);
                tile.Room.DesignateRoomBehavior(proto.Clone());
            }
        }
        else if (BuildMode == BuildMode.FURNITURE)
        {
            // Create the Furniture and assign it to the tile
            // Can we build the furniture in the selected tile?
            // Run the ValidPlacement function!
            string furnitureType = BuildModeType;

            if (
                World.Current.FurnitureManager.IsPlacementValid(furnitureType, tile, CurrentPreviewRotation) &&
                World.Current.FurnitureManager.IsWorkSpotClear(furnitureType, tile) &&
                DoesFurnitureBuildJobOverlapExistingBuildJob(tile, furnitureType, CurrentPreviewRotation) == false)
            {
                // This tile position is valid for this furniture

                // Check if there is existing furniture in this tile. If so delete it.
                if (tile.Furniture != null)
                {
                    tile.Furniture.SetDeconstructJob();
                }

                // Create a job for it to be build
                Job job;

                Furniture toBuildProto = PrototypeManager.Furniture.Get(furnitureType);
                OrderAction orderAction = toBuildProto.GetOrderAction<Build>();
                if (orderAction != null)
                {
                    job = orderAction.CreateJob(tile, furnitureType);
                    if (useCratedObject)
                    {
                        // We want to use a crated furniture, so set requested items to crated version.
                        job.RequestedItems.Clear();
                        job.RequestedItems.Add(this.BuildModeType, new ProjectPorcupine.Jobs.RequestedItem(this.BuildModeType, 1));
                        useCratedObject = false;
                    }

                    // this is here so OrderAction can be used for utility as well as furniture
                    job.OnJobCompleted += (theJob) => World.Current.FurnitureManager.ConstructJobCompleted(theJob);
                }
                else
                {
                    UnityDebugger.Debugger.LogError("BuildModeController", "There is no furniture job prototype for '" + furnitureType + "'");
                    job = new Job(tile, furnitureType, World.Current.FurnitureManager.ConstructJobCompleted, 0.1f, null, Job.JobPriority.High);
                    job.adjacent = true;
                    job.Description = "job_build_" + furnitureType + "_desc";
                }

                Furniture furnitureToBuild = PrototypeManager.Furniture.Get(furnitureType).Clone();
                furnitureToBuild.SetRotation(CurrentPreviewRotation);
                job.buildablePrototype = furnitureToBuild;

                // Add the job to the queue or build immediately if in Dev mode
                if (SettingsKeyHolder.DeveloperMode)
                {
                    World.Current.FurnitureManager.PlaceFurniture(furnitureToBuild, job.tile);
                }
                else
                {
                    for (int x_off = tile.X; x_off < (tile.X + job.buildablePrototype.Width); x_off++)
                    {
                        for (int y_off = tile.Y; y_off < (tile.Y + job.buildablePrototype.Height); y_off++)
                        {
                            // FIXME: I don't like having to manually and explicitly set
                            // flags that prevent conflicts. It's too easy to forget to set/clear them!
                            Tile offsetTile = World.Current.GetTileAt(x_off, y_off, tile.Z);
                            HashSet<Job> pendingBuildJobs = WorldController.Instance.World.GetTileAt(x_off, y_off, tile.Z).PendingBuildJobs;
                            if (pendingBuildJobs != null)
                            {
                                // if the existing buildJobs furniture is replaceable by the current furnitureType,
                                // we can pretend it does not overlap with the new build

                                // We should only have 1 furniture building job per tile, so this should return that job and only that job
                                IEnumerable<Job> pendingFurnitureJob = pendingBuildJobs.Where(pendingJob => pendingJob.buildablePrototype.GetType() == typeof(Furniture));
                                if (pendingFurnitureJob.Count() == 1)
                                {
                                    pendingFurnitureJob.Single().CancelJob();
                                }
                            }

                            offsetTile.PendingBuildJobs.Add(job);
                            job.OnJobStopped += (theJob) => offsetTile.PendingBuildJobs.Remove(job);
                        }
                    }

                    World.Current.jobQueue.Enqueue(job);

                    // Let our workspot tile know it is reserved for us
                    World.Current.ReserveTileAsWorkSpot((Furniture)job.buildablePrototype, job.tile);
                }
            }
        }
        else if (BuildMode == BuildMode.UTILITY)
        {
            // Create the Furniture and assign it to the tile
            // Can we build the furniture in the selected tile?
            // Run the ValidPlacement function!
            string utilityType = BuildModeType;
            if (
                World.Current.UtilityManager.IsPlacementValid(utilityType, tile) &&
                DoesSameUtilityTypeAlreadyExist(utilityType, tile) == false &&
                DoesUtilityBuildJobOverlapExistingBuildJob(utilityType, tile) == false)
            {
                // This tile position is valid for this furniture

                // Create a job for it to be build
                Job job;

                Utility toBuildProto = PrototypeManager.Utility.Get(utilityType);
                OrderAction orderAction = toBuildProto.GetOrderAction<Build>();
                if (orderAction != null)
                {
                    job = orderAction.CreateJob(tile, utilityType);

                    // this is here so OrderAction can be used for utility as well as furniture
                    job.OnJobCompleted += (theJob) => World.Current.UtilityManager.ConstructJobCompleted(theJob);
                }
                else
                {
                    UnityDebugger.Debugger.LogError("BuildModeController", "There is no furniture job prototype for '" + utilityType + "'");
                    job = new Job(tile, utilityType, World.Current.UtilityManager.ConstructJobCompleted, 0.1f, null, Job.JobPriority.High);
                    job.Description = "job_build_" + utilityType + "_desc";
                }

                job.buildablePrototype = PrototypeManager.Utility.Get(utilityType);

                // Add the job to the queue or build immediately if in dev mode
                if (SettingsKeyHolder.DeveloperMode)
                {
                    World.Current.UtilityManager.PlaceUtility(job.Type, job.tile, true);
                }
                else
                {
                    // FIXME: I don't like having to manually and explicitly set
                    // flags that preven conflicts. It's too easy to forget to set/clear them!
                    Tile offsetTile = World.Current.GetTileAt(tile.X, tile.Y, tile.Z);
                    offsetTile.PendingBuildJobs.Add(job);
                    job.OnJobStopped += (theJob) => offsetTile.PendingBuildJobs.Remove(job);

                    World.Current.jobQueue.Enqueue(job);
                }
            }
        }
        else if (BuildMode == BuildMode.FLOOR)
        {
            TileType tileType = buildModeTile;

            if (
                tile.Type != tileType &&
                tile.Furniture == null &&
                tile.PendingBuildJobs.Count == 0 &&
                tileType.CanBuildHere(tile))
            {
                // This tile position is valid tile type

                // Create a job for it to be build
                Job buildingJob;
                OrderAction orderAction = tileType.GetOrderAction<ChangeTileType>();
                if (orderAction != null)
                {
                    buildingJob = orderAction.CreateJob(tile, tileType.Type);

                    if (SettingsKeyHolder.DeveloperMode)
                    {
                        buildingJob.tile.SetTileType(buildingJob.JobTileType);
                    }
                    else
                    {
                        buildingJob.OnJobStopped += (theJob) => theJob.tile.PendingBuildJobs.Remove(theJob);
                        WorldController.Instance.World.jobQueue.Enqueue(buildingJob);
                    }
                }
                else
                {
                    UnityDebugger.Debugger.LogError("BuildModeController", "There is no order action called ChangeTileType in tileType: " + tileType.Type);
                }
            }
        }
        else if (BuildMode == BuildMode.DECONSTRUCT)
        {
            bool canDeconstructAll = SettingsKeyHolder.DeveloperMode;

            if (tile.Furniture != null && (canDeconstructAll || tile.Furniture.HasTypeTag("Non-deconstructible") == false))
            {
                // check if this is a WALL neighbouring a pressured and pressureless environment, and if so, bail
                if (IsTilePartOfPressuredRoom(tile))
                {
                    return;
                }

                tile.Furniture.SetDeconstructJob();
            }
            else if (tile.PendingBuildJobs != null && tile.PendingBuildJobs.Count > 0)
            {
                tile.PendingBuildJobs.Last().CancelJob();
            }
            else if (tile.Utilities.Count > 0)
            {
                tile.Utilities.Last().Value.SetDeconstructJob();
            }
        }
        else if (BuildMode == BuildMode.MINE)
        {
            if (tile.Furniture != null)
            {
                Job existingMineJob;
                bool hasMineJob = tile.Furniture.Jobs.HasJobWithPredicate(x => x.OrderName == typeof(Mine).Name, out existingMineJob);
                if (!hasMineJob)
                {
                    OrderAction mineAction = tile.Furniture.GetOrderAction<Mine>();
                    if (mineAction != null)
                    {
                        // check if this is a WALL neighbouring a pressured and pressureless environment, and if so, bail
                        if (IsTilePartOfPressuredRoom(tile))
                        {
                            return;
                        }

                        Job job = mineAction.CreateJob(tile, null);
                        if (SettingsKeyHolder.DeveloperMode)
                        {
                            // complete job right away, needs buildable
                            job.buildable = tile.Furniture;
                            job.DoWork(0);
                        }
                        else
                        {
                            tile.Furniture.Jobs.Add(job);
                        }
                    }
                }
            }
        }
        else
        {
            UnityDebugger.Debugger.LogError("BuildModeController", "UNIMPLEMENTED BUILD MODE");
        }
    }

    public bool DoesFurnitureBuildJobOverlapExistingBuildJob(Tile t, string furnitureType, float rotation = 0)
    {
        Furniture furnitureToBuild = PrototypeManager.Furniture.Get(furnitureType).Clone();
        furnitureToBuild.SetRotation(rotation);

        for (int x_off = t.X; x_off < (t.X + furnitureToBuild.Width); x_off++)
        {
            for (int y_off = t.Y; y_off < (t.Y + furnitureToBuild.Height); y_off++)
            {
                HashSet<Job> pendingBuildJobs = WorldController.Instance.World.GetTileAt(x_off, y_off, t.Z).PendingBuildJobs;
                if (pendingBuildJobs != null)
                {
                    // if the existing buildJobs furniture is replaceable by the current furnitureType,
                    // we can pretend it does not overlap with the new build

                    // We should only have 1 furniture building job per tile, so this should return that job and only that job
                    IEnumerable<Job> pendingFurnitureJob = pendingBuildJobs.Where(job => job.buildablePrototype.GetType() == typeof(Furniture));
                    if (pendingFurnitureJob.Count() == 1)
                    {
                        return !furnitureToBuild.ReplaceableFurniture.Any(pendingFurnitureJob.Single().buildablePrototype.HasTypeTag);
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Does the utility build job overlap an existing utility build job of the same type.
    /// </summary>
    /// <returns><c>true</c>, if utility build job overlaps an existing utility build job of the same type, <c>false</c> otherwise.</returns>
    /// <param name="utilityType">Utility type to check for.</param>
    /// <param name="tile">Tile to check for pending buildjobs.</param>
    public bool DoesUtilityBuildJobOverlapExistingBuildJob(string utilityType, Tile tile)
    {
        HashSet<Job> pendingBuildJobs = tile.PendingBuildJobs;
        if (pendingBuildJobs != null)
        {
            IEnumerable<Job> pendingUtilityJob = pendingBuildJobs.Where(job => job.buildablePrototype.GetType() == typeof(Utility));
            if (pendingUtilityJob.Count() > 0)
            {
                return pendingUtilityJob.Any(job => ((Utility)job.buildablePrototype).Type == utilityType);
            }
        }

        return false;
    }

    public bool DoesSameUtilityTypeAlreadyExist(string type, Tile tile)
    {
        Utility proto = PrototypeManager.Utility.Get(type);
        return tile.Utilities.ContainsKey(proto.Type);
    }

    public void HandleTooltip(Vector2 position, MouseCursor cursor, bool isDragging)
    {
        cursor.Reset();
        Tile t = WorldController.Instance.GetTileAtWorldCoord(position);

        int validPostionCount = 0;
        int invalidPositionCount = 0;
        List<GameObject> dragPreviewGameObjects = WorldController.Instance.MouseController.DragPreviewGameObjects;

        // Placing furniture object.
        if (BuildMode == BuildMode.FURNITURE)
        {
            cursor.DisplayCursorInfo(TextAnchor.LowerRight, LocalizationTable.GetLocalization(PrototypeManager.Furniture.Get(BuildModeType).GetName()), MouseCursor.DefaultTint);

            // Dragging and placing multiple furniture.
            if (t != null && isDragging == true && dragPreviewGameObjects.Count > 1)
            {
                for (int i = 0; i < dragPreviewGameObjects.Count; i++)
                {
                    Tile t1 = WorldController.Instance.GetTileAtWorldCoord(dragPreviewGameObjects[i].transform.position);
                    if (World.Current.FurnitureManager.IsPlacementValid(BuildModeType, t1) &&
                       (t1.PendingBuildJobs == null || t1.PendingBuildJobs.Count == 0))
                    {
                        validPostionCount++;
                    }
                    else
                    {
                        invalidPositionCount++;
                    }
                }

                string currentBuildRequirements = string.Empty;

                Build buildOrder = PrototypeManager.Furniture.Get(BuildModeType).GetOrderAction<Build>();
                if (buildOrder != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in buildOrder.Inventory)
                    {
                        sb.Append(string.Format("{0}x {1}", item.Amount * validPostionCount, LocalizationTable.GetLocalization(item.Type)));
                        if (buildOrder.Inventory.Count > 1)
                        {
                            sb.AppendLine();
                        }
                    }

                    currentBuildRequirements = sb.ToString();
                }
                else
                {
                    UnityDebugger.Debugger.LogError("BuildOrder is null");
                }

                cursor.DisplayCursorInfo(TextAnchor.UpperLeft, validPostionCount.ToString(), Color.green);
                cursor.DisplayCursorInfo(TextAnchor.UpperRight, invalidPositionCount.ToString(), Color.red);
                cursor.DisplayCursorInfo(TextAnchor.LowerLeft, currentBuildRequirements, MouseCursor.DefaultTint);
            }
        }
        else if (BuildMode == BuildMode.FLOOR)
        {
            // Placing tiles and dragging.
            if (t != null && isDragging == true && dragPreviewGameObjects.Count >= 1)
            {
                cursor.DisplayCursorInfo(TextAnchor.UpperLeft, dragPreviewGameObjects.Count.ToString(), MouseCursor.DefaultTint);
                cursor.DisplayCursorInfo(TextAnchor.LowerLeft, LocalizationTable.GetLocalization(GetFloorTile()), MouseCursor.DefaultTint);
            }
        }
    }

    public void HandleDrag(MouseController.DragParameters dragParams)
    {
        for (int x = dragParams.StartX; x <= dragParams.EndX; x++)
        {
            // Variables for the for-loop over the y-coordinates.
            // These are used to determine whether the loop should run from highest to lowest values or vice-versa.
            // The tiles are thus added in a snake or zig-zag pattern, which makes building more efficient.
            int begin = (x - dragParams.StartX) % 2 == 0 ? dragParams.StartY : dragParams.EndY;
            int stop = (x - dragParams.StartX) % 2 == 0 ? dragParams.EndY + 1 : dragParams.StartY - 1;
            int increment = (x - dragParams.StartX) % 2 == 0 ? 1 : -1;

            for (int y = begin; y != stop; y += increment)
            {
                Tile tile = WorldController.Instance.World.GetTileAt(x, y, WorldController.Instance.CameraController.CurrentLayer);
                if (tile == null)
                {
                    // Trying to build off the map, bail out of this cycle.
                    continue;
                }

                if (BuildMode == BuildMode.FURNITURE)
                {
                    // Check for furniture dragType.
                    Furniture proto = PrototypeManager.Furniture.Get(BuildModeType);

                    if (IsTilePartOfDrag(tile, dragParams, proto.DragType))
                    {
                        // Call BuildModeController::DoBuild().
                        DoBuild(tile);
                    }
                }
                else if (BuildMode == BuildMode.UTILITY)
                {
                    // Check for furniture dragType.
                    Utility proto = PrototypeManager.Utility.Get(BuildModeType);

                    if (IsTilePartOfDrag(tile, dragParams, proto.DragType))
                    {
                        // Call BuildModeController::DoBuild().
                        DoBuild(tile);
                    }
                }
                else
                {
                    DoBuild(tile);
                }
            }
        }

        // In devmode, utilities don't build their network, and one of the utilities built needs UpdateGrid called explicitly after all are built.
        if (BuildMode == BuildMode.UTILITY && SettingsKeyHolder.DeveloperMode)
        {
            Tile firstTile = World.Current.GetTileAt(dragParams.RawStartX, dragParams.RawStartY, WorldController.Instance.CameraController.CurrentLayer);
            Utility utility = firstTile.Utilities[PrototypeManager.Utility.Get(BuildModeType).Type];
            utility.UpdateGrid(utility);
        }
    }

    public List<GameObject> HandleDragVisual(MouseController.DragParameters dragParams, Transform parent)
    {
        List<GameObject> objects = new List<GameObject>();

        for (int x = dragParams.StartX; x <= dragParams.EndX; x++)
        {
            for (int y = dragParams.StartY; y <= dragParams.EndY; y++)
            {
                Tile t = WorldController.Instance.World.GetTileAt(x, y, WorldController.Instance.CameraController.CurrentLayer);
                if (t != null)
                {
                    // Display the building hint on top of this tile position.
                    if (BuildMode == BuildMode.FURNITURE)
                    {
                        Furniture proto = PrototypeManager.Furniture.Get(BuildModeType);
                        if (IsTilePartOfDrag(t, dragParams, proto.DragType))
                        {
                            objects.Add(ShowFurnitureSpriteAtTile(BuildModeType, t));
                            objects.Add(ShowWorkSpotSpriteAtTile(BuildModeType, t));
                        }
                    }
                    else if (BuildMode == BuildMode.UTILITY)
                    {
                        Utility proto = PrototypeManager.Utility.Get(BuildModeType);
                        if (IsTilePartOfDrag(t, dragParams, proto.DragType))
                        {
                            objects.Add(ShowUtilitySpriteAtTile(BuildModeType, t));
                        }
                    }
                    else
                    {
                        // Show generic visuals
                        GameObject go = SimplePool.Spawn(WorldController.Instance.CircleCursorPrefab, new Vector3(x, y, WorldController.Instance.CameraController.CurrentLayer), Quaternion.identity);
                        go.transform.SetParent(parent, true);
                        go.GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite("UI", "CursorCircle");
                        objects.Add(go);
                    }
                }
            }
        }

        return objects;
    }

    public Vector3 HandlePlacingPosition(Vector3 position)
    {
        if (BuildMode == BuildMode.FURNITURE
            && PrototypeManager.Furniture.Has(BuildModeType)
            && (PrototypeManager.Furniture.Get(BuildModeType).Width > 1
            || PrototypeManager.Furniture.Get(BuildModeType).Height > 1))
        {
            Furniture furnitureToBuild = PrototypeManager.Furniture.Get(BuildModeType).Clone(); // Get a copy
            furnitureToBuild.SetRotation(CurrentPreviewRotation);
            Sprite sprite = WorldController.Instance.FurnitureSpriteController.GetSpriteForFurniture(furnitureToBuild.Type);

            // Use the center of the Furniture.
            return position - ImageUtils.SpritePivotOffset(sprite, CurrentPreviewRotation);
        }
        else
        {
            return position;
        }
    }

    /// <summary>
    /// BuildModeController doesn't handle clicks, and will throw an error on call.
    /// </summary>
    public void HandleClick(Vector2 position, int mouseKey)
    {
        throw new NotImplementedException();
    }

    #region HelperFunctions

    /// <summary>
    /// Checks whether a tile is valid for the drag type, given the drag parameters
    /// Returns true if tile should be included, false otherwise.
    /// </summary>
    private bool IsTilePartOfDrag(Tile tile, MouseController.DragParameters dragParams, string dragType)
    {
        switch (dragType)
        {
            case "border":
                return tile.X == dragParams.StartX || tile.X == dragParams.EndX || tile.Y == dragParams.StartY || tile.Y == dragParams.EndY;
            case "path":
                bool withinXBounds = dragParams.StartX <= tile.X && tile.X <= dragParams.EndX;
                bool onPath = tile.Y == dragParams.RawStartY || tile.X == dragParams.RawEndX;
                return withinXBounds && onPath;
            default:
                return true;
        }
    }

    private GameObject ShowFurnitureSpriteAtTile(string furnitureType, Tile tile)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(furnitureParent.transform, true);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Jobs";
        sr.sprite = WorldController.Instance.FurnitureSpriteController.GetSpriteForFurniture(furnitureType);

        if (World.Current.FurnitureManager.IsPlacementValid(furnitureType, tile, CurrentPreviewRotation) &&
            World.Current.FurnitureManager.IsWorkSpotClear(furnitureType, tile) &&
            DoesFurnitureBuildJobOverlapExistingBuildJob(tile, furnitureType, CurrentPreviewRotation) == false)
        {
            sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            sr.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        go.name = furnitureType + "_p_" + tile.X + "_" + tile.Y + "_" + tile.Z;
        go.transform.position = tile.Vector3 + ImageUtils.SpritePivotOffset(sr.sprite, CurrentPreviewRotation);
        go.transform.Rotate(0, 0, CurrentPreviewRotation);

        return go;
    }

    private GameObject ShowWorkSpotSpriteAtTile(string furnitureType, Tile tile)
    {
        Furniture proto = PrototypeManager.Furniture.Get(furnitureType);

        // if the workspot is inside the furniture, there's no reason to show it separately
        if (proto.Jobs.WorkSpotOffset.x >= 0 && proto.Jobs.WorkSpotOffset.x < proto.Width && proto.Jobs.WorkSpotOffset.y >= 0 && proto.Jobs.WorkSpotOffset.y < proto.Height)
        {
            return null;
        }

        GameObject go = new GameObject();
        go.transform.SetParent(furnitureParent.transform, true);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Jobs";
        sr.sprite = SpriteManager.GetSprite("UI", "WorkSpotIndicator");

        if (World.Current.FurnitureManager.IsPlacementValid(furnitureType, tile) &&
            World.Current.FurnitureManager.IsWorkSpotClear(furnitureType, tile) &&
            DoesFurnitureBuildJobOverlapExistingBuildJob(tile, furnitureType) == false)
        {
            sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            sr.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        go.transform.position = new Vector3(tile.X + proto.Jobs.WorkSpotOffset.x, tile.Y + proto.Jobs.WorkSpotOffset.y, WorldController.Instance.CameraController.CurrentLayer);

        return go;
    }

    private GameObject ShowUtilitySpriteAtTile(string type, Tile tile)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(furnitureParent.transform, true);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Jobs";
        sr.sprite = WorldController.Instance.UtilitySpriteController.GetSpriteForUtility(type);

        if (World.Current.UtilityManager.IsPlacementValid(type, tile) &&
            DoesSameUtilityTypeAlreadyExist(type, tile) &&
            DoesUtilityBuildJobOverlapExistingBuildJob(type, tile) == false)
        {
            sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            sr.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        go.transform.position = new Vector3(tile.X, tile.Y, WorldController.Instance.CameraController.CurrentLayer);

        return go;
    }

    private bool IsTilePartOfPressuredRoom(Tile tile)
    {
        // check if this is a WALL neighbouring a pressured and pressureless environment, and if so, bail
        if (tile.Furniture.HasTypeTag("Wall"))
        {
            Tile[] neighbors = tile.GetNeighbours(); // diagOkay??
            int pressuredNeighbors = 0;
            int vacuumNeighbors = 0;
            foreach (Tile neighbor in neighbors)
            {
                if (neighbor != null && neighbor.Room != null)
                {
                    if (neighbor.Room.IsOutsideRoom() || MathUtilities.IsZero(neighbor.Room.Atmosphere.TotalGas))
                    {
                        vacuumNeighbors++;
                    }
                    else
                    {
                        pressuredNeighbors++;
                    }
                }
            }

            if (vacuumNeighbors > 0 && pressuredNeighbors > 0)
            {
                UnityDebugger.Debugger.Log("BuildModeController", "Someone tried to deconstruct a wall between a pressurized room and vacuum!");
                return true;
            }
        }

        return false;
    }

    #endregion

    // Rotate the preview furniture to the left.
    private void RotateFurnitireLeft()
    {
        if (BuildMode == BuildMode.FURNITURE && PrototypeManager.Furniture.Get(BuildModeType).CanRotate)
        {
            CurrentPreviewRotation = (CurrentPreviewRotation + 90) % 360;
        }
    }

    // Rotate the preview furniture to the right.
    private void RotateFurnitireRight()
    {
        if (BuildMode == BuildMode.FURNITURE && PrototypeManager.Furniture.Get(BuildModeType).CanRotate)
        {
            CurrentPreviewRotation = (CurrentPreviewRotation - 90) % 360;
        }
    }
}
