
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Economy.RFO.Events;

namespace SO.Map
{
    public class SMapRegionInitializerControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegion> regionPool = default;


        //События экономики
        readonly EcsFilterInject<Inc<RRegionInitializer>> regionInitializerRequestFilter = default;
        readonly EcsPoolInject<RRegionInitializer> regionInitializerRequestPool = default;
        readonly EcsFilterInject<Inc<RRegionInitializer, RRegionInitializerOwner>> regionInitializerOwnerRequestFilter = default;
        readonly EcsPoolInject<RRegionInitializerOwner> regionInitializerOwnerRequestPool = default;


        //Данные
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //ТЕСТ
            //Генерируем позиции для инициализаторов случайно, но без пересечений
            MapRegionInitializersRandom();
            //ТЕСТ

            //Применяем владельцев инициализаторов
            MapRegionInitializersOwner();

            //Для каждого запроса инициализатора
            foreach (int requestEntity in regionInitializerRequestFilter.Value)
            {
                regionInitializerRequestPool.Value.Del(requestEntity);
            }
        }

        /// <summary>
        /// ТЕСТ
        /// </summary>
        void MapRegionInitializersRandom()
        {
            //Создаём временный словарь для регионов
            //int1 - сущность региона, int2 - сущность инициализатора
            Dictionary<int, int> initializedRegions = new();

            //Для каждого запроса инициализатора
            foreach (int requestEntity in regionInitializerRequestFilter.Value)
            {
                //Берём запрос
                ref RRegionInitializer requestComp = ref regionInitializerRequestPool.Value.Get(requestEntity);

                bool isValidRegion = false;

                //Пока не найден подходящий регион
                while(isValidRegion == false)
                {
                    //Берём сущность случайного региона
                    regionsData.Value.GetRegionRandom().Unpack(world.Value, out int regionEntity);

                    //Если сущность региона отсутствует в словаре
                    if(initializedRegions.ContainsKey(regionEntity) == false)
                    {
                        //Берём регион
                        ref CRegion region = ref regionPool.Value.Get(regionEntity);

                        //Сохраняем сущность региона в инициализаторе
                        requestComp.regionPE = region.selfPE;

                        //Заносим регион и инициализатор в словарь
                        initializedRegions.Add(regionEntity, requestEntity);

                        //Отмечаем, что регион был найден
                        isValidRegion = true;
                    }
                }
            }
        }

        void MapRegionInitializersOwner()
        {
            //Для каждого инициализатора с компонентом владельца
            foreach (int requestEntity in regionInitializerOwnerRequestFilter.Value)
            {
                //Берём запросы
                ref RRegionInitializer requestCoreComp = ref regionInitializerRequestPool.Value.Get(requestEntity);
                ref RRegionInitializerOwner requestOwnerComp = ref regionInitializerOwnerRequestPool.Value.Get(requestEntity);

                //Запрашиваем смену владельца региона
                RegionActionRequest(
                    requestOwnerComp.ownerFactionPE,
                    requestCoreComp.regionPE,
                    RegionChangeOwnerType.Initialization);

                regionInitializerOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsPoolInject<RRFOChangeOwner> regionChangeOwnerRequestPool = default;
        void RegionActionRequest(
            EcsPackedEntity factionPE,
            EcsPackedEntity regionPE,
            RegionChangeOwnerType actionType)
        {
            //Создаём новую сущность и назначаем ей запрос смены владельца региона
            int requestEntity = world.Value.NewEntity();
            ref RRFOChangeOwner requestComp = ref regionChangeOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                factionPE,
                regionPE,
                actionType);
        }
    }
}