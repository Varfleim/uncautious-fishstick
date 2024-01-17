
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SCM.Map.Events;

namespace SCM
{
    public class SNewGameInitializationMain : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //События карты
        readonly EcsPoolInject<RMapGeneration> mapGenerationRequestPool = default;

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
                MapGenerationRequest(50);

                UnityEngine.Debug.LogWarning("Новая игра");
            }
        }

        void MapGenerationRequest(
            int subdivisions)
        {
            //Создаём новую сущность и назначаем ей запрос создания карты
            int requestEntity = world.Value.NewEntity();
            ref RMapGeneration requestComp = ref mapGenerationRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(subdivisions);
        }
    }
}