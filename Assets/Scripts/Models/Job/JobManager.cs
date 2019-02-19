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
using ProjectPorcupine.Entities;
using ProjectPorcupine.Jobs;

public class JobManager
{
    private HashSet<Job> jobQueue;

    public JobManager()
    {
        jobQueue = new HashSet<Job>();
    }

    public event Action<Job> OnJobCreated;

    public bool IsEmpty()
    {
        return jobQueue.Count == 0;
    }

    // Returns the job count in the queue.
    // (Necessary, since jobQueue is private).
    public int GetCount()
    {
        return jobQueue.Count;
    }

    /// <summary>
    /// Add a job to the JobQueue.
    /// </summary>
    /// <param name="job">The job to be inserted into the Queue.</param>
    public void Enqueue(Job job)
    {
        DebugLog("Enqueue({0})", job.Type);
        if (job.JobTime < 0)
        {
            // Job has a negative job time, so it's not actually
            // supposed to be queued up.  Just insta-complete it.
            job.DoWork(0);
            return;
        }

        // If the job requires material but there is nothing available, store it in jobsWaitingForInventory
        if (job.RequestedItems.Count > 0 && job.GetFirstFulfillableInventoryRequirement() == null)
        {
            string missing = job.acceptsAny ? "*" : job.GetFirstDesiredItem().Type;
            DebugLog(" - missingInventory {0}", missing);
            job.SuspendWaitingForInventory(missing);
        }
        else if ((job.tile != null && job.tile.IsReachableFromAnyNeighbor(true) == false) ||
            job.CharsCantReachCount == World.Current.CharacterManager.Characters.Count)
        {
            // No one can reach the job.
            DebugLog("JobQueue", "- Job can't be reached");
            job.Suspend();
        }
        else
        {
            DebugLog(" - {0}", job.acceptsAny ? "Any" : "All");
            foreach (RequestedItem item in job.RequestedItems.Values)
            {
                DebugLog("   - {0} Min: {1}, Max: {2}", item.Type, item.MinAmountRequested, item.MaxAmountRequested);
            }

            DebugLog(" - job ok");

            jobQueue.Add(job);
        }

        if (OnJobCreated != null)
        {
            OnJobCreated(job);
        }
    }

    /// <summary>
    /// Returns the first job from the JobQueue.
    /// </summary>
    public Job Dequeue()
    {
        if (jobQueue.Count == 0)
        {
            return null;
        }

        Job job = jobQueue.FirstOrDefault();
        jobQueue.Remove(job);
        return job;
    }

    /// <summary>
    /// Search for a job that can be performed by the specified character. Tests that the job can be reached and there is enough inventory to complete it, somewhere.
    /// </summary>
    public Job GetJob(Character character)
    {
        DebugLog("{0},{1} GetJob() (Queue size: {2})", character.GetName(), character.ID, jobQueue.Count);
        if (jobQueue.Count == 0)
        {
            return null;
        }

        // This makes a large assumption that we are the only one accessing the queue right now
        foreach (Job job in jobQueue)
        { 
            if (job.IsActive == false)
            {
                continue;
            }

            // TODO: This is a simplistic version and needs to be expanded.
            // If we can get all material and we can walk to the tile, the job is workable.
            if (job.IsRequiredInventoriesAvailable() && job.tile.IsReachableFromAnyNeighbor(true))
            {
                if (job.CanCharacterReach(character))
                {
                    UnityDebugger.Debugger.Log("JobQueue", "Character could not find a path to the job site.");
                    job.Suspend();
                    continue;
                }
                else if ((job.RequestedItems.Count > 0) && !job.CanGetToInventory(character))
                {
                    job.AddCharCantReach(character);

                    // Is this a bug?  Or a warning
                    // @ Decide
                    UnityDebugger.Debugger.Log("JobQueue", "Character could not find a path to any inventory available.");
                    continue;
                }

                jobQueue.Remove(job);
                return job;
            }

            DebugLog(" - job failed requirements, test the next.");
        }

        return null;
    }

    public void Remove(Job job)
    {
        jobQueue.Remove(job);
    }

    /// <summary>
    /// Returns an IEnumerable for every job, including jobs that are in the waiting state.
    /// </summary>
    public IEnumerable<Job> PeekAllJobs()
    {
        return jobQueue;
    }

    [System.Diagnostics.Conditional("FSM_DEBUG_LOG")]
    private void DebugLog(string message, params object[] par)
    {
        UnityDebugger.Debugger.LogFormat("FSM", message, par);
    }
}
