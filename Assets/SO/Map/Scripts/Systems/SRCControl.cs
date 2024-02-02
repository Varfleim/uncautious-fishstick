using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Faction;
using SO.Map.Events;

namespace SO.Map
{
    public class SRCControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsFilterInject<Inc<CRegionCore>> regionFilter = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        readonly EcsPoolInject<CExplorationRegionFractionObject> exRFOPool = default;

        //Фракции
        readonly EcsPoolInject<CFaction> factionPool = default;

        readonly EcsPoolInject<CExplorationObserver> explorationObserverPool = default;


        //Данные
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //Создание ExRFO
            ExRFOCreating();

            //Смена владельцев RC
            RCChangeOwners();
        }

        readonly EcsFilterInject<Inc<CFaction, SRExRFOsCreating>> exRFOsCreatingSelfRequestFilter = default;
        readonly EcsPoolInject<SRExRFOsCreating> exRFOsCreatingSelfRequestPool = default;
        void ExRFOCreating()
        {
            //Если фильтр самозапросов не пуст
            if (exRFOsCreatingSelfRequestFilter.Value.GetEntitiesCount() > 0)
            {
                //Создаём временный список DRFO
                List<DRegionFactionObject> tempRFO = new();

                //Определяем общее количество фракций
                int factionsCount = runtimeData.Value.factionsCount;

                //Для каждого региона
                foreach (int regionEntity in regionFilter.Value)
                {
                    //Берём RC
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //Очищаем временный список
                    tempRFO.Clear();

                    //Для каждой фракции с самозапросом создания ExRFO
                    foreach (int factionEntity in exRFOsCreatingSelfRequestFilter.Value)
                    {
                        //Берём фракцию и самозапрос
                        ref CFaction faction = ref factionPool.Value.Get(factionEntity);
                        ref SRExRFOsCreating selfRequestComp = ref exRFOsCreatingSelfRequestPool.Value.Get(factionEntity);

                        //Создаём новую сущность и назначаем ей компонент ExRFO
                        int rFOEntity = world.Value.NewEntity();
                        ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Add(rFOEntity);

                        //Заполняем основные данные ExRFO
                        exRFO = new(
                            world.Value.PackEntity(rFOEntity),
                            faction.selfPE,
                            rC.selfPE);

                        //Создаём DRFO для хранения данных фракции непосредственно в RC
                        DRegionFactionObject rFO = new(
                            exRFO.selfPE);

                        //Заносим его во временный список
                        tempRFO.Add(rFO);
                    }

                    //Сохраняем старый размер массива
                    int oldArraySize = rC.rFOPEs.Length;

                    //Расширяем массив
                    Array.Resize(
                        ref rC.rFOPEs,
                        factionsCount);

                    //Для каждого DRFO во временном массиве
                    for (int a = 0; a < tempRFO.Count; a++)
                    {
                        //Вставляем DRFO в массив по индексу
                        rC.rFOPEs[oldArraySize++] = tempRFO[a];
                    }
                }

                //Для каждого самозапроса создания ExRFO
                foreach (int factionEntity in exRFOsCreatingSelfRequestFilter.Value)
                {
                    exRFOsCreatingSelfRequestPool.Value.Del(factionEntity);
                }
            }
        }

        readonly EcsFilterInject<Inc<RRCChangeOwner>> rCChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRCChangeOwner> rCChangeOwnerRequestPool = default;
        void RCChangeOwners()
        {
            //Для каждого запроса смены владельца RC
            foreach (int requestEntity in rCChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RRCChangeOwner requestComp = ref rCChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём фракцию, которая становится владельцем RC
                requestComp.factionPE.Unpack(world.Value, out int factionEntity);
                ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                //Берём RC
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //Если смена владельца происходит при инициализации
                if (requestComp.requestType == RCChangeOwnerType.Initialization)
                {
                    RCChangeOwnerInitialization();
                }


                //Создаём событие, сообщающее о смене владельца RC
                RCChangeOwnerEvent(
                    rC.selfPE,
                    faction.selfPE, rC.ownerFactionPE);


                //Указываем фракцию-владельца RC
                rC.ownerFactionPE = faction.selfPE;

                //Заносим PE региона в список фракции
                faction.ownedRCPEs.Add(rC.selfPE);

                //ТЕСТ
                //Берём ExRFO фракции игрока
                rC.rFOPEs[faction.selfIndex].rFOPE.Unpack(world.Value, out int rFOEntity);
                ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Get(rFOEntity);

                //Назначаем RFO компонент наблюдателя и сохраняем ссылку на него в список фракции
                ref CExplorationObserver exObserver = ref explorationObserverPool.Value.Add(rFOEntity);
                faction.observerPEs.Add(exRFO.selfPE);
                //ТЕСТ

                rCChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RCChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<ERCChangeOwner> rCChangeOwnerEventPool = default;
        void RCChangeOwnerEvent(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerFactionPE, EcsPackedEntity oldOwnerFactionPE = new())
        {
            //Создаём новую сущность и назначаем ей событие смены владельца RC
            int eventEntity = world.Value.NewEntity();
            ref ERCChangeOwner eventComp = ref rCChangeOwnerEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(
                regionPE,
                newOwnerFactionPE, oldOwnerFactionPE);
        }
    }
}