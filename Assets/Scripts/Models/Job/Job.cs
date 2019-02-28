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
using ProjectPorcupine.Entities;
using ProjectPorcupine.Jobs;
using ProjectPorcupine.Localization;
using ProjectPorcupine.Pathfinding;
using ProjectPorcupine.Rooms;
using UnityEngine;

[MoonSharpUserData]
[System.Diagnostics.DebuggerDisplay("Job {JobObjectType}")]
public class Job : ISelectable
{
    // This class holds info for a queued up job, which can include
    // things like placing furniture, moving stored inventory,
    // working at a desk, and maybe even fighting enemies.
    public Tile tile;

    public IBuildable buildablePrototype;

    // The piece of furniture that owns this job. Frequently will be null.
    public IBuildable buildable;

    public bool canTakeFromStockpile = true;

    /// <summary>
    /// If true, the work will be carried out on any adjacent tile of the target tile rather than on it.
    /// </summary>
    public bool adjacent;

    /// <summary>
    /// If true the job is workable if ANY of the inventory requirements are met.
    /// Otherwise ALL requirements must be met before work can start.
    /// This is useful for stockpile/storage jobs which can accept many types of items.
    /// Defaults to false.
    /// </summary>
    public bool acceptsAny;

    protected float jobTimeRequired;

    protected bool jobRepeats = false;

    private List<string> jobWorkedLua;

    // The job has been stopped, either because it's non-repeating or was canceled.
    private List<string> jobCompletedLua;

    private List<Character> charsCantReach = new List<Character>();

    // Required for IPrototypable
    public Job()
    {
    }

    public Job(Tile tile, string type, Action<Job> jobComplete, float jobTime, RequestedItem[] requestedItems, Job.JobPriority jobPriority, string category, bool jobRepeats = false, bool need = false, bool critical = false, bool adjacent = false) :
        this(tile, type, jobComplete, jobTime, requestedItems, jobPriority, PrototypeManager.JobCategory.Get(category), jobRepeats, need, critical, adjacent)
    {
        // This is identical to the next structure, except the category is a string. Intended primarily for Lua
    }

    public Job(Tile tile, string type, Action<Job> jobComplete, float jobTime, RequestedItem[] requestedItems, Job.JobPriority jobPriority, JobCategory category, bool jobRepeats = false, bool need = false, bool critical = false, bool adjacent = false)
    {
        this.tile = tile;
        this.Type = type;
        this.OnJobCompleted += jobComplete;
        this.jobTimeRequired = this.JobTime = jobTime;
        this.jobRepeats = jobRepeats;
        this.IsNeed = need;
        this.Critical = critical;
        this.Priority = jobPriority;
        this.Category = category;
        this.adjacent = adjacent;
        this.IsActive = true;
        this.Description = "job_error_missing_desc";

        jobWorkedLua = new List<string>();
        jobCompletedLua = new List<string>();

        this.DeliveredItems = new Dictionary<string, Inventory>();
        this.RequestedItems = new Dictionary<string, RequestedItem>();

        if (requestedItems != null)
        {
            foreach (RequestedItem item in requestedItems)
            {
                this.RequestedItems[item.Type] = item.Clone();
            }
        }

        if (this.Category == null)
        {
            UnityDebugger.Debugger.LogError("Invalid category detected.");
        }
    }

    public Job(Tile tile, TileType jobTileType, Action<Job> jobCompleted, float jobTime, RequestedItem[] requestedItems, Job.JobPriority jobPriority, string category, bool jobRepeats = false, bool adjacent = false)
    {
        this.tile = tile;
        this.JobTileType = jobTileType;
        this.Type = jobTileType.LocalizationName;
        this.OnJobCompleted += jobCompleted;
        this.jobTimeRequired = this.JobTime = jobTime;
        this.jobRepeats = jobRepeats;
        this.Priority = jobPriority;
        this.Category = PrototypeManager.JobCategory.Get(category);
        this.adjacent = adjacent;
        this.Description = "job_error_missing_desc";
        this.IsActive = true;

        jobWorkedLua = new List<string>();
        jobCompletedLua = new List<string>();

        this.DeliveredItems = new Dictionary<string, Inventory>();
        this.RequestedItems = new Dictionary<string, RequestedItem>();
        if (requestedItems != null)
        {
            foreach (RequestedItem item in requestedItems)
            {
                this.RequestedItems[item.Type] = item.Clone();
            }
        }

        if (this.Category == null)
        {
            UnityDebugger.Debugger.LogError("Invalid category detected.");
        }
    }

