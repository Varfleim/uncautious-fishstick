
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SCM.Map.Pathfinding;
using SCM.Map.Events;

namespace SCM.Map
{
    public class SMapTerrain : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //�����
        readonly EcsFilterInject<Inc<CRegion>> regionFilter = default;
        readonly EcsPoolInject<CRegion> regionPool = default;

        //������� �����
        readonly EcsFilterInject<Inc<RMapGeneration>> mapGenerationRequestFilter = default;
        readonly EcsPoolInject<RMapGeneration> mapGenerationRequestPool = default;

        //������
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach(int requestEntity in mapGenerationRequestFilter.Value)
            {
                //���� ������
                ref RMapGeneration requestComp = ref mapGenerationRequestPool.Value.Get(requestEntity);

                //���������� ��������
                TerrainCreate(ref requestComp);
            }
        }

        void TerrainCreate(
            ref RMapGeneration requestComp)
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
            ref RMapGeneration requestComp)
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
            ref CRegion firstRegion = ref regionPool.Value.Get(firstRegionEntity);

            //������������� ��� ���� ������ �� �������
            regions[firstRegion.Index].distance = 0;
            regions[firstRegion.Index].priority = 2;
            regions[firstRegion.Index].status = regionsData.Value.openRegionValue;

            //������� ������ � �������
            queue.Push(firstRegion.Index);

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
                ref CRegion currentRegion = ref regionPool.Value.Get(currentRegionEntity);

                //���������� �������� ������ �������
                int originalElevation = currentRegion.Elevation;
                //���������� ����� ������ �������
                int newElevation = originalElevation + rise;

                //���� ����� ������ ������ ������������
                if (newElevation > mapGenerationData.Value.elevationMaximum)
                {
                    continue;
                }

                //����������� ������ �������
                currentRegion.Elevation = newElevation;
                currentRegion.ExtrudeAmount = (float)currentRegion.Elevation / (mapGenerationData.Value.elevationMaximum + 1);

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
                for (int i = 0; i < currentRegion.neighbourRegionPEs.Length; i++)
                {
                    //���� ������
                    currentRegion.neighbourRegionPEs[i].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                    //���� ���� ������ ������ ������ �������
                    if (regions[neighbourRegion.Index].status < regionsData.Value.openRegionValue)
                    {
                        //������������� ���� ������ �� �������
                        regions[neighbourRegion.Index].status = regionsData.Value.openRegionValue;
                        regions[neighbourRegion.Index].distance = 0;
                        regions[neighbourRegion.Index].priority = Vector3.Angle(firstRegion.center, neighbourRegion.center) * 2f;
                        regions[neighbourRegion.Index].priority += Random.value < mapGenerationData.Value.jitterProbability ? 1 : 0;

                        //������� ������ � �������
                        queue.Push(neighbourRegion.Index);
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
            ref CRegion firstRegion = ref regionPool.Value.Get(firstRegionEntity);

            //������������� ��� ���� ������ �� �������
            regions[firstRegion.Index].distance = 0;
            regions[firstRegion.Index].priority = 2;
            regions[firstRegion.Index].status = regionsData.Value.openRegionValue;

            //������� ������ � �������
            queue.Push(firstRegion.Index);

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
                ref CRegion currentRegion = ref regionPool.Value.Get(currentRegionEntity);

                //���������� �������� ������ �������
                int originalElevation = currentRegion.Elevation;
                //���������� ����� ������ �������
                int newElevation = currentRegion.Elevation - sink;

                //���� ����� ������ ������� ������ �����������
                if (newElevation < mapGenerationData.Value.elevationMinimum)
                {
                    continue;
                }

                //����������� ������ �������
                currentRegion.Elevation = newElevation;

                if (currentRegion.Elevation > 0)
                {
                    currentRegion.ExtrudeAmount = (float)currentRegion.Elevation / (mapGenerationData.Value.elevationMaximum + 1);
                }
                else
                {
                    currentRegion.ExtrudeAmount = 0;
                }

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
                for (int i = 0; i < currentRegion.neighbourRegionPEs.Length; i++)
                {
                    //���� ������
                    currentRegion.neighbourRegionPEs[i].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                    //���� ���� ������ ������ ������ �������
                    if (regions[neighbourRegion.Index].status < regionsData.Value.openRegionValue)
                    {
                        //������������� ���� ������ �� �������
                        regions[neighbourRegion.Index].status = regionsData.Value.openRegionValue;
                        regions[neighbourRegion.Index].distance = 0;
                        regions[neighbourRegion.Index].priority = Vector3.Angle(firstRegion.center, neighbourRegion.center) * 2f;
                        regions[neighbourRegion.Index].priority += Random.value < mapGenerationData.Value.jitterProbability ? 1 : 0;

                        //������� ������ � �������
                        queue.Push(neighbourRegion.Index);
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
                ref CRegion region = ref regionPool.Value.Get(regionEntity);

                //���� ������ �������� ������, ������� ��� � ������
                if (RegionIsErodible(ref region) == true)
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
                ref CRegion region = ref regionPool.Value.Get(erodibleRegions[index]);

                //�������� ������ ������ ��� ���� ������
                int targetRegionEntity = RegionGetErosionTarget(ref region);
                ref CRegion targetRegion = ref regionPool.Value.Get(targetRegionEntity);

                //��������� ������ ��������� ������� � ����������� ������ ��������
                region.Elevation -= 1;
                if (region.Elevation > 0)
                {
                    region.ExtrudeAmount = (float)region.Elevation / (mapGenerationData.Value.elevationMaximum + 1);
                }
                else
                {
                    region.ExtrudeAmount = 0;
                }
                targetRegion.Elevation += 1;
                if (targetRegion.Elevation > 0)
                {
                    targetRegion.ExtrudeAmount = (float)targetRegion.Elevation / (mapGenerationData.Value.elevationMaximum + 1);
                }
                else
                {
                    targetRegion.ExtrudeAmount = 0;
                }

                //���� ������ ������ �� �������� ������
                if (RegionIsErodible(ref region) == false)
                {
                    //������� ��� �������� �� ������, ������� ��������� �������� � ������
                    erodibleRegions[index] = erodibleRegions[erodibleRegions.Count - 1];
                    erodibleRegions.RemoveAt(erodibleRegions.Count - 1);
                }

                //��� ������� ������
                for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
                {
                    //���� ������
                    region.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                    //���� ������ ������ �� 2 ������ ������������ �������
                    if (neighbourRegion.Elevation == region.Elevation + 2
                        //���� ����� �������� ������
                        && RegionIsErodible(ref neighbourRegion) == true
                        //� ���� ������ �� �������� �������� ������
                        && erodibleRegions.Contains(neighbourRegionEntity) == false)
                    {
                        //������� ������ � ������
                        erodibleRegions.Add(neighbourRegionEntity);
                    }
                }

                //���� ������� ������ �������� ������
                if (RegionIsErodible(ref targetRegion)
                    //� ������ ��������� ���������� ������ �������� �� �������� ���
                    && erodibleRegions.Contains(targetRegionEntity) == false)
                {
                    //������� ������� ������ � ������
                    erodibleRegions.Add(targetRegionEntity);
                }

                //��� ������� ������
                for (int a = 0; a < targetRegion.neighbourRegionPEs.Length; a++)
                {
                    //���� ����� ����������
                    if (targetRegion.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity)
                        //� ���� ����� - �� �������� ����������� ������
                        && region.selfPE.EqualsTo(targetRegion.neighbourRegionPEs[a]) == false)
                    {
                        //���� ������
                        ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                        //���� ������ ������ �� ������� ������, ��� ������ �������� �������
                        if (neighbourRegion.Elevation == targetRegion.Elevation + 1
                            //� ���� ����� �� �������� ������
                            && RegionIsErodible(ref neighbourRegion) == false
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
            ref CRegion region)
        {
            //���������� ������, ��� ������� ��������� ������
            int erodibleElevation = region.Elevation - 2;

            //��� ������� ������ 
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                region.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                //���� ������ ������ ������ ��� ����� ������ ������
                if (neighbourRegion.Elevation <= erodibleElevation)
                {
                    //����������, ��� ������ ��������
                    return true;
                }
            }

            //����������, ��� ������ ����������
            return false;
        }

        int RegionGetErosionTarget(
            ref CRegion region)
        {
            //������ ������ ���������� ����� ������
            List<int> candidateEntities = ListPool<int>.Get();

            //���������� ������, ��� ������� ��������� ������
            int erodibleElevation = region.Elevation - 2;

            //��� ������� ������
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                region.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                //���� ������ ������ ������ ��� ����� ������ ������
                if (neighbourRegion.Elevation <= erodibleElevation)
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