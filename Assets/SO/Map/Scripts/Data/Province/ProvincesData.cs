
using System;
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using SO.Map.Pathfinding;

namespace SO.Map.Province
{
    public class ProvincesData : MonoBehaviour
    {
        //������ ���������
        public EcsPackedEntity[] provincePEs;

        public const float provinceRadius = 25;
        public const float provinceDistance = provinceRadius * 2;

        public EcsPackedEntity GetProvince(
            int provinceIndex)
        {
            //���������� PE ����������� ���������
            return provincePEs[provinceIndex];
        }
        public EcsPackedEntity GetProvinceRandom()
        {
            //���������� PE ��������� ���������
            return GetProvince(UnityEngine.Random.Range(0, provincePEs.Length));
        }

        //������ ���
        public EcsPackedEntity[] mapAreaPEs;

        public EcsPackedEntity GetMapArea(
            int mAIndex)
        {
            //���������� PE ����������� ����
            return mapAreaPEs[mAIndex];
        }

        public EcsPackedEntity GetMapAreaRandom()
        {
            //���������� PE ��������� ����
            return GetMapArea(UnityEngine.Random.Range(0, mapAreaPEs.Length));
        }

        #region Pathfinding
        //������ ������ ����
        public bool[] needRefreshPathMatrix = new bool[Environment.ProcessorCount];
        public DPathfindingNodeFast[][] pathfindingArray = new DPathfindingNodeFast[Environment.ProcessorCount][];
        public PathfindingQueueInt[] pathFindingQueue = new PathfindingQueueInt[Environment.ProcessorCount];
        public List<DPathfindingClosedNode>[] closedNodes = new List<DPathfindingClosedNode>[Environment.ProcessorCount];
        public byte[] openProvinceValues = new byte[Environment.ProcessorCount];
        public byte[] closeProvinceValues = new byte[Environment.ProcessorCount];
        public int[] pathfindingSearchLimits = new int[Environment.ProcessorCount];

        public bool needRefreshRouteMatrix;
        public DPathfindingNodeFast[] pfCalc;
        public PathfindingQueueInt open;
        public List<DPathfindingClosedNode> close = new();
        public byte openProvinceValue = 1;
        public byte closeProvinceValue = 2;
        public const int pathFindingSearchLimitBase = 30000;
        public int pathFindingSearchLimit;

        public List<int> GetProvinceIndicesWithinSteps(
            EcsWorld world,
            EcsFilter provinceFilter, EcsPool<CProvinceCore> pCPool,
            ref CProvinceCore pC,
            int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = ListPool<int>.Get();

            //��� ������� ������ ���������
            for (int a = 0; a < pC.neighbourProvincePEs.Length; a++)
            {
                //���� ������
                pC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourPC.Index);
            }

            //������ ������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ��������� � �������
            processed.Add(pC.Index, true);

            //������ �������� ������
            List<int> results = ListPool<int>.Get();

            //������ �������� ������� ��� �������������� ���������
            int candidatesLast = candidates.Count - 1;

