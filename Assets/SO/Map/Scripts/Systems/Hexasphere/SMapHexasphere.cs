
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.UI;
using SO.Map.Generation;
using SO.Map.Economy;
using SO.Map.Province;

namespace SO.Map.Hexasphere
{
    public class SMapHexasphere : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;
        readonly EcsPoolInject<CProvinceEconomy> pEPool = default;


        //События карты
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //Данные
        readonly EcsCustomInject<SceneData> sceneData = default;
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<ProvincesData> provincesData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого запроса генерации карты
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //Берём запрос
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //Создаём карту
                HexasphereCreate(ref requestComp);
            }
        }

        void HexasphereCreate(
            ref RMapGenerating requestComp)
        {
            //Инициализируем данные карты
            HexasphereDataInitialization(ref requestComp);

            //Генерируем простую гексасферу
            HexasphereGenerate();

            //Отмечаем, что требуется стартовое обновление
            mapGenerationData.Value.isInitializationUpdate = true;

            //Отмечаем, что требуется обновить провинции, цвета, UV-координаты и массивы текстур
            mapGenerationData.Value.isMaterialUpdated = true;
            mapGenerationData.Value.isProvinceUpdated = true;
            mapGenerationData.Value.isColorUpdated = true;
            mapGenerationData.Value.isUVUpdatedFast = true;
        }

        void HexasphereDataInitialization(
            ref RMapGenerating requestComp)
        {
            //Ограничиваем число подразделений
            mapGenerationData.Value.subdivisions = Mathf.Max(1, requestComp.subdivisions);

            //Указываем множитель выдавливания
            MapGenerationData.ExtrudeMultiplier = mapGenerationData.Value.extrudeMultiplier;

            //Указываем объект гексасферы
            SceneData.HexashpereGO = sceneData.Value.hexashpereGO;

            //Задаём настройки шейдеров
            mapGenerationData.Value.fleetProvinceHighlightMaterial.shaderKeywords = null;//new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.fleetProvinceHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            mapGenerationData.Value.hoverProvinceHighlightMaterial.shaderKeywords = null;//= new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.hoverProvinceHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            mapGenerationData.Value.currentProvinceHighlightMaterial.shaderKeywords = null;//= new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.currentProvinceHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            //Указываем, что нужно пересчитать все матрицы пути
            provincesData.Value.needRefreshRouteMatrix = true;
            for (int a = 0; a < provincesData.Value.needRefreshPathMatrix.Length; a++)
            {
                provincesData.Value.needRefreshPathMatrix[a] = true;
            }
        }

        void HexasphereGenerate()
        {
            //Определяем вершины изначального икосаэдра
            DHexaspherePoint[] corners = new DHexaspherePoint[]
            {
                new DHexaspherePoint(1, MapGenerationData.PHI, 0),
                new DHexaspherePoint(-1, MapGenerationData.PHI, 0),
                new DHexaspherePoint(1, -MapGenerationData.PHI, 0),
                new DHexaspherePoint(-1, -MapGenerationData.PHI, 0),
                new DHexaspherePoint(0, 1, MapGenerationData.PHI),
                new DHexaspherePoint(0, -1, MapGenerationData.PHI),
                new DHexaspherePoint(0, 1, -MapGenerationData.PHI),
                new DHexaspherePoint(0, -1, -MapGenerationData.PHI),
                new DHexaspherePoint(MapGenerationData.PHI, 0, 1),
                new DHexaspherePoint(-MapGenerationData.PHI, 0, 1),
                new DHexaspherePoint(MapGenerationData.PHI, 0, -1),
                new DHexaspherePoint(-MapGenerationData.PHI, 0, -1)
            };

            //Определяем треугольники изначального икосаэдра
            DHexasphereTriangle[] triangles = new DHexasphereTriangle[]
            {
                new DHexasphereTriangle(corners [0], corners [1], corners [4], false),
                new DHexasphereTriangle(corners [1], corners [9], corners [4], false),
                new DHexasphereTriangle(corners [4], corners [9], corners [5], false),
                new DHexasphereTriangle(corners [5], corners [9], corners [3], false),
                new DHexasphereTriangle(corners [2], corners [3], corners [7], false),
                new DHexasphereTriangle(corners [3], corners [2], corners [5], false),
                new DHexasphereTriangle(corners [7], corners [10], corners [2], false),
                new DHexasphereTriangle(corners [0], corners [8], corners [10], false),
                new DHexasphereTriangle(corners [0], corners [4], corners [8], false),
                new DHexasphereTriangle(corners [8], corners [2], corners [10], false),
                new DHexasphereTriangle(corners [8], corners [4], corners [5], false),
                new DHexasphereTriangle(corners [8], corners [5], corners [2], false),
                new DHexasphereTriangle(corners [1], corners [0], corners [6], false),
                new DHexasphereTriangle(corners [11], corners [1], corners [6], false),
                new DHexasphereTriangle(corners [3], corners [9], corners [11], false),
                new DHexasphereTriangle(corners [6], corners [10], corners [7], false),
                new DHexasphereTriangle(corners [3], corners [11], corners [7], false),
                new DHexasphereTriangle(corners [11], corners [6], corners [7], false),
                new DHexasphereTriangle(corners [6], corners [0], corners [10], false),
                new DHexasphereTriangle(corners [9], corners [1], corners [11], false)
            };

            //Очищаем словарь точек
            mapGenerationData.Value.points.Clear();

            //Заносим вершины икосаэдра в словарь
            for (int a = 0; a < corners.Length; a++)
            {
                mapGenerationData.Value.points[corners[a]] = corners[a];
            }

            //Создаём список точек нижнего ребра треугольника
            List<DHexaspherePoint> bottom = new();
            //Определяем количество треугольников
            int triangleCount = triangles.Length;
            //Для каждого треугольника
            for (int f = 0; f < triangleCount; f++)
            {
                //Создаём пустой список точек
                List<DHexaspherePoint> previous = null;

                //Берём первую вершину треугольника
                DHexaspherePoint point0 = triangles[f].points[0];

                //Очищаем временный список точек
                bottom.Clear();

                //Заносим в список первую вершину треугольника
                bottom.Add(point0);

                //Создаём список точек левого ребра треугольника
                List<DHexaspherePoint> left = PointSubdivide(
                    point0, triangles[f].points[1],
                    mapGenerationData.Value.subdivisions);
                //Создаём список точек правого ребра треугольника
                List<DHexaspherePoint> right = PointSubdivide(
                    point0, triangles[f].points[2],
                    mapGenerationData.Value.subdivisions);

                //Для каждого подразделения
                for (int i = 1; i <= mapGenerationData.Value.subdivisions; i++)
                {
                    //Переносим список точек нижнего ребра
                    previous = bottom;

                    //Подразделяем перемычку с левого ребра до правого
                    bottom = PointSubdivide(
                        left[i], right[i],
                        i);

                    //Создаём новый треугольник
                    new DHexasphereTriangle(previous[0], bottom[0], bottom[1]);

                    //Для каждого ...
                    for (int j = 1; j < i; j++)
                    {
                        //Создаём два новых треугольника
                        new DHexasphereTriangle(previous[j], bottom[j], bottom[j + 1]);
                        new DHexasphereTriangle(previous[j - 1], previous[j], bottom[j]);
                    }
                }
            }

            //Определяем количество точек
            int meshPointCount = mapGenerationData.Value.points.Values.Count;

            //Создаём провинции
            //Берём индекс первой
            int provinceIndex = 0;
            //Обнуляем флаг точек
            DHexaspherePoint.flag = 0;

            //Определяем размер массива провинций
            provincesData.Value.provincePEs = new EcsPackedEntity[meshPointCount];

            //Создаём родительский объект для GO провинций
            Transform provincesRoot = HexasphereCreateGOAndParent(
                sceneData.Value.coreObject,
                MapGenerationData.provincesRootName).transform;

            //Для каждой вершины в словаре
            foreach (DHexaspherePoint point in mapGenerationData.Value.points.Values)
            {
                //Создаём провинцию
                ProvinceCreate(
                    provincesRoot,
                    point,
                    provinceIndex);

                //Увеличиваем индекс
                provinceIndex++;
            }

            //Для каждой провинции
            for (int a = 0; a < provincesData.Value.provincePEs.Length; a++)
            {
                //Берём PHS и PC
                provincesData.Value.provincePEs[a].Unpack(world.Value, out int provinceEntity);
                ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //Рассчитываем соседей

                //Очищаем временный список соседей
                CProvinceCore.tempNeighbours.Clear();
                //Для каждого треугольника в данных центра провинции
                for (int b = 0; b < pHS.centerPoint.triangleCount; b++)
                {
                    //Берём треугольник
                    DHexasphereTriangle triangle = pHS.centerPoint.triangles[b];

                    //Для каждой вершины треугольника
                    for (int c = 0; c < 3; c++)
                    {
                        //Берём провинцию вершины
                        triangle.points[c].provincePE.Unpack(world.Value, out int neighbourProvinceEntity);
                        ref CProvinceCore neighbourPC = ref pCPool.Value.Get(neighbourProvinceEntity);

                        //Если это не текущая провинция и временный список ещё не содержит её
                        if (neighbourPC.Index != pC.Index && CProvinceCore.tempNeighbours.Contains(neighbourPC.selfPE) == false)
                        {
                            //Заносим PE соседа в список
                            CProvinceCore.tempNeighbours.Add(neighbourPC.selfPE);
                        }
                    }
                }

                //Заносим соседей в массив провинции
                pC.neighbourProvincePEs = CProvinceCore.tempNeighbours.ToArray();
            }
        }

        GameObject HexasphereCreateGOAndParent(
            Transform parent,
            string name)
        {
            //Создаём GO
            GameObject gO = new(name);

            //Заполняем основные данные GO
            gO.layer = parent.gameObject.layer;
            gO.transform.SetParent(parent, false);
            gO.transform.localPosition = Vector3.zero;
            gO.transform.localScale = Vector3.one;
            gO.transform.localRotation = Quaternion.Euler(0, 0, 0);

            return gO;
        }

        List<DHexaspherePoint> PointSubdivide(
            DHexaspherePoint startPoint, DHexaspherePoint endPoint,
            int count)
        {
            //Создаём список точек, определяющих сегменты грани, и заносим в него текущую вершину
            List<DHexaspherePoint> segments = new(count + 1);
            segments.Add(startPoint);

            //Рассчитываем координаты точек
            double dx = endPoint.x - startPoint.x;
            double dy = endPoint.y - startPoint.y;
            double dz = endPoint.z - startPoint.z;
            double doublex = startPoint.x;
            double doubley = startPoint.y;
            double doublez = startPoint.z;
            double doubleCount = count;

            //Для каждого подразделения
            for (int a = 1; a < count; a++)
            {
                //Создаём новую вершину
                DHexaspherePoint newPoint = new(
                    (float)(doublex + dx * a / doubleCount),
                    (float)(doubley + dy * a / doubleCount),
                    (float)(doublez + dz * a / doubleCount));

                //Проверяем вершину
                newPoint = PointGetCached(newPoint);

                //Заносим вершину в список
                segments.Add(newPoint);
            }

            //Заносим в список конечную вершину
            segments.Add(endPoint);

            //Возвращаем список точеку
            return segments;
        }

        DHexaspherePoint PointGetCached(
            DHexaspherePoint point)
        {
            DHexaspherePoint thePoint;

            //Если запрошенная вершина существует в словаре
            if (mapGenerationData.Value.points.TryGetValue(point, out thePoint))
            {
                //Возвращаем вершину
                return thePoint;
            }
            //Иначе
            else
            {
                //Обновляем вершину в словаре
                mapGenerationData.Value.points[point] = point;

                //Возвращаем вершину
                return point;
            }
        }

        void ProvinceCreate(
            Transform parentGO,
            DHexaspherePoint centerPoint,
            int provinceIndex)
        {
            //Создаём новую сущность и назначаем ей компоненты PHS, PC, PE
            int provinceEntity = world.Value.NewEntity();
            ref CProvinceHexasphere currentPHS = ref pHSPool.Value.Add(provinceEntity);
            ref CProvinceCore currentPC = ref pCPool.Value.Add(provinceEntity);
            ref CProvinceEconomy currentPE = ref pEPool.Value.Add(provinceEntity);

            //Заполняем основные данные PHS
            currentPHS = new(
                world.Value.PackEntity(provinceEntity),
                centerPoint);

            //Заполняем основные данные PC
            currentPC = new(
                currentPHS.selfPE, provinceIndex,
                currentPHS.centerPoint.ProjectedVector3);

            //Заполняем основные данные PE
            currentPE = new(
                currentPHS.selfPE);

            //Заносим провинцию в массив провинций
            provincesData.Value.provincePEs[provinceIndex] = currentPHS.selfPE;

            //Создаём GO провинции и меш
            GOProvince provinceGO = Object.Instantiate(uIData.Value.provincePrefab);

            //Заполняем основные данные GO
            provinceGO.gameObject.layer = parentGO.gameObject.layer;
            provinceGO.transform.SetParent(parentGO, false);
            provinceGO.transform.localPosition = Vector3.zero;
            provinceGO.transform.localScale = Vector3.one;
            provinceGO.transform.localRotation = Quaternion.Euler(0, 0, 0);

            //Даём провинции ссылку на свой GO
            currentPHS.selfObject = provinceGO.gameObject;
        }
    }
}