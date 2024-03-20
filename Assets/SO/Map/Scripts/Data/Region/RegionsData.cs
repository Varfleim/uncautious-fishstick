
using System;
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using SO.Map.Pathfinding;

namespace SO.Map.Region
{
    public class RegionsData : MonoBehaviour
    {
        //Данные регионов
        public EcsPackedEntity[] regionPEs;

        public const float regionRadius = 25;
        public const float regionDistance = regionRadius * 2;

        public EcsPackedEntity GetRegion(
            int regionIndex)
        {
            //Возвращаем PE запрошенного региона
            return regionPEs[regionIndex];
        }
        public EcsPackedEntity GetRegionRandom()
        {
            //Возвращаем PE случайного региона
            return GetRegion(UnityEngine.Random.Range(0, regionPEs.Length));
        }

        //Данные областей
        public EcsPackedEntity[] strategicAreaPEs;

        public EcsPackedEntity GetStrategicArea(
            int sAIndex)
        {
            //Возвращаем PE запрошенной области
            return strategicAreaPEs[sAIndex];
        }

        public EcsPackedEntity GetStrategicAreaRandom()
        {
            //Возвращаем PE случайной области
            return GetStrategicArea(UnityEngine.Random.Range(0, strategicAreaPEs.Length));
        }

        #region Pathfinding
        //Данные поиска пути
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
            //Создаём промежуточный список
            List<int> candidates = ListPool<int>.Get();

            //Для каждого соседа региона
            for (int a = 0; a < rc.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа
                rc.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourRC.Index);
            }

            //Создаём промежуточный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальный регион в словарь
            processed.Add(rc.Index, true);

            //Создаём итоговый список
            List<int> results = ListPool<int>.Get();

            //Создаём обратный счётчик для обрабатываемых регионов
            int candidatesLast = candidates.Count - 1;

