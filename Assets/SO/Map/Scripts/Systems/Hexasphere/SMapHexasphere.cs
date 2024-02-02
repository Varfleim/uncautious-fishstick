
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.UI;
using SO.Map.Events;

namespace SO.Map.Hexasphere
{
    public class SMapHexasphere : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;


        //События карты
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //Данные
        readonly EcsCustomInject<SceneData> sceneData = default;
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

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

            //Отмечаем, что требуется обновить регионы, цвета, UV-координаты и массивы текстур
            mapGenerationData.Value.isMaterialUpdated = true;
            mapGenerationData.Value.isRegionUpdated = true;
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
            mapGenerationData.Value.fleetRegionHighlightMaterial.shaderKeywords = null;//new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.fleetRegionHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            mapGenerationData.Value.hoverRegionHighlightMaterial.shaderKeywords = null;//= new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.hoverRegionHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            mapGenerationData.Value.currentRegionHighlightMaterial.shaderKeywords = null;//= new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.currentRegionHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            //Указываем, что нужно пересчитать все матрицы пути
            regionsData.Value.needRefreshRouteMatrix = true;
            for (int a = 0; a < regionsData.Value.needRefreshPathMatrix.Length; a++)
            {
                regionsData.Value.needRefreshPathMatrix[a] = true;
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

            //Создаём регионы
            //Берём индекс первого региона
            int regionIndex = 0;
            //Обнуляем флаг точек
            DHexaspherePoint.flag = 0;

            //Определяем размер массива регионов
            regionsData.Value.regionPEs = new EcsPackedEntity[meshPointCount];

            //Создаём родительский объект для GO регионов
            Transform regionsRoot = HexasphereCreateGOAndParent(
                sceneData.Value.coreObject,
                MapGenerationData.regionsRootName).transform;

            //Для каждой вершины в словаре
            foreach (DHexaspherePoint point in mapGenerationData.Value.points.Values)
            {
                //Создаём регион
                RegionCreate(
                    regionsRoot,
                    point,
                    regionIndex);

                //Увеличиваем индекс
                regionIndex++;
            }

            //Для каждого региона
            for (int a = 0; a < regionsData.Value.regionPEs.Length; a++)
            {
                //Берём RHS и RC
                regionsData.Value.regionPEs[a].Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //Рассчитываем соседей региона

                //Очищаем временный список соседей
                CRegionCore.tempNeighbours.Clear();
                //Для каждого треугольника в данных центра региона
                for (int b = 0; b < rHS.centerPoint.triangleCount; b++)
                {
                    //Берём треугольник
                    DHexasphereTriangle triangle = rHS.centerPoint.triangles[b];

                    //Для каждой вершины треугольника
                    for (int c = 0; c < 3; c++)
                    {
                        //Берём регион вершины
                        triangle.points[c].regionPE.Unpack(world.Value, out int neighbourRegionEntity);
                        ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                        //Если это не текущий регион и временный список ещё не содержит его
                        if (neighbourRC.Index != rC.Index && CRegionCore.tempNeighbours.Contains(neighbourRC.selfPE) == false)
                        {
                            //Заносим PE соседа в список
                            CRegionCore.tempNeighbours.Add(neighbourRC.selfPE);
                        }
                    }
                }

                //Заносим соседей в массив региона
                rC.neighbourRegionPEs = CRegionCore.tempNeighbours.ToArray();
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

        void RegionCreate(
            Transform parentGO,
            DHexaspherePoint centerPoint,
            int regionIndex)
        {
            //Создаём новую сущность и назначаем ей компонент RHS и RC
            int regionEntity = world.Value.NewEntity();
            ref CRegionHexasphere currentRHS = ref rHSPool.Value.Add(regionEntity);
            ref CRegionCore currentRC = ref rCPool.Value.Add(regionEntity);

            //Заполняем основные данные RHS
            currentRHS = new(
                world.Value.PackEntity(regionEntity),
                centerPoint);

            //Заполняем основные данные RC
            currentRC = new(
                currentRHS.selfPE, regionIndex,
                currentRHS.centerPoint.ProjectedVector3);

            //Заносим регион в массив регионов
            regionsData.Value.regionPEs[regionIndex] = currentRHS.selfPE;

            //Создаём GO региона и меш
            GORegion regionGO = Object.Instantiate(uIData.Value.regionPrefab);

            //Заполняем основные данные GO
            regionGO.gameObject.layer = parentGO.gameObject.layer;
            regionGO.transform.SetParent(parentGO, false);
            regionGO.transform.localPosition = Vector3.zero;
            regionGO.transform.localScale = Vector3.one;
            regionGO.transform.localRotation = Quaternion.Euler(0, 0, 0);

            //Даём региону ссылку на свой GO
            currentRHS.selfObject = regionGO.gameObject;

            //Создаём рендереры региона
            currentRHS.fleetRenderer = RegionCreateRenderer(ref currentRHS, regionGO.fleetRenderer);
            currentRHS.hoverRenderer = RegionCreateRenderer(ref currentRHS, regionGO.hoverRenderer);
            currentRHS.currentRenderer = RegionCreateRenderer(ref currentRHS, regionGO.currentRenderer);
        }

        MeshRenderer RegionCreateRenderer(
            ref CRegionHexasphere rHS, MeshRenderer renderer)
        {
            //Создаём новые мешфильтр и меш
            MeshFilter meshFilter = renderer.gameObject.AddComponent<MeshFilter>();
            Mesh mesh = new();
            mesh.hideFlags = HideFlags.DontSave;

            //Определяем высоту рендерера
            float extrusionAmount = rHS.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier;

            //Рассчитываем выдавленные вершины региона
            Vector3[] extrudedVertices = new Vector3[rHS.vertexPoints.Length];
            //Для каждой вершины
            for (int a = 0; a < rHS.vertices.Length; a++)
            {
                //Рассчитываем положение выдавленной вершины
                extrudedVertices[a] = rHS.vertices[a] * (1f + extrusionAmount);
            }
            //Назначаем вершины мешу
            mesh.vertices = extrudedVertices;

            //Если у региона шесть вершин
            if (rHS.vertices.Length == 6)
            {
                mesh.SetIndices(
                    mapGenerationData.Value.hexagonIndices,
                    MeshTopology.Triangles,
                    0,
                    false);
                mesh.uv = mapGenerationData.Value.hexagonUVs;
            }
            //Иначе
            else
            {
                mesh.SetIndices(
                    mapGenerationData.Value.pentagonIndices,
                    MeshTopology.Triangles,
                    0,
                    false);
                mesh.uv = mapGenerationData.Value.pentagonUVs;
            }

            //Рассчитываем нормали меша
            mesh.normals = rHS.vertices;
            mesh.RecalculateNormals();

            //Назначаем меш рендереру и отключаем визуализацию
            meshFilter.sharedMesh = mesh;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.enabled = false;

            //Возвращаем рендерер
            return renderer;
        }
    }
}