    protected Job(Job other)
    {
        this.tile = other.tile;
        this.Type = other.Type;
        this.JobTileType = other.JobTileType;
        this.OnJobCompleted = other.OnJobCompleted;
        this.JobTime = other.JobTime;
        this.Priority = other.Priority;
        this.Category = other.Category;
        this.adjacent = other.adjacent;
        this.Description = other.Description;
        this.acceptsAny = other.acceptsAny;
        this.OrderName = other.OrderName;
        this.IsActive = true; // A copied job should always start out as active.

        jobWorkedLua = new List<string>(other.jobWorkedLua);
        jobCompletedLua = new List<string>(other.jobWorkedLua);

        this.DeliveredItems = new Dictionary<string, Inventory>();
        this.RequestedItems = new Dictionary<string, RequestedItem>();
        if (other.RequestedItems != null)
        {
            foreach (RequestedItem item in other.RequestedItems.Values)
            {
                this.RequestedItems[item.Type] = item.Clone();
            }
        }
    }

    // We have finished the work cycle and so things should probably get built or whatever.
    public event Action<Job> OnJobCompleted;

    public event Action<Job> OnJobStopped;

    // Gets called each time some work is performed -- maybe update the UI?
    public event Action<Job> OnJobWorked;

    public enum JobPriority
    {
        High,
        Medium,
        Low
    }

    public enum JobState
    {
        Active,
        CantReach,
        MissingInventory,
        Suspended
    }

    // The items needed to do this job.
    public Dictionary<string, RequestedItem> RequestedItems { get; set; }

    // The items that have been delivered to the jobsite.
    public Dictionary<string, Inventory> DeliveredItems { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; protected set; }

    /// <summary>
    /// Name of order that created this job. This should prevent multiple same orders on same things if not allowed.
    /// </summary>
    public string OrderName { get; set; }

    public string Type
    {
        get;
        protected set;
    }

    public bool IsNeed
    {
        get;
        protected set;
    }

    public bool Critical
    {
        get;
        protected set;
    }

    public bool IsBeingWorked { get; set; }

    public TileType JobTileType
    {
        get;
        protected set;
    }

    public float JobTime
    {
        get;
        protected set;
    }

    public JobPriority Priority
    {
        get;
        set;
    }

    public JobCategory Category
    {
        get;
        set;
    }

    public bool IsSelected
    {
        get;
        set;
    }

    public bool IsRepeating
    {
        get
        {
            return jobRepeats;
        }
    }

    public int CharsCantReachCount
    {
        get
        {
            return charsCantReach.Count;
        }
    }

    public Pathfinder.GoalEvaluator IsTileAtJobSite
    {
        get
        {
            if (tile == null)
            {
                return null;
            }

            // TODO: This doesn't handle multi-tile furniture
            return Pathfinder.GoalTileEvaluator(tile, adjacent);
        }
    }

    public RequestedItem[] GetInventoryRequirementValues()
    {
        return RequestedItems.Values.ToArray();
    }

    public void SetTileFromNeedFurniture(Tile currentTile, string needFurniture)
    {
        tile = Pathfinder.FindPathToFurniture(currentTile, needFurniture).Last();
    }

    public virtual Job Clone()
    {
        return new Job(this);
    }

    #region Callbacks
    public void RegisterJobCompletedCallback(string cb)
    {
        jobCompletedLua.Add(cb);
    }

    public void UnregisterJobCompletedCallback(string cb)
    {
        jobCompletedLua.Remove(cb);
    }

    public void RegisterJobWorkedCallback(string cb)
    {
        jobWorkedLua.Add(cb);
    }

    public void UnregisterJobWorkedCallback(string cb)
    {
        jobWorkedLua.Remove(cb);
    }
    #endregion

    public void DoWork(float workTime)
    {
        // We don't know if the Job can actually be worked, but still call the callbacks
        // so that animations and whatnot can be updated.
        if (OnJobWorked != null)
        {
            OnJobWorked(this);
        }

        foreach (string luaFunction in jobWorkedLua.ToList())
        {
            FunctionsManager.Furniture.Call(luaFunction, this);
        }

        // Check to make sure we actually have everything we need.
        // If not, don't register the work time.
        if (MaterialNeedsMet() == false)
        {
            return;
        }

        JobTime -= workTime;

        if (JobTime <= 0)
        {
            foreach (string luaFunction in jobCompletedLua.ToList())
            {
                FunctionsManager.Furniture.Call(luaFunction, this);
            }

            // Do whatever is supposed to happen with a job cycle completes.
            if (OnJobCompleted != null)
            {
                OnJobCompleted(this);
            }

            World.Current.jobManager.Remove(this);

            if (jobRepeats != true)
            {
                // Let everyone know that the job is officially concluded
                if (OnJobStopped != null)
                {
                    OnJobStopped(this);
                }
            }
            else
            {
                // This is a repeating job and must be reset.
                JobTime += jobTimeRequired;
            }
        }
    }

