
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country;
using SO.Map.Events;

namespace SO.Map.MapArea
{
    public class SMapAreaControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CMapArea> mAPool = default;


        //Страны
        readonly EcsPoolInject<CCountry> countryPool = default;

        public void Run(IEcsSystems systems)
        {
            //Смена владельцев областей карты
            MapAreaChangeOwner();
        }

        readonly EcsFilterInject<Inc<RMapAreaChangeOwner>> mAChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RMapAreaChangeOwner> mAChangeOwnerRequestPool = default;
        void MapAreaChangeOwner()
        {
            //Для каждого запроса смены владельца области карты
            foreach (int requestEntity in mAChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RMapAreaChangeOwner requestComp = ref mAChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём страну, которая становится владельцем области
                requestComp.countryPE.Unpack(world.Value, out int countryEntity);
                ref CCountry newOwnerCountry = ref countryPool.Value.Get(countryEntity);

                //Берём область
                requestComp.mAPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //Если смена владельца происходит при инициализации
                if(requestComp.requestType == MapAreaChangeOwnerType.Initialization)
                {
                    MapAreaChangeOwnerInitialization();
                }


                //Запрашиваем событие, когда ещё владелец области не изменён, поскольку нам необходима его PE
                //Создаём событие, сообщающее о смене владельца области
                MapAreaChangeOwnerEvent(
                    mA.selfPE,
                    newOwnerCountry.selfPE, mA.ownerCountryPE);


                //Указываем страну-владельца области
                mA.ownerCountryPE = newOwnerCountry.selfPE;

                //ТЕСТ
                //Заносим PE области в список страны
                newOwnerCountry.ownedMAPEs.Add(mA.selfPE);
                //ТЕСТ

                mAChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void MapAreaChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<EMapAreaChangeOwner> mAChangeOwnerEventPool = default;
        void MapAreaChangeOwnerEvent(
            EcsPackedEntity mAPE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE)
        {
            //Создаём новую сущность и назначаем ей событие смены владельца области карты
            int eventEntity = world.Value.NewEntity();
            ref EMapAreaChangeOwner eventComp = ref mAChangeOwnerEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(
                mAPE,
                newOwnerCountryPE, oldOwnerCountryPE);
        }
    }
}