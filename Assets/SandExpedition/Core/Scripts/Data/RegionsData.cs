
using System;
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;

using SCM.Map.Pathfinding;

namespace SCM.Map
{
    public class RegionsData : MonoBehaviour
    {
        //������ ��������
        public EcsPackedEntity[] regionPEs;

        //������ ������ ����
        public bool[] needRefreshPathMatrix = new bool[Environment.ProcessorCount];
        public DPathfindingNodeFast[][] pathfindingArray = new DPathfindingNodeFast[Environment.ProcessorCount][];
        public PathfindingQueueInt[] pathFindingQueue = new PathfindingQueueInt[Environment.ProcessorCount];
        public List<DPathfindingClosedNode>[] closedNodes = new List<DPathfindingClosedNode>[Environment.ProcessorCount];
        public byte[] openRegionValues = new byte[Environment.ProcessorCount];
        public byte[] closeRegionValues = new byte[Environment.ProcessorCount];
        public int pathfindingSearchLimit = 30000;

        public bool needRefreshRouteMatrix;
        public DPathfindingNodeFast[] pfCalc;
        public PathfindingQueueInt open;
        public List<DPathfindingClosedNode> close = new();
        public byte openRegionValue = 1;
        public byte closeRegionValue = 2;
        public const int pathFindingSearchLimitBase = 30000;
        public int pathFindingSearchLimit;

        public EcsPackedEntity GetRegion(
            int regionIndex)
        {
            //���������� PE ������������ �������
            return regionPEs[regionIndex];
        }
        public EcsPackedEntity GetRegionRandom()
        {
            //���������� PE ���������� �������
            return GetRegion(UnityEngine.Random.Range(0, regionPEs.Length));
        }

        public List<int> GetRegionIndicesWithinSteps(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegion> regionPool,
            ref CRegion region,
            int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = new();

            //��� ������� ������ �������
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                region.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourRegion.Index);
            }

            //������ �������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ������ � �������
            processed.Add(region.Index, true);

            //������ �������� ������
            List<int> results = new();

            //������ �������� ������� ��� �������������� ��������
            int candidatesLast = candidates.Count - 1;

