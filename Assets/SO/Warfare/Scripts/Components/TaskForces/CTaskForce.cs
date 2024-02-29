
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
            EcsPackedEntity ownerCharacterPE)
        {
            this.selfPE = selfPE;
            
            this.ownerCharacterPE = ownerCharacterPE;

            currentRegionPE = new();
            previousRegionPE = new();

            currentMissionType = TaskForceMissionType.None;
        }

        public readonly EcsPackedEntity selfPE;

        public EcsPackedEntity ownerCharacterPE;

        public EcsPackedEntity currentRegionPE;
        public EcsPackedEntity previousRegionPE;

        public TaskForceMissionType currentMissionType;
    }
}