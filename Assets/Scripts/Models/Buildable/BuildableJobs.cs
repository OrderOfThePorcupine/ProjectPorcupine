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
using UnityEngine;

[MoonSharpUserData]
public class BuildableJobs
{
    private IBuildable buildable;
    private List<Job> activeJobs;
    private List<Job> pausedJobs;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildableJobs"/> class.
    /// </summary>
    /// <param name="buildable">The current buildable.</param>
    public BuildableJobs(IBuildable buildEntity)
    {
        buildable = buildEntity;
        activeJobs = new List<Job>();
        pausedJobs = new List<Job>();

        WorkSpotOffset = Vector2.zero;
        InputSpotOffset = Vector2.zero;
        OutputSpotOffset = Vector2.zero;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildableJobs"/> class by copying some of the values from another instance.
    /// </summary>
    /// <param name="buildEntity">The current buildable.</param>
    /// <param name="jobs">The buildable jobs to copy from.</param>
    public BuildableJobs(IBuildable buildEntity, BuildableJobs jobs)
    {
        buildable = buildEntity;
        activeJobs = new List<Job>();
        pausedJobs = new List<Job>();

        WorkSpotOffset = jobs.WorkSpotOffset;
        InputSpotOffset = jobs.InputSpotOffset;
        OutputSpotOffset = jobs.OutputSpotOffset;
    }

    /// <summary>
    /// Gets the spot offset where the Character will stand when he is using the buildable. This is relative to the bottom
    /// left tile of the sprite. This can be outside of the actual buildable.
    /// </summary>
    /// <value>The spot offset where the Character will stand when he uses the buildable.</value>
    public Vector2 WorkSpotOffset { get; private set; }

    /// <summary>
    /// Gets the spot offset where inventory is inserted for a Job with this buildable.
    /// </summary>
    /// <value>The spawn spot offset.</value>
    public Vector2 InputSpotOffset { get; private set; }

    /// <summary>
    /// Gets the spot offset where inventory is spawn when a Job is done with this buildable.
    /// </summary>
    /// <value>The spawn spot offset.</value>
    public Vector2 OutputSpotOffset { get; private set; }

    /// <summary>
    /// Gets the tile that is used to do a job.
    /// </summary>
    /// <value>Tile that is used for jobs.</value>
    public Tile WorkSpotTile
    {
        get { return GetTileAtOffset(WorkSpotOffset); }
    }

    /// <summary>
    /// Gets the tile where inventory is placed to be used by this buildable.
    /// </summary>
    /// <value>Tile where inventory is placed to be used by this buildable.</value>
    public Tile InputSpotTile
    {
        get { return GetTileAtOffset(InputSpotOffset); }
    }

    /// <summary>
    /// Gets the tile that is used to spawn new objects (i.e. Inventory, Character).
    /// </summary>
    /// <value>Tile that is used to spawn objects (i.e. Inventory, Character).</value>
    public Tile OutputSpotTile
    {
        get { return GetTileAtOffset(OutputSpotOffset); }
    }

    /// <summary>
    /// How many active jobs are linked to this buildable.
    /// </summary>
    /// <value>The number of active jobs linked to this buildable.</value>
    public int Count
    {
        get { return activeJobs.Count; }
    }

    /// <summary>
    /// Gets the active <see cref="Job"/> with the specified index.
    /// </summary>
    /// <param name="i">The index.</param>
    public Job this[int i]
    {
        get { return activeJobs[i]; }
    }

    public void ReadOffsets(JToken jobToken)
    {
        if (jobToken == null)
        {
            return;
        }

        WorkSpotOffset = ReadVector(jobToken["WorkSpotOffset"]);
        InputSpotOffset = ReadVector(jobToken["InputSpotOffset"]);
        OutputSpotOffset = ReadVector(jobToken["OutputSpotOffset"]);
    }

    /// <summary>
    /// Checks for the first buildable job with specific condition.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="job">Job fulfilling predicate.</param>
    /// <returns>True if there is job with predicate.</returns>
    public bool HasJobWithPredicate(Func<Job, bool> predicate, out Job job)
    {
        job = activeJobs.FirstOrDefault(predicate);
        if (job == null)
        {
            job = pausedJobs.FirstOrDefault(predicate);
        }

        return job != null;
    }

    /// <summary>
    /// Checks if the work spot is contained within the buildable's height and width.
    /// </summary>
    /// <returns><c>true</c>, if the work spot is internal to the buildable, <c>false</c> otherwise.</returns>
    public bool WorkSpotIsInternal()
    {
        return WorkSpotOffset.x >= 0 && WorkSpotOffset.x < buildable.Width && WorkSpotOffset.y >= 0 && WorkSpotOffset.y < buildable.Height;
    }

    /// <summary>
    /// Link a job to the current buildable.
    /// </summary>
    /// <param name="job">The job that you want to link to the buildable.</param>
    public void Add(Job job)
    {
        if (buildable.IsBeingDestroyed)
        {
            return;
        }

        job.buildable = buildable;
        activeJobs.Add(job);
        job.OnJobStopped += OnJobStopped;
        World.Current.jobManager.Enqueue(job);
    }

    /// <summary>
    /// Cancel all the active jobs linked to the current buildable.
    /// </summary>
    public void CancelAll()
    {
        Job[] jobsArray = activeJobs.ToArray();
        foreach (Job job in jobsArray)
        {
            job.CancelJob();
        }
    }

    /// <summary>
    /// Resumes all the paused jobs linked to the current buildable.
    /// </summary>
    /// TODO: Refactor this when the new job system is implemented
    public void ResumeAll()
    {
        if (pausedJobs.Count > 0)
        {
            Job[] jobsArray = pausedJobs.ToArray();
            foreach (Job job in jobsArray)
            {
                Add(job);
                pausedJobs.Remove(job);
            }
        }
    }

    /// <summary>
    /// Pauses all the active jobs linked to the current buildable.
    /// </summary>
    /// TODO: Refactor this when the new job system is implemented
    public void PauseAll()
    {
        if (activeJobs.Count > 0)
        {
            Job[] jobsArray = activeJobs.ToArray();
            foreach (Job job in jobsArray)
            {
                pausedJobs.Add(job);

                // We formerly called Called job.CancelJob() here, but that is incorrect for pausing a job, we may have to do cleanup that is now no longer done
            }
        }
    }

    /// <summary>
    /// Remove the specified job. It removes the link to the buildable and the event.
    /// </summary>
    /// <param name="job">The job to remove.</param>
    private void Remove(Job job)
    {
        job.OnJobStopped -= OnJobStopped;
        activeJobs.Remove(job);
        job.buildable = null;
    }

    /// <summary>
    /// Removes all the active jobs.
    /// </summary>
    private void RemoveAll()
    {
        Job[] jobsArray = activeJobs.ToArray();
        foreach (Job job in jobsArray)
        {
            Remove(job);
        }
    }

    /// <summary>
    /// Called when a job stops to remove the job from the active jobs.
    /// </summary>
    /// <param name="job">The stopped job.</param>
    private void OnJobStopped(Job job)
    {
        Remove(job);
    }

    /// <summary>
    /// Gets the a tile at the buildable tile plus an offset.
    /// </summary>
    /// <returns>The a tile at the buildable tile plus an offset.</returns>
    /// <param name="offset">A an offset from the buildable Tile.</param>
    private Tile GetTileAtOffset(Vector2 offset)
    {
        return World.Current.GetTileAt(buildable.Tile.X + (int)offset.x, buildable.Tile.Y + (int)offset.y, buildable.Tile.Z);
    }

    private Vector2 ReadVector(JToken vectorToken)
    {
        Vector2 vector = new Vector2();

        if (vectorToken != null)
        {
            int x = PrototypeReader.ReadJson(0, vectorToken["X"]);
            int y = PrototypeReader.ReadJson(0, vectorToken["Y"]);
            vector.x = x;
            vector.y = y;
        }

        return vector;
    }
}
