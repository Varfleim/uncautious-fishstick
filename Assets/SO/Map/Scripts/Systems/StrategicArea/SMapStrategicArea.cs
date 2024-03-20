
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using SO.Map.Generation;
using SO.Map.Region;

namespace SO.Map.StrategicArea
{
    public class SMapStrategicArea : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsFilterInject<Inc<CRegionCore>> rCFilter = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;
        readonly EcsFilterInject<Inc<CRegionCore, CTRegionStrategicArea>> regionSAFilter = default;
        readonly EcsPoolInject<CTRegionStrategicArea> rSAPool = default;

        readonly EcsFilterInject<Inc<CStrategicArea, CTStrategicAreaGeneration>> sAGFilter = default;
        readonly EcsPoolInject<CStrategicArea> sAPool = default;
        readonly EcsPoolInject<CTStrategicAreaGeneration> sAGPool = default;

        readonly EcsFilterInject<Inc<CTWithoutFreeNeighbours>> withoutFreeNeighboursFilter = default;
        readonly EcsFilterInject<Inc<CStrategicArea>, Exc<CTWithoutFreeNeighbours>> sAWithFreeNeighboursFilter = default;
        readonly EcsPoolInject<CTWithoutFreeNeighbours> withoutFreeNeighboursPool = default;


