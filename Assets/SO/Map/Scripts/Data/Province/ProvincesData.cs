
using System;
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using SO.Map.Pathfinding;

namespace SO.Map.Province
{
    public class ProvincesData : MonoBehaviour
    {
        //Данные провинций
        public EcsPackedEntity[] provincePEs;

        public const float provinceRadius = 25;
        public const float provinceDistance = provinceRadius * 2;

        public EcsPackedEntity GetProvince(
            int provinceIndex)
        {
            //Возвращаем PE запрошенной провинции
            return provincePEs[provinceIndex];
        }
        public EcsPackedEntity GetProvinceRandom()
        {
            //Возвращаем PE случайной провинции
            return GetProvince(UnityEngine.Random.Range(0, provincePEs.Length));
        }

        //Данные зон
        public EcsPackedEntity[] mapAreaPEs;

        public EcsPackedEntity GetMapArea(
            int mAIndex)
        {
            //Возвращаем PE запрошенной зоны
            return mapAreaPEs[mAIndex];
        }

        public EcsPackedEntity GetMapAreaRandom()
        {
            //Возвращаем PE случайной зоны
            return GetMapArea(UnityEngine.Random.Range(0, mapAreaPEs.Length));
        }

        #region Pathfinding
        //Данные поиска пути
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
            //Создаём промежуточный список
            List<int> candidates = ListPool<int>.Get();

            //Для каждого соседа провинции
            for (int a = 0; a < pC.neighbourProvincePEs.Length; a++)
            {
                //Берём соседа
                pC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourPC.Index);
            }

            //Создаём промежуточный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальную провинцию в словарь
            processed.Add(pC.Index, true);

            //Создаём итоговый список
            List<int> results = ListPool<int>.Get();

            //Создаём обратный счётчик для обрабатываемых провинций
            int candidatesLast = candidates.Count - 1;

