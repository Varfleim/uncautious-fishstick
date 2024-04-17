
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Generation;
using SO.Map.MapArea;
using SO.Map.Events.State;

namespace SO.Map.State
{
    public class SMapState : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsFilterInject<Inc<CMapArea>> mAFilter = default;
        readonly EcsPoolInject<CMapArea> mAPool = default;


        //События карты
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого события генерации карты
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //Берём запрос
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //Создаём пустые области
                MapAreaEmptyStatesCreating();
            }
        }

        void MapAreaEmptyStatesCreating()
        {
            //Для каждой зоны карты
            foreach(int mAEntity in mAFilter.Value)
            {
                //Берём зону
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //Запрашиваем стартовое создание области для неё
                StateStartCreatingRequest(ref mA);
            }
        }

        readonly EcsPoolInject<RStateStartCreating> stateStartCreatingRequestPool = default;
        void StateStartCreatingRequest(
            ref CMapArea mA)
        {
            //Создаём новую сущность и назначаем ей запрос стартового создания области
            int requestEntity = world.Value.NewEntity();
            ref RStateStartCreating requestComp = ref stateStartCreatingRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                mA.selfPE);
        }
    }
}