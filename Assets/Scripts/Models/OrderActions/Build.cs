#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Linq;
using ProjectPorcupine.Jobs;

namespace ProjectPorcupine.OrderActions
{
    [Serializable]
    [OrderActionName("Build")]
    public class Build : OrderAction
    {
        public Build()
        {
            Category = PrototypeManager.JobCategory.Get("construct");
            Priority = Job.JobPriority.Medium;
        }

        private Build(Build other) : base(other)
        {
        }

        public override OrderAction Clone()
        {
            return new Build(this);
        }

        public override Job CreateJob(Tile tile, string type)
        {
            Job job = null;
            if (tile != null)
            {
                job = CheckJobFromFunction(JobTimeFunction, tile.Furniture);
            } else
            {
                UnityDebugger.Debugger.LogError("Build", "Invalid tile detected. If this wasn't a test, you have an issue.");
            }

            if (job == null)
            {
                job = new Job(
                tile,
                type,
                null,
                JobTime,
                Inventory.Select(it => new RequestedItem(it.Key, it.Value)).ToArray(),
                Priority, 
                Category);
                job.Description = "job_build_" + type + "_desc";
                job.OrderName = Type;
            }

            return job;
        }
    }
}
