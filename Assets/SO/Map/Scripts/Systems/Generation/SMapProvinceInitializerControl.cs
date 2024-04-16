
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Map.Hexasphere;
using SO.Map.Province;

namespace SO.Map.Generation
{
    public class SMapProvinceInitializerControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;


        //События экономики
        readonly EcsFilterInject<Inc<RProvinceInitializer>> provinceInitializerRequestFilter = default;
        readonly EcsPoolInject<RProvinceInitializer> provinceInitializerRequestPool = default;
        readonly EcsFilterInject<Inc<RProvinceInitializer, RProvinceInitializerOwner>> provinceInitializerOwnerRequestFilter = default;
        readonly EcsPoolInject<RProvinceInitializerOwner> provinceInitializerOwnerRequestPool = default;


        //Данные
        readonly EcsCustomInject<ProvincesData> provincesData = default;

        public void Run(IEcsSystems systems)
        {
            //ТЕСТ
            //Генерируем позиции для инициализаторов случайно, но без пересечений
            MapProvinceInitializersRandom();
            //ТЕСТ

            //Применяем владельцев инициализаторов
            MapProvinceInitializersOwner();

            //Для каждого запроса инициализатора
            foreach (int requestEntity in provinceInitializerRequestFilter.Value)
            {
                provinceInitializerRequestPool.Value.Del(requestEntity);
            }
        }

        /// <summary>
        /// ТЕСТ
        /// </summary>
        void MapProvinceInitializersRandom()
        {
            //Создаём временный словарь для провинций
            //int1 - сущность провинции, int2 - сущность инициализатора
            Dictionary<int, int> initializedProvinces = new();

            //Для каждого запроса инициализатора
            foreach (int requestEntity in provinceInitializerRequestFilter.Value)
            {
                //Берём запрос
                ref RProvinceInitializer requestComp = ref provinceInitializerRequestPool.Value.Get(requestEntity);

                bool isValidProvince = false;

                //Пока не найдена подходящая провинция
                while (isValidProvince == false)
                {
                    //Берём сущность случайной
                    provincesData.Value.GetProvinceRandom().Unpack(world.Value, out int provinceEntity);

                    //Если сущность отсутствует в словаре
                    if (initializedProvinces.ContainsKey(provinceEntity) == false)
                    {
                        //Берём провинцию
                        ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

                        //Сохраняем сущность провинции в инициализаторе
                        requestComp.provincePE = pHS.selfPE;

                        //Заносим провинцию и инициализатор в словарь
                        initializedProvinces.Add(provinceEntity, requestEntity);

                        //Отмечаем, что провинция была найдена
                        isValidProvince = true;
                    }
                }
            }
        }

        void MapProvinceInitializersOwner()
        {
            //Для каждого инициализатора с компонентом владельца
            foreach (int requestEntity in provinceInitializerOwnerRequestFilter.Value)
            {
                //Берём запросы
                ref RProvinceInitializer requestCoreComp = ref provinceInitializerRequestPool.Value.Get(requestEntity);
                ref RProvinceInitializerOwner requestOwnerComp = ref provinceInitializerOwnerRequestPool.Value.Get(requestEntity);

                //Запрашиваем смену владельца провинции
                //ProvinceChangeOwnerRequest(
                //    requestOwnerComp.ownerCountryPE,
                //    requestCoreComp.provincePE,
                //    PCChangeOwnerType.Initialization);

                provinceInitializerOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvinceChangeOwnerRequest(
            EcsPackedEntity countryPE,
            EcsPackedEntity provincePE,
            ProvinceChangeOwnerType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос смены владельца провинции
            int requestEntity = world.Value.NewEntity();
            ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                countryPE,
                provincePE,
                requestType);
        }
    }
}