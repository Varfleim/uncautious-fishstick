
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.Threads;

using SO.Map.Province;

namespace SO.Warfare.Fleet.Movement
{
    public class SMTTaskForceMovement : EcsThreadSystem<TTaskForceMovement,
        CTaskForceMovement, CTaskForce,
        CProvinceCore>
    {
        readonly EcsWorldInject world = default;

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
            return world.Filter<CTaskForceMovement>().End();
        }

        protected override void SetData(IEcsSystems systems, ref TTaskForceMovement thread)
        {
            thread.world = world.Value;
        }
    }
}