            //���� �� ��������� ��������� ������ � ������
            while (candidatesLast >= 0)
            {
                //���� ���������� ���������
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                regionPEs[candidateIndex].Unpack(world, out int regionEntity);
                ref CRegion candidateRegion = ref regionPool.Get(regionEntity);

                //������� ���� �� ����
                List<int> pathRegions = FindPath(
                    world,
                    regionFilter, regionPool,
                    ref region, ref candidateRegion,
                    maxSteps);

                //���� ������� ��� �� �������� ��� � ���������� ���� 
                if (processed.ContainsKey(candidateIndex) == false && pathRegions != null)
                {
                    //������� ��������� � �������� ������ � �������
                    results.Add(candidateRegion.Index);
                    processed.Add(candidateRegion.Index, true);

                    //��� ������� ��������� �������
                    for (int a = 0; a < candidateRegion.neighbourRegionPEs.Length; a++)
                    {
                        //���� ������
                        candidateRegion.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                        ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);

                        //���� ������� �� �������� ���
                        if (processed.ContainsKey(neighbourRegion.Index) == false)
                        {
                            //������� ��� � ������ � ����������� �������
                            candidates.Add(neighbourRegion.Index);
                            candidatesLast++;
                        }
                    }
                }
            }

            return results;
        }

        public List<int> GetRegionIndicesWithinSteps(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegion> regionPool,
            ref CRegion region,
            int minSteps, int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = new();

            //��� ������� ������ �������
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                region.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourRegion.Index);
            }

            //������ �������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ������ � �������
            processed.Add(region.Index, true);

            //������ �������� ������
            List<int> results = new();

            //������ �������� ������� ��� �������������� ��������
            int candidatesLast = candidates.Count - 1;

            //���� �� ��������� ��������� ������ � ������
            while (candidatesLast >= 0)
            {
                //���� ���������� ���������
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                regionPEs[candidateIndex].Unpack(world, out int regionEntity);
                ref CRegion candidateRegion = ref regionPool.Get(regionEntity);

                //������� ���� �� ����
                List<int> pathRegions = FindPath(
                    world,
                    regionFilter, regionPool,
                    ref region, ref candidateRegion,
                    maxSteps);

                //���� ������� ��� �� �������� ��� � ���������� ���� 
                if (processed.ContainsKey(candidateIndex) == false && pathRegions != null)
                {
                    //���� ����� ���� ������ ��� ����� ����������� � ������ ��� ����� ������������
                    if (pathRegions.Count >= minSteps && pathRegions.Count <= maxSteps)
                    {
                        //������� ��������� � �������� ������
                        results.Add(candidateRegion.Index);
                    }

                    //������� ��������� � ������� ������������
                    processed.Add(candidateRegion.Index, true);

                    //��� ������� ��������� �������
                    for (int a = 0; a < candidateRegion.neighbourRegionPEs.Length; a++)
                    {
                        //���� ������
                        candidateRegion.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                        ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);

                        //���� ������� �� �������� ���
                        if (processed.ContainsKey(neighbourRegion.Index) == false)
                        {
                            //������� ��� � ������ � ����������� �������
                            candidates.Add(neighbourRegion.Index);
                            candidatesLast++;
                        }
                    }
                }
            }

            return results;
        }

        public List<int> FindPath(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegion> regionPool,
            ref CRegion startRegion, ref CRegion endRegion,
            int searchLimit = 0)
        {
            //������ ������ ��� �������� �������� ����
            List<int> results = new();

            //������� ���� � ���������� ���������� �������� � ����
            int count = FindPath(
                world,
                regionFilter, regionPool,
                ref startRegion, ref endRegion,
                results,
                searchLimit);

            //���� ���������� ����� ����, �� ���������� ������ ������
            return count == 0 ? null : results;
        }

        public int FindPath(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegion> regionPool,
            ref CRegion fromRegion, ref CRegion toRegion,
            List<int> results,
            int searchLimit = 0)
        {
            //������� ������
            results.Clear();

            //���� ��������� ������ �� ����� ���������
            if (fromRegion.Index != toRegion.Index)
            {
                //������������ ������� ����
                CalculateRouteMatrix(regionFilter, regionPool);

                //���������� ������������ ���������� ����� ��� ������
                pathFindingSearchLimit = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //������� ����
                List<DPathfindingClosedNode> path = FindPathFast(
                    world,
                    regionPool,
                    ref fromRegion, ref toRegion);

                //���� ���� �� ����
                if (path != null)
                {
                    //���� ���������� �������� � ����
                    int routeCount = path.Count;

                    //��� ������� ������� � ����, ����� ���� ���������, � �������� �������
                    for (int r = routeCount - 2; r > 0; r--)
                    {
                        //������� ��� � ������ ��������
                        results.Add(path[r].index);
                    }
                    //������� � ������ �������� ������ ���������� �������
                    results.Add(toRegion.Index);
                }
                //����� ���������� 0, ���������, ��� ���� ����
                else
                {
                    return 0;
                }
            }

            //���������� ���������� �������� � ����
            return results.Count;
        }

        void CalculateRouteMatrix(
            EcsFilter regionFilter, EcsPool<CRegion> regionPool)
        {
            //���� ������� ���� �� ������� ����������, �� ������� �� �������
            if (needRefreshRouteMatrix == false)
            {
                return;
            }

            //��������, ��� ������� ���� �� ������� ����������
            needRefreshRouteMatrix = false;

            //��� ������� �������
            foreach (int regionEntity in regionFilter)
            {
                //���� ������
                ref CRegion region = ref regionPool.Get(regionEntity);

                //������������ ��������� ������� �� �������
                float cost = region.crossCost;

                //��������� ��������� ������� �� �������
                region.crossCost = cost;
            }

            //���� ������ ��� ������ ����
            if (pfCalc == null)
            {
                //������ ������
                pfCalc = new DPathfindingNodeFast[regionPEs.Length];

                //������ ������� 
                open = new(
                    new PathfindingNodesComparer(pfCalc),
                    regionPEs.Length);
            }
            //�����
            else
            {
                //������� ������� � ������
                open.Clear();
                Array.Clear(pfCalc, 0, pfCalc.Length);

                //��������� ���������� �������� � �������
                PathfindingNodesComparer comparer = (PathfindingNodesComparer)open.Comparer;
                comparer.SetMatrix(pfCalc);
            }
        }

        List<DPathfindingClosedNode> FindPathFast(
            EcsWorld world,
            EcsPool<CRegion> regionPool,
            ref CRegion fromRegion, ref CRegion toRegion)
        {
            //������ ���������� ��� ������������ ������� ����
            bool found = false;

            //������ ������� �����
            int stepsCounter = 0;

            //���� ���� ������ ������ 250
            if (openRegionValue > 250)
            {
                //�������� ����
                openRegionValue = 1;
                closeRegionValue = 2;
            }
            //�����
            else
            {
                //��������� ����
                openRegionValue += 2;
                closeRegionValue += 2;
            }
            //������� ������� � ����
            open.Clear();
            close.Clear();

            //���� �������� ������
            Vector3 destinationCenter = toRegion.center;

            //������ ���������� ��� ���������� �������
            int nextRegionIndex;

            //�������� ������ ���������� ������� � �������
            pfCalc[fromRegion.Index].distance = 0;
            pfCalc[fromRegion.Index].priority = 2;
            pfCalc[fromRegion.Index].prevIndex = fromRegion.Index;
            pfCalc[fromRegion.Index].status = openRegionValue;

            //������� ��������� ������ � �������
            open.Push(fromRegion.Index);

            //���� � ������� ���� �������
            while (open.regionsCount > 0)
            {
                //���� ������ ������ � ������� ��� �������
                int currentRegionIndex = open.Pop();

                //���� ������ ������ ��� ����� �� ������� ������, �� ��������� � ����������
                if (pfCalc[currentRegionIndex].status == closeRegionValue)
                {
                    continue;
                }

                //���� ������ ������� ����� ������� ��������� �������
                if (currentRegionIndex == toRegion.Index)
                {
                    //������� ������ �� ������� ������
                    pfCalc[currentRegionIndex].status = closeRegionValue;

                    //��������, ��� ���� ������, � ������� �� �����
                    found = true;
                    break;
                }

                //���� ������� ����� ������ �������
                if (stepsCounter >= pathFindingSearchLimit)
                {
                    return null;
                }

                //���� ������� ������
                regionPEs[currentRegionIndex].Unpack(world, out int currentRegionEntity);
                ref CRegion currentRegion = ref regionPool.Get(currentRegionEntity);

                //��� ������� ������ �������� �������
                for (int a = 0; a < currentRegion.neighbourRegionPEs.Length; a++)
                {
                    //���� ������
                    currentRegion.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                    ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);
                    nextRegionIndex = neighbourRegion.Index;

                    //������������ ���������� �� ������
                    float newDistance = pfCalc[currentRegionIndex].distance + neighbourRegion.crossCost;

                    //���� ������ ��������� � ������� ������ ��� ��� ������� �� �������
                    if (pfCalc[nextRegionIndex].status == openRegionValue
                        || pfCalc[nextRegionIndex].status == closeRegionValue)
                    {
                        //���� ���������� �� ������� ������ ��� ����� ������, �� ��������� � ���������� ������
                        if (pfCalc[nextRegionIndex].distance <= newDistance)
                        {
                            continue;
                        }
                    }
                    //����� ��������� ����������

                    //��������� ������ ����������� ������� � ����������
                    pfCalc[nextRegionIndex].prevIndex = currentRegionIndex;
                    pfCalc[nextRegionIndex].distance = newDistance;

                    //������������ ��������� ������
                    //������������ ����, ������������ � �������� ���������
                    float angle = Vector3.Angle(destinationCenter, neighbourRegion.center);

                    //��������� ��������� ������� � ������
                    pfCalc[nextRegionIndex].priority = newDistance + 2f * angle;
                    pfCalc[nextRegionIndex].status = openRegionValue;

                    //������� ������ � �������
                    open.Push(nextRegionIndex);
                }

                //��������� ������� �����
                stepsCounter++;

                //������� ������� ������ �� ������� ������
                pfCalc[currentRegionIndex].status = closeRegionValue;
            }

            //���� ���� ������
            if (found == true)
            {
                //������� ������ ����
                close.Clear();

                //���� �������� ������
                int pos = toRegion.Index;

                //������ ��������� ��������� � ������� � �� ������ ��������� �������
                DPathfindingNodeFast tempRegion = pfCalc[toRegion.Index];
                DPathfindingClosedNode stepRegion;

                //��������� ������ �� �������� � ��������
                stepRegion.priority = tempRegion.priority;
                stepRegion.distance = tempRegion.distance;
                stepRegion.prevIndex = tempRegion.prevIndex;
                stepRegion.index = toRegion.Index;

                //���� ������ ������� �� ����� ������� �����������,
                //�� ���� ���� �� ��������� ��������� ������
                while (stepRegion.index != stepRegion.prevIndex)
                {
                    //������� ������ � ������ ����
                    close.Add(stepRegion);

                    //���� �������� ������ ����������� �������
                    pos = stepRegion.prevIndex;
                    tempRegion = pfCalc[pos];

                    //��������� ������ �� �������� � ��������
                    stepRegion.priority = tempRegion.priority;
                    stepRegion.distance = tempRegion.distance;
                    stepRegion.prevIndex = tempRegion.prevIndex;
                    stepRegion.index = pos;
                }
                //������� ��������� ������ � ������ ����
                close.Add(stepRegion);

                //���������� ������ ����
                return close;
            }
            return null;
        }
    }
}