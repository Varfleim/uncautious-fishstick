
using System;
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using SO.Map.Pathfinding;

namespace SO.Map.Region
{
    public class RegionsData : MonoBehaviour
    {
        //������ ��������
        public EcsPackedEntity[] regionPEs;

        public const float regionRadius = 25;
        public const float regionDistance = regionRadius * 2;

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

        //������ ��������
        public EcsPackedEntity[] strategicAreaPEs;

        public EcsPackedEntity GetStrategicArea(
            int sAIndex)
        {
            //���������� PE ����������� �������
            return strategicAreaPEs[sAIndex];
        }

        public EcsPackedEntity GetStrategicAreaRandom()
        {
            //���������� PE ��������� �������
            return GetStrategicArea(UnityEngine.Random.Range(0, strategicAreaPEs.Length));
        }

        #region Pathfinding
        //������ ������ ����
        public bool[] needRefreshPathMatrix = new bool[Environment.ProcessorCount];
        public DPathfindingNodeFast[][] pathfindingArray = new DPathfindingNodeFast[Environment.ProcessorCount][];
        public PathfindingQueueInt[] pathFindingQueue = new PathfindingQueueInt[Environment.ProcessorCount];
        public List<DPathfindingClosedNode>[] closedNodes = new List<DPathfindingClosedNode>[Environment.ProcessorCount];
        public byte[] openRegionValues = new byte[Environment.ProcessorCount];
        public byte[] closeRegionValues = new byte[Environment.ProcessorCount];
        public int[] pathfindingSearchLimits = new int[Environment.ProcessorCount];

        public bool needRefreshRouteMatrix;
        public DPathfindingNodeFast[] pfCalc;
        public PathfindingQueueInt open;
        public List<DPathfindingClosedNode> close = new();
        public byte openRegionValue = 1;
        public byte closeRegionValue = 2;
        public const int pathFindingSearchLimitBase = 30000;
        public int pathFindingSearchLimit;

        public List<int> GetRegionIndicesWithinSteps(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegionCore> rCPool,
            ref CRegionCore rc,
            int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = ListPool<int>.Get();

            //��� ������� ������ �������
            for (int a = 0; a < rc.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                rc.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourRC.Index);
            }

            //������ ������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ������ � �������
            processed.Add(rc.Index, true);

            //������ �������� ������
            List<int> results = ListPool<int>.Get();

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
                ref CRegionCore candidateRC = ref rCPool.Get(regionEntity);

                //���� ������� ��� �� �������� ���
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //������� ���� �� ����
                    List<int> pathRegions = PathFind(
                    world,
                    regionFilter, rCPool,
                    ref rc, ref candidateRC,
                    maxSteps);

                    //���� ���������� ���� 
                    if (pathRegions != null)
                    {
                        //������� ��������� � �������� ������ � �������
                        results.Add(candidateRC.Index);
                        processed.Add(candidateRC.Index, true);

                        //��� ������� ��������� �������
                        for (int a = 0; a < candidateRC.neighbourRegionPEs.Length; a++)
                        {
                            //���� ������
                            candidateRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);

                            //���� ������� �� �������� ���
                            if (processed.ContainsKey(neighbourRC.Index) == false)
                            {
                                //������� ��� � ������ � ����������� �������
                                candidates.Add(neighbourRC.Index);
                                candidatesLast++;
                            }
                        }

