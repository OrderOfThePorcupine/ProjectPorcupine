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
using ProjectPorcupine.Entities;
using ProjectPorcupine.Jobs;
using ProjectPorcupine.Pathfinding;

public class JobManager
{
    private Dictionary<JobCategory, HashSet<Job>> jobQueue;

    private CharacterJobPriority[] characterPriorityLevels = (CharacterJobPriority[])Enum.GetValues(typeof(CharacterJobPriority));

    private JobCategory[] categories;

    public JobManager()
    {
        jobQueue = new Dictionary<JobCategory, HashSet<Job>>();
        categories = PrototypeManager.JobCategory.Values.ToArray();
        foreach (JobCategory category in categories)
        {
            jobQueue[category] = new HashSet<Job>();
        }
    }

    public delegate void JobChanged(Job job);

    public event JobChanged JobCreated;

    public event JobChanged JobModified;

    public event JobChanged JobRemoved;

    /// <summary>
    /// Add a job to the JobQueue.
    /// </summary>
    /// <param name="job">The job to be inserted into the Queue.</param>
    public void Enqueue(Job job)
    {
        UnityDebugger.Debugger.LogFormat("JobManager","Enqueue({0})", job.Type);

        job.IsBeingWorked = false;

        if (job.Category == null)
        {
            UnityDebugger.Debugger.LogErrorFormat("JobManager","Invalid category for job {1}", job);
        }

        jobQueue[job.Category].Add(job);

        if (job.JobTime < 0)
        {
            // Job has a negative job time, so it's not actually
            // supposed to be queued up.  Just insta-complete it.
            job.DoWork(0);
            return;
        }

        if (JobCreated != null)
        {
            JobCreated(job);
        }
    }

    /// <summary>
    /// Search for a job that can be performed by the specified character. Tests that the job can be reached and there is enough inventory to complete it, somewhere.
    /// </summary>
    public Job GetJob(Character character)
    {
        UnityDebugger.Debugger.LogFormat("JobManager","{0},{1} GetJob() (Queue size: {2})", character.GetName(), character.ID, jobQueue.Count);
        if (jobQueue.Count == 0)
        {
            return null;
        }

        foreach (CharacterJobPriority charPriority in characterPriorityLevels)
        {
            List<JobCategory> jobTypes = character.CategoriesOfPriority(charPriority);
            foreach (JobCategory category in categories)
            {
                if (jobTypes.Contains(category) == false)
                {
                    continue;
                }

                UnityDebugger.Debugger.LogFormat("JobManager","{0} Looking for job of category {1} - {2} options available", character.Name, category.Type, jobQueue[category].Count);

                Job.JobPriority bestJobPriority = Job.JobPriority.Low;

                foreach (Job job in jobQueue[category])
                {
                    if (job.IsActive == false || job.IsBeingWorked == true)
                    {
                        continue;
                    }

                    // Lower numbers indicate higher priority.
                    if (bestJobPriority > job.Priority)
                    {
                        bestJobPriority = job.Priority;
                    }
                }

                Job bestJob = null;
                float bestJobPathtime = int.MaxValue;

                foreach (Job job in jobQueue[category])
                {
                    if (job.IsActive == false || job.IsBeingWorked == true || job.Priority != bestJobPriority)
                    {
                        continue;
                    }

                    if (CanJobRun(job))
                    {
                        float pathtime = Pathfinder.FindMinPathTime(character.CurrTile, job.tile, job.adjacent, bestJobPathtime);
                        if (pathtime < bestJobPathtime)
                        {
                            bestJob = job;
                            bestJobPathtime = pathtime;
                        }
                    }
                }

                if (bestJob != null)
                {
                    UnityDebugger.Debugger.LogFormat("JobManager","{0} Job Assigned {1} at {2}", character.ID, bestJob, bestJob.tile);
                    if (JobModified != null)
                    {
                        JobModified(bestJob);
                    }

                    return bestJob;
                }
            }
        }

        return null;
    }

    public void Remove(Job job)
    {
        jobQueue[job.Category].Remove(job);

        if (JobRemoved != null)
        {
            JobRemoved(job);
        }
    }

    /// <summary>
    /// Returns an IEnumerable for every job, including jobs that are in the waiting state.
    /// </summary>
    public IEnumerable<Job> PeekAllJobs()
    {
        foreach (IEnumerable<Job> queue in jobQueue.Values)
        {
            foreach (Job job in queue)
            {
                yield return job;
            }
        }
    }

    private bool CanJobRun(Job job)
    {
        // If the job requires material but there is nothing available, store it in jobsWaitingForInventory
        if (job.RequestedItems.Count > 0 && job.GetFirstFulfillableInventoryRequirement() == null)
        {
            string missing = job.acceptsAny ? "*" : job.GetFirstDesiredItem().Type;
            UnityDebugger.Debugger.LogFormat("JobManager"," - missingInventory {0}", missing);
            job.SuspendWaitingForInventory(missing);
            if (JobModified != null)
            {
                JobModified(job);
            }

            return false;
        }
        else if ((job.tile != null && job.tile.IsReachableFromAnyNeighbor(true) == false) ||
            job.CharsCantReachCount == World.Current.CharacterManager.Characters.Count)
        {
            // No one can reach the job.
            UnityDebugger.Debugger.LogFormat("JobManager","JobQueue", "- Job can't be reached");
            job.Suspend();
            if (JobModified != null)
            {
                JobModified(job);
            }

            return false;
        }
        else
        {
            UnityDebugger.Debugger.LogFormat("JobManager"," - {0}", job.acceptsAny ? "Any" : "All");
            foreach (RequestedItem item in job.RequestedItems.Values)
            {
                UnityDebugger.Debugger.LogFormat("JobManager","   - {0} Min: {1}, Max: {2}", item.Type, item.MinAmountRequested, item.MaxAmountRequested);
            }
        }

        return true;
    }
}
