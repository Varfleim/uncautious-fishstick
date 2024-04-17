
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Generation;
using SO.Map.Province;

namespace SO.Map.MapArea
{
    public class SMapMapAreaTerrain : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CMapArea> mAPool = default;


        //События карты
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //Данные
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<ProvincesData> provincesData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого события генерации карты
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //Берём запрос
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //Создаём ландшафт зон карты
                MapAreaTerrainCreate();
            }
        }

        void MapAreaTerrainCreate()
        {
            //Первично определяем высоту зон
            MapAreaSetElevationFirst();

            //Вторично определяем высоту зон
            MapAreaSetElevationSecond();
        }

        void MapAreaSetElevationFirst()
        {
            //Определяем количество зон для первичной установки высоты
            int firstElevationSetMACount = Mathf.RoundToInt(
                provincesData.Value.mapAreaPEs.Length
                * mapGenerationData.Value.mapAreaFirstElevationSetCount * 0.01f);

            //Пока счётчик больше нуля
            while (firstElevationSetMACount > 0)
            {
                //Берём случайную зону
                provincesData.Value.GetMapAreaRandom().Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //Пока полученная зона имеет высоту, отличную от нуля
                while (mA.Elevation != 0)
                {
                    //Берём случайную зону
                    provincesData.Value.GetMapAreaRandom().Unpack(world.Value, out mAEntity);
                    mA = ref mAPool.Value.Get(mAEntity);
                }

                //Генерируем случайную высоту зоны
                mA.Elevation = Random.Range(mapGenerationData.Value.elevationMinimum, mapGenerationData.Value.elevationMaximum + 1);

                //Уменьшаем счётчик зон для первичной установки высоты
                firstElevationSetMACount--;
            }
        }

        void MapAreaSetElevationSecond()
        {
            //Определяем количество зон для вторичной установки высоты
            int secondElevationSetMACount = Mathf.RoundToInt(
                provincesData.Value.mapAreaPEs.Length 
                * mapGenerationData.Value.mapAreaSecondElevationSetCount * 0.01f);

            //Пока счётчик больше нуля
            while (secondElevationSetMACount > 0)
            {
                //Берём случайную зону
                provincesData.Value.GetMapAreaRandom().Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //Пока полученная зона имеет высоту, отличную от нуля
                while (mA.Elevation != 0)
                {
                    //Берём случайную зону
                    provincesData.Value.GetMapAreaRandom().Unpack(world.Value, out mAEntity);
                    mA = ref mAPool.Value.Get(mAEntity);
                }

                //Определяем среднюю высоту соседних зон
                int averageNeighbourElevation = 0;
                int neighbourWithElevationCount = 0;

                //Для каждой соседней зоны 
                for (int a = 0; a < mA.neighbourMAPEs.Length; a++)
                {
                    //Берём соседнюю зону
                    mA.neighbourMAPEs[a].Unpack(world.Value, out int neighbourMAEntity);
                    ref CMapArea neighbourMA = ref mAPool.Value.Get(neighbourMAEntity);

                    //Если высота зоны не равна нулю
                    if (neighbourMA.Elevation != 0)
                    {
                        //Прибавляем высоту соседней зоны 
                        averageNeighbourElevation += neighbourMA.Elevation;

                        //Увеличиваем счётчик соседей с высотой
                        neighbourWithElevationCount++;
                    }
                }

                //Если средняя высота отличается от нуля
                if (averageNeighbourElevation != 0)
                {
                    //Вычисляем среднюю высоту
                    averageNeighbourElevation = Mathf.CeilToInt((float)averageNeighbourElevation / neighbourWithElevationCount);

                    //Генерируем случайную высоту зоны в рамках от средней минус два до средней
                    mA.Elevation = Random.Range(averageNeighbourElevation - 2, averageNeighbourElevation);

                    //Ограничиваем высоту 
                    mA.Elevation = Mathf.Clamp(mA.Elevation, mapGenerationData.Value.elevationMinimum, mapGenerationData.Value.elevationMaximum);

                    //Если итоговая высота не равно нулю
                    if (mA.Elevation != 0)
                    {
                        //Уменьшаем счётчик зон для вторичной установки высоты
                        secondElevationSetMACount--;
                    }
                }
            }
        }
    }
}