            //���� �� ���������� ��������� ��������� � ������
            while (candidatesLast >= 0)
            {
                //���� ���������� ���������
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                provincePEs[candidateIndex].Unpack(world, out int provinceEntity);
                ref CProvinceCore candidatePC = ref pCPool.Get(provinceEntity);

                //���� ������� ��� �� �������� ���
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //������� ���� �� ����
                    List<int> pathProvinces = PathFind(
                    world,
                    provinceFilter, pCPool,
                    ref pC, ref candidatePC,
                    maxSteps);

                    //���� ���������� ���� 
                    if (pathProvinces != null)
                    {
                        //������� ��������� � �������� ������ � �������
                        results.Add(candidatePC.Index);
                        processed.Add(candidatePC.Index, true);

                        //��� ������ �������� ���������
                        for (int a = 0; a < candidatePC.neighbourProvincePEs.Length; a++)
                        {
                            //���� ������
                            candidatePC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                            ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);

                            //���� ������� �� �������� ���
                            if (processed.ContainsKey(neighbourPC.Index) == false)
                            {
                                //������� ��� � ������ � ����������� �������
                                candidates.Add(neighbourPC.Index);
                                candidatesLast++;
                            }
                        }

                        //���������� ������ � ���
                        ListPool<int>.Add(pathProvinces);
                    }
                }
            }

            //���������� ������ � ���
            ListPool<int>.Add(candidates);

            return results;
        }

        public List<int> GetProvinceIndicesWithinSteps(
            EcsWorld world,
            EcsFilter provinceFilter, EcsPool<CProvinceCore> pCPool,
            ref CProvinceCore pC,
            int minSteps, int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = ListPool<int>.Get();

            //��� ������� ������ ���������
            for (int a = 0; a < pC.neighbourProvincePEs.Length; a++)
            {
                //���� ������
                pC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourPC.Index);
            }

            //������ �������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ��������� � �������
            processed.Add(pC.Index, true);

            //������ �������� ������
            List<int> results = ListPool<int>.Get();

            //������ �������� ������� ��� �������������� ���������
            int candidatesLast = candidates.Count - 1;

            //���� �� ���������� ��������� ��������� � ������
            while (candidatesLast >= 0)
            {
                //���� ���������� ���������
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                provincePEs[candidateIndex].Unpack(world, out int provinceEntity);
                ref CProvinceCore candidatePC = ref pCPool.Get(provinceEntity);

                //���� ������� ��� �� �������� ���
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //������� ���� �� ����
                    List<int> pathProvinces = PathFind(
                        world,
                        provinceFilter, pCPool,
                        ref pC, ref candidatePC,
                        maxSteps);

                    //���� ���������� ���� 
                    if (pathProvinces != null)
                    {
                        //���� ����� ���� ������ ��� ����� ����������� � ������ ��� ����� ������������
                        if (pathProvinces.Count >= minSteps && pathProvinces.Count <= maxSteps)
                        {
                            //������� ��������� � �������� ������
                            results.Add(candidatePC.Index);
                        }

                        //������� ��������� � ������� ������������
                        processed.Add(candidatePC.Index, true);

                        //��� ������ �������� ���������
                        for (int a = 0; a < candidatePC.neighbourProvincePEs.Length; a++)
                        {
                            //���� ������
                            candidatePC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                            ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);

                            //���� ������� �� �������� ���
                            if (processed.ContainsKey(neighbourPC.Index) == false)
                            {
                                //������� ��� � ������ � ����������� �������
                                candidates.Add(neighbourPC.Index);
                                candidatesLast++;
                            }
                        }

                        //���������� ������ � ���
                        ListPool<int>.Add(pathProvinces);
                    }
                }
            }

            //���������� ������ � ���
            ListPool<int>.Add(candidates);

            return results;
        }

        public List<int> GetProvinceIndicesWithinStepsThreads(
            EcsWorld world,
            ref CProvinceCore[] pCPool, ref int[] pCIndices,
            int threadId,
            ref CProvinceCore pC,
            int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = ListPool<int>.Get(threadId);

            //��� ������� ������ ���������
            for (int a = 0; a < pC.neighbourProvincePEs.Length; a++)
            {
                //���� ������
                pC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourPC.Index);
            }

            //������ �������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ��������� � �������
            processed.Add(pC.Index, true);

            //������ �������� ������
            List<int> results = ListPool<int>.Get(threadId);

            //������ �������� ������� ��� �������������� ���������
            int candidatesLast = candidates.Count - 1;

            //���� �� ���������� ��������� ��������� � ������
            while (candidatesLast >= 0)
            {
                //���� ���������� ���������
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                provincePEs[candidateIndex].Unpack(world, out int provinceEntity);
                ref CProvinceCore candidatePC = ref pCPool[pCIndices[provinceEntity]];

                //���� ������� ��� �� �������� ���
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //������� ���� �� ����
                    List<int> pathProvinces = PathFindThreads(
                        world,
                        ref pCPool, ref pCIndices,
                        threadId,
                        ref pC, ref candidatePC,
                        maxSteps);

                    //���� ���������� ���� 
                    if (pathProvinces != null)
                    {
                        //������� ��������� � �������� ������ � �������
                        results.Add(candidatePC.Index);
                        processed.Add(candidatePC.Index, true);

                        //��� ������ �������� ���������
                        for (int a = 0; a < candidatePC.neighbourProvincePEs.Length; a++)
                        {
                            //���� ������
                            candidatePC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                            ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];

                            //���� ������� �� �������� ���
                            if (processed.ContainsKey(neighbourPC.Index) == false)
                            {
                                //������� ��� � ������ � ����������� �������
                                candidates.Add(neighbourPC.Index);
                                candidatesLast++;
                            }
                        }

                        //���������� ������ � ���
                        ListPool<int>.Add(
                            threadId,
                            pathProvinces);
                    }
                }
            }

            //���������� ������ � ���
            ListPool<int>.Add(
                threadId,
                candidates);

            return results;
        }

        public List<int> GetProvinceIndicesWithinStepsThreads(
            EcsWorld world,
            ref CProvinceCore[] pCPool, ref int[] pCIndices,
            int threadId,
            ref CProvinceCore pC,
            int minSteps, int maxSteps)
        {
            //������ ������������� ������
            List<int> candidates = ListPool<int>.Get(threadId);

            //��� ������� ������ ���������
            for (int a = 0; a < pC.neighbourProvincePEs.Length; a++)
            {
                //���� ������
                pC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];

                //������� ������ ������ � ������ ����������
                candidates.Add(neighbourPC.Index);
            }

            //������ �������������� �������
            Dictionary<int, bool> processed = new();

            //������� ����������� ��������� � �������
            processed.Add(pC.Index, true);

            //������ �������� ������
            List<int> results = ListPool<int>.Get(threadId);

            //������ �������� ������� ��� �������������� ���������
            int candidatesLast = candidates.Count - 1;

            //���� �� ���������� ��������� ��������� � ������
            while (candidatesLast >= 0)
            {
                //���� ���������� ���������
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                provincePEs[candidateIndex].Unpack(world, out int provinceEntity);
                ref CProvinceCore candidatePC = ref pCPool[pCIndices[provinceEntity]];

                //���� ������� ��� �� �������� ���
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //������� ���� �� ����
                    List<int> pathProvinces = PathFindThreads(
                        world,
                        ref pCPool, ref pCIndices,
                        threadId,
                        ref pC, ref candidatePC,
                        maxSteps);

                    //���� ���������� ���� 
                    if (pathProvinces != null)
                    {
                        //���� ����� ���� ������ ��� ����� ����������� � ������ ��� ����� ������������
                        if (pathProvinces.Count >= minSteps && pathProvinces.Count <= maxSteps)
                        {
                            //������� ��������� � �������� ������
                            results.Add(candidatePC.Index);
                        }

                        //������� ��������� � ������� ������������
                        processed.Add(candidatePC.Index, true);

                        //��� ������ �������� ���������
                        for (int a = 0; a < candidatePC.neighbourProvincePEs.Length; a++)
                        {
                            //���� ������
                            candidatePC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                            ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];

                            //���� ������� �� �������� ���
                            if (processed.ContainsKey(neighbourPC.Index) == false)
                            {
                                //������� ��� � ������ � ����������� �������
                                candidates.Add(neighbourPC.Index);
                                candidatesLast++;
                            }
                        }

                        //���������� ������ � ���
                        ListPool<int>.Add(
                            threadId,
                            pathProvinces);
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
            EcsFilter provinceFilter, EcsPool<CProvinceCore> pCPool)
        {
            //���� ������� ���� �� ������� ����������, �� ������� �� �������
            if (needRefreshRouteMatrix == false)
            {
                return;
            }

            //��������, ��� ������� ���� �� ������� ����������
            needRefreshRouteMatrix = false;

            //��� ������ ���������
            //foreach (int provinceEntity in provinceFilter)
            //{
            //    //���� ���������
            //    ref CProvinceCore pC = ref pCPool.Get(provinceEntity);

            //    //������������ ��������� ������� �� ���������
            //    float cost = pC.crossCost;

            //    //��������� ��������� ������� �� ���������
            //    pC.crossCost = cost;
            //}

            //���� ������ ��� ������ ����
            if (pfCalc == null)
            {
                //������ ������
                pfCalc = new DPathfindingNodeFast[provincePEs.Length];

                //������ ������� 
                open = new(
                    new PathfindingNodesComparer(pfCalc),
                    provincePEs.Length);
            }
            //�����
            else
            {
                //������� ������� � ������
                open.Clear();
                Array.Clear(pfCalc, 0, pfCalc.Length);

                //��������� ���������� ��������� � �������
                PathfindingNodesComparer comparer = (PathfindingNodesComparer)open.Comparer;
                comparer.SetMatrix(pfCalc);
            }
        }

        public List<int> PathFind(
            EcsWorld world,
            EcsFilter provinceFilter, EcsPool<CProvinceCore> pCPool,
            ref CProvinceCore startPC, ref CProvinceCore endPC,
            int searchLimit = 0)
        {
            //������ ������ ��� �������� ��������� ����
            List<int> results = ListPool<int>.Get();

            //������� ���� � ���������� ���������� ��������� � ����
            int count = PathFind(
                world,
                provinceFilter, pCPool,
                ref startPC, ref endPC,
                results,
                searchLimit);

            //���� ���������� ����� ����, �� ���������� ������ ������
            return count == 0 ? null : results;
        }

        public int PathFind(
            EcsWorld world,
            EcsFilter provinceFilter, EcsPool<CProvinceCore> pCPool,
            ref CProvinceCore startPC, ref CProvinceCore endPC,
            List<int> results,
            int searchLimit = 0)
        {
            //������� ������
            results.Clear();

            //���� ��������� ��������� �� ����� ���������
            if (startPC.Index != endPC.Index)
            {
                //������������ ������� ����
                PathMatrixRefresh(provinceFilter, pCPool);

                //���������� ������������ ���������� ����� ��� ������
                pathFindingSearchLimit = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //������� ����
                List<DPathfindingClosedNode> path = PathFindFast(
                    world,
                    pCPool,
                    ref startPC, ref endPC);

                //���� ���� �� ����
                if (path != null)
                {
                    //���� ���������� ��������� � ����
                    int routeCount = path.Count;

                    //��� ������ ��������� � ����, ����� ���� ���������, � �������� �������
                    for (int r = routeCount - 2; r > 0; r--)
                    {
                        //������� ��� � ������ ��������
                        results.Add(path[r].index);
                    }
                    //������� � ������ �������� ������ ��������� ���������
                    results.Add(endPC.Index);
                }
                //����� ���������� 0, ���������, ��� ���� ����
                else
                {
                    return 0;
                }
            }

            //���������� ���������� ��������� � ����
            return results.Count;
        }

        List<DPathfindingClosedNode> PathFindFast(
            EcsWorld world,
            EcsPool<CProvinceCore> pCPool,
            ref CProvinceCore startPC, ref CProvinceCore toPC)
        {
            //������ ���������� ��� ������������ ������� ����
            bool found = false;

            //������ ������� �����
            int stepsCounter = 0;

            //���� ���� ������ ������ 250
            if (openProvinceValue > 250)
            {
                //�������� ����
                openProvinceValue = 1;
                closeProvinceValue = 2;
            }
            //�����
            else
            {
                //��������� ����
                openProvinceValue += 2;
                closeProvinceValue += 2;
            }
            //������� ������� � ����
            open.Clear();
            close.Clear();

            //���� �������� ���������
            ref Vector3 destinationCenter = ref toPC.center;

            //������ ���������� ��� ��������� ���������
            int nextProvinceIndex;

            //�������� ������ ��������� ��������� � �������
            pfCalc[startPC.Index].distance = 0;
            pfCalc[startPC.Index].priority = 2;
            pfCalc[startPC.Index].prevIndex = startPC.Index;
            pfCalc[startPC.Index].status = openProvinceValue;

            //������� ��������� ��������� � �������
            open.Push(startPC.Index);

            //���� � ������� ���� ���������
            while (open.provincesCount > 0)
            {
                //���� ������ ��������� � ������� ��� �������
                int currentProvinceIndex = open.Pop();

                //���� ������ ��������� ��� ����� �� ������� ������, �� ��������� � ����������
                if (pfCalc[currentProvinceIndex].status == closeProvinceValue)
                {
                    continue;
                }

                //���� ������ ��������� ����� ������� �������� ���������
                if (currentProvinceIndex == toPC.Index)
                {
                    //������� ��������� �� ������� ������
                    pfCalc[currentProvinceIndex].status = closeProvinceValue;

                    //��������, ��� ���� ������, � ������� �� �����
                    found = true;
                    break;
                }

                //���� ������� ����� ������ �������
                if (stepsCounter >= pathFindingSearchLimit)
                {
                    return null;
                }

                //���� ������� ���������
                provincePEs[currentProvinceIndex].Unpack(world, out int currentProvinceEntity);
                ref CProvinceCore currentPC = ref pCPool.Get(currentProvinceEntity);

                //��� ������� ������ ������� ���������
                for (int a = 0; a < currentPC.neighbourProvincePEs.Length; a++)
                {
                    //���� ������
                    currentPC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                    ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);
                    nextProvinceIndex = neighbourPC.Index;

                    //������������ ���������� �� ������
                    float newDistance = pfCalc[currentProvinceIndex].distance + neighbourPC.crossCost;

                    //���� ��������� ��������� � ������� ������ ��� ��� ������� �� �������
                    if (pfCalc[nextProvinceIndex].status == openProvinceValue
                        || pfCalc[nextProvinceIndex].status == closeProvinceValue)
                    {
                        //���� ���������� �� ��������� ������ ��� ����� ������, �� ��������� � ���������� ������
                        if (pfCalc[nextProvinceIndex].distance <= newDistance)
                        {
                            continue;
                        }
                    }
                    //����� ��������� ����������

                    //��������� ������ ���������� ��������� � ����������
                    pfCalc[nextProvinceIndex].prevIndex = currentProvinceIndex;
                    pfCalc[nextProvinceIndex].distance = newDistance;

                    //������������ ��������� ������
                    //������������ ����, ������������ � �������� ���������
                    float angle = Vector3.Angle(destinationCenter, neighbourPC.center);

                    //��������� ��������� ��������� � ������
                    pfCalc[nextProvinceIndex].priority = newDistance + 2f * angle;
                    pfCalc[nextProvinceIndex].status = openProvinceValue;

                    //������� ��������� � �������
                    open.Push(nextProvinceIndex);
                }

                //��������� ������� �����
                stepsCounter++;

                //������� ������� ��������� �� ������� ������
                pfCalc[currentProvinceIndex].status = closeProvinceValue;
            }

            //���� ���� ������
            if (found == true)
            {
                //������� ������ ����
                close.Clear();

                //���� �������� ���������
                int pos = toPC.Index;

                //������ ��������� ��������� � ������� � �� ������ �������� ���������
                ref DPathfindingNodeFast tempProvince = ref pfCalc[toPC.Index];
                DPathfindingClosedNode stepProvince;

                //��������� ������ �� �������� � ��������
                stepProvince.priority = tempProvince.priority;
                stepProvince.distance = tempProvince.distance;
                stepProvince.prevIndex = tempProvince.prevIndex;
                stepProvince.index = toPC.Index;

                //���� ������ ��������� �� ����� ������� �����������,
                //�� ���� ���� �� ���������� ��������� ���������
                while (stepProvince.index != stepProvince.prevIndex)
                {
                    //������� ��������� � ������ ����
                    close.Add(stepProvince);

                    //���� �������� ������ ���������� ���������
                    pos = stepProvince.prevIndex;
                    tempProvince = ref pfCalc[pos];

                    //��������� ������ �� �������� � ��������
                    stepProvince.priority = tempProvince.priority;
                    stepProvince.distance = tempProvince.distance;
                    stepProvince.prevIndex = tempProvince.prevIndex;
                    stepProvince.index = pos;
                }
                //������� ��������� ��������� � ������ ����
                close.Add(stepProvince);

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
                openProvinceValues[threadId] = 1;
                closeProvinceValues[threadId] = 2;

                //������ ������
                pathfindingArray[threadId] = new DPathfindingNodeFast[provincePEs.Length];

                //������ �������
                pathFindingQueue[threadId] = new(
                    new PathfindingNodesComparer(pathfindingArray[threadId]),
                    provincePEs.Length);

                //������ ������ ��������� ����
                closedNodes[threadId] = new();
            }
            //�����
            else
            {
                //������� ������� � ������
                pathFindingQueue[threadId].Clear();
                Array.Clear(pathfindingArray[threadId], 0, pathfindingArray[threadId].Length);

                //��������� ���������� ��������� � �������
                PathfindingNodesComparer comparer = (PathfindingNodesComparer)pathFindingQueue[threadId].Comparer;
                comparer.SetMatrix(pathfindingArray[threadId]);
            }
        }

        public List<int> PathFindThreads(
            EcsWorld world,
            ref CProvinceCore[] pCPool, ref int[] pCIndices,
            int threadId,
            ref CProvinceCore startPC, ref CProvinceCore endPC,
            int searchLimit = 0)
        {
            //���� ��������� ��������� �� ����� ���������
            if (startPC.selfPE.EqualsTo(endPC.selfPE) == false)
            {
                //��������� ������� ����
                PathMatrixRefreshThreads(threadId);

                //���������� ������������ ���������� ����� ��� ������
                pathfindingSearchLimits[threadId] = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //������� ����
                List<DPathfindingClosedNode> path = PathFindFastThreads(
                    world,
                    ref pCPool, ref pCIndices,
                    threadId,
                    ref startPC, ref endPC);

                //���� ���� �� ����
                if (path != null)
                {
                    //������ ������ ��� ��������
                    List<int> results = ListPool<int>.Get(threadId);

                    //��� ������ ��������� � ����
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
            ref CProvinceCore[] pCPool, ref int[] pCIndices,
            int threadId,
            ref CProvinceCore startPC, ref CProvinceCore endPC)
        {
            //������ ���������� ��� ������������ ������� ����
            bool found = false;

            //������ ������� �����
            int stepsCount = 0;

            //���� ���� ������ ������ 250
            if (openProvinceValues[threadId] > 250)
            {
                //�������� ����
                openProvinceValues[threadId] = 1;
                closeProvinceValues[threadId] = 2;
            }
            //�����
            else
            {
                //��������� ����
                openProvinceValues[threadId] += 2;
                closeProvinceValues[threadId] += 2;
            }
            //������� ������� � ����
            pathFindingQueue[threadId].Clear();
            closedNodes[threadId].Clear();

            //���� ����� �������� ���������
            ref Vector3 destinationCenter = ref endPC.center;

            //������ ���������� ��� ��������� ���������
            int nextProvinceIndex;

            //�������� ������ ��������� ��������� � �������
            pathfindingArray[threadId][startPC.Index].distance = 0;
            pathfindingArray[threadId][startPC.Index].priority = 2;
            pathfindingArray[threadId][startPC.Index].prevIndex = startPC.Index;
            pathfindingArray[threadId][startPC.Index].status = openProvinceValues[threadId];

            //������� ��������� ��������� � �������
            pathFindingQueue[threadId].Push(startPC.Index);

            //���� � ������� ���� ���������
            while (pathFindingQueue[threadId].provincesCount > 0)
            {
                //���� ������ ��������� � ������� ��� �������
                int currentProvinceIndex = pathFindingQueue[threadId].Pop();

                //���� ������ ��������� ��� ����� �� ������� ������, �� ��������� � ���������
                if (pathfindingArray[threadId][currentProvinceIndex].status == closeProvinceValues[threadId])
                {
                    continue;
                }

                //���� ������ ��������� ����� ������� �������� ���������
                if (currentProvinceIndex == endPC.Index)
                {
                    //������� ��������� �� ������� ������
                    pathfindingArray[threadId][currentProvinceIndex].status = closeProvinceValues[threadId];

                    //��������, ��� ���� ������, � ������� �� �����
                    found = true;
                    break;
                }

                //���� ������� ����� ������ �������
                if (stepsCount >= pathfindingSearchLimits[threadId])
                {
                    return null;
                }

                //���� ������� ���������
                provincePEs[currentProvinceIndex].Unpack(world, out int currentProvinceEntity);
                ref CProvinceCore currentPC = ref pCPool[pCIndices[currentProvinceEntity]];

                //��� ������� ������ ������� ���������
                for (int a = 0; a < currentPC.neighbourProvincePEs.Length; a++)
                {
                    //���� ������
                    currentPC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                    ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];
                    nextProvinceIndex = neighbourPC.Index;

                    //������������ ���������� �� ������
                    float newDistance = pathfindingArray[threadId][currentProvinceIndex].distance + neighbourPC.crossCost;

                    //���� ��������� ��������� � ������� ������ ��� ��� ������� �� �������
                    if (pathfindingArray[threadId][nextProvinceIndex].status == openProvinceValues[threadId]
                        || pathfindingArray[threadId][nextProvinceIndex].status == closeProvinceValues[threadId])
                    {
                        //���� ���������� �� ��������� ������ ��� ����� ������, �� ��������� � ���������� ������
                        if (pathfindingArray[threadId][nextProvinceIndex].distance <= newDistance)
                        {
                            continue;
                        }
                    }
                    //����� ��������� ����������

                    //��������� ������ ���������� ��������� � ����������
                    pathfindingArray[threadId][nextProvinceIndex].prevIndex = currentProvinceIndex;
                    pathfindingArray[threadId][nextProvinceIndex].distance = newDistance;

                    //������������ ��������� ������
                    //������������ ����, ������������ � �������� ���������
                    float angle = Vector3.Angle(destinationCenter, neighbourPC.center);

                    //��������� ��������� ��������� � ������
                    pathfindingArray[threadId][nextProvinceIndex].priority = newDistance + 2f * angle;
                    pathfindingArray[threadId][nextProvinceIndex].status = openProvinceValues[threadId];

                    //������� ��������� � �������
                    pathFindingQueue[threadId].Push(nextProvinceIndex);
                }

                //��������� ������� �����
                stepsCount++;

                //������� ������� ��������� �� ������� ������
                pathfindingArray[threadId][currentProvinceIndex].status = closeProvinceValues[threadId];
            }

            //���� ���� ������
            if (found == true)
            {
                //������� ������ ����
                closedNodes[threadId].Clear();

                //���� �������� ���������
                int pos = endPC.Index;

                //������ ��������� ��������� � ������� � �� ������ �������� ���������
                ref DPathfindingNodeFast tempProvince = ref pathfindingArray[threadId][endPC.Index];
                DPathfindingClosedNode stepProvince;

                //��������� ������ �� �������� � ��������
                stepProvince.priority = tempProvince.priority;
                stepProvince.distance = tempProvince.distance;
                stepProvince.prevIndex = tempProvince.prevIndex;
                stepProvince.index = endPC.Index;

                //���� ������ ��������� �� ����� ������� ����������,
                //�� ���� ���� �� ���������� ��������� ���������
                while (stepProvince.index != stepProvince.prevIndex)
                {
                    //������� ��������� � ������ ����
                    closedNodes[threadId].Add(stepProvince);

                    //���� �������� ������ ���������� ���������
                    pos = stepProvince.prevIndex;
                    tempProvince = ref pathfindingArray[threadId][pos];

                    //��������� ������ �� �������� � ��������
                    stepProvince.priority = tempProvince.priority;
                    stepProvince.distance = tempProvince.distance;
                    stepProvince.prevIndex = tempProvince.prevIndex;
                    stepProvince.index = pos;
                }
                //������� ��������� ��������� � ������ ����
                closedNodes[threadId].Add(stepProvince);

                //���������� ������ ����
                return closedNodes[threadId];
            }
            return null;
        }
        #endregion
    }
}