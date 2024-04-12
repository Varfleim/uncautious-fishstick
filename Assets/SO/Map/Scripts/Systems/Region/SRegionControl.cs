using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country;
using SO.Map.Events;
using SO.Map.Economy;

namespace SO.Map.Region
{
    public class SRegionControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegionCore> rCPool = default;
        readonly EcsPoolInject<CRegionEconomy> rEPool = default;

        //Страны
        readonly EcsPoolInject<CCountry> countryPool = default;

        public void Run(IEcsSystems systems)
        {
            //Смена владельцев регионов
            RegionChangeOwners();
        }

        readonly EcsFilterInject<Inc<RRegionChangeOwner>> regionChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRegionChangeOwner> regionChangeOwnerRequestPool = default;
        void RegionChangeOwners()
        {
            //Для каждого запроса смены владельца региона
            foreach (int requestEntity in regionChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RRegionChangeOwner requestComp = ref regionChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём страну, который становится владельцем региона
                requestComp.countryPE.Unpack(world.Value, out int countryEntity);
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //Берём регион
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //Если смена владельца происходит при инициализации
                if (requestComp.requestType == RegionChangeOwnerType.Initialization)
                {
                    RCChangeOwnerInitialization();
                }


                //Создаём событие, сообщающее о смене владельца региона
                RegionChangeOwnerEvent(
                    rC.selfPE,
                    country.selfPE, rC.ownerCountryPE);


                //Указываем страну-владельца региона
                rC.ownerCountryPE = country.selfPE;

                //ТЕСТ
                //Заносим PE региона в список страны
                country.ownedRCPEs.Add(rC.selfPE);

                //Берём экономический компонент региона
                ref CRegionEconomy rE = ref rEPool.Value.Get(regionEntity);

                //Создаём тестовые группы населения
                for(int a = 0; a < 5; a++)
                {
                    //Создаём новый запрос создания ПОПа
                    Population.Events.DROrderedPopulation orderedPOP = new(
                        rE.selfPE,
                        0,
                        100);

                    //Заносим его в список заказанных ПОПов
                    rE.orderedPOPs.Add(orderedPOP);
                }
                //ТЕСТ

                regionChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RCChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<ERegionChangeOwner> regionChangeOwnerEventPool = default;
        void RegionChangeOwnerEvent(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE = new())
        {
            //Создаём новую сущность и назначаем ей событие смены владельца RC
            int eventEntity = world.Value.NewEntity();
            ref ERegionChangeOwner eventComp = ref regionChangeOwnerEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(
                regionPE,
                newOwnerCountryPE, oldOwnerCountryPE);
        }
    }
}