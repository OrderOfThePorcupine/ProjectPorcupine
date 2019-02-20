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
    [OrderActionName("Mine")]
    public class Mine : OrderAction
    {
        public Mine()
        {
            Category = PrototypeManager.JobCategory.Get("mining");
            Priority = Job.JobPriority.Medium;
        }

        private Mine(Mine other) : base(other)
        {
        }

        public override OrderAction Clone()
        {
            return new Mine(this);
        }

        public override Job CreateJob(Tile tile, string type)
        {
            Job job = CheckJobFromFunction(JobTimeFunction, tile.Furniture);
            job.Priority = Priority;
            job.Category = Category;

            return job;
        }
    }
}
