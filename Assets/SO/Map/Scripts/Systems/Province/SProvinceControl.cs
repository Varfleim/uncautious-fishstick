using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country;
using SO.Map.Events;
using SO.Map.Economy;

namespace SO.Map.Province
{
    public class SProvinceControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CProvinceCore> pCPool = default;
        readonly EcsPoolInject<CProvinceEconomy> pEPool = default;

        //Страны
        readonly EcsPoolInject<CCountry> countryPool = default;

        public void Run(IEcsSystems systems)
        {
            //Смена владельцев провинций
            ProvinceChangeOwners();
        }

        readonly EcsFilterInject<Inc<RProvinceChangeOwner>> provinceChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvinceChangeOwners()
        {
            //Для каждого запроса смены владельца провинции
            foreach (int requestEntity in provinceChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём страну, которая становится владельцем провинции
                requestComp.countryPE.Unpack(world.Value, out int countryEntity);
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //Берём провинцию
                requestComp.provincePE.Unpack(world.Value, out int provinceEntity);
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //Если смена владельца происходит при инициализации
                if (requestComp.requestType == ProvinceChangeOwnerType.Initialization)
                {
                    PCChangeOwnerInitialization();
                }


                //Создаём событие, сообщающее о смене владельца провинции
                ProvinceChangeOwnerEvent(
                    pC.selfPE,
                    country.selfPE, pC.ownerCountryPE);


                //Указываем страну-владельца провинции
                pC.ownerCountryPE = country.selfPE;

                //ТЕСТ
                //Заносим PE провинции в список страны
                country.ownedPCPEs.Add(pC.selfPE);

                //Берём экономический компонент провинции
                ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);

                //Создаём тестовые группы населения
                for(int a = 0; a < 5; a++)
                {
                    //Создаём новый запрос создания ПОПа
                    Population.Events.DROrderedPopulation orderedPOP = new(
                        pE.selfPE,
                        0,
                        100);

                    //Заносим его в список заказанных ПОПов
                    pE.orderedPOPs.Add(orderedPOP);
                }
                //ТЕСТ

                provinceChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void PCChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<EProvinceChangeOwner> provinceChangeOwnerEventPool = default;
        void ProvinceChangeOwnerEvent(
            EcsPackedEntity provincePE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE = new())
        {
            //Создаём новую сущность и назначаем ей событие смены владельца PC
            int eventEntity = world.Value.NewEntity();
            ref EProvinceChangeOwner eventComp = ref provinceChangeOwnerEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(
                provincePE,
                newOwnerCountryPE, oldOwnerCountryPE);
        }
    }
}