            //Пока не достигнута последняя провинция в списке
            while (candidatesLast >= 0)
            {
                //Берём последнего кандидата
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                provincePEs[candidateIndex].Unpack(world, out int provinceEntity);
                ref CProvinceCore candidatePC = ref pCPool.Get(provinceEntity);

                //Если словарь ещё не содержит его
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //Находим путь до него
                    List<int> pathProvinces = PathFind(
                    world,
                    provinceFilter, pCPool,
                    ref pC, ref candidatePC,
                    maxSteps);

                    //Если существует путь 
                    if (pathProvinces != null)
                    {
                        //Заносим кандидата в итоговый список и словарь
                        results.Add(candidatePC.Index);
                        processed.Add(candidatePC.Index, true);

                        //Для каждой соседней провинции
                        for (int a = 0; a < candidatePC.neighbourProvincePEs.Length; a++)
                        {
                            //Берём соседа
                            candidatePC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                            ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);

                            //Если словарь не содержит его
                            if (processed.ContainsKey(neighbourPC.Index) == false)
                            {
                                //Заносим его в список и увеличиваем счётчик
                                candidates.Add(neighbourPC.Index);
                                candidatesLast++;
                            }
                        }

                        //Возвращаем список в пул
                        ListPool<int>.Add(pathProvinces);
                    }
                }
            }

            //Возвращаем список в пул
            ListPool<int>.Add(candidates);

            return results;
        }

        public List<int> GetProvinceIndicesWithinSteps(
            EcsWorld world,
            EcsFilter provinceFilter, EcsPool<CProvinceCore> pCPool,
            ref CProvinceCore pC,
            int minSteps, int maxSteps)
        {
            //Создаём промежуточный список
            List<int> candidates = ListPool<int>.Get();

            //Для каждого соседа провинции
            for (int a = 0; a < pC.neighbourProvincePEs.Length; a++)
            {
                //Берём соседа
                pC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourPC.Index);
            }

            //Создаём промежуточыный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальную провинцию в словарь
            processed.Add(pC.Index, true);

            //Создаём итоговый список
            List<int> results = ListPool<int>.Get();

            //Создаём обратный счётчик для обрабатываемых провинций
            int candidatesLast = candidates.Count - 1;

            //Пока не достигнута последняя провинция в списке
            while (candidatesLast >= 0)
            {
                //Берём последнего кандидата
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                provincePEs[candidateIndex].Unpack(world, out int provinceEntity);
                ref CProvinceCore candidatePC = ref pCPool.Get(provinceEntity);

                //Если словарь ещё не содержит его
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //Находим путь до него
                    List<int> pathProvinces = PathFind(
                        world,
                        provinceFilter, pCPool,
                        ref pC, ref candidatePC,
                        maxSteps);

                    //Если существует путь 
                    if (pathProvinces != null)
                    {
                        //Если длина пути больше или равна минимальной и меньше или равна максимальной
                        if (pathProvinces.Count >= minSteps && pathProvinces.Count <= maxSteps)
                        {
                            //Заносим кандидата в итоговый список
                            results.Add(candidatePC.Index);
                        }

                        //Заносим кандидата в словарь обработанных
                        processed.Add(candidatePC.Index, true);

                        //Для каждой соседней провинции
                        for (int a = 0; a < candidatePC.neighbourProvincePEs.Length; a++)
                        {
                            //Берём соседа
                            candidatePC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                            ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);

                            //Если словарь не содержит его
                            if (processed.ContainsKey(neighbourPC.Index) == false)
                            {
                                //Заносим его в список и увеличиваем счётчик
                                candidates.Add(neighbourPC.Index);
                                candidatesLast++;
                            }
                        }

                        //Возвращаем список в пул
                        ListPool<int>.Add(pathProvinces);
                    }
                }
            }

            //Возвращаем список в пул
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
            //Создаём промежуточный список
            List<int> candidates = ListPool<int>.Get(threadId);

            //Для каждого соседа провинции
            for (int a = 0; a < pC.neighbourProvincePEs.Length; a++)
            {
                //Берём соседа
                pC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourPC.Index);
            }

            //Создаём промежуточыный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальную провинцию в словарь
            processed.Add(pC.Index, true);

            //Создаём итоговый список
            List<int> results = ListPool<int>.Get(threadId);

            //Создаём обратный счётчик для обрабатываемых провинций
            int candidatesLast = candidates.Count - 1;

            //Пока не достигнута последняя провинция в списке
            while (candidatesLast >= 0)
            {
                //Берём последнего кандидата
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                provincePEs[candidateIndex].Unpack(world, out int provinceEntity);
                ref CProvinceCore candidatePC = ref pCPool[pCIndices[provinceEntity]];

                //Если словарь ещё не содержит его
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //Находим путь до него
                    List<int> pathProvinces = PathFindThreads(
                        world,
                        ref pCPool, ref pCIndices,
                        threadId,
                        ref pC, ref candidatePC,
                        maxSteps);

                    //Если существует путь 
                    if (pathProvinces != null)
                    {
                        //Заносим кандидата в итоговый список и словарь
                        results.Add(candidatePC.Index);
                        processed.Add(candidatePC.Index, true);

                        //Для каждой соседней провинции
                        for (int a = 0; a < candidatePC.neighbourProvincePEs.Length; a++)
                        {
                            //Берём соседа
                            candidatePC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                            ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];

                            //Если словарь не содержит его
                            if (processed.ContainsKey(neighbourPC.Index) == false)
                            {
                                //Заносим его в список и увеличиваем счётчик
                                candidates.Add(neighbourPC.Index);
                                candidatesLast++;
                            }
                        }

                        //Возвращаем список в пул
                        ListPool<int>.Add(
                            threadId,
                            pathProvinces);
                    }
                }
            }

            //Возвращаем список в пул
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
            //Создаём промежуточный список
            List<int> candidates = ListPool<int>.Get(threadId);

            //Для каждого соседа провинции
            for (int a = 0; a < pC.neighbourProvincePEs.Length; a++)
            {
                //Берём соседа
                pC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourPC.Index);
            }

            //Создаём промежуточыный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальную провинцию в словарь
            processed.Add(pC.Index, true);

            //Создаём итоговый список
            List<int> results = ListPool<int>.Get(threadId);

            //Создаём обратный счётчик для обрабатываемых провинций
            int candidatesLast = candidates.Count - 1;

            //Пока не достигнута последняя провинция в списке
            while (candidatesLast >= 0)
            {
                //Берём последнего кандидата
                int candidateIndex = candidates[candidatesLast];
                candidates.RemoveAt(candidatesLast);
                candidatesLast--;
                provincePEs[candidateIndex].Unpack(world, out int provinceEntity);
                ref CProvinceCore candidatePC = ref pCPool[pCIndices[provinceEntity]];

                //Если словарь ещё не содержит его
                if (processed.ContainsKey(candidateIndex) == false)
                {
                    //Находим путь до него
                    List<int> pathProvinces = PathFindThreads(
                        world,
                        ref pCPool, ref pCIndices,
                        threadId,
                        ref pC, ref candidatePC,
                        maxSteps);

                    //Если существует путь 
                    if (pathProvinces != null)
                    {
                        //Если длина пути больше или равна минимальной и меньше или равна максимальной
                        if (pathProvinces.Count >= minSteps && pathProvinces.Count <= maxSteps)
                        {
                            //Заносим кандидата в итоговый список
                            results.Add(candidatePC.Index);
                        }

                        //Заносим кандидата в словарь обработанных
                        processed.Add(candidatePC.Index, true);

                        //Для каждой соседней провинции
                        for (int a = 0; a < candidatePC.neighbourProvincePEs.Length; a++)
                        {
                            //Берём соседа
                            candidatePC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                            ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];

                            //Если словарь не содержит его
                            if (processed.ContainsKey(neighbourPC.Index) == false)
                            {
                                //Заносим его в список и увеличиваем счётчик
                                candidates.Add(neighbourPC.Index);
                                candidatesLast++;
                            }
                        }

                        //Возвращаем список в пул
                        ListPool<int>.Add(
                            threadId,
                            pathProvinces);
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
            EcsFilter provinceFilter, EcsPool<CProvinceCore> pCPool)
        {
            //Если матрица пути не требует обновления, то выходим из функции
            if (needRefreshRouteMatrix == false)
            {
                return;
            }

            //Отмечаем, что матрица пути не требует обновления
            needRefreshRouteMatrix = false;

            //Для каждой провинции
            //foreach (int provinceEntity in provinceFilter)
            //{
            //    //Берём провинцию
            //    ref CProvinceCore pC = ref pCPool.Get(provinceEntity);

            //    //Рассчитываем стоимость прохода по провинции
            //    float cost = pC.crossCost;

            //    //Обновляем стоимость прохода по провинции
            //    pC.crossCost = cost;
            //}

            //Если массив для поиска пуст
            if (pfCalc == null)
            {
                //Создаём массив
                pfCalc = new DPathfindingNodeFast[provincePEs.Length];

                //Создаём очередь 
                open = new(
                    new PathfindingNodesComparer(pfCalc),
                    provincePEs.Length);
            }
            //Иначе
            else
            {
                //Очищаем очередь и массив
                open.Clear();
                Array.Clear(pfCalc, 0, pfCalc.Length);

                //Обновляем сравнитель провинций в очереди
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
            //Создаём список для индексов провинций пути
            List<int> results = ListPool<int>.Get();

            //Находим путь и определяем количество провинций в пути
            int count = PathFind(
                world,
                provinceFilter, pCPool,
                ref startPC, ref endPC,
                results,
                searchLimit);

            //Если количество равно нулю, то возвращаем пустой список
            return count == 0 ? null : results;
        }

        public int PathFind(
            EcsWorld world,
            EcsFilter provinceFilter, EcsPool<CProvinceCore> pCPool,
            ref CProvinceCore startPC, ref CProvinceCore endPC,
            List<int> results,
            int searchLimit = 0)
        {
            //Очищаем список
            results.Clear();

            //Если стартовая провинция не равен конечному
            if (startPC.Index != endPC.Index)
            {
                //Рассчитываем матрицу пути
                PathMatrixRefresh(provinceFilter, pCPool);

                //Определяем максимальное количество шагов при поиске
                pathFindingSearchLimit = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //Находим путь
                List<DPathfindingClosedNode> path = PathFindFast(
                    world,
                    pCPool,
                    ref startPC, ref endPC);

                //Если путь не пуст
                if (path != null)
                {
                    //Берём количество провинций в пути
                    int routeCount = path.Count;

                    //Для каждой провинции в пути, кроме двух последних, в обратном порядке
                    for (int r = routeCount - 2; r > 0; r--)
                    {
                        //Заносим его в список индексов
                        results.Add(path[r].index);
                    }
                    //Заносим в список индексов индекс последней провинции
                    results.Add(endPC.Index);
                }
                //Иначе возвращаем 0, обозначая, что путь пуст
                else
                {
                    return 0;
                }
            }

            //Возвращаем количество провинций в пути
            return results.Count;
        }

        List<DPathfindingClosedNode> PathFindFast(
            EcsWorld world,
            EcsPool<CProvinceCore> pCPool,
            ref CProvinceCore startPC, ref CProvinceCore toPC)
        {
            //Создаём переменную для отслеживания наличия пути
            bool found = false;

            //Создаём счётчик шагов
            int stepsCounter = 0;

            //Если фаза поиска больше 250
            if (openProvinceValue > 250)
            {
                //Обнуляем фазу
                openProvinceValue = 1;
                closeProvinceValue = 2;
            }
            //Иначе
            else
            {
                //Обновляем фазу
                openProvinceValue += 2;
                closeProvinceValue += 2;
            }
            //Очищаем очередь и путь
            open.Clear();
            close.Clear();

            //Берём конечную провинцию
            ref Vector3 destinationCenter = ref toPC.center;

            //Создаём переменную для следующей провинции
            int nextProvinceIndex;

            //Обнуляем данные стартовой провинции в массиве
            pfCalc[startPC.Index].distance = 0;
            pfCalc[startPC.Index].priority = 2;
            pfCalc[startPC.Index].prevIndex = startPC.Index;
            pfCalc[startPC.Index].status = openProvinceValue;

            //Заносим стартовую провинцию в очередь
            open.Push(startPC.Index);

            //Пока в очереди есть провинции
            while (open.provincesCount > 0)
            {
                //Берём первую провинцию в очереди как текущую
                int currentProvinceIndex = open.Pop();

                //Если данная провинция уже вышел за границу поиска, то переходим с следующему
                if (pfCalc[currentProvinceIndex].status == closeProvinceValue)
                {
                    continue;
                }

                //Если индекс провинции равен индексу конечной провинции
                if (currentProvinceIndex == toPC.Index)
                {
                    //Выводим провинцию за границу поиска
                    pfCalc[currentProvinceIndex].status = closeProvinceValue;

                    //Отмечаем, что путь найден, и выходим из цикла
                    found = true;
                    break;
                }

                //Если счётчик шагов больше предела
                if (stepsCounter >= pathFindingSearchLimit)
                {
                    return null;
                }

                //Берём текущую провинцию
                provincePEs[currentProvinceIndex].Unpack(world, out int currentProvinceEntity);
                ref CProvinceCore currentPC = ref pCPool.Get(currentProvinceEntity);

                //Для каждого соседа текущей провинции
                for (int a = 0; a < currentPC.neighbourProvincePEs.Length; a++)
                {
                    //Берём соседа
                    currentPC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                    ref CProvinceCore neighbourPC = ref pCPool.Get(neighbourProvinceEntity);
                    nextProvinceIndex = neighbourPC.Index;

                    //Рассчитываем расстояние до соседа
                    float newDistance = pfCalc[currentProvinceIndex].distance + neighbourPC.crossCost;

                    //Если провинция находится в границе поиска или уже выведен за границу
                    if (pfCalc[nextProvinceIndex].status == openProvinceValue
                        || pfCalc[nextProvinceIndex].status == closeProvinceValue)
                    {
                        //Если расстояние до провинции меньше или равно новому, то переходим к следующему соседу
                        if (pfCalc[nextProvinceIndex].distance <= newDistance)
                        {
                            continue;
                        }
                    }
                    //Иначе обновляем расстояние

                    //Обновляем индекс предыдущей провинции и расстояние
                    pfCalc[nextProvinceIndex].prevIndex = currentProvinceIndex;
                    pfCalc[nextProvinceIndex].distance = newDistance;

                    //Рассчитываем приоритет поиска
                    //Рассчитываем угол, используемый в качестве эвристики
                    float angle = Vector3.Angle(destinationCenter, neighbourPC.center);

                    //Обновляем приоритет провинции и статус
                    pfCalc[nextProvinceIndex].priority = newDistance + 2f * angle;
                    pfCalc[nextProvinceIndex].status = openProvinceValue;

                    //Заносим провинцию в очередь
                    open.Push(nextProvinceIndex);
                }

                //Обновляем счётчик шагов
                stepsCounter++;

                //Выводим текущую провинцию за границу поиска
                pfCalc[currentProvinceIndex].status = closeProvinceValue;
            }

            //Если путь найден
            if (found == true)
            {
                //Очищаем список пути
                close.Clear();

                //Берём конечную провинцию
                int pos = toPC.Index;

                //Создаём временную структуру и заносим в неё данные конечной провинции
                ref DPathfindingNodeFast tempProvince = ref pfCalc[toPC.Index];
                DPathfindingClosedNode stepProvince;

                //Переносим данные из активных в итоговые
                stepProvince.priority = tempProvince.priority;
                stepProvince.distance = tempProvince.distance;
                stepProvince.prevIndex = tempProvince.prevIndex;
                stepProvince.index = toPC.Index;

                //Пока индекс провинции не равен индексу предыдущего,
                //то есть пока не достигнута стартовая провинция
                while (stepProvince.index != stepProvince.prevIndex)
                {
                    //Заносим провинцию в список пути
                    close.Add(stepProvince);

                    //Берём активные данные предыдущей провинции
                    pos = stepProvince.prevIndex;
                    tempProvince = ref pfCalc[pos];

                    //Переносим данные из активных в итоговые
                    stepProvince.priority = tempProvince.priority;
                    stepProvince.distance = tempProvince.distance;
                    stepProvince.prevIndex = tempProvince.prevIndex;
                    stepProvince.index = pos;
                }
                //Заносим последнюю провинцию в список пути
                close.Add(stepProvince);

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
                openProvinceValues[threadId] = 1;
                closeProvinceValues[threadId] = 2;

                //Создаём массив
                pathfindingArray[threadId] = new DPathfindingNodeFast[provincePEs.Length];

                //Создаём очередь
                pathFindingQueue[threadId] = new(
                    new PathfindingNodesComparer(pathfindingArray[threadId]),
                    provincePEs.Length);

                //Создаём список итогового пути
                closedNodes[threadId] = new();
            }
            //Иначе
            else
            {
                //Очищаем очередь и массив
                pathFindingQueue[threadId].Clear();
                Array.Clear(pathfindingArray[threadId], 0, pathfindingArray[threadId].Length);

                //Обновляем сравнитель провинций в очереди
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
            //Если стартовый провинция не равен конечному
            if (startPC.selfPE.EqualsTo(endPC.selfPE) == false)
            {
                //Обновляем матрицу пути
                PathMatrixRefreshThreads(threadId);

                //Определяем максимальное количество шагов при поиске
                pathfindingSearchLimits[threadId] = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //Находим путь
                List<DPathfindingClosedNode> path = PathFindFastThreads(
                    world,
                    ref pCPool, ref pCIndices,
                    threadId,
                    ref startPC, ref endPC);

                //Если путь не пуст
                if (path != null)
                {
                    //Создаём список для возврата
                    List<int> results = ListPool<int>.Get(threadId);

                    //Для каждой провинции в пути
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
            ref CProvinceCore[] pCPool, ref int[] pCIndices,
            int threadId,
            ref CProvinceCore startPC, ref CProvinceCore endPC)
        {
            //Создаём переменную для отслеживания наличия пути
            bool found = false;

            //Создаём счётчик шагов
            int stepsCount = 0;

            //Если фаза поиска больше 250
            if (openProvinceValues[threadId] > 250)
            {
                //Обнуляем фазу
                openProvinceValues[threadId] = 1;
                closeProvinceValues[threadId] = 2;
            }
            //Иначе
            else
            {
                //Обновляем фазу
                openProvinceValues[threadId] += 2;
                closeProvinceValues[threadId] += 2;
            }
            //Очищаем очередь и путь
            pathFindingQueue[threadId].Clear();
            closedNodes[threadId].Clear();

            //Берём центр конечной провинции
            ref Vector3 destinationCenter = ref endPC.center;

            //Создаём переменную для следующей провинции
            int nextProvinceIndex;

            //Обнуляем данные стартовой провинции в массиве
            pathfindingArray[threadId][startPC.Index].distance = 0;
            pathfindingArray[threadId][startPC.Index].priority = 2;
            pathfindingArray[threadId][startPC.Index].prevIndex = startPC.Index;
            pathfindingArray[threadId][startPC.Index].status = openProvinceValues[threadId];

            //Заносим стартовую провинцию в очередь
            pathFindingQueue[threadId].Push(startPC.Index);

            //Пока в очереди есть провинции
            while (pathFindingQueue[threadId].provincesCount > 0)
            {
                //Берём первую провинцию в очереди как текущую
                int currentProvinceIndex = pathFindingQueue[threadId].Pop();

                //Если данная провинция уже вышла за границу поиска, то переходим с следующей
                if (pathfindingArray[threadId][currentProvinceIndex].status == closeProvinceValues[threadId])
                {
                    continue;
                }

                //Если индекс провинции равен индексу конечной провинции
                if (currentProvinceIndex == endPC.Index)
                {
                    //Выводим провинцию за границу поиска
                    pathfindingArray[threadId][currentProvinceIndex].status = closeProvinceValues[threadId];

                    //Отмечаем, что путь найден, и выходим из цикла
                    found = true;
                    break;
                }

                //Если счётчик шагов больше предела
                if (stepsCount >= pathfindingSearchLimits[threadId])
                {
                    return null;
                }

                //Берём текущую провинцию
                provincePEs[currentProvinceIndex].Unpack(world, out int currentProvinceEntity);
                ref CProvinceCore currentPC = ref pCPool[pCIndices[currentProvinceEntity]];

                //Для каждого соседа текущей провинции
                for (int a = 0; a < currentPC.neighbourProvincePEs.Length; a++)
                {
                    //Берём соседа
                    currentPC.neighbourProvincePEs[a].Unpack(world, out int neighbourProvinceEntity);
                    ref CProvinceCore neighbourPC = ref pCPool[pCIndices[neighbourProvinceEntity]];
                    nextProvinceIndex = neighbourPC.Index;

                    //Рассчитываем расстояние до соседа
                    float newDistance = pathfindingArray[threadId][currentProvinceIndex].distance + neighbourPC.crossCost;

                    //Если провинция находится в границе поиска или уже выведен за границу
                    if (pathfindingArray[threadId][nextProvinceIndex].status == openProvinceValues[threadId]
                        || pathfindingArray[threadId][nextProvinceIndex].status == closeProvinceValues[threadId])
                    {
                        //Если расстояние до провинции меньше или равно новому, то переходим к следующему соседу
                        if (pathfindingArray[threadId][nextProvinceIndex].distance <= newDistance)
                        {
                            continue;
                        }
                    }
                    //Иначе обновляем расстояние

                    //Обновляем индекс предыдущей провинции и расстояние
                    pathfindingArray[threadId][nextProvinceIndex].prevIndex = currentProvinceIndex;
                    pathfindingArray[threadId][nextProvinceIndex].distance = newDistance;

                    //Рассчитываем приоритет поиска
                    //Рассчитываем угол, используемый в качестве эвристики
                    float angle = Vector3.Angle(destinationCenter, neighbourPC.center);

                    //Обновляем приоритет провинции и статус
                    pathfindingArray[threadId][nextProvinceIndex].priority = newDistance + 2f * angle;
                    pathfindingArray[threadId][nextProvinceIndex].status = openProvinceValues[threadId];

                    //Заносим провинцию в очередь
                    pathFindingQueue[threadId].Push(nextProvinceIndex);
                }

                //Обновляем счётчик шагов
                stepsCount++;

                //Выводим текущую провинцию за границу поиска
                pathfindingArray[threadId][currentProvinceIndex].status = closeProvinceValues[threadId];
            }

            //Если путь найден
            if (found == true)
            {
                //Очищаем список пути
                closedNodes[threadId].Clear();

                //Берём конечную провинцию
                int pos = endPC.Index;

                //Создаём временную структуру и заносим в неё данные конечной провинции
                ref DPathfindingNodeFast tempProvince = ref pathfindingArray[threadId][endPC.Index];
                DPathfindingClosedNode stepProvince;

                //Переносим данные из активных в итоговые
                stepProvince.priority = tempProvince.priority;
                stepProvince.distance = tempProvince.distance;
                stepProvince.prevIndex = tempProvince.prevIndex;
                stepProvince.index = endPC.Index;

                //Пока индекс провинции не равен индексу предыдущей,
                //то есть пока не достигнута стартовая провинция
                while (stepProvince.index != stepProvince.prevIndex)
                {
                    //Заносим провинцию в список пути
                    closedNodes[threadId].Add(stepProvince);

                    //Берём активные данные предыдущей провинции
                    pos = stepProvince.prevIndex;
                    tempProvince = ref pathfindingArray[threadId][pos];

                    //Переносим данные из активных в итоговые
                    stepProvince.priority = tempProvince.priority;
                    stepProvince.distance = tempProvince.distance;
                    stepProvince.prevIndex = tempProvince.prevIndex;
                    stepProvince.index = pos;
                }
                //Заносим последнюю провинцию в список пути
                closedNodes[threadId].Add(stepProvince);

                //Возвращаем список пути
                return closedNodes[threadId];
            }
            return null;
        }
        #endregion
    }
}