
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Warfare.Fleet.Movement;

namespace SO
{
    public class SEventClear : IEcsRunSystem
    {
        //События карты
        readonly EcsFilterInject<Inc<EProvinceChangeOwner>> provinceChangeOwnerEventFilter = default;
        readonly EcsPoolInject<EProvinceChangeOwner> provinceChangeOwnerEventPool = default;

        //События военного дела
        readonly EcsFilterInject<Inc<ETaskForceChangeProvince>> tFChangeProvinceEventFilter = default;
        readonly EcsPoolInject<ETaskForceChangeProvince> tFChangeProvinceEventPool = default;

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

            //Для каждого события смены владельца провинции
            foreach (int eventEntity in provinceChangeOwnerEventFilter.Value)
            {
                //Удаляем компонент события
                provinceChangeOwnerEventPool.Value.Del(eventEntity);
            }

            //Для каждого события смены провинции оперативной группой
            foreach (int eventEntity in tFChangeProvinceEventFilter.Value)
            {
                //Удаляем компонент события
                tFChangeProvinceEventPool.Value.Del(eventEntity);
            }
        }
    }
}