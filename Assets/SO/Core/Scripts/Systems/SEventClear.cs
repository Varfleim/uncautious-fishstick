
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;

namespace SO
{
    public class SEventClear : IEcsRunSystem
    {
        //События карты
        readonly EcsFilterInject<Inc<ERCChangeOwner>> rFOChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERCChangeOwner> rFOChangeOwnerEventPool = default;

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
        }
    }
}