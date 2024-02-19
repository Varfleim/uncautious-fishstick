
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;

namespace SO.Map.StrategicArea
{
    public class SMapStrategicAreaTerrain : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CStrategicArea> sAPool = default;


        //События карты
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //Данные
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого события генерации карты
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //Берём запрос
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //Создаём ландшафт стратегических областей
                StrategicAreaTerrainCreate();
            }
        }

        void StrategicAreaTerrainCreate()
        {
            //Первично определяем высоту областей
            StrategicAreaSetElevationFirst();

            //Вторично определяем высоту областей
            StrategicAreaSetElevationSecond();
        }

        void StrategicAreaSetElevationFirst()
        {
            //Определяем количество областей для первичной установки высоты
            int firstElevationSetSACount = Mathf.RoundToInt(
                regionsData.Value.strategicAreaPEs.Length
                * mapGenerationData.Value.strategicAreaFirstElevationSetCount * 0.01f);

            //Пока счётчик больше нуля
            while (firstElevationSetSACount > 0)
            {
                //Берём случайную область
                regionsData.Value.GetStrategicAreaRandom().Unpack(world.Value, out int sAEntity);
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                //Пока полученная область имеет высоту, отличную от нуля
                while (sA.Elevation != 0)
                {
                    //Берём случайную область
                    regionsData.Value.GetStrategicAreaRandom().Unpack(world.Value, out sAEntity);
                    sA = ref sAPool.Value.Get(sAEntity);
                }

                //Генерируем случайную высоту области
                sA.Elevation = Random.Range(mapGenerationData.Value.elevationMinimum, mapGenerationData.Value.elevationMaximum + 1);

                //Уменьшаем счётчик областей для первичной установки высоты
                firstElevationSetSACount--;
            }
        }

        void StrategicAreaSetElevationSecond()
        {
            //Определяем количество областей для вторичной установки высоты
            int secondElevationSetSACount = Mathf.RoundToInt(
                regionsData.Value.strategicAreaPEs.Length 
                * mapGenerationData.Value.strategicAreaSecondElevationSetCount * 0.01f);

            //Пока счётчик больше нуля
            while (secondElevationSetSACount > 0)
            {
                //Берём случайную область
                regionsData.Value.GetStrategicAreaRandom().Unpack(world.Value, out int sAEntity);
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                //Пока полученная область имеет высоту, отличную от нуля
                while (sA.Elevation != 0)
                {
                    //Берём случайную область
                    regionsData.Value.GetStrategicAreaRandom().Unpack(world.Value, out sAEntity);
                    sA = ref sAPool.Value.Get(sAEntity);
                }

                //Определяем среднюю высоту соседних областей
                int averageNeighbourElevation = 0;
                int neighbourWithElevationCount = 0;

                //Для каждой соседней области 
                for (int a = 0; a < sA.neighbourSAPEs.Length; a++)
                {
                    //Берём соседнюю область
                    sA.neighbourSAPEs[a].Unpack(world.Value, out int neighbourSAEntity);
                    ref CStrategicArea neighbourSA = ref sAPool.Value.Get(neighbourSAEntity);

                    //Если высота области не равна нулю
                    if (neighbourSA.Elevation != 0)
                    {
                        //Прибавляем высоту соседней области 
                        averageNeighbourElevation += neighbourSA.Elevation;

                        //Увеличиваем счётчик соседей с высотой
                        neighbourWithElevationCount++;
                    }
                }

                //Если средняя высота отличается от нуля
                if (averageNeighbourElevation != 0)
                {
                    //Вычисляем среднюю высоту
                    averageNeighbourElevation = Mathf.CeilToInt((float)averageNeighbourElevation / neighbourWithElevationCount);

                    //Генерируем случайную высоту области в рамках от средней минус два до средней
                    sA.Elevation = Random.Range(averageNeighbourElevation - 2, averageNeighbourElevation);

                    //Ограничиваем высоту 
                    sA.Elevation = Mathf.Clamp(sA.Elevation, mapGenerationData.Value.elevationMinimum, mapGenerationData.Value.elevationMaximum);

                    //Если итоговая высота не равно нулю
                    if (sA.Elevation != 0)
                    {
                        //Уменьшаем счётчик областей для вторичной установки высоты
                        secondElevationSetSACount--;
                    }
                }
            }
        }
    }
}