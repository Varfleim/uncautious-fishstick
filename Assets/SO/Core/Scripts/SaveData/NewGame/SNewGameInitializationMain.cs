
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country.Events;
using SO.Map.Generation;

namespace SO
{
    public class SNewGameInitializationMain : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //События карты
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //События стран
        readonly EcsPoolInject<RCountryCreating> countryCreatingRequestPool = default;

        //Общие события
        readonly EcsFilterInject<Inc<RStartNewGame>> startNewGameRequestFilter = default;
        readonly EcsPoolInject<RStartNewGame> startNewGameRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого запроса начала новой игры
            foreach(int requestEntity in startNewGameRequestFilter.Value)
            {
                //Берём запрос
                ref RStartNewGame requestComp = ref startNewGameRequestPool.Value.Get(requestEntity);

                //Запрашиваем генерацию карты
                MapGeneratingRequest(50);

                //Запрашиваем создание тестовой страны
                CountryCreatingRequest("TestCountry");

                UnityEngine.Debug.LogWarning("Новая игра");
            }
        }

        void MapGeneratingRequest(
            int subdivisions)
        {
            //Создаём новую сущность и назначаем ей запрос создания карты
            int requestEntity = world.Value.NewEntity();
            ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(subdivisions);
        }
        
        void CountryCreatingRequest(
            string countryName)
        {
            //Создаём новую сущность и назначаем ей запрос создания страны
            int requestEntity = world.Value.NewEntity();
            ref RCountryCreating requestComp = ref countryCreatingRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(countryName);
        }
    }
}