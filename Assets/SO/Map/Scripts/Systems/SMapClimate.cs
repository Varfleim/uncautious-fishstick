
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Pathfinding;
using SO.Map.Events;

namespace SO.Map
{
    public class SMapClimate : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //Карта
        readonly EcsFilterInject<Inc<CRegionCore>> regionFilter = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;
        readonly EcsPoolInject<CRegionGenerationData> regionGenerationDataPool = default;

        //События карты
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //Данные
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        //readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого события генерации карты
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //Берём запрос
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //Генерируем климат
                ClimateCreate(ref requestComp);

                mapGeneratingRequestPool.Value.Del(requestEntity);
            }
        }

        void ClimateCreate(
            ref RMapGenerating requestComp)
        {
            //Инициализируем данные климата
            ClimateDataInitialization(ref requestComp);

            //Для каждого региона
            foreach (int regionEntity in regionFilter.Value)
            {
                //Назначаем сущности компонент данных генерации
                CRegionGenerationData rGD = regionGenerationDataPool.Value.Add(regionEntity);

                //Заполняем основные данные генерации
                rGD = new(new(0, mapGenerationData.Value.startingMoisture), new(0, 0));
            }

            //Генерируем температуру и влажность
            ClimateGeneration();

            //Распределяем типы ландшафта
            ClimateTerrainTypes();

            //Для каждого региона
            foreach (int regionEntity in regionFilter.Value)
            {
                //Удаляем с сущности компонент данных генерации
                regionGenerationDataPool.Value.Del(regionEntity);
            }
        }

        void ClimateDataInitialization(
            ref RMapGenerating requestComp)
        {

        }

        void ClimateGeneration()
        {
            //Несколько раз
            for (int a = 0; a < 40; a++)
            {
                //Для каждого региона
                foreach (int regionEntity in regionFilter.Value)
                {
                    //Берём RC и данные генерации
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                    ref CRegionGenerationData rGD = ref regionGenerationDataPool.Value.Get(regionEntity);

                    //Рассчитываем климатические данные
                    RegionEvolveClimate(
                        ref rC,
                        ref rGD);
                }

                //Для каждого региона
                foreach (int regionEntity in regionFilter.Value)
                {
                    //Берём данные генерации
                    ref CRegionGenerationData rGD = ref regionGenerationDataPool.Value.Get(regionEntity);

                    //Переносим следующие данные климата в текущие, затем следующие приводим к значению по умолчанию
                    rGD.currentClimate = rGD.nextClimate;
                    rGD.nextClimate = new();
                }
            }
        }

        void RegionEvolveClimate(
            ref CRegionCore rC,
            ref CRegionGenerationData rGD)
        {
            //Берём данные климата
            ref DRegionClimate regionClimate = ref rGD.currentClimate;

            //Если регион находится под водой
            if (rC.IsUnderwater == true)
            {
                //Определяем влажность
                regionClimate.moisture = 1f;

                //Рассчитываем испарение
                regionClimate.clouds += mapGenerationData.Value.evaporationFactor;
            }
            //Иначе
            else
            {
                //Рассчитываем испарение 
                float evaporation = regionClimate.moisture * mapGenerationData.Value.evaporationFactor;
                regionClimate.moisture -= evaporation;
                regionClimate.clouds += evaporation;
            }

            //Рассчитываем, сколько облаков превратится в осадки
            float precipitation = regionClimate.clouds * mapGenerationData.Value.precipitationFactor;
            //Применяем осадки к облакам и влажности
            regionClimate.clouds -= precipitation;
            regionClimate.moisture += precipitation;

            //Рассчитываем максимум облаков для региона
            float cloudMaximum = 1f - rC.ViewElevation / (mapGenerationData.Value.elevationMaximum + 1);
            //Если облаков больше возможного
            if (regionClimate.clouds > cloudMaximum)
            {
                //То избыток переходит во влагу
                regionClimate.moisture += regionClimate.clouds - cloudMaximum;
                regionClimate.clouds = cloudMaximum;
            }

            //Определяем направление ветра
            //HexDirection mainDispersalDirection = mapGenerationData.Value.windDirection.Opposite();
            //Рассчитываем, сколько облаков рассеется
            float cloudDispersal = regionClimate.clouds * (1f / (5f + mapGenerationData.Value.windStrength));

            //Рассчитываем, сколько стекает в регионы ниже
            float runoff = regionClimate.moisture * mapGenerationData.Value.runoffFactor * (1f / 6f);
            //Рассчитываем, сколько влаги просачивается
            float seepage = regionClimate.moisture * mapGenerationData.Value.seepageFactor * (1f / 6f);

            //Для каждого соседа
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа и следующие данные климата
                rC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);
                ref CRegionGenerationData neighbourRGD = ref regionGenerationDataPool.Value.Get(neighbourRegionEntity);
                ref DRegionClimate neighbourRegionClimate = ref neighbourRGD.nextClimate;

                //Рассеивается обычный объём облаков
                neighbourRegionClimate.clouds += cloudDispersal;

                //Рассчитываем разницу в высоте между регионами
                int elevationDelta = neighbourRC.ViewElevation - rC.ViewElevation;
                //Если разница меньше нуля
                if (elevationDelta < 0)
                {
                    //Сток уходит в соседа
                    neighbourRegionClimate.moisture -= runoff;
                    neighbourRegionClimate.moisture += runoff;
                }
                //Иначе, если разница равна нулю
                else if (elevationDelta == 0)
                {
                    //Просачивание уходит в соседей
                    neighbourRegionClimate.moisture -= seepage;
                    neighbourRegionClimate.moisture += seepage;
                }
            }

            //Берём следующие данные климата
            ref DRegionClimate regionNextClimate = ref rGD.nextClimate;

            //Переносим влажность из текущего
            regionNextClimate.moisture = regionClimate.moisture;

            //Если будущая влажность больше единицы
            if (regionNextClimate.moisture > 1f)
            {
                //Устанавливаем её на единицу
                regionNextClimate.moisture = 1f;
            }
        }

        void ClimateTerrainTypes()
        {
            //Рассчитываем высоту, необходимую для появления каменистой пустыни
            int rockDesertElevation = mapGenerationData.Value.elevationMaximum
                - (mapGenerationData.Value.elevationMaximum - mapGenerationData.Value.waterLevel) / 2;

            //Для каждого региона
            foreach (int regionEntity in regionFilter.Value)
            {
                //Берём регион и данные генерации
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                ref CRegionGenerationData rGD = ref regionGenerationDataPool.Value.Get(regionEntity);

                //Берём данные климата
                ref DRegionClimate regionClimate = ref rGD.currentClimate;

                //Рассчитываем температуру региона
                float temperature = RegionDetermineTemperature(ref rC);

                //Если регион не под водой
                if (rC.IsUnderwater == false)
                {
                    //Определяем уровень температуры региона
                    int t = 0;

                    //Для каждого уровня температуры
                    for (; t < mapGenerationData.Value.temperatureBands.Length; t++)
                    {
                        //Если температуры меньше уровня
                        if (temperature < mapGenerationData.Value.temperatureBands[t])
                        {
                            break;
                        }
                    }

                    //Опрелеояем уррвень влажности региона
                    int m = 0;

                    //Для каждого уровня влажности
                    for (; m < mapGenerationData.Value.moistureBands.Length; m++)
                    {
                        //Если влажность меньше уровня
                        if (regionClimate.moisture < mapGenerationData.Value.moistureBands[m])
                        {
                            break;
                        }
                    }

                    //Определяем биом региона
                    ref DRegionBiome biome = ref mapGenerationData.Value.biomes[t * 4 + m];

                    //Если тип ландшафта биома - пустыня
                    if (biome.terrainTypeIndex == 0)
                    {
                        //Если высота региона больше или равна высоты, требуемой для каменистой пустыни
                        if (rC.Elevation >= rockDesertElevation)
                        {
                            //Устанавливаем тип ландшафта региона на камень
                            rC.TerrainTypeIndex = 3;
                        }
                        //Иначе
                        else
                        {
                            //Устанавливаем тип ландшафта по стандартным правилам
                            rC.TerrainTypeIndex = biome.terrainTypeIndex;
                        }
                    }
                    //Иначе, если высота региона равна максимальной
                    else if (rC.Elevation == mapGenerationData.Value.elevationMaximum)
                    {
                        //Устанавливаем тип ландшафта региона на снег
                        rC.TerrainTypeIndex = 4;
                    }
                    //Иначе
                    else
                    {
                        //Устанавливаем тип ландшафта региона
                        rC.TerrainTypeIndex = biome.terrainTypeIndex;
                    }
                }
                //Иначе
                else
                {
                    //Определяем тип ландшафта
                    int terrainTypeIndex;

                    //Если регион на один уровень ниже уровня моря
                    if (rC.Elevation == mapGenerationData.Value.waterLevel - 1)
                    {
                        //Определяем количество соседей со склонами и обрывами
                        int cliffs = 0, slopes = 0;

                        //Для каждого соседа
                        for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
                        {
                            //Берём соседа
                            rC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                            //Определяем разницу в высоте с уровнем воды
                            int elevationDelta = neighbourRC.Elevation - rC.WaterLevel;

                            //Если разница равна нулю
                            if (elevationDelta == 0)
                            {
                                //То имеем склон
                                slopes += 1;
                            }
                            //Иначе, если разница больше нуля
                            else if (elevationDelta > 0)
                            {
                                //То имеем обрыв
                                cliffs += 1;
                            }
                        }

                        //Если число склонов и обрывов больше трёх
                        if (cliffs + slopes > 3)
                        {
                            //Тип ландшафта - трава
                            terrainTypeIndex = 1;
                        }
                        //Иначе, если обрывов больше нуля
                        else if (cliffs > 0)
                        {
                            //Тип ландшафта - камень
                            terrainTypeIndex = 3;
                        }
                        //Иначе, если склонов больше нуля
                        else if (slopes > 0)
                        {
                            //Тип ландшафта - пустыня
                            terrainTypeIndex = 0;
                        }
                        //Иначе
                        else
                        {
                            //Тип ландшафта - трава
                            terrainTypeIndex = 1;
                        }
                    }
                    //Иначе, если высота региона больше уровня моря
                    else if (rC.Elevation >= mapGenerationData.Value.waterLevel)
                    {
                        //Тип ландшафта - трава
                        terrainTypeIndex = 1;
                    }
                    //Иначе, если высота региона отрицательна
                    else if (rC.Elevation < 0)
                    {
                        //Тип ландшафта - камень
                        terrainTypeIndex = 3;
                    }
                    //Иначе
                    else
                    {
                        //Тип ландшафта - грязь
                        terrainTypeIndex = 2;
                    }

                    //Если тип ландшафта - трава и температура находится в самом низком диапазоне
                    if (terrainTypeIndex == 1 && temperature < mapGenerationData.Value.temperatureBands[0])
                    {
                        //Тип ландшафта - грязь
                        terrainTypeIndex = 2;
                    }

                    //Устанавливаем тип ландшафта
                    rC.TerrainTypeIndex = terrainTypeIndex;
                }
            }
        }

        float RegionDetermineTemperature(
            ref CRegionCore rC)
        {
            //Рассчитываем широту региона
            float latitude = (float)rC.center.z / 1f;
            latitude *= 2f;
            //Если широта больше единицы, то это другое полушарие
            if (latitude > 1f)
            {
                latitude = 2f - latitude;
            }

            //Рассчитываем температуру региона
            //По широте
            float temperature = Mathf.LerpUnclamped(mapGenerationData.Value.lowTemperature, mapGenerationData.Value.highTemperature, latitude);
            //По высоте
            temperature *= 1f - (rC.ViewElevation - mapGenerationData.Value.waterLevel)
                / (mapGenerationData.Value.elevationMaximum - mapGenerationData.Value.waterLevel + 1f);
            //Возвращаем температуру
            return temperature;
        }
    }
}