                        //���������� ������ � ���
                        ListPool<int>.Add(pathRegions);
                    }
                }
            }

            //���������� ������ � ���
            ListPool<int>.Add(candidates);

            return results;
        }

        public List<int> GetRegionIndicesWithinSteps(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegionCore> rCPool,
            ref CRegionCore rC,
            int minSteps, int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = ListPool<int>.Get();

            //��� ������� ������ �������
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                rC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourRC.Index);
            }

            //������ �������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ������ � �������
            processed.Add(rC.Index, true);

            //������ �������� ������
            List<int> results = ListPool<int>.Get();

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
                ref CRegionCore candidateRC = ref rCPool.Get(regionEntity);

                //���� ������� ��� �� �������� ���
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //������� ���� �� ����
                    List<int> pathRegions = PathFind(
                        world,
                        regionFilter, rCPool,
                        ref rC, ref candidateRC,
                        maxSteps);

                    //���� ���������� ���� 
                    if (pathRegions != null)
                    {
                        //���� ����� ���� ������ ��� ����� ����������� � ������ ��� ����� ������������
                        if (pathRegions.Count >= minSteps && pathRegions.Count <= maxSteps)
                        {
                            //������� ��������� � �������� ������
                            results.Add(candidateRC.Index);
                        }

                        //������� ��������� � ������� ������������
                        processed.Add(candidateRC.Index, true);

                        //��� ������� ��������� �������
                        for (int a = 0; a < candidateRC.neighbourRegionPEs.Length; a++)
                        {
                            //���� ������
                            candidateRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);

                            //���� ������� �� �������� ���
                            if (processed.ContainsKey(neighbourRC.Index) == false)
                            {
                                //������� ��� � ������ � ����������� �������
                                candidates.Add(neighbourRC.Index);
                                candidatesLast++;
                            }
                        }

                        //���������� ������ � ���
                        ListPool<int>.Add(pathRegions);
                    }
                }
            }

            //���������� ������ � ���
            ListPool<int>.Add(candidates);

            return results;
        }

        public List<int> GetRegionIndicesWithinStepsThreads(
            EcsWorld world,
            ref CRegionCore[] rCPool, ref int[] rCIndices,
            int threadId,
            ref CRegionCore rC,
            int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = ListPool<int>.Get(threadId);

            //��� ������� ������ �������
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                rC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourRC.Index);
            }

            //������ �������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ������ � �������
            processed.Add(rC.Index, true);

            //������ �������� ������
            List<int> results = ListPool<int>.Get(threadId);

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
                ref CRegionCore candidateRC = ref rCPool[rCIndices[regionEntity]];

                //���� ������� ��� �� �������� ���
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //������� ���� �� ����
                    List<int> pathRegions = PathFindThreads(
                        world,
                        ref rCPool, ref rCIndices,
                        threadId,
                        ref rC, ref candidateRC,
                        maxSteps);

                    //���� ���������� ���� 
                    if (pathRegions != null)
                    {
                        //������� ��������� � �������� ������ � �������
                        results.Add(candidateRC.Index);
                        processed.Add(candidateRC.Index, true);

                        //��� ������� ��������� �������
                        for (int a = 0; a < candidateRC.neighbourRegionPEs.Length; a++)
                        {
                            //���� ������
                            candidateRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                            //���� ������� �� �������� ���
                            if (processed.ContainsKey(neighbourRC.Index) == false)
                            {
                                //������� ��� � ������ � ����������� �������
                                candidates.Add(neighbourRC.Index);
                                candidatesLast++;
                            }
                        }

                        //���������� ������ � ���
                        ListPool<int>.Add(
                            threadId,
                            pathRegions);
                    }
                }
            }

            //���������� ������ � ���
            ListPool<int>.Add(
                threadId,
                candidates);

            return results;
        }

        public List<int> GetRegionIndicesWithinStepsThreads(
            EcsWorld world,
            ref CRegionCore[] rCPool, ref int[] rCIndices,
            int threadId,
            ref CRegionCore rC,
            int minSteps, int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = ListPool<int>.Get(threadId);

            //��� ������� ������ �������
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //���� ������
                rC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourRC.Index);
            }

            //������ �������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ������ � �������
            processed.Add(rC.Index, true);

            //������ �������� ������
            List<int> results = ListPool<int>.Get(threadId);

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
                ref CRegionCore candidateRC = ref rCPool[rCIndices[regionEntity]];

                //���� ������� ��� �� �������� ���
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //������� ���� �� ����
                    List<int> pathRegions = PathFindThreads(
                        world,
                        ref rCPool, ref rCIndices,
                        threadId,
                        ref rC, ref candidateRC,
                        maxSteps);

                    //���� ���������� ���� 
                    if (pathRegions != null)
                    {
                        //���� ����� ���� ������ ��� ����� ����������� � ������ ��� ����� ������������
                        if (pathRegions.Count >= minSteps && pathRegions.Count <= maxSteps)
                        {
                            //������� ��������� � �������� ������
                            results.Add(candidateRC.Index);
                        }

                        //������� ��������� � ������� ������������
                        processed.Add(candidateRC.Index, true);

                        //��� ������� ��������� �������
                        for (int a = 0; a < candidateRC.neighbourRegionPEs.Length; a++)
                        {
                            //���� ������
                            candidateRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                            //���� ������� �� �������� ���
                            if (processed.ContainsKey(neighbourRC.Index) == false)
                            {
                                //������� ��� � ������ � ����������� �������
                                candidates.Add(neighbourRC.Index);
                                candidatesLast++;
                            }
                        }

                        //���������� ������ � ���
                        ListPool<int>.Add(
                            threadId,
                            pathRegions);
                    }
                }
            }

            //���������� ������ � ���
            ListPool<int>.Add(
                threadId,
                candidates);

            return results;
        }

        void PathMatrixRefresh(
            EcsFilter regionFilter, EcsPool<CRegionCore> rCPool)
        {
            //���� ������� ���� �� ������� ����������, �� ������� �� �������
            if (needRefreshRouteMatrix == false)
            {
                return;
            }

            //��������, ��� ������� ���� �� ������� ����������
            needRefreshRouteMatrix = false;

            //��� ������� �������
            //foreach (int regionEntity in regionFilter)
            //{
            //    //���� ������
            //    ref CRegionCore rC = ref rCPool.Get(regionEntity);

            //    //������������ ��������� ������� �� �������
            //    float cost = rC.crossCost;

            //    //��������� ��������� ������� �� �������
            //    rC.crossCost = cost;
            //}

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

        public List<int> PathFind(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegionCore> rCPool,
            ref CRegionCore startRC, ref CRegionCore endRC,
            int searchLimit = 0)
        {
            //������ ������ ��� �������� �������� ����
            List<int> results = ListPool<int>.Get();

            //������� ���� � ���������� ���������� �������� � ����
            int count = PathFind(
                world,
                regionFilter, rCPool,
                ref startRC, ref endRC,
                results,
                searchLimit);

            //���� ���������� ����� ����, �� ���������� ������ ������
            return count == 0 ? null : results;
        }

        public int PathFind(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegionCore> rCPool,
            ref CRegionCore startRC, ref CRegionCore endRC,
            List<int> results,
            int searchLimit = 0)
        {
            //������� ������
            results.Clear();

            //���� ��������� ������ �� ����� ���������
            if (startRC.Index != endRC.Index)
            {
                //������������ ������� ����
                PathMatrixRefresh(regionFilter, rCPool);

                //���������� ������������ ���������� ����� ��� ������
                pathFindingSearchLimit = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //������� ����
                List<DPathfindingClosedNode> path = PathFindFast(
                    world,
                    rCPool,
                    ref startRC, ref endRC);

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
                    results.Add(endRC.Index);
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

        List<DPathfindingClosedNode> PathFindFast(
            EcsWorld world,
            EcsPool<CRegionCore> rCPool,
            ref CRegionCore startRC, ref CRegionCore toRC)
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
            ref Vector3 destinationCenter = ref toRC.center;

            //������ ���������� ��� ���������� �������
            int nextRegionIndex;

            //�������� ������ ���������� ������� � �������
            pfCalc[startRC.Index].distance = 0;
            pfCalc[startRC.Index].priority = 2;
            pfCalc[startRC.Index].prevIndex = startRC.Index;
            pfCalc[startRC.Index].status = openRegionValue;

            //������� ��������� ������ � �������
            open.Push(startRC.Index);

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
                if (currentRegionIndex == toRC.Index)
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
                ref CRegionCore currentRC = ref rCPool.Get(currentRegionEntity);

                //��� ������� ������ �������� �������
                for (int a = 0; a < currentRC.neighbourRegionPEs.Length; a++)
                {
                    //���� ������
                    currentRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                    ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);
                    nextRegionIndex = neighbourRC.Index;

                    //������������ ���������� �� ������
                    float newDistance = pfCalc[currentRegionIndex].distance + neighbourRC.crossCost;

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
                    float angle = Vector3.Angle(destinationCenter, neighbourRC.center);

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
                int pos = toRC.Index;

                //������ ��������� ��������� � ������� � �� ������ ��������� �������
                ref DPathfindingNodeFast tempRegion = ref pfCalc[toRC.Index];
                DPathfindingClosedNode stepRegion;

                //��������� ������ �� �������� � ��������
                stepRegion.priority = tempRegion.priority;
                stepRegion.distance = tempRegion.distance;
                stepRegion.prevIndex = tempRegion.prevIndex;
                stepRegion.index = toRC.Index;

                //���� ������ ������� �� ����� ������� �����������,
                //�� ���� ���� �� ��������� ��������� ������
                while (stepRegion.index != stepRegion.prevIndex)
                {
                    //������� ������ � ������ ����
                    close.Add(stepRegion);

                    //���� �������� ������ ����������� �������
                    pos = stepRegion.prevIndex;
                    tempRegion = ref pfCalc[pos];

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

        void PathMatrixRefreshThreads(
            int threadId)
        {
            //���� ������� ���� �� ������� ����������, �� ������� �� �������
            if (needRefreshPathMatrix[threadId] == false)
            {
                return;
            }

            //��������, ��� ������� ���� �� ������� ����������
            needRefreshPathMatrix[threadId] = false;

            //���� ������ ��� ������ ���� ����
            if (pathfindingArray[threadId] == null)
            {
                //����� ��������� �������� � �������� ����
                openRegionValues[threadId] = 1;
                closeRegionValues[threadId] = 2;

                //������ ������
                pathfindingArray[threadId] = new DPathfindingNodeFast[regionPEs.Length];

                //������ �������
                pathFindingQueue[threadId] = new(
                    new PathfindingNodesComparer(pathfindingArray[threadId]),
                    regionPEs.Length);

                //������ ������ ��������� ����
                closedNodes[threadId] = new();
            }
            //�����
            else
            {
                //������� ������� � ������
                pathFindingQueue[threadId].Clear();
                Array.Clear(pathfindingArray[threadId], 0, pathfindingArray[threadId].Length);

                //��������� ���������� �������� � �������
                PathfindingNodesComparer comparer = (PathfindingNodesComparer)pathFindingQueue[threadId].Comparer;
                comparer.SetMatrix(pathfindingArray[threadId]);
            }
        }

        public List<int> PathFindThreads(
            EcsWorld world,
            ref CRegionCore[] rCPool, ref int[] rCIndices,
            int threadId,
            ref CRegionCore startRC, ref CRegionCore endRC,
            int searchLimit = 0)
        {
            //���� ��������� ������ �� ����� ���������
            if (startRC.selfPE.EqualsTo(endRC.selfPE) == false)
            {
                //��������� ������� ����
                PathMatrixRefreshThreads(threadId);

                //���������� ������������ ���������� ����� ��� ������
                pathfindingSearchLimits[threadId] = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //������� ����
                List<DPathfindingClosedNode> path = PathFindFastThreads(
                    world,
                    ref rCPool, ref rCIndices,
                    threadId,
                    ref startRC, ref endRC);

                //���� ���� �� ����
                if (path != null)
                {
                    //������ ������ ��� ��������
                    List<int> results = ListPool<int>.Get(threadId);

                    //��� ������� ������� � ����
                    for (int a = 0; a < path.Count - 1; a++)
                    {
                        //������� ��� � ������
                        results.Add(path[a].index);
                    }

                    //���������� ������
                    return results;
                }
            }

            return null;
        }

        List<DPathfindingClosedNode> PathFindFastThreads(
            EcsWorld world,
            ref CRegionCore[] rCPool, ref int[] rCIndices,
            int threadId,
            ref CRegionCore startRC, ref CRegionCore endRC)
        {
            //������ ���������� ��� ������������ ������� ����
            bool found = false;

            //������ ������� �����
            int stepsCount = 0;

            //���� ���� ������ ������ 250
            if (openRegionValues[threadId] > 250)
            {
                //�������� ����
                openRegionValues[threadId] = 1;
                closeRegionValues[threadId] = 2;
            }
            //�����
            else
            {
                //��������� ����
                openRegionValues[threadId] += 2;
                closeRegionValues[threadId] += 2;
            }
            //������� ������� � ����
            pathFindingQueue[threadId].Clear();
            closedNodes[threadId].Clear();

            //���� ����� ��������� �������
            ref Vector3 destinationCenter = ref endRC.center;

            //������ ���������� ��� ���������� �������
            int nextRegionIndex;

            //�������� ������ ���������� ������� � �������
            pathfindingArray[threadId][startRC.Index].distance = 0;
            pathfindingArray[threadId][startRC.Index].priority = 2;
            pathfindingArray[threadId][startRC.Index].prevIndex = startRC.Index;
            pathfindingArray[threadId][startRC.Index].status = openRegionValues[threadId];

            //������� ��������� ������ � �������
            pathFindingQueue[threadId].Push(startRC.Index);

            //���� � ������� ���� �������
            while (pathFindingQueue[threadId].regionsCount > 0)
            {
                //���� ������ ������ � ������� ��� �������
                int currentRegionIndex = pathFindingQueue[threadId].Pop();

                //���� ������ ������ ��� ����� �� ������� ������, �� ��������� � ����������
                if (pathfindingArray[threadId][currentRegionIndex].status == closeRegionValues[threadId])
                {
                    continue;
                }

                //���� ������ ������� ����� ������� ��������� �������
                if (currentRegionIndex == endRC.Index)
                {
                    //������� ������ �� ������� ������
                    pathfindingArray[threadId][currentRegionIndex].status = closeRegionValues[threadId];

                    //��������, ��� ���� ������, � ������� �� �����
                    found = true;
                    break;
                }

                //���� ������� ����� ������ �������
                if (stepsCount >= pathfindingSearchLimits[threadId])
                {
                    return null;
                }

                //���� ������� ������
                regionPEs[currentRegionIndex].Unpack(world, out int currentRegionEntity);
                ref CRegionCore currentRC = ref rCPool[rCIndices[currentRegionEntity]];

                //��� ������� ������ �������� �������
                for (int a = 0; a < currentRC.neighbourRegionPEs.Length; a++)
                {
                    //���� ������
                    currentRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                    ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];
                    nextRegionIndex = neighbourRC.Index;

                    //������������ ���������� �� ������
                    float newDistance = pathfindingArray[threadId][currentRegionIndex].distance + neighbourRC.crossCost;

                    //���� ������ ��������� � ������� ������ ��� ��� ������� �� �������
                    if (pathfindingArray[threadId][nextRegionIndex].status == openRegionValues[threadId]
                        || pathfindingArray[threadId][nextRegionIndex].status == closeRegionValues[threadId])
                    {
                        //���� ���������� �� ������� ������ ��� ����� ������, �� ��������� � ���������� ������
                        if (pathfindingArray[threadId][nextRegionIndex].distance <= newDistance)
                        {
                            continue;
                        }
                    }
                    //����� ��������� ����������

                    //��������� ������ ����������� ������� � ����������
                    pathfindingArray[threadId][nextRegionIndex].prevIndex = currentRegionIndex;
                    pathfindingArray[threadId][nextRegionIndex].distance = newDistance;

                    //������������ ��������� ������
                    //������������ ����, ������������ � �������� ���������
                    float angle = Vector3.Angle(destinationCenter, neighbourRC.center);

                    //��������� ��������� ������� � ������
                    pathfindingArray[threadId][nextRegionIndex].priority = newDistance + 2f * angle;
                    pathfindingArray[threadId][nextRegionIndex].status = openRegionValues[threadId];

                    //������� ������ � �������
                    pathFindingQueue[threadId].Push(nextRegionIndex);
                }

                //��������� ������� �����
                stepsCount++;

                //������� ������� ������ �� ������� ������
                pathfindingArray[threadId][currentRegionIndex].status = closeRegionValues[threadId];
            }

            //���� ���� ������
            if (found == true)
            {
                //������� ������ ����
                closedNodes[threadId].Clear();

                //���� �������� ������
                int pos = endRC.Index;

                //������ ��������� ��������� � ������� � �� ������ ��������� �������
                ref DPathfindingNodeFast tempRegion = ref pathfindingArray[threadId][endRC.Index];
                DPathfindingClosedNode stepRegion;

                //��������� ������ �� �������� � ��������
                stepRegion.priority = tempRegion.priority;
                stepRegion.distance = tempRegion.distance;
                stepRegion.prevIndex = tempRegion.prevIndex;
                stepRegion.index = endRC.Index;

                //���� ������ ������� �� ����� ������� �����������,
                //�� ���� ���� �� ��������� ��������� ������
                while (stepRegion.index != stepRegion.prevIndex)
                {
                    //������� ������ � ������ ����
                    closedNodes[threadId].Add(stepRegion);

                    //���� �������� ������ ����������� �������
                    pos = stepRegion.prevIndex;
                    tempRegion = ref pathfindingArray[threadId][pos];

                    //��������� ������ �� �������� � ��������
                    stepRegion.priority = tempRegion.priority;
                    stepRegion.distance = tempRegion.distance;
                    stepRegion.prevIndex = tempRegion.prevIndex;
                    stepRegion.index = pos;
                }
                //������� ��������� ������ � ������ ����
                closedNodes[threadId].Add(stepRegion);

                //���������� ������ ����
                return closedNodes[threadId];
            }
            return null;
        }
        #endregion
    }
}