
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
        //����
        readonly EcsWorldInject world = default;

        //�����
        readonly EcsFilterInject<Inc<CRegion>> regionFilter = default;
        readonly EcsPoolInject<CRegion> regionPool = default;
        readonly EcsPoolInject<CRegionGenerationData> regionGenerationDataPool = default;

        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //������
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        //readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //���� ������
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //���������� ������
                ClimateCreate(ref requestComp);

                mapGeneratingRequestPool.Value.Del(requestEntity);
            }
        }

        void ClimateCreate(
            ref RMapGenerating requestComp)
        {
            //�������������� ������ �������
            ClimateDataInitialization(ref requestComp);

            //��� ������� �������
            foreach (int regionEntity in regionFilter.Value)
            {
                //��������� �������� ��������� ������ ���������
                CRegionGenerationData regionGenerationData = regionGenerationDataPool.Value.Add(regionEntity);

                //��������� �������� ������ ���������
                regionGenerationData = new(new(0, mapGenerationData.Value.startingMoisture), new(0, 0));
            }

            //���������� ����������� � ���������
            ClimateGeneration();

            //������������ ���� ���������
            ClimateTerrainTypes();

            //��� ������� �������
            foreach (int regionEntity in regionFilter.Value)
            {
                //������� � �������� ��������� ������ ���������
                regionGenerationDataPool.Value.Del(regionEntity);
            }
        }

        void ClimateDataInitialization(
            ref RMapGenerating requestComp)
        {

        }

        void ClimateGeneration()
        {
            //��������� ���
            for (int a = 0; a < 40; a++)
            {
                //��� ������� �������
                foreach (int regionEntity in regionFilter.Value)
                {
                    //���� ������ � ������ ���������
                    ref CRegion region = ref regionPool.Value.Get(regionEntity);
                    ref CRegionGenerationData regionGenerationData = ref regionGenerationDataPool.Value.Get(regionEntity);

                    //������������ ������������� ������
                    RegionEvolveClimate(
                        ref region,
                        ref regionGenerationData);
                }

                //��� ������� �������
                foreach (int regionEntity in regionFilter.Value)
                {
                    //���� ������ ���������
                    ref CRegionGenerationData regionGenerationData = ref regionGenerationDataPool.Value.Get(regionEntity);

                    //��������� ��������� ������ ������� � �������, ����� ��������� �������� � �������� �� ���������
                    regionGenerationData.currentClimate = regionGenerationData.nextClimate;
                    regionGenerationData.nextClimate = new();
                }
            }
        }

        void RegionEvolveClimate(
            ref CRegion region,
            ref CRegionGenerationData regionGenerationData)
        {
            //���� ������ �������
            ref DRegionClimate regionClimate = ref regionGenerationData.currentClimate;

            //���� ������ ��������� ��� �����
            if (region.IsUnderwater == true)
            {
                //���������� ���������
                regionClimate.moisture = 1f;

                //������������ ���������
                regionClimate.clouds += mapGenerationData.Value.evaporationFactor;
            }
            //�����
            else
            {
                //������������ ��������� 
                float evaporation = regionClimate.moisture * mapGenerationData.Value.evaporationFactor;
                regionClimate.moisture -= evaporation;
                regionClimate.clouds += evaporation;
            }

            //������������, ������� ������� ����������� � ������
            float precipitation = regionClimate.clouds * mapGenerationData.Value.precipitationFactor;
            //��������� ������ � ������� � ���������
            regionClimate.clouds -= precipitation;
            regionClimate.moisture += precipitation;

            //������������ �������� ������� ��� �������
            float cloudMaximum = 1f - region.ViewElevation / (mapGenerationData.Value.elevationMaximum + 1);
            //���� ������� ������ ����������
            if (regionClimate.clouds > cloudMaximum)
            {
                //�� ������� ��������� �� �����
                regionClimate.moisture += regionClimate.clouds - cloudMaximum;
                regionClimate.clouds = cloudMaximum;
            }

            //���������� ����������� �����
            //HexDirection mainDispersalDirection = mapGenerationData.Value.windDirection.Opposite();
            //������������, ������� ������� ���������
            float cloudDispersal = regionClimate.clouds * (1f / (5f + mapGenerationData.Value.windStrength));

            //������������, ������� ������� � ������� ����
            float runoff = regionClimate.moisture * mapGenerationData.Value.runoffFactor * (1f / 6f);
            //������������, ������� ����� �������������
            float seepage = regionClimate.moisture * mapGenerationData.Value.seepageFactor * (1f / 6f);

            //��� ������� ������
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //���� ������ � ��������� ������ �������
                region.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);
                ref CRegionGenerationData neighbourRegionGenerationData = ref regionGenerationDataPool.Value.Get(neighbourRegionEntity);
                ref DRegionClimate neighbourRegionClimate = ref neighbourRegionGenerationData.nextClimate;

                //������������ ������� ����� �������
                neighbourRegionClimate.clouds += cloudDispersal;

                //������������ ������� � ������ ����� ���������
                int elevationDelta = neighbourRegion.ViewElevation - region.ViewElevation;
                //���� ������� ������ ����
                if (elevationDelta < 0)
                {
                    //���� ������ � ������
                    neighbourRegionClimate.moisture -= runoff;
                    neighbourRegionClimate.moisture += runoff;
                }
                //�����, ���� ������� ����� ����
                else if (elevationDelta == 0)
                {
                    //������������ ������ � �������
                    neighbourRegionClimate.moisture -= seepage;
                    neighbourRegionClimate.moisture += seepage;
                }
            }

            //���� ��������� ������ �������
            ref DRegionClimate regionNextClimate = ref regionGenerationData.nextClimate;

            //��������� ��������� �� ��������
            regionNextClimate.moisture = regionClimate.moisture;

            //���� ������� ��������� ������ �������
            if (regionNextClimate.moisture > 1f)
            {
                //������������� � �� �������
                regionNextClimate.moisture = 1f;
            }
        }

        void ClimateTerrainTypes()
        {
            //������������ ������, ����������� ��� ��������� ���������� �������
            int rockDesertElevation = mapGenerationData.Value.elevationMaximum
                - (mapGenerationData.Value.elevationMaximum - mapGenerationData.Value.waterLevel) / 2;

            //��� ������� �������
            foreach (int regionEntity in regionFilter.Value)
            {
                //���� ������ � ������ ���������
                ref CRegion region = ref regionPool.Value.Get(regionEntity);
                ref CRegionGenerationData regionGenerationData = ref regionGenerationDataPool.Value.Get(regionEntity);

                //���� ������ �������
                ref DRegionClimate regionClimate = ref regionGenerationData.currentClimate;

                //������������ ����������� �������
                float temperature = RegionDetermineTemperature(ref region);

                //���� ������ �� ��� �����
                if (region.IsUnderwater == false)
                {
                    //���������� ������� ����������� �������
                    int t = 0;

                    //��� ������� ������ �����������
                    for (; t < mapGenerationData.Value.temperatureBands.Length; t++)
                    {
                        //���� ����������� ������ ������
                        if (temperature < mapGenerationData.Value.temperatureBands[t])
                        {
                            break;
                        }
                    }

                    //���������� ������� ��������� �������
                    int m = 0;

                    //��� ������� ������ ���������
                    for (; m < mapGenerationData.Value.moistureBands.Length; m++)
                    {
                        //���� ��������� ������ ������
                        if (regionClimate.moisture < mapGenerationData.Value.moistureBands[m])
                        {
                            break;
                        }
                    }

                    //���������� ���� �������
                    ref DRegionBiome biome = ref mapGenerationData.Value.biomes[t * 4 + m];

                    //���� ��� ��������� ����� - �������
                    if (biome.terrainTypeIndex == 0)
                    {
                        //���� ������ ������� ������ ��� ����� ������, ��������� ��� ���������� �������
                        if (region.Elevation >= rockDesertElevation)
                        {
                            //������������� ��� ��������� ������� �� ������
                            region.TerrainTypeIndex = 3;
                        }
                        //�����
                        else
                        {
                            //������������� ��� ��������� �� ����������� ��������
                            region.TerrainTypeIndex = biome.terrainTypeIndex;
                        }
                    }
                    //�����, ���� ������ ������� ����� ������������
                    else if (region.Elevation == mapGenerationData.Value.elevationMaximum)
                    {
                        //������������� ��� ��������� ������� �� ����
                        region.TerrainTypeIndex = 4;
                    }
                    //�����
                    else
                    {
                        //������������� ��� ��������� �������
                        region.TerrainTypeIndex = biome.terrainTypeIndex;
                    }
                }
                //�����
                else
                {
                    //���������� ��� ���������
                    int terrainTypeIndex;

                    //���� ������ �� ���� ������� ���� ������ ����
                    if (region.Elevation == mapGenerationData.Value.waterLevel - 1)
                    {
                        //���������� ���������� ������� �� �������� � ��������
                        int cliffs = 0, slopes = 0;

                        //��� ������� ������
                        for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
                        {
                            //���� ������
                            region.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                            ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                            //���������� ������� � ������ � ������� ����
                            int elevationDelta = neighbourRegion.Elevation - region.WaterLevel;

                            //���� ������� ����� ����
                            if (elevationDelta == 0)
                            {
                                //�� ����� �����
                                slopes += 1;
                            }
                            //�����, ���� ������� ������ ����
                            else if (elevationDelta > 0)
                            {
                                //�� ����� �����
                                cliffs += 1;
                            }
                        }

                        //���� ����� ������� � ������� ������ ���
                        if (cliffs + slopes > 3)
                        {
                            //��� ��������� - �����
                            terrainTypeIndex = 1;
                        }
                        //�����, ���� ������� ������ ����
                        else if (cliffs > 0)
                        {
                            //��� ��������� - ������
                            terrainTypeIndex = 3;
                        }
                        //�����, ���� ������� ������ ����
                        else if (slopes > 0)
                        {
                            //��� ��������� - �������
                            terrainTypeIndex = 0;
                        }
                        //�����
                        else
                        {
                            //��� ��������� - �����
                            terrainTypeIndex = 1;
                        }
                    }
                    //�����, ���� ������ ������� ������ ������ ����
                    else if (region.Elevation >= mapGenerationData.Value.waterLevel)
                    {
                        //��� ��������� - �����
                        terrainTypeIndex = 1;
                    }
                    //�����, ���� ������ ������� ������������
                    else if (region.Elevation < 0)
                    {
                        //��� ��������� - ������
                        terrainTypeIndex = 3;
                    }
                    //�����
                    else
                    {
                        //��� ��������� - �����
                        terrainTypeIndex = 2;
                    }

                    //���� ��� ��������� - ����� � ����������� ��������� � ����� ������ ���������
                    if (terrainTypeIndex == 1 && temperature < mapGenerationData.Value.temperatureBands[0])
                    {
                        //��� ��������� - �����
                        terrainTypeIndex = 2;
                    }

                    //������������� ��� ���������
                    region.TerrainTypeIndex = terrainTypeIndex;
                }
            }
        }

        float RegionDetermineTemperature(
            ref CRegion region)
        {
            //������������ ������ �������
            float latitude = (float)region.centerPoint.z / 1f;
            latitude *= 2f;
            //���� ������ ������ �������, �� ��� ������ ���������
            if (latitude > 1f)
            {
                latitude = 2f - latitude;
            }

            //������������ ����������� �������
            //�� ������
            float temperature = Mathf.LerpUnclamped(mapGenerationData.Value.lowTemperature, mapGenerationData.Value.highTemperature, latitude);
            //�� ������
            temperature *= 1f - (region.ViewElevation - mapGenerationData.Value.waterLevel)
                / (mapGenerationData.Value.elevationMaximum - mapGenerationData.Value.waterLevel + 1f);
            //���������� �����������
            return temperature;
        }
    }
}