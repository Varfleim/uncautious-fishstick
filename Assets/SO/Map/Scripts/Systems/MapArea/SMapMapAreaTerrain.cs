
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Generation;
using SO.Map.Province;

namespace SO.Map.MapArea
{
    public class SMapMapAreaTerrain : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CMapArea> mAPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //������
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<ProvincesData> provincesData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //���� ������
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //������ �������� ��� �����
                MapAreaTerrainCreate();
            }
        }

        void MapAreaTerrainCreate()
        {
            //�������� ���������� ������ ���
            MapAreaSetElevationFirst();

            //�������� ���������� ������ ���
            MapAreaSetElevationSecond();
        }

        void MapAreaSetElevationFirst()
        {
            //���������� ���������� ��� ��� ��������� ��������� ������
            int firstElevationSetMACount = Mathf.RoundToInt(
                provincesData.Value.mapAreaPEs.Length
                * mapGenerationData.Value.mapAreaFirstElevationSetCount * 0.01f);

            //���� ������� ������ ����
            while (firstElevationSetMACount > 0)
            {
                //���� ��������� ����
                provincesData.Value.GetMapAreaRandom().Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //���� ���������� ���� ����� ������, �������� �� ����
                while (mA.Elevation != 0)
                {
                    //���� ��������� ����
                    provincesData.Value.GetMapAreaRandom().Unpack(world.Value, out mAEntity);
                    mA = ref mAPool.Value.Get(mAEntity);
                }

                //���������� ��������� ������ ����
                mA.Elevation = Random.Range(mapGenerationData.Value.elevationMinimum, mapGenerationData.Value.elevationMaximum + 1);

                //��������� ������� ��� ��� ��������� ��������� ������
                firstElevationSetMACount--;
            }
        }

        void MapAreaSetElevationSecond()
        {
            //���������� ���������� ��� ��� ��������� ��������� ������
            int secondElevationSetMACount = Mathf.RoundToInt(
                provincesData.Value.mapAreaPEs.Length 
                * mapGenerationData.Value.mapAreaSecondElevationSetCount * 0.01f);

            //���� ������� ������ ����
            while (secondElevationSetMACount > 0)
            {
                //���� ��������� ����
                provincesData.Value.GetMapAreaRandom().Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //���� ���������� ���� ����� ������, �������� �� ����
                while (mA.Elevation != 0)
                {
                    //���� ��������� ����
                    provincesData.Value.GetMapAreaRandom().Unpack(world.Value, out mAEntity);
                    mA = ref mAPool.Value.Get(mAEntity);
                }

                //���������� ������� ������ �������� ���
                int averageNeighbourElevation = 0;
                int neighbourWithElevationCount = 0;

                //��� ������ �������� ���� 
                for (int a = 0; a < mA.neighbourMAPEs.Length; a++)
                {
                    //���� �������� ����
                    mA.neighbourMAPEs[a].Unpack(world.Value, out int neighbourMAEntity);
                    ref CMapArea neighbourMA = ref mAPool.Value.Get(neighbourMAEntity);

                    //���� ������ ���� �� ����� ����
                    if (neighbourMA.Elevation != 0)
                    {
                        //���������� ������ �������� ���� 
                        averageNeighbourElevation += neighbourMA.Elevation;

                        //����������� ������� ������� � �������
                        neighbourWithElevationCount++;
                    }
                }

                //���� ������� ������ ���������� �� ����
                if (averageNeighbourElevation != 0)
                {
                    //��������� ������� ������
                    averageNeighbourElevation = Mathf.CeilToInt((float)averageNeighbourElevation / neighbourWithElevationCount);

                    //���������� ��������� ������ ���� � ������ �� ������� ����� ��� �� �������
                    mA.Elevation = Random.Range(averageNeighbourElevation - 2, averageNeighbourElevation);

                    //������������ ������ 
                    mA.Elevation = Mathf.Clamp(mA.Elevation, mapGenerationData.Value.elevationMinimum, mapGenerationData.Value.elevationMaximum);

                    //���� �������� ������ �� ����� ����
                    if (mA.Elevation != 0)
                    {
                        //��������� ������� ��� ��� ��������� ��������� ������
                        secondElevationSetMACount--;
                    }
                }
            }
        }
    }
}