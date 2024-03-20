
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Warfare.Fleet.Movement;

namespace SO
{
    public class SEventClear : IEcsRunSystem
    {
        //События карты
        readonly EcsFilterInject<Inc<ERegionChangeOwner>> regionChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERegionChangeOwner> regionChangeOwnerEventPool = default;

        readonly EcsFilterInject<Inc<EStrategicAreaChangeOwner>> sAChangeOwnerEventFilter = default;
        readonly EcsPoolInject<EStrategicAreaChangeOwner> sAChangeOwnerEventPool = default;

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

            //Для каждого события смены владельца региона
            foreach (int eventEntity in regionChangeOwnerEventFilter.Value)
            {
                //Удаляем компонент события
                regionChangeOwnerEventPool.Value.Del(eventEntity);
            }

            //Для каждого события смены владельца стратегической области
            foreach (int eventEntity in sAChangeOwnerEventFilter.Value)
            {
                //Удаляем компонент события
                sAChangeOwnerEventPool.Value.Del(eventEntity);
            }

            //Для каждого события смены региона оперативной группой
            foreach (int eventEntity in tFChangeRegionEventFilter.Value)
            {
                //Удаляем компонент события
                tFChangeRegionEventPool.Value.Del(eventEntity);
            }
        }
    }
}