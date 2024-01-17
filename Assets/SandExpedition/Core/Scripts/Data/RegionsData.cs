
using System;
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;

using SCM.Map.Pathfinding;

namespace SCM.Map
{
    public class RegionsData : MonoBehaviour
    {
        //Данные регионов
        public EcsPackedEntity[] regionPEs;

        //Данные поиска пути
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
            //Возвращаем PE запрошенного региона
            return regionPEs[regionIndex];
        }
        public EcsPackedEntity GetRegionRandom()
        {
            //Возвращаем PE случайного региона
            return GetRegion(UnityEngine.Random.Range(0, regionPEs.Length));
        }

        public List<int> GetRegionIndicesWithinSteps(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegion> regionPool,
            ref CRegion region,
            int maxSteps)
        {
            //Создаём промежуточный список
            List<int> candidates = new();

            //Для каждого соседа региона
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа
                region.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourRegion.Index);
            }

            //Создаём промежуточыный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальный регион в словарь
            processed.Add(region.Index, true);

            //Создаём итоговый список
            List<int> results = new();

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
                ref CRegion candidateRegion = ref regionPool.Get(regionEntity);

                //Находим путь до него
                List<int> pathRegions = FindPath(
                    world,
                    regionFilter, regionPool,
                    ref region, ref candidateRegion,
                    maxSteps);

                //Если словарь ещё не содержит его и существует путь 
                if (processed.ContainsKey(candidateIndex) == false && pathRegions != null)
                {
                    //Заносим кандидата в итоговый список и словарь
                    results.Add(candidateRegion.Index);
                    processed.Add(candidateRegion.Index, true);

                    //Для каждого соседнего региона
                    for (int a = 0; a < candidateRegion.neighbourRegionPEs.Length; a++)
                    {
                        //Берём соседа
                        candidateRegion.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                        ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);

                        //Если словарь не содержит его
                        if (processed.ContainsKey(neighbourRegion.Index) == false)
                        {
                            //Заносим его в список и увеличиваем счётчик
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
            //Создаём промежуточный список
            List<int> candidates = new();

            //Для каждого соседа региона
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа
                region.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);

                //Заносим индекс соседа в список кандидатов
                candidates.Add(neighbourRegion.Index);
            }

            //Создаём промежуточыный словарь
            Dictionary<int, bool> processed = new();

            //Заносим изначальный регион в словарь
            processed.Add(region.Index, true);

            //Создаём итоговый список
            List<int> results = new();

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
                ref CRegion candidateRegion = ref regionPool.Get(regionEntity);

                //Находим путь до него
                List<int> pathRegions = FindPath(
                    world,
                    regionFilter, regionPool,
                    ref region, ref candidateRegion,
                    maxSteps);

                //Если словарь ещё не содержит его и существует путь 
                if (processed.ContainsKey(candidateIndex) == false && pathRegions != null)
                {
                    //Если длина пути больше или равна минимальной и меньше или равна максимальной
                    if (pathRegions.Count >= minSteps && pathRegions.Count <= maxSteps)
                    {
                        //Заносим кандидата в итоговый список
                        results.Add(candidateRegion.Index);
                    }

                    //Заносим кандидата в словарь обработанных
                    processed.Add(candidateRegion.Index, true);

                    //Для каждого соседнего региона
                    for (int a = 0; a < candidateRegion.neighbourRegionPEs.Length; a++)
                    {
                        //Берём соседа
                        candidateRegion.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                        ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);

                        //Если словарь не содержит его
                        if (processed.ContainsKey(neighbourRegion.Index) == false)
                        {
                            //Заносим его в список и увеличиваем счётчик
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
            //Создаём список для индексов регионов пути
            List<int> results = new();

            //Находим путь и определяем количество регионов в пути
            int count = FindPath(
                world,
                regionFilter, regionPool,
                ref startRegion, ref endRegion,
                results,
                searchLimit);

            //Если количество равно нулю, то возвращаем пустой список
            return count == 0 ? null : results;
        }

        public int FindPath(
            EcsWorld world,
            EcsFilter regionFilter, EcsPool<CRegion> regionPool,
            ref CRegion fromRegion, ref CRegion toRegion,
            List<int> results,
            int searchLimit = 0)
        {
            //Очищаем список
            results.Clear();

            //Если стартовый регион не равен конечному
            if (fromRegion.Index != toRegion.Index)
            {
                //Рассчитываем матрицу пути
                CalculateRouteMatrix(regionFilter, regionPool);

                //Определяем максимальное количество шагов при поиске
                pathFindingSearchLimit = searchLimit == 0 ? pathFindingSearchLimitBase : searchLimit;

                //Находим путь
                List<DPathfindingClosedNode> path = FindPathFast(
                    world,
                    regionPool,
                    ref fromRegion, ref toRegion);

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
                    results.Add(toRegion.Index);
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

        void CalculateRouteMatrix(
            EcsFilter regionFilter, EcsPool<CRegion> regionPool)
        {
            //Если матрица пути не требует обновления, то выходим из функции
            if (needRefreshRouteMatrix == false)
            {
                return;
            }

            //Отмечаем, что матрица пути не требует обновления
            needRefreshRouteMatrix = false;

            //Для каждого региона
            foreach (int regionEntity in regionFilter)
            {
                //Берём регион
                ref CRegion region = ref regionPool.Get(regionEntity);

                //Рассчитываем стоимость прохода по региону
                float cost = region.crossCost;

                //Обновляем стоимость прохода по региону
                region.crossCost = cost;
            }

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

        List<DPathfindingClosedNode> FindPathFast(
            EcsWorld world,
            EcsPool<CRegion> regionPool,
            ref CRegion fromRegion, ref CRegion toRegion)
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
            Vector3 destinationCenter = toRegion.center;

            //Создаём переменную для следующего региона
            int nextRegionIndex;

            //Обнуляем данные стартового региона в массиве
            pfCalc[fromRegion.Index].distance = 0;
            pfCalc[fromRegion.Index].priority = 2;
            pfCalc[fromRegion.Index].prevIndex = fromRegion.Index;
            pfCalc[fromRegion.Index].status = openRegionValue;

            //Заносим стартовый регион в очередь
            open.Push(fromRegion.Index);

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
                if (currentRegionIndex == toRegion.Index)
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
                ref CRegion currentRegion = ref regionPool.Get(currentRegionEntity);

                //Для каждого соседа текущего региона
                for (int a = 0; a < currentRegion.neighbourRegionPEs.Length; a++)
                {
                    //Берём соседа
                    currentRegion.neighbourRegionPEs[a].Unpack(world, out int neighbourRegionEntity);
                    ref CRegion neighbourRegion = ref regionPool.Get(neighbourRegionEntity);
                    nextRegionIndex = neighbourRegion.Index;

                    //Рассчитываем расстояние до соседа
                    float newDistance = pfCalc[currentRegionIndex].distance + neighbourRegion.crossCost;

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
                    float angle = Vector3.Angle(destinationCenter, neighbourRegion.center);

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
                int pos = toRegion.Index;

                //Создаём временную структуру и заносим в неё данные конечного региона
                DPathfindingNodeFast tempRegion = pfCalc[toRegion.Index];
                DPathfindingClosedNode stepRegion;

                //Переносим данные из активных в итоговые
                stepRegion.priority = tempRegion.priority;
                stepRegion.distance = tempRegion.distance;
                stepRegion.prevIndex = tempRegion.prevIndex;
                stepRegion.index = toRegion.Index;

                //Пока индекс региона не равен индексу предыдущего,
                //то есть пока не достигнут стартовый регион
                while (stepRegion.index != stepRegion.prevIndex)
                {
                    //Заносим регион в список пути
                    close.Add(stepRegion);

                    //Берём активные данные предыдущего региона
                    pos = stepRegion.prevIndex;
                    tempRegion = pfCalc[pos];

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
    }
}