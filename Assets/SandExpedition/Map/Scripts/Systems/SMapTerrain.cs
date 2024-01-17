
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
        //Миры
        readonly EcsWorldInject world = default;

        //Карта
        readonly EcsFilterInject<Inc<CRegion>> regionFilter = default;
        readonly EcsPoolInject<CRegion> regionPool = default;

        //События карты
        readonly EcsFilterInject<Inc<RMapGeneration>> mapGenerationRequestFilter = default;
        readonly EcsPoolInject<RMapGeneration> mapGenerationRequestPool = default;

        //Данные
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого события генерации карты
            foreach(int requestEntity in mapGenerationRequestFilter.Value)
            {
                //Берём запрос
                ref RMapGeneration requestComp = ref mapGenerationRequestPool.Value.Get(requestEntity);

                //Генерируем ландшафт
                TerrainCreate(ref requestComp);
            }
        }

        void TerrainCreate(
            ref RMapGeneration requestComp)
        {
            //Инициализируем данные ландшафта
            TerrainDataInitialization(ref requestComp);

            //Создаём массив и очередь для поиска
            DPathfindingNodeFast[] regions = new DPathfindingNodeFast[regionsData.Value.regionPEs.Length];
            PathfindingQueueInt queue = new PathfindingQueueInt(
                new PathfindingNodesComparer(regions),
                regionsData.Value.regionPEs.Length);

            //Создаём сушу
            TerrainCreateLand(queue, ref regions);

            //Проводим эрозию
            TerrainErodeLand();
        }

        void TerrainDataInitialization(
            ref RMapGeneration requestComp)
        {

        }

        void TerrainCreateLand(
            PathfindingQueueInt queue, ref DPathfindingNodeFast[] regions)
        {
            //Определяем бюджет суши
            int landBudget = Mathf.RoundToInt(regionsData.Value.regionPEs.Length * mapGenerationData.Value.landPercentage * 0.01f);

            //Пока бюджет суши больше нуля и прошло менее предела итераций
            for (int a = 0; a < 25000; a++)
            {
                //Определяем, нужно ли утопить сушу
                bool sink = Random.value < mapGenerationData.Value.sinkProbability;

                //Определяем размер чанка
                int chunkSize = Random.Range(mapGenerationData.Value.chunkSizeMin, mapGenerationData.Value.chunkSizeMax + 1);

                //Если требуется утопить сушу
                if (sink == true)
                {
                    //Топим сушу
                    MapSinkTerrain(
                        queue, ref regions,
                        chunkSize,
                        landBudget,
                        out landBudget);
                }
                //Иначе
                else
                {
                    //Поднимаем сушу
                    MapRaiseTerrain(
                        queue, ref regions,
                        chunkSize,
                        landBudget,
                        out landBudget);

                    //Если бюджет суши равен нулю
                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }

            //Если бюджет суши больше нуля
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
            //Если фаза поиска больше 250
            if (regionsData.Value.openRegionValue > 250)
            {
                //Обнуляем фазу
                regionsData.Value.openRegionValue = 1;
                regionsData.Value.closeRegionValue = 2;
            }
            //Иначе
            else
            {
                //Обновляем фазу
                regionsData.Value.openRegionValue += 2;
                regionsData.Value.closeRegionValue += 2;
            }
            //Очищаем очередь
            queue.Clear();

            //Берём случайный регион
            regionsData.Value.GetRegionRandom().Unpack(world.Value, out int firstRegionEntity);
            ref CRegion firstRegion = ref regionPool.Value.Get(firstRegionEntity);

            //Устанавливаем его фазу поиска на текущую
            regions[firstRegion.Index].distance = 0;
            regions[firstRegion.Index].priority = 2;
            regions[firstRegion.Index].status = regionsData.Value.openRegionValue;

            //Заносим регион в очередь
            queue.Push(firstRegion.Index);

            //Определяем, будет это высоким поднятием суши или обычным
            int rise = Random.value < mapGenerationData.Value.highRiseProbability ? 2 : 1;
            //Создаём счётчик размера
            int currentSize = 0;

            //Пока счётчик меньше требуемого размера и очередь не пуста
            while (currentSize < chunkSize && queue.regionsCount > 0)
            {
                //Берём первый регион в очереди
                int currentRegionIndex = queue.Pop();
                regionsData.Value.regionPEs[currentRegionIndex].Unpack(world.Value, out int currentRegionEntity);
                ref CRegion currentRegion = ref regionPool.Value.Get(currentRegionEntity);

                //Запоминаем исходную высоту региона
                int originalElevation = currentRegion.Elevation;
                //Определяем новую высоту региона
                int newElevation = originalElevation + rise;

                //Если новая высота больше максимальной
                if (newElevation > mapGenerationData.Value.elevationMaximum)
                {
                    continue;
                }

                //Увеличиваем высоту региона
                currentRegion.Elevation = newElevation;
                currentRegion.ExtrudeAmount = (float)currentRegion.Elevation / (mapGenerationData.Value.elevationMaximum + 1);

                //Если исходная высота региона меньше уровня моря
                if (originalElevation < mapGenerationData.Value.waterLevel
                    //И новая высота больше или равна уровню моря
                    && newElevation >= mapGenerationData.Value.waterLevel
                    //И уменьшенный бюджет суши равен нулю
                    && --landBudget == 0)
                {
                    break;
                }

                //Увеличиваем счётчик
                currentSize += 1;

                //Для каждого соседа текущего региона
                for (int i = 0; i < currentRegion.neighbourRegionPEs.Length; i++)
                {
                    //Берём соседа
                    currentRegion.neighbourRegionPEs[i].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                    //Если фаза поиска соседа меньше текущей
                    if (regions[neighbourRegion.Index].status < regionsData.Value.openRegionValue)
                    {
                        //Устанавливаем фазу поиска на текущую
                        regions[neighbourRegion.Index].status = regionsData.Value.openRegionValue;
                        regions[neighbourRegion.Index].distance = 0;
                        regions[neighbourRegion.Index].priority = Vector3.Angle(firstRegion.center, neighbourRegion.center) * 2f;
                        regions[neighbourRegion.Index].priority += Random.value < mapGenerationData.Value.jitterProbability ? 1 : 0;

                        //Заносим регион в очередь
                        queue.Push(neighbourRegion.Index);
                    }
                }
            }

            //Возвращаем бюджет суши
            currentLandBudget = landBudget;
        }

        void MapSinkTerrain(
            PathfindingQueueInt queue, ref DPathfindingNodeFast[] regions,
            int chunkSize,
            int landBudget,
            out int currentLandBudget)
        {
            //Если фаза поиска больше 250
            if (regionsData.Value.openRegionValue > 250)
            {
                //Обнуляем фазу
                regionsData.Value.openRegionValue = 1;
                regionsData.Value.closeRegionValue = 2;
            }
            //Иначе
            else
            {
                //Обновляем фазу
                regionsData.Value.openRegionValue += 2;
                regionsData.Value.closeRegionValue += 2;
            }
            //Очищаем очередь
            queue.Clear();

            //Берём случайный регион
            regionsData.Value.GetRegionRandom().Unpack(world.Value, out int firstRegionEntity);
            ref CRegion firstRegion = ref regionPool.Value.Get(firstRegionEntity);

            //Устанавливаем его фазу поиска на текущую
            regions[firstRegion.Index].distance = 0;
            regions[firstRegion.Index].priority = 2;
            regions[firstRegion.Index].status = regionsData.Value.openRegionValue;

            //Заносим регион в очередь
            queue.Push(firstRegion.Index);

            //Определяем, будет это высотным утоплением суши или обычным
            int sink = Random.value < mapGenerationData.Value.highRiseProbability ? 2 : 1;
            //Создаём счётчик размера
            int currentSize = 0;

            //Пока счётчик меньше требуемого размера и очередь не пуста
            while (currentSize < chunkSize && queue.regionsCount > 0)
            {
                //Берём первый регион в очереди
                int currentRegionIndex = queue.Pop();
                regionsData.Value.regionPEs[currentRegionIndex].Unpack(world.Value, out int currentRegionEntity);
                ref CRegion currentRegion = ref regionPool.Value.Get(currentRegionEntity);

                //Запоминаем исходную высоту региона
                int originalElevation = currentRegion.Elevation;
                //Определяем новую высоту региона
                int newElevation = currentRegion.Elevation - sink;

                //Если новая высота региона меньше минимальной
                if (newElevation < mapGenerationData.Value.elevationMinimum)
                {
                    continue;
                }

                //Увеличиваем высоту региона
                currentRegion.Elevation = newElevation;

                if (currentRegion.Elevation > 0)
                {
                    currentRegion.ExtrudeAmount = (float)currentRegion.Elevation / (mapGenerationData.Value.elevationMaximum + 1);
                }
                else
                {
                    currentRegion.ExtrudeAmount = 0;
                }

                //Если исходная высота региона больше или равна уровню моря
                if (originalElevation >= mapGenerationData.Value.waterLevel
                    //И новая высота меньше уровню моря
                    && newElevation < mapGenerationData.Value.waterLevel)
                {
                    //Увеличиваем бюджет суши
                    landBudget += 1;
                }

                //Увеличиваем счётчик
                currentSize += 1;

                //Для каждого соседа текущего региона
                for (int i = 0; i < currentRegion.neighbourRegionPEs.Length; i++)
                {
                    //Берём соседа
                    currentRegion.neighbourRegionPEs[i].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                    //Если фаза поиска соседа меньше текущей
                    if (regions[neighbourRegion.Index].status < regionsData.Value.openRegionValue)
                    {
                        //Устанавливаем фазу поиска на текущую
                        regions[neighbourRegion.Index].status = regionsData.Value.openRegionValue;
                        regions[neighbourRegion.Index].distance = 0;
                        regions[neighbourRegion.Index].priority = Vector3.Angle(firstRegion.center, neighbourRegion.center) * 2f;
                        regions[neighbourRegion.Index].priority += Random.value < mapGenerationData.Value.jitterProbability ? 1 : 0;

                        //Заносим регион в очередь
                        queue.Push(neighbourRegion.Index);
                    }
                }
            }

            //Возвращаем бюджет суши
            currentLandBudget = landBudget;
        }

        void TerrainErodeLand()
        {
            //Создаём список сущностей регионов, подлежащих эрозии
            List<int> erodibleRegions = ListPool<int>.Get();

            //Для каждого региона
            foreach (int regionEntity in regionFilter.Value)
            {
                //Берём регион
                ref CRegion region = ref regionPool.Value.Get(regionEntity);

                //Если регион подлежит эрозии, заносим его в список
                if (RegionIsErodible(ref region) == true)
                {
                    erodibleRegions.Add(regionEntity);
                }
            }

            //Определяем, сколько регионов должно подвергнуться эрозии
            int erodibleRegionsCount = (int)(erodibleRegions.Count * (100 - mapGenerationData.Value.erosionPercentage) * 0.01f);

            //Пока список сущностей регионов больше требуемого числа
            while (erodibleRegions.Count > erodibleRegionsCount)
            {
                //Выбираем случайный регион в списке
                int index = Random.Range(0, erodibleRegions.Count);
                ref CRegion region = ref regionPool.Value.Get(erodibleRegions[index]);

                //Выбираем одного соседа как цель эрозии
                int targetRegionEntity = RegionGetErosionTarget(ref region);
                ref CRegion targetRegion = ref regionPool.Value.Get(targetRegionEntity);

                //Уменьшаем высоту исходного региона и увеличиваем высоту целевого
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

                //Если регион больше не подлежит эрозии
                if (RegionIsErodible(ref region) == false)
                {
                    //Удаляем его сущность из списка, заменяя последним регионом в списке
                    erodibleRegions[index] = erodibleRegions[erodibleRegions.Count - 1];
                    erodibleRegions.RemoveAt(erodibleRegions.Count - 1);
                }

                //Для каждого соседа
                for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
                {
                    //Берём соседа
                    region.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                    //Если высота соседа на 2 больше эрозируемого региона
                    if (neighbourRegion.Elevation == region.Elevation + 2
                        //Если сосед подлежит эрозии
                        && RegionIsErodible(ref neighbourRegion) == true
                        //И если список не содержит сущности соседа
                        && erodibleRegions.Contains(neighbourRegionEntity) == false)
                    {
                        //Заносим соседа в список
                        erodibleRegions.Add(neighbourRegionEntity);
                    }
                }

                //Если целевой регион подлежит эрозии
                if (RegionIsErodible(ref targetRegion)
                    //И список сущностей подлежащих эрозии регионов не содержит его
                    && erodibleRegions.Contains(targetRegionEntity) == false)
                {
                    //Заносим целевой регион в список
                    erodibleRegions.Add(targetRegionEntity);
                }

                //Для каждого соседа
                for (int a = 0; a < targetRegion.neighbourRegionPEs.Length; a++)
                {
                    //Если сосед существует
                    if (targetRegion.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity)
                        //И если сосед - не исходный эрозируемый регион
                        && region.selfPE.EqualsTo(targetRegion.neighbourRegionPEs[a]) == false)
                    {
                        //Берём соседа
                        ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                        //Если высота соседа на единицу больше, чем высота целевого региона
                        if (neighbourRegion.Elevation == targetRegion.Elevation + 1
                            //И если сосед не подлежит эрозии
                            && RegionIsErodible(ref neighbourRegion) == false
                            //И если список содержит сущность соседа
                            && erodibleRegions.Contains(neighbourRegionEntity) == true)
                        {
                            //Удаляем соседа из списка
                            erodibleRegions.Remove(neighbourRegionEntity);
                        }
                    }
                }
            }

            //Выносим список в пул
            ListPool<int>.Add(erodibleRegions);
        }

        bool RegionIsErodible(
            ref CRegion region)
        {
            //Определяем высоту, при которой произойдёт эрозия
            int erodibleElevation = region.Elevation - 2;

            //Для каждого соседа 
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа
                region.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                //Если высота соседа меньше или равна высоте эрозии
                if (neighbourRegion.Elevation <= erodibleElevation)
                {
                    //Возвращаем, что эрозия возможна
                    return true;
                }
            }

            //Возвращаем, что эрозия невозможна
            return false;
        }

        int RegionGetErosionTarget(
            ref CRegion region)
        {
            //Создаём список кандидатов целей эрозии
            List<int> candidateEntities = ListPool<int>.Get();

            //Определяем высоту, при которой произойдёт эрозия
            int erodibleElevation = region.Elevation - 2;

            //Для каждого соседа
            for (int a = 0; a < region.neighbourRegionPEs.Length; a++)
            {
                //Берём соседа
                region.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                //Если высота соседа меньше или равна высоте эрозии
                if (neighbourRegion.Elevation <= erodibleElevation)
                {
                    //Заносим соседа в список
                    candidateEntities.Add(neighbourRegionEntity);
                }
            }

            //Случайно выбираем регион из списка
            int targetRegionEntity = candidateEntities[Random.Range(0, candidateEntities.Count)];

            //Выносим список в пул
            ListPool<int>.Add(candidateEntities);

            //Возвращаем сущность целевого региона
            return targetRegionEntity;
        }
    }
}