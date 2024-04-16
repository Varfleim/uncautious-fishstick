
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using SO.Map.Generation;
using SO.Map.Province;

namespace SO.Map.MapArea
{
    public class SMapMapArea : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsFilterInject<Inc<CProvinceCore>> pCFilter = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;
        readonly EcsFilterInject<Inc<CProvinceCore, CTProvinceMapArea>> provinceMAFilter = default;
        readonly EcsPoolInject<CTProvinceMapArea> pMAPool = default;

        readonly EcsFilterInject<Inc<CMapArea, CTMapAreaGeneration>> mAGFilter = default;
        readonly EcsPoolInject<CMapArea> mAPool = default;
        readonly EcsPoolInject<CTMapAreaGeneration> mAGPool = default;

        readonly EcsFilterInject<Inc<CTWithoutFreeNeighbours>> withoutFreeNeighboursFilter = default;
        readonly EcsFilterInject<Inc<CMapArea>, Exc<CTWithoutFreeNeighbours>> mAWithFreeNeighboursFilter = default;
        readonly EcsPoolInject<CTWithoutFreeNeighbours> withoutFreeNeighboursPool = default;


        //События карты
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //Данные
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<ProvincesData> provincesData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого события генерации карты
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //Берём запрос
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //Создаём области карты
                MapAreaCreate();
            }
        }

        void MapAreaCreate()
        {
            //Для каждой провинции
            foreach (int provinceEntity in pCFilter.Value)
            {
                //Назначаем провинции компонент области карты
                ref CTProvinceMapArea provinceMA = ref pMAPool.Value.Add(provinceEntity);

                //Заполняем данные компонента
                provinceMA = new(world.Value.PackEntity(provinceEntity));
            }

            //Случайно генерируем области карты
            MapAreaGenerate();

            //Определяем соседей областей
            MapAreaSetNeighbours();

            //Для каждой сущности с компонентом отсутствия соседей
            foreach (int entity in withoutFreeNeighboursFilter.Value)
            {
                //Удаляем компонент с сущностит
                withoutFreeNeighboursPool.Value.Del(entity);
            }

            //Для каждой провинции с областью карты
            foreach (int provinceEntity in provinceMAFilter.Value)
            {
                //Берём провинцию
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

                //Переносим временные данные в основные
                pC.SetParentMapArea(pMA.parentMAPE);

                //Удаляем компонент с сущности
                pMAPool.Value.Del(provinceEntity);
            }

            //Для каждой области с компонентом генерации
            foreach(int mAEntity in mAGFilter.Value)
            {
                //Берём область
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);
                ref CTMapAreaGeneration mAG = ref mAGPool.Value.Get(mAEntity);

                //Переносим временные данные в основные
                mA.provincePEs = mAG.tempProvincePEs.ToArray();
                mA.neighbourMAPEs = mAG.tempNeighbourMAPEs.ToArray();

                mAGPool.Value.Del(mAEntity);
            }
        }

        void MapAreaGenerate()
        {
            //Определяем количество областей карты
            int mapAreaCount = provincesData.Value.provincePEs.Length / mapGenerationData.Value.mapAreaAverageSize;

            //Создаём массив для PE областей
            provincesData.Value.mapAreaPEs = new EcsPackedEntity[mapAreaCount];

            //Для каждой требуемой области карты
            for (int a = 0; a < provincesData.Value.mapAreaPEs.Length; a++)
            {
                //Создаём новую сущность и назначаем ей компоненты области карты и генерации области карты
                int mAEntity = world.Value.NewEntity();
                ref CMapArea mA = ref mAPool.Value.Add(mAEntity);
                ref CTMapAreaGeneration mAG = ref mAGPool.Value.Add(mAEntity);

                //Определяем цвет области
                Color mAColor = new Color32((byte)(Random.value * 255), (byte)(Random.value * 255), (byte)(Random.value * 255), 255);

                //Заполняем основные данные области
                mA = new(
                    world.Value.PackEntity(mAEntity),
                    mAColor);

                //Заполняем данные компонента генерации
                mAG = new(0);

                //Заносим PE области в массив
                provincesData.Value.mapAreaPEs[a] = mA.selfPE;

                //Берём компонент области случайной провинции
                provincesData.Value.GetProvinceRandom().Unpack(world.Value, out int provinceEntity);
                ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

                //Пока полученная провинция принадлежит какой-либо области
                while (pMA.parentMAPE.Unpack(world.Value, out int provinceParentMAEntity))
                {
                    //Берём компонент области случайной провинции
                    provincesData.Value.GetProvinceRandom().Unpack(world.Value, out provinceEntity);
                    pMA = ref pMAPool.Value.Get(provinceEntity);
                }

                //Берём полученную провинцию
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //Заносим провинцию во временный список области
                mAG.tempProvincePEs.Add(pMA.selfPE);

                //Заносим PE области в данные провинции
                pMA.parentMAPE = mA.selfPE;

                //Увеличиваем количество занятых соседних провинций для всех провинций, соседних PC
                //Для каждого соседа
                for (int b = 0; b < pC.neighbourProvincePEs.Length; b++)
                {
                    //Берём сущность соседа
                    pC.neighbourProvincePEs[b].Unpack(world.Value, out int neighbourProvinceEntity);

                    //Увеличиваем количество занятых соседних провинций для него
                    ProvinceMapAreaAddNeighbourWithPMA(
                        neighbourProvinceEntity,
                        mA.selfPE);
                }
            }

            //Создаём счётчик ошибок
            int errorCount = 0;

            //Пока существуют области, имеющие свободных соседей
            while (mAWithFreeNeighboursFilter.Value.GetEntitiesCount() > 0)
            {
                //Определяем, сколько провинций было добавлено к областям в этом цикле
                int addedProvincesCount = 0;

                //Для каждой области, имеющей свободных соседей
                foreach (int mAEntity in mAWithFreeNeighboursFilter.Value)
                {
                    //Берём область
                    ref CMapArea currentMA = ref mAPool.Value.Get(mAEntity);
                    ref CTMapAreaGeneration currentMAG = ref mAGPool.Value.Get(mAEntity);

                    //Берём сущность случайной провинции в области
                    currentMAG.tempProvincePEs[Random.Range(0, currentMAG.tempProvincePEs.Count)].Unpack(world.Value, out int currentFreeProvinceEntity);

                    //Создаём счётчик ошибок
                    int provinceErrorCount = 0;
                    //И определяем максимальное количество ошибок
                    int maxProvinceErrorCount = currentMAG.tempProvincePEs.Count * currentMAG.tempProvincePEs.Count;

                    //Пока полученная провинция не будет иметь свободных соседей
                    while (withoutFreeNeighboursPool.Value.Has(currentFreeProvinceEntity) == true)
                    {
                        //Увеличиваем счётчик ошибок
                        provinceErrorCount++;

                        //Если счётчик ошибок больше максимального значения
                        if (provinceErrorCount > maxProvinceErrorCount)
                        {
                            break;
                        }

                        //Берём сущность случайной провинции в области
                        currentMAG.tempProvincePEs[Random.Range(0, currentMAG.tempProvincePEs.Count)].Unpack(world.Value, out currentFreeProvinceEntity);
                    }

                    //Если счётчик ошибок меньше максимального значения
                    if (provinceErrorCount <= maxProvinceErrorCount)
                    {
                        //Берём полученную провинцию
                        ref CProvinceCore currentFreePC = ref pCPool.Value.Get(currentFreeProvinceEntity);
                        ref CTProvinceMapArea currentFreePMA = ref pMAPool.Value.Get(currentFreeProvinceEntity);

                        //Берём компонент области случайной соседней провинции
                        currentFreePC.neighbourProvincePEs[Random.Range(0, currentFreePC.neighbourProvincePEs.Length)]
                            .Unpack(world.Value, out int neighbourFreeProvinceEntity);
                        ref CTProvinceMapArea neighbourFreePMA = ref pMAPool.Value.Get(neighbourFreeProvinceEntity);

                        //Создаём счётчик ошибок
                        int neighbourErrorCount = 0;
                        //И определяем максимальное количество ошибок
                        int maxNeighbourErrorCount = currentFreePC.neighbourProvincePEs.Length
                            * (currentFreePC.neighbourProvincePEs.Length - currentFreePMA.neighboursWithMACount);

                        //Пока полученная провинция принадлежит какой-либо области
                        while (neighbourFreePMA.parentMAPE.Unpack(world.Value, out int neighboutParentMAEntity))
                        {
                            //Увеличиваем счётчик ошибок
                            neighbourErrorCount++;

                            //Если счётчик ошибок больше максимального значения
                            if (neighbourErrorCount > maxNeighbourErrorCount)
                            {
                                break;
                            }

                            //Берём компонент области случайной соседней провинции
                            currentFreePC.neighbourProvincePEs[Random.Range(0, currentFreePC.neighbourProvincePEs.Length)]
                                .Unpack(world.Value, out neighbourFreeProvinceEntity);
                            neighbourFreePMA = ref pMAPool.Value.Get(neighbourFreeProvinceEntity);
                        }

                        //Если счётчик ошибок меньше максимального значения
                        if (neighbourErrorCount <= maxNeighbourErrorCount)
                        {
                            //Берём полученную соседнюю провинцию
                            ref CProvinceCore neighbourFreePC = ref pCPool.Value.Get(neighbourFreeProvinceEntity);

                            //Заносим его во временный список области
                            currentMAG.tempProvincePEs.Add(neighbourFreePC.selfPE);

                            //Заносим PE области в данные провинции
                            neighbourFreePMA.parentMAPE = currentMA.selfPE;

                            //Увеличиваем количество занятых соседних провинций для всех провинций, соседних neighbourFreePC
                            //Для каждого соседа
                            for (int a = 0; a < neighbourFreePC.neighbourProvincePEs.Length; a++)
                            {
                                //Берём сущность соседа
                                neighbourFreePC.neighbourProvincePEs[a].Unpack(world.Value, out int neighbourNeighbourProvinceEntity);

                                //Увеличиваем количество занятых соседних провинций для него
                                ProvinceMapAreaAddNeighbourWithPMA(
                                    neighbourNeighbourProvinceEntity,
                                    currentMA.selfPE);
                            }

                            addedProvincesCount++;

                            //Если провинция имеет компонент отсутствия соседей, а область не имеет
                            if (withoutFreeNeighboursPool.Value.Has(neighbourFreeProvinceEntity) == true
                                && withoutFreeNeighboursPool.Value.Has(mAEntity) == false)
                            {
                                //Проводим проверку области
                                MapAreaCheckProvincesWithoutFreeNeighbours(mAEntity);
                            }
                        }
                    }
                }

                //Если за цикл не было добавлено ни одной провинции
                if (addedProvincesCount == 0)
                {
                    //Увеличиваем счётчик ошибок
                    errorCount++;
                }

                //Если счётчик ошибок достиг предела
                if (errorCount >= mapAreaCount)
                {
                    //Выводим ошибку и прерываем цикл
                    Debug.LogWarning("Невозможно дальнейшее расширение областей карты!");

                    break;
                }
            }
        }

        void MapAreaSetNeighbours()
        {
            //Для каждой провинции
            foreach (int provinceEntity in provinceMAFilter.Value)
            {
                //Берём компонент области провинции
                ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

                //Удаляем из списка соседних областей область, к которой провинция принадлежит
                pMA.neighbourMAPEs.Remove(pMA.parentMAPE);
            }

            //Для каждой области карты
            foreach (int mAEntity in mAGFilter.Value)
            {
                //Берём область
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);
                ref CTMapAreaGeneration mAG = ref mAGPool.Value.Get(mAEntity);

                //Для каждой провинции области
                for (int a = 0; a < mAG.tempProvincePEs.Count; a++)
                {
                    //Берём компонент области провинции
                    mAG.tempProvincePEs[a].Unpack(world.Value, out int provinceEntity);
                    ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

                    //Для каждой соседней провинции области
                    for (int b = 0; b < pMA.neighbourMAPEs.Count; b++)
                    {
                        //Если такой области нет в списке соседних областей текущей области
                        if (mAG.tempNeighbourMAPEs.Contains(pMA.neighbourMAPEs[b]) == false)
                        {
                            //Заносим её PE в список
                            mAG.tempNeighbourMAPEs.Add(pMA.neighbourMAPEs[b]);
                        }
                    }
                }
            }
        }

        void MapAreaAddProvinceWithoutFreeNeighbours(
        int mAEntity)
        {
            //Берём область карты
            ref CTMapAreaGeneration mAG = ref mAGPool.Value.Get(mAEntity);

            //Если количество провинций без свободных соседей меньше количества провинций
            if (mAG.provinceWithoutFreeNeighboursCount < mAG.tempProvincePEs.Count)
            {
                //Увеличиваем количество провинций без свободных соседей
                mAG.provinceWithoutFreeNeighboursCount++;

                //Если количество провинций без свободных соседей равно количеству провинций
                if (mAG.provinceWithoutFreeNeighboursCount >= mAG.tempProvincePEs.Count)
                {
                    //Назначаем области компонент отсутствия соседей
                    ref CTWithoutFreeNeighbours mAWFN = ref withoutFreeNeighboursPool.Value.Add(mAEntity);
                }
            }
        }

        void MapAreaCheckProvincesWithoutFreeNeighbours(
            int mAEntity)
        {
            //Берём область карты
            ref CTMapAreaGeneration mAG = ref mAGPool.Value.Get(mAEntity);

            //Подсчитываем количество провинций, не имеющих свободных соседей
            int rWFNCount = 0;

            //Для каждой провинции в области
            for (int a = 0; a < mAG.tempProvincePEs.Count; a++)
            {
                //Берём сущность провинции
                mAG.tempProvincePEs[a].Unpack(world.Value, out int provinceEntity);

                //Если провинция имеет компонент отсутствия соседей
                if (withoutFreeNeighboursPool.Value.Has(provinceEntity) == true)
                {
                    //Увеличиваем счётчик
                    rWFNCount++;
                }
            }

            //Обновляем счётчик в данных области
            mAG.provinceWithoutFreeNeighboursCount = rWFNCount;

            //Если количество провинций без свободных соседей равно количеству провинций
            if (mAG.provinceWithoutFreeNeighboursCount >= mAG.tempProvincePEs.Count)
            {
                //Назначаем области компонент отсутствия соседей
                ref CTWithoutFreeNeighbours mAWFN = ref withoutFreeNeighboursPool.Value.Add(mAEntity);
            }
        }

        void ProvinceMapAreaAddNeighbourWithPMA(
            int provinceEntity,
            EcsPackedEntity neighbourMAPE)
        {
            //Берём провинцию
            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
            ref CTProvinceMapArea pMA = ref pMAPool.Value.Get(provinceEntity);

            //Если количество соседей, принадлежащих областям, меньше количества соседей
            if(pMA.neighboursWithMACount < pC.neighbourProvincePEs.Length)
            {
                //Увеличиваем количество соседей, принадлежащих областям
                pMA.neighboursWithMACount++;

                //Заносим новую соседнюю область в список соседних областей провинции
                pMA.neighbourMAPEs.Add(neighbourMAPE);

                //Если количество соседей, принадлежащих областям, равно количеству соседей
                if (pMA.neighboursWithMACount >= pC.neighbourProvincePEs.Length)
                {
                    //Назначаем провинции компонент отсутствия свободных соседей
                    ref CTWithoutFreeNeighbours rWFN = ref withoutFreeNeighboursPool.Value.Add(provinceEntity);

                    //Если провинция принадлежит области
                    if (pMA.parentMAPE.Unpack(world.Value, out int parentMAEntity))
                    {
                        //То увеличиваем количество провинций без свободных соседей для этой области
                        MapAreaAddProvinceWithoutFreeNeighbours(parentMAEntity);
                    }
                }
            }
        }
    }
}