        //События карты
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //Данные
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого события генерации карты
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //Берём запрос
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //Создаём стратегические области
                StrategicAreaCreate();
            }
        }

        void StrategicAreaCreate()
        {
            //Для каждого региона
            foreach(int regionEntity in rCFilter.Value)
            {
                //Назначаем региону компонент стратегической области
                ref CTRegionStrategicArea regionSA = ref rSAPool.Value.Add(regionEntity);

                //Заполняем данные компонента
                regionSA = new(world.Value.PackEntity(regionEntity));
            }

            //Случайно генерируем стратегические области
            StrategicAreaGenerate();

            //Определяем соседей областей
            StrategicAreaSetNeighbours();

            //Для каждой сущности с компонентом отсутствия соседей
            foreach (int entity in withoutFreeNeighboursFilter.Value)
            {
                //Удаляем компонент с сущностит
                withoutFreeNeighboursPool.Value.Del(entity);
            }

            //Для каждого региона со стратегической областью
            foreach (int regionEntity in regionSAFilter.Value)
            {
                //Берём регион
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

                //Переносим временные данные в основные
                rC.SetParentStrategicArea(rSA.parentSAPE);

                //Удаляем компонент с сущности
                rSAPool.Value.Del(regionEntity);
            }

            //Для каждой области с компонентом генерации
            foreach(int sAEntity in sAGFilter.Value)
            {
                //Берём область
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);
                ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Get(sAEntity);

                //Переносим временные данные в основные
                sA.regionPEs = sAG.tempRegionPEs.ToArray();
                sA.neighbourSAPEs = sAG.tempNeighbourSAPEs.ToArray();

                sAGPool.Value.Del(sAEntity);
            }
        }

        void StrategicAreaGenerate()
        {
            //Определяем количество стратегических областей
            int strategicAreaCount = regionsData.Value.regionPEs.Length / mapGenerationData.Value.strategicAreaAverageSize;

            //Создаём массив для PE областей
            regionsData.Value.strategicAreaPEs = new EcsPackedEntity[strategicAreaCount];

            //Для каждой требуемой стратегической области
            for (int a = 0; a < regionsData.Value.strategicAreaPEs.Length; a++)
            {
                //Создаём новую сущность и назначаем ей компоненты стратегической области и генерации стратегической области
                int sAEntity = world.Value.NewEntity();
                ref CStrategicArea sA = ref sAPool.Value.Add(sAEntity);
                ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Add(sAEntity);

                //Определяем цвет области
                Color sAColor = new Color32((byte)(Random.value * 255), (byte)(Random.value * 255), (byte)(Random.value * 255), 255);

                //Заполняем основные данные области
                sA = new(
                    world.Value.PackEntity(sAEntity),
                    sAColor);

                //Заполняем данные компонента генерации
                sAG = new(0);

                //Заносим PE области в массив
                regionsData.Value.strategicAreaPEs[a] = sA.selfPE;

                //Берём компонент области случайного региона
                regionsData.Value.GetRegionRandom().Unpack(world.Value, out int regionEntity);
                ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

                //Пока полученный регион принадлежит какой-либо области
                while (rSA.parentSAPE.Unpack(world.Value, out int regionParentSAEntity))
                {
                    //Берём компонент области случайного региона
                    regionsData.Value.GetRegionRandom().Unpack(world.Value, out regionEntity);
                    rSA = ref rSAPool.Value.Get(regionEntity);
                }

                //Берём полученный регион
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //Заносим регион во временный список области
                sAG.tempRegionPEs.Add(rSA.selfPE);

                //Заносим PE области в данные региона
                rSA.parentSAPE = sA.selfPE;

                //Увеличиваем количество занятых соседних регионов для всех регионов, соседних rC
                //Для каждого соседа
                for (int b = 0; b < rC.neighbourRegionPEs.Length; b++)
                {
                    //Берём сущность соседа
                    rC.neighbourRegionPEs[b].Unpack(world.Value, out int neighbourRegionEntity);

                    //Увеличиваем количество занятых соседних регионов для него
                    RegionStrategicAreaAddNeighbourWithRSA(
                        neighbourRegionEntity,
                        sA.selfPE);
                }
            }

            //Создаём счётчик ошибок
            int errorCount = 0;

            //Пока существуют области, имеющие свободных соседей
            while (sAWithFreeNeighboursFilter.Value.GetEntitiesCount() > 0)
            {
                //Определяем, сколько регионов было добавлено к областям в этом цикле
                int addedRegionsCount = 0;

                //Для каждой области, имеющей свободных соседей
                foreach (int sAEntity in sAWithFreeNeighboursFilter.Value)
                {
                    //Берём область
                    ref CStrategicArea currentSA = ref sAPool.Value.Get(sAEntity);
                    ref CTStrategicAreaGeneration currentSAG = ref sAGPool.Value.Get(sAEntity);

                    //Берём сущность случайного региона в области
                    currentSAG.tempRegionPEs[Random.Range(0, currentSAG.tempRegionPEs.Count)].Unpack(world.Value, out int currentFreeRegionEntity);

                    //Создаём счётчик ошибок
                    int regionErrorCount = 0;
                    //И определяем максимальное количество ошибок
                    int maxRegionErrorCount = currentSAG.tempRegionPEs.Count * currentSAG.tempRegionPEs.Count;

                    //Пока полученный регион не будет иметь свободных соседей
                    while (withoutFreeNeighboursPool.Value.Has(currentFreeRegionEntity) == true)
                    {
                        //Увеличиваем счётчик ошибок
                        regionErrorCount++;

                        //Если счётчик ошибок больше максимального значения
                        if (regionErrorCount > maxRegionErrorCount)
                        {
                            break;
                        }

                        //Берём сущность случайного региона в области
                        currentSAG.tempRegionPEs[Random.Range(0, currentSAG.tempRegionPEs.Count)].Unpack(world.Value, out currentFreeRegionEntity);
                    }

                    //Если счётчик ошибок меньше максимального значения
                    if (regionErrorCount <= maxRegionErrorCount)
                    {
                        //Берём полученный регион
                        ref CRegionCore currentFreeRC = ref rCPool.Value.Get(currentFreeRegionEntity);
                        ref CTRegionStrategicArea currentFreeRSA = ref rSAPool.Value.Get(currentFreeRegionEntity);

                        //Берём компонент области случайного соседнего региона
                        currentFreeRC.neighbourRegionPEs[Random.Range(0, currentFreeRC.neighbourRegionPEs.Length)]
                            .Unpack(world.Value, out int neighbourFreeRegionEntity);
                        ref CTRegionStrategicArea neighbourFreeRSA = ref rSAPool.Value.Get(neighbourFreeRegionEntity);

                        //Создаём счётчик ошибок
                        int neighbourErrorCount = 0;
                        //И определяем максимальное количество ошибок
                        int maxNeighbourErrorCount = currentFreeRC.neighbourRegionPEs.Length
                            * (currentFreeRC.neighbourRegionPEs.Length - currentFreeRSA.neighboursWithSACount);

                        //Пока полученный регион принадлежит какой-либо области
                        while (neighbourFreeRSA.parentSAPE.Unpack(world.Value, out int neighboutParentSAEntity))
                        {
                            //Увеличиваем счётчик ошибок
                            neighbourErrorCount++;

                            //Если счётчик ошибок больше максимального значения
                            if (neighbourErrorCount > maxNeighbourErrorCount)
                            {
                                break;
                            }

                            //Берём компонент области случайного соседнего региона
                            currentFreeRC.neighbourRegionPEs[Random.Range(0, currentFreeRC.neighbourRegionPEs.Length)]
                                .Unpack(world.Value, out neighbourFreeRegionEntity);
                            neighbourFreeRSA = ref rSAPool.Value.Get(neighbourFreeRegionEntity);
                        }

                        //Если счётчик ошибок меньше максимального значения
                        if (neighbourErrorCount <= maxNeighbourErrorCount)
                        {
                            //Берём полученный соседний регион
                            ref CRegionCore neighbourFreeRC = ref rCPool.Value.Get(neighbourFreeRegionEntity);

                            //Заносим его во временный список области
                            currentSAG.tempRegionPEs.Add(neighbourFreeRC.selfPE);

                            //Заносим PE области в данные региона
                            neighbourFreeRSA.parentSAPE = currentSA.selfPE;

                            //Увеличиваем количество занятых соседних регионов для всех регионов, соседних neighbourFreeRC
                            //Для каждого соседа
                            for (int a = 0; a < neighbourFreeRC.neighbourRegionPEs.Length; a++)
                            {
                                //Берём сущность соседа
                                neighbourFreeRC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourNeighbourRegionEntity);

                                //Увеличиваем количество занятых соседних регионов для него
                                RegionStrategicAreaAddNeighbourWithRSA(
                                    neighbourNeighbourRegionEntity,
                                    currentSA.selfPE);
                            }

                            addedRegionsCount++;

                            //Если регион имеет компонент отсутствия соседей, а область не имеет
                            if (withoutFreeNeighboursPool.Value.Has(neighbourFreeRegionEntity) == true
                                && withoutFreeNeighboursPool.Value.Has(sAEntity) == false)
                            {
                                //Проводим проверку области
                                StrategicAreaCheckRegionsWithoutFreeNeighbours(sAEntity);
                            }
                        }
                    }
                }

                //Если за цикл не было добавлено ни одного региона
                if (addedRegionsCount == 0)
                {
                    //Увеличиваем счётчик ошибок
                    errorCount++;
                }

                //Если счётчик ошибок достиг предела
                if (errorCount >= strategicAreaCount)
                {
                    //Выводим ошибку и прерываем цикл
                    Debug.LogWarning("Невозможно дальнейшее расширение стратегических зон!");

                    break;
                }
            }
        }

        void StrategicAreaSetNeighbours()
        {
            //Для каждого региона
            foreach (int regionEntity in regionSAFilter.Value)
            {
                //Берём компонент области региона
                ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

                //Удаляем из списка соседних областей область, к которой регион принадлежит
                rSA.neighbourSAPEs.Remove(rSA.parentSAPE);
            }

            //Для каждой стратегической области
            foreach (int sAEntity in sAGFilter.Value)
            {
                //Берём область
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);
                ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Get(sAEntity);

                //Для каждого региона области
                for(int a = 0; a < sAG.tempRegionPEs.Count; a++)
                {
                    //Берём компонент области региона
                    sAG.tempRegionPEs[a].Unpack(world.Value, out int regionEntity);
                    ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

                    //Для каждой соседней региону области
                    for(int b = 0; b < rSA.neighbourSAPEs.Count; b++)
                    {
                        //Если такой области нет в списке соседних областей текущей области
                        if (sAG.tempNeighbourSAPEs.Contains(rSA.neighbourSAPEs[b]) == false)
                        {
                            //Заносим её PE в список
                            sAG.tempNeighbourSAPEs.Add(rSA.neighbourSAPEs[b]);
                        }
                    }
                }
            }
        }

        void StrategicAreaAddRegionWithoutFreeNeighbours(
        int sAEntity)
        {
            //Берём стратегическую область
            ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Get(sAEntity);

            //Если количество регионов без свободных соседей меньше количества регионов
            if (sAG.regionWithoutFreeNeighboursCount < sAG.tempRegionPEs.Count)
            {
                //Увеличиваем количество регионов без свободных соседей
                sAG.regionWithoutFreeNeighboursCount++;

                //Если количество регионов без свободных соседей равно количеству регионов
                if (sAG.regionWithoutFreeNeighboursCount >= sAG.tempRegionPEs.Count)
                {
                    //Назначаем области компонент отсутствия соседей
                    ref CTWithoutFreeNeighbours sAWFN = ref withoutFreeNeighboursPool.Value.Add(sAEntity);
                }
            }
        }

        void StrategicAreaCheckRegionsWithoutFreeNeighbours(
            int sAEntity)
        {
            //Берём стратегическую область
            ref CTStrategicAreaGeneration sAG = ref sAGPool.Value.Get(sAEntity);

            //Подсчитываем количество регионов, не имеющих свободных соседей
            int rWFNCount = 0;

            //Для каждого региона в области
            for(int a = 0; a < sAG.tempRegionPEs.Count; a++)
            {
                //Берём сущность региона
                sAG.tempRegionPEs[a].Unpack(world.Value, out int regionEntity);

                //Если регион имеет компонент отсутствия соседей
                if(withoutFreeNeighboursPool.Value.Has(regionEntity) == true)
                {
                    //Увеличиваем счётчик
                    rWFNCount++;
                }
            }

            //Обновляем счётчик в данных области
            sAG.regionWithoutFreeNeighboursCount = rWFNCount;

            //Если количество регионов без свободных соседей равно количеству регионов
            if(sAG.regionWithoutFreeNeighboursCount >= sAG.tempRegionPEs.Count)
            {
                //Назначаем области компонент отсутствия соседей
                ref CTWithoutFreeNeighbours sAWFN = ref withoutFreeNeighboursPool.Value.Add(sAEntity);
            }
        }

        void RegionStrategicAreaAddNeighbourWithRSA(
            int regionEntity,
            EcsPackedEntity neighbourSAPE)
        {
            //Берём регион
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
            ref CTRegionStrategicArea rSA = ref rSAPool.Value.Get(regionEntity);

            //Если количество соседей, принадлежащих областям, меньше количества соседей
            if(rSA.neighboursWithSACount < rC.neighbourRegionPEs.Length)
            {
                //Увеличиваем количество соседей, принадлежащих областям
                rSA.neighboursWithSACount++;

                //Заносим новую соседнюю область в список соседних областей региона
                rSA.neighbourSAPEs.Add(neighbourSAPE);

                //Если количество соседей, принадлежащих областям, равно количеству соседей
                if (rSA.neighboursWithSACount >= rC.neighbourRegionPEs.Length)
                {
                    //Назначаем региону компонент отсутствия свободных соседей
                    ref CTWithoutFreeNeighbours rWFN = ref withoutFreeNeighboursPool.Value.Add(regionEntity); 

                    //Если регион принадлежит области
                    if(rSA.parentSAPE.Unpack(world.Value, out int parentSAEntity))
                    {
                        //То увеличиваем количество регионов без свободных соседей для этой области
                        StrategicAreaAddRegionWithoutFreeNeighbours(parentSAEntity);
                    }
                }
            }
        }
    }
}