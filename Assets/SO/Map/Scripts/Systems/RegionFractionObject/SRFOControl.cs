using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Faction;
using SO.Map.RFO.Events;

namespace SO.Map.RFO
{
    public class SRFOControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsFilterInject<Inc<CRegion>> rFOFilter = default;
        readonly EcsPoolInject<CRegionFO> rFOPool = default;

        readonly EcsPoolInject<CExplorationFRFO> exFRFOPool = default;

        //Фракции
        readonly EcsPoolInject<CFaction> factionPool = default;

        readonly EcsPoolInject<CExplorationObserver> explorationObserverPool = default;

        //Данные
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //Создание ExFRFO
            ExFRFOCreating();

            //Смена владельцев RFO
            RFOChangeOwners();
        }

        readonly EcsFilterInject<Inc<CFaction, SRExFRFOsCreating>> exFRFOsCreatingSelfRequestFilter = default;
        readonly EcsPoolInject<SRExFRFOsCreating> exFRFOsCreatingSelfRequestPool = default;
        void ExFRFOCreating()
        {
            //Если фильтр самозапросов не пуст
            if(exFRFOsCreatingSelfRequestFilter.Value.GetEntitiesCount() > 0)
            {
                //Создаём временный список DFRFO
                List<DFactionRFO> tempFRFO = new();

                //Определяем общее количество фракций
                int factionsCount = runtimeData.Value.factionsCount;

                //Для каждого RFO
                foreach(int regionEntity in rFOFilter.Value)
                {
                    //Берём RFO
                    ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

                    //Очищаем временный список
                    tempFRFO.Clear();

                    //Для каждой фракции с самозапросом создания ExFRFO
                    foreach (int factionEntity in exFRFOsCreatingSelfRequestFilter.Value)
                    {
                        //Берём фракцию и самозапрос
                        ref CFaction faction = ref factionPool.Value.Get(factionEntity);
                        ref SRExFRFOsCreating selfRequestComp = ref exFRFOsCreatingSelfRequestPool.Value.Get(factionEntity);

                        //Создаём новую сущность и назначаем ей компонент ExFRFO
                        int fRFOEntity = world.Value.NewEntity();
                        ref CExplorationFRFO exFRFO = ref exFRFOPool.Value.Add(fRFOEntity);

                        //Заполняем основные данные ExFRFO
                        exFRFO = new(
                            world.Value.PackEntity(fRFOEntity),
                            faction.selfPE,
                            rFO.selfPE);

                        //Создаём DFRFO для хранения данных фракции непосредственно в RFO
                        DFactionRFO factionRFO = new(
                            exFRFO.selfPE);

                        //Заносим его во временный список
                        tempFRFO.Add(factionRFO);
                    }

                    //Сохраняем старый размер массива
                    int oldArraySize = rFO.factionRFOs.Length;

                    //Расширяем массив
                    Array.Resize(
                        ref rFO.factionRFOs,
                        factionsCount);

                    //Для каждого DFRFO во временном массиве
                    for(int a = 0; a < tempFRFO.Count; a++)
                    {
                        //Вставляем DFRFO в массив по индексу
                        rFO.factionRFOs[oldArraySize++] = tempFRFO[a];
                    }
                }

                //Для каждого самозапроса создания ExFRFO
                foreach(int factionEntity in exFRFOsCreatingSelfRequestFilter.Value)
                {
                    exFRFOsCreatingSelfRequestPool.Value.Del(factionEntity);
                }
            }
        }

        readonly EcsFilterInject<Inc<RRFOChangeOwner>> rFOChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRFOChangeOwner> rFOChangeOwnerRequestPool = default;
        void RFOChangeOwners()
        {
            //Для каждого запроса смены владельца RFO
            foreach (int requestEntity in rFOChangeOwnerRequestFilter.Value)
            {
                //Берём запрос
                ref RRFOChangeOwner requestComp = ref rFOChangeOwnerRequestPool.Value.Get(requestEntity);

                //Берём фракцию, которая становится владельцем RFO
                requestComp.factionPE.Unpack(world.Value, out int factionEntity);
                ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                //Берём RFO
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

                //Если смена владельца происходит при инициализации
                if (requestComp.actionType == RegionChangeOwnerType.Initialization)
                {
                    RFOChangeOwnerInitialization();
                }

                //Указываем фракцию-владельца RFO
                rFO.ownerFactionPE = faction.selfPE;

                //Заносим PE RFO в список фракции
                faction.ownedRFOPEs.Add(rFO.selfPE);

                //ТЕСТ
                //Берём ExFRFO фракции игрока
                rFO.factionRFOs[faction.selfIndex].fRFOPE.Unpack(world.Value, out int fRFOEntity);
                ref CExplorationFRFO exFRFO = ref exFRFOPool.Value.Get(fRFOEntity);

                //Назначаем FRFO компонент наблюдателя и сохраняем ссылку на него в список фракции
                ref CExplorationObserver exObserver = ref explorationObserverPool.Value.Add(fRFOEntity);
                faction.observerPEs.Add(exFRFO.selfPE);
                //ТЕСТ

                rFOChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RFOChangeOwnerInitialization()
        {

        }
    }
}