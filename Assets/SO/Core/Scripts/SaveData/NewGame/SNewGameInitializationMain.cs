
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Faction.Events;

namespace SO
{
    public class SNewGameInitializationMain : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //События карты
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //События фракций
        readonly EcsPoolInject<RFactionCreating> factionCreatingRequestPool = default;

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

                //Запрашиваем создание тестовой фракции
                FactionCreatingRequest("TestFaction");

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
        
        void FactionCreatingRequest(
            string factionName)
        {
            //Создаём новую сущность и назначаем ей запрос создания фракции
            int requestEntity = world.Value.NewEntity();
            ref RFactionCreating requestComp = ref factionCreatingRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(factionName);
        }
    }
}