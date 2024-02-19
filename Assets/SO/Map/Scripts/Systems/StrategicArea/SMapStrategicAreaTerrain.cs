
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;

namespace SO.Map.StrategicArea
{
    public class SMapStrategicAreaTerrain : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CStrategicArea> sAPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //������
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //���� ������
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //������ �������� �������������� ��������
                StrategicAreaTerrainCreate();
            }
        }

        void StrategicAreaTerrainCreate()
        {
            //�������� ���������� ������ ��������
            StrategicAreaSetElevationFirst();

            //�������� ���������� ������ ��������
            StrategicAreaSetElevationSecond();
        }

        void StrategicAreaSetElevationFirst()
        {
            //���������� ���������� �������� ��� ��������� ��������� ������
            int firstElevationSetSACount = Mathf.RoundToInt(
                regionsData.Value.strategicAreaPEs.Length
                * mapGenerationData.Value.strategicAreaFirstElevationSetCount * 0.01f);

            //���� ������� ������ ����
            while (firstElevationSetSACount > 0)
            {
                //���� ��������� �������
                regionsData.Value.GetStrategicAreaRandom().Unpack(world.Value, out int sAEntity);
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                //���� ���������� ������� ����� ������, �������� �� ����
                while (sA.Elevation != 0)
                {
                    //���� ��������� �������
                    regionsData.Value.GetStrategicAreaRandom().Unpack(world.Value, out sAEntity);
                    sA = ref sAPool.Value.Get(sAEntity);
                }

                //���������� ��������� ������ �������
                sA.Elevation = Random.Range(mapGenerationData.Value.elevationMinimum, mapGenerationData.Value.elevationMaximum + 1);

                //��������� ������� �������� ��� ��������� ��������� ������
                firstElevationSetSACount--;
            }
        }

        void StrategicAreaSetElevationSecond()
        {
            //���������� ���������� �������� ��� ��������� ��������� ������
            int secondElevationSetSACount = Mathf.RoundToInt(
                regionsData.Value.strategicAreaPEs.Length 
                * mapGenerationData.Value.strategicAreaSecondElevationSetCount * 0.01f);

            //���� ������� ������ ����
            while (secondElevationSetSACount > 0)
            {
                //���� ��������� �������
                regionsData.Value.GetStrategicAreaRandom().Unpack(world.Value, out int sAEntity);
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                //���� ���������� ������� ����� ������, �������� �� ����
                while (sA.Elevation != 0)
                {
                    //���� ��������� �������
                    regionsData.Value.GetStrategicAreaRandom().Unpack(world.Value, out sAEntity);
                    sA = ref sAPool.Value.Get(sAEntity);
                }

                //���������� ������� ������ �������� ��������
                int averageNeighbourElevation = 0;
                int neighbourWithElevationCount = 0;

                //��� ������ �������� ������� 
                for (int a = 0; a < sA.neighbourSAPEs.Length; a++)
                {
                    //���� �������� �������
                    sA.neighbourSAPEs[a].Unpack(world.Value, out int neighbourSAEntity);
                    ref CStrategicArea neighbourSA = ref sAPool.Value.Get(neighbourSAEntity);

                    //���� ������ ������� �� ����� ����
                    if (neighbourSA.Elevation != 0)
                    {
                        //���������� ������ �������� ������� 
                        averageNeighbourElevation += neighbourSA.Elevation;

                        //����������� ������� ������� � �������
                        neighbourWithElevationCount++;
                    }
                }

                //���� ������� ������ ���������� �� ����
                if (averageNeighbourElevation != 0)
                {
                    //��������� ������� ������
                    averageNeighbourElevation = Mathf.CeilToInt((float)averageNeighbourElevation / neighbourWithElevationCount);

                    //���������� ��������� ������ ������� � ������ �� ������� ����� ��� �� �������
                    sA.Elevation = Random.Range(averageNeighbourElevation - 2, averageNeighbourElevation);

                    //������������ ������ 
                    sA.Elevation = Mathf.Clamp(sA.Elevation, mapGenerationData.Value.elevationMinimum, mapGenerationData.Value.elevationMaximum);

                    //���� �������� ������ �� ����� ����
                    if (sA.Elevation != 0)
                    {
                        //��������� ������� �������� ��� ��������� ��������� ������
                        secondElevationSetSACount--;
                    }
                }
            }
        }
    }
}