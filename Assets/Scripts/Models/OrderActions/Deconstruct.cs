#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;

namespace ProjectPorcupine.OrderActions
{
    [Serializable]
    [OrderActionName("Deconstruct")]
    public class Deconstruct : OrderAction
    {
        public Deconstruct()
        {
            Category = PrototypeManager.JobCategory.Get("construct");
            Priority = Job.JobPriority.Medium;
        }

        private Deconstruct(Deconstruct other) : base(other)
        {
        }

        public override OrderAction Clone()
        {
            return new Deconstruct(this);
        }

        public override Job CreateJob(Tile tile, string type)
        {
            Job job = null;
            if (tile != null)
            {
                CheckJobFromFunction(JobTimeFunction, tile.Furniture);
            }
            else
            {
                UnityDebugger.Debugger.LogError("Deconstruct", "Invalid tile detected. If this wasn't a test, you have an issue.");
            }

            if (job == null)
            {
                job = new Job(
                tile,
                type,
                null,
                JobTime,
                null,
                Priority,
                Category);
                job.Description = "job_deconstruct_" + type + "_desc";
                job.adjacent = true;
                job.OrderName = Type;
            }

            return job;
        }
    }
}
