
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Missions.Events
{
    public enum TaskForceChangeMissionType : byte
    {
        None,
        MissionHold
    }

    public readonly struct RTaskForceChangeMission
    {
        public RTaskForceChangeMission(
            EcsPackedEntity tFPE, 
            TaskForceChangeMissionType requestType)
        {
            this.tFPE = tFPE;
            
            this.requestType = requestType;
        }

        public readonly EcsPackedEntity tFPE;

        public readonly TaskForceChangeMissionType requestType;
    }
}