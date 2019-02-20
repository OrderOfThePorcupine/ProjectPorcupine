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
    [OrderActionName("ChangeTileType")]
    public class ChangeTileType : OrderAction
    {
        public ChangeTileType()
        {
        }

        private ChangeTileType(ChangeTileType other) : base(other)
        {
        }

        public override OrderAction Clone()
        {
            return new ChangeTileType(this);
        }

        public override Job CreateJob(Tile tile, string type)
        {
            Job job = CheckJobFromFunction(JobTimeFunction, tile.Furniture);
            TileType tileType = PrototypeManager.TileType.Get(type);

            if (job == null)
            {
                job = new Job(
                tile,
                tileType,
                Tile.ChangeTileTypeJobComplete,
                JobTime,
                Inventory.Select(it => new RequestedItem(it.Key, it.Value)).ToArray(),
                Job.JobPriority.High,
                jobRepeats: false,
                adjacent: true);
                job.Description = "job_build_" + type + "_desc";
                job.OrderName = Type;
            }

            return job;
        }
    }
}