    public void SuspendCantReach()
    {
        World.Current.RoomManager.Removed += (room) => ClearCharCantReach();
        tile.TileChanged += (tile) => ClearCharCantReach();
        Suspend();
    }

    public void SuspendWaitingForInventory(string missing)
    {
        if (missing == "*")
        {
            World.Current.InventoryManager.InventoryCreated += InventoryAvailable;
        }
        else
        {
            World.Current.InventoryManager.RegisterInventoryTypeCreated(CheckIfInventorySufficient, missing);
        }

        Suspend();
    }

    public void InventoryAvailable(Inventory inventory)
    {
        IsActive = true;
        World.Current.InventoryManager.InventoryCreated -= InventoryAvailable;
    }

    public bool CheckIfInventorySufficient(Inventory inventory)
    {
        RequestedItem item = GetFirstDesiredItem();
        if (item.Type == inventory.Type)
        {
            IsActive = true;
            return true;
        }

        return false;
    }

    public void CancelJob()
    {
        if (OnJobStopped != null)
        {
            OnJobStopped(this);
        }

        // If we are a building job let our tile know we are no longer pending
        if (buildablePrototype != null)
        {
            // If we are a furniture building job, Let our workspot tile know it is no longer reserved for us.
            if (buildablePrototype.GetType() == typeof(Furniture))
            {
                World.Current.UnreserveTileAsWorkSpot((Furniture)buildablePrototype, tile);
            }
        }

        // Remove the job out of job queue.
        World.Current.jobManager.Remove(this);
    }

    /// <summary>
    /// Checks to see if the job has met the material requirements needed to do this job.
    /// </summary>
    /// <returns> Returns True if the job can do work based on material requirements.</returns>
    public bool MaterialNeedsMet()
    {
        if (acceptsAny && HasAnyMaterial())
        {
            return true;
        }

        if ((acceptsAny == false) && HasAllMaterial())
        {
            return true;
        }

        return false;
    }

    public bool HasAllMaterial()
    {
        if (RequestedItems == null)
        {
            return true;
        }

        foreach (RequestedItem item in RequestedItems.Values)
        {
            Inventory inventory;
            if (DeliveredItems.TryGetValue(item.Type, out inventory) == false || item.AmountNeeded(inventory) > 0)
            {
                return false;
            }
        }

        return true;
    }

    public bool HasAnyMaterial()
    {
        return DeliveredItems.Count > 0 && DeliveredItems.First().Value.StackSize > 0;
    }

    public int AmountDesiredOfInventoryType(string type)
    {
        RequestedItem requestedItem;
        if (RequestedItems.TryGetValue(type, out requestedItem) == false)
        {
            return 0;
        }

        Inventory inventory;
        if (DeliveredItems.TryGetValue(type, out inventory) == false)
        {
            inventory = null;
        }

        return requestedItem.AmountDesired(inventory);
    }

    public bool IsRequiredInventoriesAvailable()
    {
        return FulfillableInventoryRequirements() != null;
    }

