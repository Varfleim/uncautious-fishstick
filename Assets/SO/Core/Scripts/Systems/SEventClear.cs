
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Warfare.Fleet.Movement;

namespace SO
{
    public class SEventClear : IEcsRunSystem
    {
        //События карты
        readonly EcsFilterInject<Inc<ERegionCoreChangeOwner>> rFOChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERegionCoreChangeOwner> rFOChangeOwnerEventPool = default;

        //События военного дела
        readonly EcsFilterInject<Inc<ETaskForceChangeRegion>> tFChangeRegionEventFilter = default;
        readonly EcsPoolInject<ETaskForceChangeRegion> tFChangeRegionEventPool = default;

        //Общие события
        readonly EcsFilterInject<Inc<EObjectNewCreated>> objectNewCreatedEventFilter = default;
        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого события создания нового объекта
            foreach(int eventEntity in objectNewCreatedEventFilter.Value)
            {
                //Удаляем компонент события
                objectNewCreatedEventPool.Value.Del(eventEntity);
            }

            //Для каждого события смены владельца RFO
            foreach (int eventEntity in rFOChangeOwnerEventFilter.Value)
            {
                //Удаляем компонент события
                rFOChangeOwnerEventPool.Value.Del(eventEntity);
            }

            //Для каждого события смены региона оперативной группой
            foreach(int eventEntity in tFChangeRegionEventFilter.Value)
            {
                //Удаляем компонент события
                tFChangeRegionEventPool.Value.Del(eventEntity);
            }
        }
    }
}