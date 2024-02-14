
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.Threads;

using SO.Map;
using SO.Warfare.Fleet.Movement.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class SMTTaskForcePathfinding : EcsThreadSystem<TTaskForcePathfinding,
        SRTaskForceFindPath,
        CTaskForce, CTaskForceMovement,
        CRegionCore>
    {
        readonly EcsWorldInject world = default;

        readonly EcsCustomInject<RegionsData> regionsData = default;

        protected override int GetChunkSize(IEcsSystems systems)
        {
            return 32;
        }

        protected override EcsWorld GetWorld(IEcsSystems systems)
        {
            return systems.GetWorld();
        }

        protected override EcsFilter GetFilter(EcsWorld world)
        {
            return world.Filter<SRTaskForceFindPath>().End();
        }

        protected override void SetData(IEcsSystems systems, ref TTaskForcePathfinding thread)
        {
            thread.world = world.Value;

            thread.regionsData = regionsData.Value;
        }
    }
}