    /// <summary>
    /// Returns the first fulfillable requirement of this job. Especially useful for jobs that has a long list of materials and can use any of them.
    /// </summary>
    public RequestedItem GetFirstFulfillableInventoryRequirement()
    {
        foreach (RequestedItem item in GetInventoryRequirementValues())
        {
            if (World.Current.InventoryManager.HasInventoryOfType(item.Type, canTakeFromStockpile))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// Fulfillable inventory requirements for job.
    /// </summary>
    /// <returns>A list of (string) Type for job inventory requirements that can be met. Returns null if the job requires materials which do not exist on the map.</returns>
    public List<string> FulfillableInventoryRequirements()
    {
        List<string> fulfillableInventoryRequirements = new List<string>();

        foreach (RequestedItem item in this.GetInventoryRequirementValues())
        {
            if (this.acceptsAny == false)
            {
                if (World.Current.InventoryManager.HasInventoryOfType(item.Type, canTakeFromStockpile) == false)
                {
                    // the job requires ALL inventory requirements to be met, and there is no source of a desired Type
                    return null;
                }
                else
                {
                    fulfillableInventoryRequirements.Add(item.Type);
                }
            }
            else if (World.Current.InventoryManager.HasInventoryOfType(item.Type, canTakeFromStockpile))
            {
                // there is a source for a desired Type that the job will accept
                fulfillableInventoryRequirements.Add(item.Type);
            }
        }

        return fulfillableInventoryRequirements;
    }

    public RequestedItem GetFirstDesiredItem()
    {
        foreach (RequestedItem item in RequestedItems.Values)
        {
            Inventory inventory;
            if (DeliveredItems.TryGetValue(item.Type, out inventory) == false)
            {
                inventory = null;
            }

            if (item.DesiresMore(inventory))
            {
                return item;
            }
        }

        return null;
    }

    public void DropPriority()
    {
        this.Priority = (Job.JobPriority)Mathf.Min((int)Job.JobPriority.Low, (int)Priority + 1);
    }

    public void RaisePriority()
    {
        this.Priority = (Job.JobPriority)Mathf.Max((int)Job.JobPriority.High, (int)Priority - 1);
    }

    public string GetName()
    {
        try
        {
            return LocalizationTable.GetLocalization(PrototypeManager.Furniture.Get(Type.ToString()).GetName());
        }
        catch
        {
            return LocalizationTable.GetLocalization(Type);
        }
    }

    public string GetDescription()
    {
        string description = "Requirements:\n";
        foreach (RequestedItem item in RequestedItems.Values)
        {
            description += string.Format("\t{0} {1}..{2}\n", item.Type, item.MinAmountRequested, item.MaxAmountRequested);

            // TODO: Not sure if this works or not.
            description = LocalizationTable.GetLocalization(description);
        }

        return description;
    }

    public string GetJobDescription()
    {
        return GetDescription();
    }

    /// <summary>
    /// Add the character to the list of characters that can not reach this job.
    /// </summary>
    public void AddCharCantReach(Character character)
    {
        if (!charsCantReach.Contains(character))
        {
            charsCantReach.Add(character);
        }
    }

    public bool CanCharacterReach(Character character)
    {
        return false == charsCantReach.Contains(character);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns> True if the character can reach the inventory it needs.</returns>
    public bool CanGetToInventory(Character character)
    {
        List<Tile> path = null;
        path = World.Current.InventoryManager.GetPathToClosestInventoryOfType(RequestedItems.Keys.ToArray(), character.CurrTile, canTakeFromStockpile);
        if (path != null && path.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Resets the list of character unable to reach the job.
    /// </summary>
    public void ClearCharCantReach()
    {
        World.Current.RoomManager.Removed -= (room) => ClearCharCantReach();
        if (tile != null)
        {
            tile.TileChanged -= (tile) => ClearCharCantReach();
        }

        charsCantReach.Clear();
        IsActive = true;
    }

    public IEnumerable<string> GetAdditionalInfo()
    {
        yield break;
    }

    /// <summary>
    /// Checks to see if a job can run, and suspends if it can't.
    /// </summary>
    /// <param name="characterRoom">Room character is in.</param>
    /// <param name="changeState">If true allows changing the state.</param>
    /// <returns>true if the job can be run.</returns>
    public JobState CanJobRun(Room characterRoom, bool changeState)
    {
        // If the job requires material but there is nothing available, store it in jobsWaitingForInventory
        if (RequestedItems.Count > 0 && GetFirstFulfillableInventoryRequirement() == null)
        {
            if (changeState)
            {
                string missing = acceptsAny ? "*" : GetFirstDesiredItem().Type;
                SuspendWaitingForInventory(missing);
            }

            return JobState.MissingInventory;
        }
        else if (tile != null)
        {
            List<Room> roomsChecked = new List<Room>();

            if (((adjacent == false && tile.IsEnterable() != Enterability.Never) ||
                (adjacent && tile.IsReachableFromAnyNeighbor(false))) &&
                tile.CanSee)
            {
                if (CanReachRoom(tile.Room, roomsChecked, characterRoom))
                {
                    return JobState.Active;
                }

                foreach (Tile neighbor in tile.GetNeighbours(false))
                {
                    if (CanReachRoom(neighbor.Room, roomsChecked, characterRoom))
                    {
                        return JobState.Active;
                    }
                }
            }

            if (changeState)
            {
                // No one can reach the job.
                SuspendCantReach();
            }
            return JobState.CantReach;
        }

        return JobState.Active;
    }

    private bool CanReachRoom(Room room, List<Room> roomsToCheck, Room characterRoom)
    {
        if (room == null)
        {
            return false;
        }

        if (roomsToCheck.Contains(room))
        {
            return false;
        }

        if (Pathfinder.IsRoomReachable(characterRoom, room) == false)
        {
            return false;
        }

        roomsToCheck.Add(room);

        return true;
    }

    private void Suspend()
    {
        IsActive = false;
    }
}
