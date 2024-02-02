
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.Threads;

using SO.Faction;
using SO.Map.Hexasphere;

namespace SO.Map
{
    public class SMTObserverExplorationCalc : EcsThreadSystem<TObserverExplorationCalc,
        CFaction,
        CExplorationObserver,
        CExplorationRegionFractionObject,
        CRegionHexasphere,
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
            return world.Filter<CFaction>().End();
        }

        protected override void SetData(IEcsSystems systems, ref TObserverExplorationCalc thread)
        {
            thread.world = world.Value;

            thread.regionsData = regionsData.Value;
        }
    }
}