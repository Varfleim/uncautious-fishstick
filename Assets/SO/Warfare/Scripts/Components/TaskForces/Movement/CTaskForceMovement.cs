
using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Movement
{
    public enum TaskForceMovementTargetType : byte
    {
        None,
        Province,
        TaskForce
    }

    public struct CTaskForceMovement
    {
        public CTaskForceMovement(
            EcsPackedEntity targetPE, TaskForceMovementTargetType targetType)
        {
            this.targetPE = targetPE;
            this.targetType = targetType;

            pathProvincePEs = new();

            traveledDistance = 0;
            isTraveled = false;
        }

        public EcsPackedEntity targetPE;
        public TaskForceMovementTargetType targetType;

        public List<EcsPackedEntity> pathProvincePEs;

        public float traveledDistance;
        public bool isTraveled;
    }
}