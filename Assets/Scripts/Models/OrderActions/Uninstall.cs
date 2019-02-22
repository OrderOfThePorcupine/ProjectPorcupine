﻿#region License
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
    [OrderActionName("Uninstall")]
    public class Uninstall : OrderAction
    {
        public Uninstall()
        {
            Category = PrototypeManager.JobCategory.Get("construct");
            Priority = Job.JobPriority.High;
        }

        private Uninstall(Uninstall other) : base(other)
        {
        }

        public override OrderAction Clone()
        {
            return new Uninstall(this);
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
                job.Description = "job_uninstall_" + type + "_desc";
                job.adjacent = true;
                job.OrderName = Type;
            }

            return job;
        }
    }
}
