#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Xml.Serialization;

namespace ProjectPorcupine.OrderActions
{
    [Serializable]
    [OrderActionName("Deconstruct")]
    public class Deconstruct : OrderAction
    {
        public Deconstruct()
        {
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
            Job job = CheckJobFromFunction(JobTimeFunction, tile.Furniture);

            if (job == null)
            {
                job = new Job(
                tile,
                type,
                null,
                JobTime,
                null,
                Job.JobPriority.Medium, 
                "construct");
                job.Description = "job_deconstruct_" + type + "_desc";
                job.adjacent = true;
                job.OrderName = Type;
            }

            return job;
        }
    }
}