            //Пока не достигнут последний регион в списке
            while (candidatesLast >= 0)
            {
                //Берём последнего кандидата
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                regionPEs[candidateIndex].Unpack(world, out int regionEntity);
                ref CRegionCore candidateRC = ref rCPool.Get(regionEntity);

                //Если словарь ещё не содержит его
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //Находим путь до него
                    List<int> pathRegions = PathFind(
                    world,
                    regionFilter, rCPool,
                    ref rc, ref candidateRC,
                    maxSteps);

                    //Если существует путь 
                    if (pathRegions != null)
                    {
                        //Заносим кандидата в итоговый список и словарь
                        results.Add(candidateRC.Index);
                        processed.Add(candidateRC.Index, true);

                        //Для каждого соседнего региона
                        for (int a = 0; a < candidateRC.neighbourRegionPEs.Length; a++)
                        {
                            //Берём соседа
                            candidateRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);

                            //Если словарь не содержит его
                            if (processed.ContainsKey(neighbourRC.Index) == false)
                            {
                                //Заносим его в список и увеличиваем счётчик
                                candidates.Add(neighbourRC.Index);
                                candidatesLast++;
                            }
                        }

                        //Возвращаем список в пул
                        ListPool<int>.Add(pathRegions);
                    }
                }
            }

            //Возвращаем список в пул
            ListPool<int>.Add(candidates);

            return results;
        }

        public List<int> GetRegionIndicesWithinSteps(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegionCore> rCPool,
            ref CRegionCore rC,
            int minSteps, int maxSteps)
        {
            //Создаём промежуточный список
            List<int> candidates = ListPool<int>.Get();

            //Для каждого соседа региона
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа
                rC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourRC.Index);
            }

            //Создаём промежуточыный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальный регион в словарь
            processed.Add(rC.Index, true);

            //Создаём итоговый список
            List<int> results = ListPool<int>.Get();

            //Создаём обратный счётчик для обрабатываемых регионов
            int candidatesLast = candidates.Count - 1;

            //Пока не достигнут последний регион в списке
            while (candidatesLast >= 0)
            {
                //Берём последнего кандидата
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                regionPEs[candidateIndex].Unpack(world, out int regionEntity);
                ref CRegionCore candidateRC = ref rCPool.Get(regionEntity);

                //Если словарь ещё не содержит его
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //Находим путь до него
                    List<int> pathRegions = PathFind(
                        world,
                        regionFilter, rCPool,
                        ref rC, ref candidateRC,
                        maxSteps);

                    //Если существует путь 
                    if (pathRegions != null)
                    {
                        //Если длина пути больше или равна минимальной и меньше или равна максимальной
                        if (pathRegions.Count >= minSteps && pathRegions.Count <= maxSteps)
                        {
                            //Заносим кандидата в итоговый список
                            results.Add(candidateRC.Index);
                        }

                        //Заносим кандидата в словарь обработанных
                        processed.Add(candidateRC.Index, true);

                        //Для каждого соседнего региона
                        for (int a = 0; a < candidateRC.neighbourRegionPEs.Length; a++)
                        {
                            //Берём соседа
                            candidateRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);

                            //Если словарь не содержит его
                            if (processed.ContainsKey(neighbourRC.Index) == false)
                            {
                                //Заносим его в список и увеличиваем счётчик
                                candidates.Add(neighbourRC.Index);
                                candidatesLast++;
                            }
                        }

                        //Возвращаем список в пул
                        ListPool<int>.Add(pathRegions);
                    }
                }
            }

            //Возвращаем список в пул
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
            //Создаём промежуточный список
            List<int> candidates = ListPool<int>.Get(threadId);

            //Для каждого соседа региона
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа
                rC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourRC.Index);
            }

            //Создаём промежуточыный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальный регион в словарь
            processed.Add(rC.Index, true);

            //Создаём итоговый список
            List<int> results = ListPool<int>.Get(threadId);

            //Создаём обратный счётчик для обрабатываемых регионов
            int candidatesLast = candidates.Count - 1;

            //Пока не достигнут последний регион в списке
            while (candidatesLast >= 0)
            {
                //Берём последнего кандидата
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                regionPEs[candidateIndex].Unpack(world, out int regionEntity);
                ref CRegionCore candidateRC = ref rCPool[rCIndices[regionEntity]];

                //Если словарь ещё не содержит его
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //Находим путь до него
                    List<int> pathRegions = PathFindThreads(
                        world,
                        ref rCPool, ref rCIndices,
                        threadId,
                        ref rC, ref candidateRC,
                        maxSteps);

                    //Если существует путь 
                    if (pathRegions != null)
                    {
                        //Заносим кандидата в итоговый список и словарь
                        results.Add(candidateRC.Index);
                        processed.Add(candidateRC.Index, true);

                        //Для каждого соседнего региона
                        for (int a = 0; a < candidateRC.neighbourRegionPEs.Length; a++)
                        {
                            //Берём соседа
                            candidateRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                            //Если словарь не содержит его
                            if (processed.ContainsKey(neighbourRC.Index) == false)
                            {
                                //Заносим его в список и увеличиваем счётчик
                                candidates.Add(neighbourRC.Index);
                                candidatesLast++;
                            }
                        }

                        //Возвращаем список в пул
                        ListPool<int>.Add(
                            threadId,
                            pathRegions);
                    }
                }
            }

            //Возвращаем список в пул
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
            //Создаём промежуточный список
            List<int> candidates = ListPool<int>.Get(threadId);

            //Для каждого соседа региона
            for (int a = 0; a < rC.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа
                rC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourRC.Index);
            }

            //Создаём промежуточыный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальный регион в словарь
            processed.Add(rC.Index, true);

            //Создаём итоговый список
            List<int> results = ListPool<int>.Get(threadId);

            //Создаём обратный счётчик для обрабатываемых регионов
            int candidatesLast = candidates.Count - 1;

            //Пока не достигнут последний регион в списке
            while (candidatesLast >= 0)
            {
                //Берём последнего кандидата
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                regionPEs[candidateIndex].Unpack(world, out int regionEntity);
                ref CRegionCore candidateRC = ref rCPool[rCIndices[regionEntity]];

                //Если словарь ещё не содержит его
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //Находим путь до него
                    List<int> pathRegions = PathFindThreads(
                        world,
                        ref rCPool, ref rCIndices,
                        threadId,
                        ref rC, ref candidateRC,
                        maxSteps);

                    //Если существует путь 
                    if (pathRegions != null)
                    {
                        //Если длина пути больше или равна минимальной и меньше или равна максимальной
                        if (pathRegions.Count >= minSteps && pathRegions.Count <= maxSteps)
                        {
                            //Заносим кандидата в итоговый список
                            results.Add(candidateRC.Index);
                        }

                        //Заносим кандидата в словарь обработанных
                        processed.Add(candidateRC.Index, true);

                        //Для каждого соседнего региона
                        for (int a = 0; a < candidateRC.neighbourRegionPEs.Length; a++)
                        {
                            //Берём соседа
                            candidateRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                            ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];

                            //Если словарь не содержит его
                            if (processed.ContainsKey(neighbourRC.Index) == false)
                            {
                                //Заносим его в список и увеличиваем счётчик
                                candidates.Add(neighbourRC.Index);
                                candidatesLast++;
                            }
                        }

                        //Возвращаем список в пул
                        ListPool<int>.Add(
                            threadId,
                            pathRegions);
                    }
                }
            }

            //Возвращаем список в пул
            ListPool<int>.Add(
                threadId,
                candidates);

            return results;
        }

        void PathMatrixRefresh(
            EcsFilter regionFilter, EcsPool<CRegionCore> rCPool)
        {
            //Если матрица пути не требует обновления, то выходим из функции
            if (needRefreshRouteMatrix == false)
            {
                return;
            }

            //Отмечаем, что матрица пути не требует обновления
            needRefreshRouteMatrix = false;

            //Для каждого региона
            //foreach (int regionEntity in regionFilter)
            //{
            //    //Берём регион
            //    ref CRegionCore rC = ref rCPool.Get(regionEntity);

            //    //Рассчитываем стоимость прохода по региону
            //    float cost = rC.crossCost;

            //    //Обновляем стоимость прохода по региону
            //    rC.crossCost = cost;
            //}

            //Если массив для поиска пуст
            if (pfCalc == null)
            {
                //Создаём массив
                pfCalc = new DPathfindingNodeFast[regionPEs.Length];

                //Создаём очередь 
                open = new(
                    new PathfindingNodesComparer(pfCalc),
                    regionPEs.Length);
            }
            //Иначе
            else
            {
                //Очищаем очередь и массив
                open.Clear();
                Array.Clear(pfCalc, 0, pfCalc.Length);

                //Обновляем сравнитель регионов в очереди
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
            //Создаём список для индексов регионов пути
            List<int> results = ListPool<int>.Get();

            //Находим путь и определяем количество регионов в пути
            int count = PathFind(
                world,
                regionFilter, rCPool,
                ref startRC, ref endRC,
                results,
                searchLimit);

            //Если количество равно нулю, то возвращаем пустой список
            return count == 0 ? null : results;
        }

        public int PathFind(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegionCore> rCPool,
            ref CRegionCore startRC, ref CRegionCore endRC,
            List<int> results,
            int searchLimit = 0)
        {
            //Очищаем список
            results.Clear();

            //Если стартовый регион не равен конечному
            if (startRC.Index != endRC.Index)
            {
                //Рассчитываем матрицу пути
                PathMatrixRefresh(regionFilter, rCPool);

                //Определяем максимальное количество шагов при поиске
                pathFindingSearchLimit = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //Находим путь
                List<DPathfindingClosedNode> path = PathFindFast(
                    world,
                    rCPool,
                    ref startRC, ref endRC);

                //Если путь не пуст
                if (path != null)
                {
                    //Берём количество регионов в пути
                    int routeCount = path.Count;

                    //Для каждого региона в пути, кроме двух последних, в обратном порядке
                    for (int r = routeCount - 2; r > 0; r--)
                    {
                        //Заносим его в список индексов
                        results.Add(path[r].index);
                    }
                    //Заносим в список индексов индекс последнего региона
                    results.Add(endRC.Index);
                }
                //Иначе возвращаем 0, обозначая, что путь пуст
                else
                {
                    return 0;
                }
            }

            //Возвращаем количество регионов в пути
            return results.Count;
        }

        List<DPathfindingClosedNode> PathFindFast(
            EcsWorld world,
            EcsPool<CRegionCore> rCPool,
            ref CRegionCore startRC, ref CRegionCore toRC)
        {
            //Создаём переменную для отслеживания наличия пути
            bool found = false;

            //Создаём счётчик шагов
            int stepsCounter = 0;

            //Если фаза поиска больше 250
            if (openRegionValue > 250)
            {
                //Обнуляем фазу
                openRegionValue = 1;
                closeRegionValue = 2;
            }
            //Иначе
            else
            {
                //Обновляем фазу
                openRegionValue += 2;
                closeRegionValue += 2;
            }
            //Очищаем очередь и путь
            open.Clear();
            close.Clear();

            //Берём конечный регион
            ref Vector3 destinationCenter = ref toRC.center;

            //Создаём переменную для следующего региона
            int nextRegionIndex;

            //Обнуляем данные стартового региона в массиве
            pfCalc[startRC.Index].distance = 0;
            pfCalc[startRC.Index].priority = 2;
            pfCalc[startRC.Index].prevIndex = startRC.Index;
            pfCalc[startRC.Index].status = openRegionValue;

            //Заносим стартовый регион в очередь
            open.Push(startRC.Index);

            //Пока в очереди есть регионы
            while (open.regionsCount > 0)
            {
                //Берём первый регион в очереди как текущий
                int currentRegionIndex = open.Pop();

                //Если данный регион уже вышел за границу поиска, то переходим с следующему
                if (pfCalc[currentRegionIndex].status == closeRegionValue)
                {
                    continue;
                }

                //Если индекс региона равен индексу конечного региона
                if (currentRegionIndex == toRC.Index)
                {
                    //Выводим регион за границу поиска
                    pfCalc[currentRegionIndex].status = closeRegionValue;

                    //Отмечаем, что путь найден, и выходим из цикла
                    found = true;
                    break;
                }

                //Если счётчик шагов больше предела
                if (stepsCounter >= pathFindingSearchLimit)
                {
                    return null;
                }

                //Берём текущий регион
                regionPEs[currentRegionIndex].Unpack(world, out int currentRegionEntity);
                ref CRegionCore currentRC = ref rCPool.Get(currentRegionEntity);

                //Для каждого соседа текущего региона
                for (int a = 0; a < currentRC.neighbourRegionPEs.Length; a++)
                {
                    //Берём соседа
                    currentRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                    ref CRegionCore neighbourRC = ref rCPool.Get(neighbourRegionEntity);
                    nextRegionIndex = neighbourRC.Index;

                    //Рассчитываем расстояние до соседа
                    float newDistance = pfCalc[currentRegionIndex].distance + neighbourRC.crossCost;

                    //Если регион находится в границе поиска или уже выведен за границу
                    if (pfCalc[nextRegionIndex].status == openRegionValue
                        || pfCalc[nextRegionIndex].status == closeRegionValue)
                    {
                        //Если расстояние до региона меньше или равно новому, то переходим к следующему соседу
                        if (pfCalc[nextRegionIndex].distance <= newDistance)
                        {
                            continue;
                        }
                    }
                    //Иначе обновляем расстояние

                    //Обновляем индекс предыдущего региона и расстояние
                    pfCalc[nextRegionIndex].prevIndex = currentRegionIndex;
                    pfCalc[nextRegionIndex].distance = newDistance;

                    //Рассчитываем приоритет поиска
                    //Рассчитываем угол, используемый в качестве эвристики
                    float angle = Vector3.Angle(destinationCenter, neighbourRC.center);

                    //Обновляем приоритет региона и статус
                    pfCalc[nextRegionIndex].priority = newDistance + 2f * angle;
                    pfCalc[nextRegionIndex].status = openRegionValue;

                    //Заносим регион в очередь
                    open.Push(nextRegionIndex);
                }

                //Обновляем счётчик шагов
                stepsCounter++;

                //Выводим текущий регион за границу поиска
                pfCalc[currentRegionIndex].status = closeRegionValue;
            }

            //Если путь найден
            if (found == true)
            {
                //Очищаем список пути
                close.Clear();

                //Берём конечный регион
                int pos = toRC.Index;

                //Создаём временную структуру и заносим в неё данные конечного региона
                ref DPathfindingNodeFast tempRegion = ref pfCalc[toRC.Index];
                DPathfindingClosedNode stepRegion;

                //Переносим данные из активных в итоговые
                stepRegion.priority = tempRegion.priority;
                stepRegion.distance = tempRegion.distance;
                stepRegion.prevIndex = tempRegion.prevIndex;
                stepRegion.index = toRC.Index;

                //Пока индекс региона не равен индексу предыдущего,
                //то есть пока не достигнут стартовый регион
                while (stepRegion.index != stepRegion.prevIndex)
                {
                    //Заносим регион в список пути
                    close.Add(stepRegion);

                    //Берём активные данные предыдущего региона
                    pos = stepRegion.prevIndex;
                    tempRegion = ref pfCalc[pos];

                    //Переносим данные из активных в итоговые
                    stepRegion.priority = tempRegion.priority;
                    stepRegion.distance = tempRegion.distance;
                    stepRegion.prevIndex = tempRegion.prevIndex;
                    stepRegion.index = pos;
                }
                //Заносим последний регион в список пути
                close.Add(stepRegion);

                //Возвращаем список пути
                return close;
            }
            return null;
        }

        void PathMatrixRefreshThreads(
            int threadId)
        {
            //Если матрица пути не требует обновления, то выходим из функции
            if (needRefreshPathMatrix[threadId] == false)
            {
                return;
            }

            //Отмечаем, что матрица пути не требует обновления
            needRefreshPathMatrix[threadId] = false;

            //Если массив для поиска пути пуст
            if (pathfindingArray[threadId] == null)
            {
                //Задаём стартовые открытую и закрытую фазы
                openRegionValues[threadId] = 1;
                closeRegionValues[threadId] = 2;

                //Создаём массив
                pathfindingArray[threadId] = new DPathfindingNodeFast[regionPEs.Length];

                //Создаём очередь
                pathFindingQueue[threadId] = new(
                    new PathfindingNodesComparer(pathfindingArray[threadId]),
                    regionPEs.Length);

                //Создаём список итогового пути
                closedNodes[threadId] = new();
            }
            //Иначе
            else
            {
                //Очищаем очередь и массив
                pathFindingQueue[threadId].Clear();
                Array.Clear(pathfindingArray[threadId], 0, pathfindingArray[threadId].Length);

                //Обновляем сравнитель регионов в очереди
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
            //Если стартовый регион не равен конечному
            if (startRC.selfPE.EqualsTo(endRC.selfPE) == false)
            {
                //Обновляем матрицу пути
                PathMatrixRefreshThreads(threadId);

                //Определяем максимальное количество шагов при поиске
                pathfindingSearchLimits[threadId] = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //Находим путь
                List<DPathfindingClosedNode> path = PathFindFastThreads(
                    world,
                    ref rCPool, ref rCIndices,
                    threadId,
                    ref startRC, ref endRC);

                //Если путь не пуст
                if (path != null)
                {
                    //Создаём список для возврата
                    List<int> results = ListPool<int>.Get(threadId);

                    //Для каждого региона в пути
                    for (int a = 0; a < path.Count - 1; a++)
                    {
                        //Заносим его в список
                        results.Add(path[a].index);
                    }

                    //Возвращаем список
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
            //Создаём переменную для отслеживания наличия пути
            bool found = false;

            //Создаём счётчик шагов
            int stepsCount = 0;

            //Если фаза поиска больше 250
            if (openRegionValues[threadId] > 250)
            {
                //Обнуляем фазу
                openRegionValues[threadId] = 1;
                closeRegionValues[threadId] = 2;
            }
            //Иначе
            else
            {
                //Обновляем фазу
                openRegionValues[threadId] += 2;
                closeRegionValues[threadId] += 2;
            }
            //Очищаем очередь и путь
            pathFindingQueue[threadId].Clear();
            closedNodes[threadId].Clear();

            //Берём центр конечного региона
            ref Vector3 destinationCenter = ref endRC.center;

            //Создаём переменную для следующего региона
            int nextRegionIndex;

            //Обнуляем данные стартового региона в массиве
            pathfindingArray[threadId][startRC.Index].distance = 0;
            pathfindingArray[threadId][startRC.Index].priority = 2;
            pathfindingArray[threadId][startRC.Index].prevIndex = startRC.Index;
            pathfindingArray[threadId][startRC.Index].status = openRegionValues[threadId];

            //Заносим стартовый регион в очередь
            pathFindingQueue[threadId].Push(startRC.Index);

            //Пока в очереди есть регионы
            while (pathFindingQueue[threadId].regionsCount > 0)
            {
                //Берём первый регион в очереди как текущий
                int currentRegionIndex = pathFindingQueue[threadId].Pop();

                //Если данный регион уже вышел за границу поиска, то переходим с следующему
                if (pathfindingArray[threadId][currentRegionIndex].status == closeRegionValues[threadId])
                {
                    continue;
                }

                //Если индекс региона равен индексу конечного региона
                if (currentRegionIndex == endRC.Index)
                {
                    //Выводим регион за границу поиска
                    pathfindingArray[threadId][currentRegionIndex].status = closeRegionValues[threadId];

                    //Отмечаем, что путь найден, и выходим из цикла
                    found = true;
                    break;
                }

                //Если счётчик шагов больше предела
                if (stepsCount >= pathfindingSearchLimits[threadId])
                {
                    return null;
                }

                //Берём текущий регион
                regionPEs[currentRegionIndex].Unpack(world, out int currentRegionEntity);
                ref CRegionCore currentRC = ref rCPool[rCIndices[currentRegionEntity]];

                //Для каждого соседа текущего региона
                for (int a = 0; a < currentRC.neighbourRegionPEs.Length; a++)
                {
                    //Берём соседа
                    currentRC.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                    ref CRegionCore neighbourRC = ref rCPool[rCIndices[neighbourRegionEntity]];
                    nextRegionIndex = neighbourRC.Index;

                    //Рассчитываем расстояние до соседа
                    float newDistance = pathfindingArray[threadId][currentRegionIndex].distance + neighbourRC.crossCost;

                    //Если регион находится в границе поиска или уже выведен за границу
                    if (pathfindingArray[threadId][nextRegionIndex].status == openRegionValues[threadId]
                        || pathfindingArray[threadId][nextRegionIndex].status == closeRegionValues[threadId])
                    {
                        //Если расстояние до региона меньше или равно новому, то переходим к следующему соседу
                        if (pathfindingArray[threadId][nextRegionIndex].distance <= newDistance)
                        {
                            continue;
                        }
                    }
                    //Иначе обновляем расстояние

                    //Обновляем индекс предыдущего региона и расстояние
                    pathfindingArray[threadId][nextRegionIndex].prevIndex = currentRegionIndex;
                    pathfindingArray[threadId][nextRegionIndex].distance = newDistance;

                    //Рассчитываем приоритет поиска
                    //Рассчитываем угол, используемый в качестве эвристики
                    float angle = Vector3.Angle(destinationCenter, neighbourRC.center);

                    //Обновляем приоритет региона и статус
                    pathfindingArray[threadId][nextRegionIndex].priority = newDistance + 2f * angle;
                    pathfindingArray[threadId][nextRegionIndex].status = openRegionValues[threadId];

                    //Заносим регион в очередь
                    pathFindingQueue[threadId].Push(nextRegionIndex);
                }

                //Обновляем счётчик шагов
                stepsCount++;

                //Выводим текущий регион за границу поиска
                pathfindingArray[threadId][currentRegionIndex].status = closeRegionValues[threadId];
            }

            //Если путь найден
            if (found == true)
            {
                //Очищаем список пути
                closedNodes[threadId].Clear();

                //Берём конечный регион
                int pos = endRC.Index;

                //Создаём временную структуру и заносим в неё данные конечного региона
                ref DPathfindingNodeFast tempRegion = ref pathfindingArray[threadId][endRC.Index];
                DPathfindingClosedNode stepRegion;

                //Переносим данные из активных в итоговые
                stepRegion.priority = tempRegion.priority;
                stepRegion.distance = tempRegion.distance;
                stepRegion.prevIndex = tempRegion.prevIndex;
                stepRegion.index = endRC.Index;

                //Пока индекс региона не равен индексу предыдущего,
                //то есть пока не достигнут стартовый регион
                while (stepRegion.index != stepRegion.prevIndex)
                {
                    //Заносим регион в список пути
                    closedNodes[threadId].Add(stepRegion);

                    //Берём активные данные предыдущего региона
                    pos = stepRegion.prevIndex;
                    tempRegion = ref pathfindingArray[threadId][pos];

                    //Переносим данные из активных в итоговые
                    stepRegion.priority = tempRegion.priority;
                    stepRegion.distance = tempRegion.distance;
                    stepRegion.prevIndex = tempRegion.prevIndex;
                    stepRegion.index = pos;
                }
                //Заносим последний регион в список пути
                closedNodes[threadId].Add(stepRegion);

                //Возвращаем список пути
                return closedNodes[threadId];
            }
            return null;
        }
        #endregion
    }
}