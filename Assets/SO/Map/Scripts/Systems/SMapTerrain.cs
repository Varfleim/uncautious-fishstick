
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Pathfinding;
using SO.Map.Events;

namespace SO.Map
{
    public class SMapTerrain : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //�����
        readonly EcsFilterInject<Inc<CRegionCore>> regionFilter = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //������
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach(int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //���� ������
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //���������� ��������
                TerrainCreate(ref requestComp);
            }
        }

        void TerrainCreate(
            ref RMapGenerating requestComp)
        {
            //�������������� ������ ���������
            TerrainDataInitialization(ref requestComp);

            //������ ������ � ������� ��� ������
            DPathfindingNodeFast[] regions = new DPathfindingNodeFast[regionsData.Value.regionPEs.Length];
            PathfindingQueueInt queue = new PathfindingQueueInt(
                new PathfindingNodesComparer(regions),
                regionsData.Value.regionPEs.Length);

            //������ ����
            TerrainCreateLand(queue, ref regions);

            //�������� ������
            TerrainErodeLand();
        }

        void TerrainDataInitialization(
            ref RMapGenerating requestComp)
        {

        }

        void TerrainCreateLand(
            PathfindingQueueInt queue, ref DPathfindingNodeFast[] regions)
        {
            //���������� ������ ����
            int landBudget = Mathf.RoundToInt(regionsData.Value.regionPEs.Length * mapGenerationData.Value.landPercentage * 0.01f);

            //���� ������ ���� ������ ���� � ������ ����� ������� ��������
            for (int a = 0; a < 25000; a++)
            {
                //����������, ����� �� ������� ����
                bool sink = Random.value < mapGenerationData.Value.sinkProbability;

                //���������� ������ �����
                int chunkSize = Random.Range(mapGenerationData.Value.chunkSizeMin, mapGenerationData.Value.chunkSizeMax + 1);

                //���� ��������� ������� ����
                if (sink == true)
                {
                    //����� ����
                    MapSinkTerrain(
                        queue, ref regions,
                        chunkSize,
                        landBudget,
                        out landBudget);
                }
                //�����
                else
                {
                    //��������� ����
                    MapRaiseTerrain(
                        queue, ref regions,
                        chunkSize,
                        landBudget,
                        out landBudget);

                    //���� ������ ���� ����� ����
                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }

            //���� ������ ���� ������ ����
            if (landBudget > 0)
            {
                Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
            }
        }

        void MapRaiseTerrain(
            PathfindingQueueInt queue, ref DPathfindingNodeFast[] regions,
            int chunkSize,
            int landBudget,
            out int currentLandBudget)
        {
            //���� ���� ������ ������ 250
            if (regionsData.Value.openRegionValue > 250)
            {
                //�������� ����
                regionsData.Value.openRegionValue = 1;
                regionsData.Value.closeRegionValue = 2;
            }
            //�����
            else
            {
                //��������� ����
                regionsData.Value.openRegionValue += 2;
                regionsData.Value.closeRegionValue += 2;
            }
            //������� �������
            queue.Clear();

            //���� ��������� ������
            regionsData.Value.GetRegionRandom().Unpack(world.Value, out int firstRegionEntity);
            ref CRegionCore firstRC = ref rCPool.Value.Get(firstRegionEntity);

            //������������� ��� ���� ������ �� �������
            regions[firstRC.Index].distance = 0;
            regions[firstRC.Index].priority = 2;
            regions[firstRC.Index].status = regionsData.Value.openRegionValue;

            //������� ������ � �������
            queue.Push(firstRC.Index);

            //����������, ����� ��� ������� ��������� ���� ��� �������
            int rise = Random.value < mapGenerationData.Value.highRiseProbability ? 2 : 1;
            //������ ������� �������
            int currentSize = 0;

            //���� ������� ������ ���������� ������� � ������� �� �����
            while (currentSize < chunkSize && queue.regionsCount > 0)
            {
                //���� ������ ������ � �������
                int currentRegionIndex = queue.Pop();
                regionsData.Value.regionPEs[currentRegionIndex].Unpack(world.Value, out int currentRegionEntity);
                ref CRegionCore currentRC = ref rCPool.Value.Get(currentRegionEntity);

                //���������� �������� ������ �������
                int originalElevation = currentRC.Elevation;
                //���������� ����� ������ �������
                int newElevation = originalElevation + rise;

                //���� ����� ������ ������ ������������
                if (newElevation > mapGenerationData.Value.elevationMaximum)
                {
                    continue;
                }

                //����������� ������ �������
                currentRC.Elevation = newElevation;

                //���� �������� ������ ������� ������ ������ ����
                if (originalElevation < mapGenerationData.Value.waterLevel
                    //� ����� ������ ������ ��� ����� ������ ����
                    && newElevation >= mapGenerationData.Value.waterLevel
                    //� ����������� ������ ���� ����� ����
                    && --landBudget == 0)
                {
                    break;
                }

                //����������� �������
                currentSize += 1;

                //��� ������� ������ �������� �������
                for (int i = 0; i < currentRC.neighbourRegionPEs.Length; i++)
                {
                    //���� ������
                    currentRC.neighbourRegionPEs[i].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                    //���� ���� ������ ������ ������ �������
                    if (regions[neighbourRC.Index].status < regionsData.Value.openRegionValue)
                    {
                        //������������� ���� ������ �� �������
                        regions[neighbourRC.Index].status = regionsData.Value.openRegionValue;
                        regions[neighbourRC.Index].distance = 0;
                        regions[neighbourRC.Index].priority = Vector3.Angle(firstRC.center, neighbourRC.center) * 2f;
                        regions[neighbourRC.Index].priority += Random.value < mapGenerationData.Value.jitterProbability ? 1 : 0;

                        //������� ������ � �������
                        queue.Push(neighbourRC.Index);
                    }
                }
            }

            //���������� ������ ����
            currentLandBudget = landBudget;
        }

        void MapSinkTerrain(
            PathfindingQueueInt queue, ref DPathfindingNodeFast[] regions,
            int chunkSize,
            int landBudget,
            out int currentLandBudget)
        {
            //���� ���� ������ ������ 250
            if (regionsData.Value.openRegionValue > 250)
            {
                //�������� ����
                regionsData.Value.openRegionValue = 1;
                regionsData.Value.closeRegionValue = 2;
            }
            //�����
            else
            {
                //��������� ����
                regionsData.Value.openRegionValue += 2;
                regionsData.Value.closeRegionValue += 2;
            }
            //������� �������
            queue.Clear();

            //���� ��������� ������
            regionsData.Value.GetRegionRandom().Unpack(world.Value, out int firstRegionEntity);
            ref CRegionCore firstRC = ref rCPool.Value.Get(firstRegionEntity);

            //������������� ��� ���� ������ �� �������
            regions[firstRC.Index].distance = 0;
            regions[firstRC.Index].priority = 2;
            regions[firstRC.Index].status = regionsData.Value.openRegionValue;

            //������� ������ � �������
            queue.Push(firstRC.Index);

            //����������, ����� ��� �������� ���������� ���� ��� �������
            int sink = Random.value < mapGenerationData.Value.highRiseProbability ? 2 : 1;
            //������ ������� �������
            int currentSize = 0;

            //���� ������� ������ ���������� ������� � ������� �� �����
            while (currentSize < chunkSize && queue.regionsCount > 0)
            {
                //���� ������ ������ � �������
                int currentRegionIndex = queue.Pop();
                regionsData.Value.regionPEs[currentRegionIndex].Unpack(world.Value, out int currentRegionEntity);
                ref CRegionCore currentRC = ref rCPool.Value.Get(currentRegionEntity);

                //���������� �������� ������ �������
                int originalElevation = currentRC.Elevation;
                //���������� ����� ������ �������
                int newElevation = currentRC.Elevation - sink;

                //���� ����� ������ ������� ������ �����������
                if (newElevation < mapGenerationData.Value.elevationMinimum)
                {
                    continue;
                }

                //����������� ������ �������
                currentRC.Elevation = newElevation;

                //���� �������� ������ ������� ������ ��� ����� ������ ����
                if (originalElevation >= mapGenerationData.Value.waterLevel
                    //� ����� ������ ������ ������ ����
                    && newElevation < mapGenerationData.Value.waterLevel)
                {
                    //����������� ������ ����
                    landBudget += 1;
                }

                //����������� �������
                currentSize += 1;

                //��� ������� ������ �������� �������
                for (int i = 0; i < currentRC.neighbourRegionPEs.Length; i++)
                {
                    //���� ������
                    currentRC.neighbourRegionPEs[i].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                    //���� ���� ������ ������ ������ �������
                    if (regions[neighbourRC.Index].status < regionsData.Value.openRegionValue)
                    {
                        //������������� ���� ������ �� �������
                        regions[neighbourRC.Index].status = regionsData.Value.openRegionValue;
                        regions[neighbourRC.Index].distance = 0;
                        regions[neighbourRC.Index].priority = Vector3.Angle(firstRC.center, neighbourRC.center) * 2f;
                        regions[neighbourRC.Index].priority += Random.value < mapGenerationData.Value.jitterProbability ? 1 : 0;

                        //������� ������ � �������
                        queue.Push(neighbourRC.Index);
                    }
                }
            }

            //���������� ������ ����
            currentLandBudget = landBudget;
        }

        void TerrainErodeLand()
        {
            //������ ������ ��������� ��������, ���������� ������
            List<int> erodibleRegions = ListPool<int>.Get();

            //��� ������� �������
            foreach (int regionEntity in regionFilter.Value)
            {
                //���� ������
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //���� ������ �������� ������, ������� ��� � ������
                if (RegionIsErodible(ref rC) == true)
                {
                    erodibleRegions.Add(regionEntity);
                }
            }

            //����������, ������� �������� ������ ������������� ������
            int erodibleRegionsCount = (int)(erodibleRegions.Count * (100 - mapGenerationData.Value.erosionPercentage) * 0.01f);

            //���� ������ ��������� �������� ������ ���������� �����
            while (erodibleRegions.Count > erodibleRegionsCount)
            {
                //�������� ��������� ������ � ������
                int index = Random.Range(0, erodibleRegions.Count);
                ref CRegionCore rC = ref rCPool.Value.Get(erodibleRegions[index]);

                //�������� ������ ������ ��� ���� ������
                int targetRegionEntity = RegionGetErosionTarget(ref rC);
                ref CRegionCore targetRC = ref rCPool.Value.Get(targetRegionEntity);

                //��������� ������ ��������� ������� � ����������� ������ ��������
                rC.Elevation -= 1;
                targetRC.Elevation += 1;

                //���� ������ ������ �� �������� ������
                if (RegionIsErodible(ref rC) == false)
                {
                    //������� ��� �������� �� ������, ������� ��������� �������� � ������
                    erodibleRegions[index] = erodibleRegions[erodibleRegions.Count - 1];
                    erodibleRegions.RemoveAt(erodibleRegions.Count - 1);
                }

                //��� ������� ������
                for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
                {
                    //���� ������
                    rC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                    //���� ������ ������ �� 2 ������ ������������ �������
                    if (neighbourRC.Elevation == rC.Elevation + 2
                        //���� ����� �������� ������
                        && RegionIsErodible(ref neighbourRC) == true
                        //� ���� ������ �� �������� �������� ������
                        && erodibleRegions.Contains(neighbourRegionEntity) == false)
                    {
                        //������� ������ � ������
                        erodibleRegions.Add(neighbourRegionEntity);
                    }
                }

                //���� ������� ������ �������� ������
                if (RegionIsErodible(ref targetRC)
                    //� ������ ��������� ���������� ������ �������� �� �������� ���
                    && erodibleRegions.Contains(targetRegionEntity) == false)
                {
                    //������� ������� ������ � ������
                    erodibleRegions.Add(targetRegionEntity);
                }

                //��� ������� ������
                for (int a = 0; a < targetRC.neighbourRegionPEs.Length; a++)
                {
                    //���� ����� ����������
                    if (targetRC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity)
                        //� ���� ����� - �� �������� ����������� ������
                        && rC.selfPE.EqualsTo(targetRC.neighbourRegionPEs[a]) == false)
                    {
                        //���� ������
                        ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                        //���� ������ ������ �� ������� ������, ��� ������ �������� �������
                        if (neighbourRC.Elevation == targetRC.Elevation + 1
                            //� ���� ����� �� �������� ������
                            && RegionIsErodible(ref neighbourRC) == false
                            //� ���� ������ �������� �������� ������
                            && erodibleRegions.Contains(neighbourRegionEntity) == true)
                        {
                            //������� ������ �� ������
                            erodibleRegions.Remove(neighbourRegionEntity);
                        }
                    }
                }
            }

            //������� ������ � ���
            ListPool<int>.Add(erodibleRegions);
        }

        bool RegionIsErodible(
            ref CRegionCore rC)
        {
            //���������� ������, ��� ������� ��������� ������
            int erodibleElevation = rC.Elevation - 2;

            //��� ������� ������ 
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                rC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                //���� ������ ������ ������ ��� ����� ������ ������
                if (neighbourRC.Elevation <= erodibleElevation)
                {
                    //����������, ��� ������ ��������
                    return true;
                }
            }

            //����������, ��� ������ ����������
            return false;
        }

        int RegionGetErosionTarget(
            ref CRegionCore rC)
        {
            //������ ������ ���������� ����� ������
            List<int> candidateEntities = ListPool<int>.Get();

            //���������� ������, ��� ������� ��������� ������
            int erodibleElevation = rC.Elevation - 2;

            //��� ������� ������
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                rC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                //���� ������ ������ ������ ��� ����� ������ ������
                if (neighbourRC.Elevation <= erodibleElevation)
                {
                    //������� ������ � ������
                    candidateEntities.Add(neighbourRegionEntity);
                }
            }

            //�������� �������� ������ �� ������
            int targetRegionEntity = candidateEntities[Random.Range(0, candidateEntities.Count)];

            //������� ������ � ���
            ListPool<int>.Add(candidateEntities);

            //���������� �������� �������� �������
            return targetRegionEntity;
        }
    }
}