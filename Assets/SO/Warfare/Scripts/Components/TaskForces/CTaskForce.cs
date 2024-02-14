
using Leopotam.EcsLite;

using SO.Warfare.Fleet.Missions;

namespace SO.Warfare.Fleet.Missions
{
    public enum TaskForceMissionType : byte
    {
        None,
        Hold
    }

    public enum TaskForceMissionStatus : byte
    {
        None,
        Movement,
        Waiting
    }
}

namespace SO.Warfare.Fleet
{
    public struct CTaskForce
    {
        public CTaskForce(
            EcsPackedEntity selfPE, 
            EcsPackedEntity ownerFactionPE)
        {
            this.selfPE = selfPE;
            
            this.ownerFactionPE = ownerFactionPE;

            currentRegionPE = new();
            previousRegionPE = new();

            currentMissionType = TaskForceMissionType.None;
        }

        public readonly EcsPackedEntity selfPE;

        public EcsPackedEntity ownerFactionPE;

        public EcsPackedEntity currentRegionPE;
        public EcsPackedEntity previousRegionPE;

        public TaskForceMissionType currentMissionType;
    }
}