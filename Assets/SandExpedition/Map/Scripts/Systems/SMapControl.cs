
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SCM.UI;

namespace SCM.Map
{
    public class SMapControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //Карта
        readonly EcsPoolInject<CRegion> regionPool = default;

        //Данные
        readonly EcsCustomInject<SceneData> sceneData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //Обновляем яркость материалов подсветки
            mapGenerationData.Value.fleetRegionHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));
            mapGenerationData.Value.hoverRegionHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));
            mapGenerationData.Value.currentRegionHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));

            //Если требуется обновление материалов
            if (mapGenerationData.Value.isMaterialUpdated == true)
            {
                //Обновляем свойства материалов
                MapMaterialPropertiesUpdate(mapGenerationData.Value.isInitializationUpdate);

                mapGenerationData.Value.isMaterialUpdated = false;
                mapGenerationData.Value.isRegionUpdated = false;
                mapGenerationData.Value.isInitializationUpdate = false;
            }

            //Если требуется обновление массива текстур
            if (mapGenerationData.Value.isTextureArrayUpdated == true)
            {
                //Обновляем материалы
                MapUpdateShadedMaterials(mapGenerationData.Value.isInitializationUpdate);

                mapGenerationData.Value.isInitializationUpdate = false;
                mapGenerationData.Value.isTextureArrayUpdated = false;
            }
            //Иначе, если требуется обновление UV-координат или цветов
            else if (mapGenerationData.Value.isUVUpdatedFast == true
                || mapGenerationData.Value.isColorUpdated == true)
            {
                //Обновляем материалы упрощённо
                MapUpdateShadedMaterialsFast();

                mapGenerationData.Value.isColorUpdated = false;
                mapGenerationData.Value.isUVUpdatedFast = false;
            }
        }

        void MapMaterialPropertiesUpdate(
            bool isInitializationUpdate = false)
        {
            //Если нужно обновить регионы
            if (mapGenerationData.Value.isRegionUpdated == true)
            {
                //Обновляем регионы
                MapRebuildTiles();

                //Отмечаем, что регионы были обновлены
                mapGenerationData.Value.isRegionUpdated = false;
            }

            //Обновляем материалы регионов и тени
            MapUpdateShadedMaterials(isInitializationUpdate);
            MapUpdateMeshRenderersShadowSupport();

            //Обновляем материал регионов
            mapGenerationData.Value.regionMaterial.SetFloat("_GradientIntensity", 1f - mapGenerationData.Value.gradientIntensity);
            mapGenerationData.Value.regionMaterial.SetFloat("_ExtrusionMultiplier", MapGenerationData.ExtrudeMultiplier);
            mapGenerationData.Value.regionMaterial.SetColor("_Color", mapGenerationData.Value.tileTintColor);
            mapGenerationData.Value.regionMaterial.SetColor("_AmbientColor", mapGenerationData.Value.ambientColor);
            mapGenerationData.Value.regionMaterial.SetFloat("_MinimumLight", mapGenerationData.Value.minimumLight);

            //Обновляем размер коллайдера
            sceneData.Value.hexashpereCollider.radius = 0.5f * (1.0f + MapGenerationData.ExtrudeMultiplier);

            //Обновляем свет
            MapUpdateLightingMode();

            //Обновляем скос
            MapUpdateBevel();
        }

        void MapRebuildTiles()
        {
            //Удаляем заглушки
            GameObject placeholder = GameObject.Find(MapGenerationData.shaderframeName);
            if (placeholder != null)
            {
                GameObject.DestroyImmediate(placeholder);
            }

            //Создаём меш тайлов
            MapBuildTiles();
        }

        void MapBuildTiles()
        {
            //Берём индекс первого чанка
            int chunkIndex = 0;

            //Создаём списки для вершин, индексов и UV-координат
            List<Vector3> vertexChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.verticesShaded[chunkIndex]);
            List<int> indicesChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.indicesShaded[chunkIndex]);
            List<Vector4> uv2Chunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.uv2Shaded[chunkIndex]);

            //Создаём счётчик вершин
            int verticesCount = 0;

            //Определяем количество регионов
            int tileCount = regionsData.Value.regionPEs.Length;

            //Создаём массивы индексов гексов и пентагонов
            int[] hexIndices = mapGenerationData.Value.hexagonIndicesExtruded;
            int[] pentIndices = mapGenerationData.Value.pentagonIndicesExtruded;

            //Для каждого региона
            for (int k = 0; k < tileCount; k++)
            {
                //Берём компонент региона
                regionsData.Value.regionPEs[k].Unpack(world.Value, out int regionEntity);
                ref CRegion region = ref regionPool.Value.Get(regionEntity);

                //Если количество вершин больше максимального количества вершин на чанк
                if (verticesCount > MapGenerationData.maxVertexCountPerChunk)
                {
                    //Увеличиваем индекс чанка
                    chunkIndex++;

                    //Обновляем списки вершин, индексов и UV-координат
                    vertexChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.verticesShaded[chunkIndex]);
                    indicesChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.indicesShaded[chunkIndex]);
                    uv2Chunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.uv2Shaded[chunkIndex]);

                    //Обнуляем счётчик вершин
                    verticesCount = 0;
                }

                //Берём массив вершин региона
                DHexaspherePoint[] tileVertices = region.vertexPoints;

                //Определяем количество вершин
                int tileVerticesCount = tileVertices.Length;

                //Создаём структуру для UV4-координат
                Vector4 gpos = Vector4.zero;

                //Для каждой вершины
                for (int b = 0; b < tileVerticesCount; b++)
                {
                    //Берём вершину региона
                    DHexaspherePoint point = tileVertices[b];

                    //Берём координату центра вершины и заносим её в список вершин
                    Vector3 vertex = point.ProjectedVector3;
                    vertexChunk.Add(vertex);

                    //Обновляем gpos
                    gpos.x += vertex.x;
                    gpos.y += vertex.y;
                    gpos.z += vertex.z;
                }

                //Корректируем gpos
                gpos.x /= tileVerticesCount;
                gpos.y /= tileVerticesCount;
                gpos.z /= tileVerticesCount;

                //Создаём массив индексов
                int[] indicesArray;

                //Если число вершин региона равно шести
                if (tileVerticesCount == 6)
                {
                    //Заносим вершины основания гекса в список
                    vertexChunk.Add((tileVertices[1].ProjectedVector3 + tileVertices[5].ProjectedVector3) * 0.5f);
                    vertexChunk.Add((tileVertices[2].ProjectedVector3 + tileVertices[4].ProjectedVector3) * 0.5f);

                    //Обновляем счётчик вершин
                    tileVerticesCount += 2;

                    //Переносим индексы 
                    indicesArray = hexIndices;
                }
                //Иначе это пентагон
                else
                {
                    //Заносим вершины основания пентагона в список
                    vertexChunk.Add((tileVertices[1].ProjectedVector3 + tileVertices[4].ProjectedVector3) * 0.5f);
                    vertexChunk.Add((tileVertices[2].ProjectedVector3 + tileVertices[4].ProjectedVector3) * 0.5f);

                    //Обновляем счётчик вершин
                    tileVerticesCount += 2;

                    //Обнуляем bevel для пентагонов
                    gpos.w = 1.0f;

                    //Переносим индексы 
                    indicesArray = pentIndices;
                }

                //Для каждой вершины
                for (int b = 0; b < tileVerticesCount; b++)
                {
                    //Заносим gpos в список UV-координат
                    uv2Chunk.Add(gpos);
                }

                //Для каждого индекса
                for (int b = 0; b < indicesArray.Length; b++)
                {
                    //Заносим индекс вершины в список индексов
                    indicesChunk.Add(verticesCount + indicesArray[b]);
                }

                //Обновляем счётчик вершин
                verticesCount += tileVerticesCount;
            }

            //Создаём родительский GO
            GameObject partsRoot = MapCreateGOAndParent(
                sceneData.Value.coreObject,
                MapGenerationData.shaderframeName);

            //Для каждого чанка
            for (int k = 0; k <= chunkIndex; k++)
            {
                //Создаём объект тайлов
                GameObject go = MapCreateGOAndParent(
                    partsRoot.transform,
                    MapGenerationData.shaderframeGOName);

                //Назначаем ему компонент мешфильтра и заполняем данные
                MeshFilter mf = go.AddComponent<MeshFilter>();
                mapGenerationData.Value.shadedMFs[k] = mf;
                if (mapGenerationData.Value.shadedMeshes[k] == null)
                {
                    mapGenerationData.Value.shadedMeshes[k] = new Mesh();
                    mapGenerationData.Value.shadedMeshes[k].hideFlags = HideFlags.DontSave;
                }
                mapGenerationData.Value.shadedMeshes[k].Clear();
                mapGenerationData.Value.shadedMeshes[k].SetVertices(mapGenerationData.Value.verticesShaded[k]);
                mapGenerationData.Value.shadedMeshes[k].SetTriangles(mapGenerationData.Value.indicesShaded[k], 0);
                mapGenerationData.Value.shadedMeshes[k].SetUVs(1, mapGenerationData.Value.uv2Shaded[k]);
                mf.sharedMesh = mapGenerationData.Value.shadedMeshes[k];

                //Назначаем ему компонент мешрендерера и заполняем данные
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mapGenerationData.Value.shadedMRs[k] = mr;


                mr.sharedMaterial = mapGenerationData.Value.regionMaterial;
            }
        }

        void MapUpdateShadedMaterials(
            bool isInitializationUpdate = false)
        {
            //Берём индекс первого чанка
            int chunkIndex = 0;

            //Создаём списки вершин и индексов
            List<Vector4> uvChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.uvShaded[chunkIndex]);
            List<Color32> colorChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.colorShaded[chunkIndex]);

            //Если белая текстура не существует, создаём её
            if (mapGenerationData.Value.whiteTex == null)
            {
                mapGenerationData.Value.whiteTex = MapGetCachedSolidTexture(Color.white);
            }

            //Очищаем массив текстур и заносим в него белую текстуру
            mapGenerationData.Value.texArray.Clear();
            mapGenerationData.Value.texArray.Add(mapGenerationData.Value.whiteTex);

            //Создаём счётчик вершин
            int verticesCount = 0;

            //Определяем количество регионов
            int tileCount = regionsData.Value.regionPEs.Length;

            //Определяем стандартный цвет региона
            Color32 color = mapGenerationData.Value.DefaultShadedColor;

            //Для каждого региона
            for (int k = 0; k < tileCount; k++)
            {
                //Берём компонент региона
                regionsData.Value.regionPEs[k].Unpack(world.Value, out int regionEntity);
                ref CRegion region = ref regionPool.Value.Get(regionEntity);

                //Если количество вершин больше максимального количества вершин на чанк
                if (verticesCount > MapGenerationData.maxVertexCountPerChunk)
                {
                    //Увеличиваем индекс чанка
                    chunkIndex++;

                    //Обновляем списки вершин и индексов
                    uvChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.uvShaded[chunkIndex]);
                    colorChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.colorShaded[chunkIndex]);

                    //Обнуляем счётчик вершин
                    verticesCount = 0;
                }

                //Берём массив вершин региона
                DHexaspherePoint[] tileVertices = region.vertexPoints;

                //Определяем количество вершин
                int tileVerticesCount = tileVertices.Length;

                //Создаём массив UV-координат
                Vector2[] uvArray;

                //Если количество вершин равно шести
                if (tileVerticesCount == 6)
                {
                    //Переносим индексы
                    uvArray = mapGenerationData.Value.hexagonUVsExtruded;
                }
                //Иначе это пентагон
                else
                {
                    //Переносим индексы
                    uvArray = mapGenerationData.Value.pentagonUVsExtruded;
                }

                //Помещаем цвет региона или текстуру в массив текстур
                Texture2D tileTexture;

                //Определяем индекс, масштаб и смещение текстуры
                int textureIndex = 0;
                Vector2 textureScale;
                Vector2 textureOffset;

                //Если регион имеет собственный материал, этот материал имеет текстуру и текстура не пуста
                if (region.customMaterial && region.customMaterial.HasProperty(ShaderParameters.MainTex) && region.customMaterial.mainTexture != null)
                {
                    //Берём параметры этой текстуры
                    tileTexture = (Texture2D)region.customMaterial.mainTexture;
                    textureIndex = mapGenerationData.Value.texArray.IndexOf(tileTexture);
                    textureScale = region.customMaterial.mainTextureScale;
                    textureOffset = region.customMaterial.mainTextureOffset;
                }
                //Иначе
                else
                {
                    //Берём параметры стандартной текстуры
                    tileTexture = mapGenerationData.Value.whiteTex;
                    textureScale = Vector2.one;
                    textureOffset = Vector2.zero;
                }

                //Если индекс текстуры меньше нуля
                if (textureIndex < 0)
                {
                    //Заносим текстуру в массив и обновляем индекс
                    mapGenerationData.Value.texArray.Add(tileTexture);
                    textureIndex = mapGenerationData.Value.texArray.Count - 1;
                }

                //Если требуется обновление цветов
                if (mapGenerationData.Value.isColorUpdated == true)
                {
                    //Если регион имеет собственный материал, то берём его цвет
                    if (region.customMaterial != null)
                    {
                        color = region.customMaterial.color;
                    }
                    //Иначе берём стандартный цвет
                    else
                    {
                        color = mapGenerationData.Value.DefaultShadedColor;
                    }
                }

                //Если это обновление на этапе инициализации
                if (isInitializationUpdate == true)
                {
                    //Заполняем индексы UV-координат региона
                    region.uvShadedChunkStart = verticesCount;
                    region.uvShadedChunkIndex = chunkIndex;
                    region.uvShadedChunkLength = uvArray.Length;
                }

                //Для каждых UV-координат в списке
                for (int b = 0; b < uvArray.Length; b++)
                {
                    //Собираем UV4-координаты
                    Vector4 uv4;
                    uv4.x = uvArray[b].x * textureScale.x + textureOffset.x;
                    uv4.y = uvArray[b].y * textureScale.y + textureOffset.y;
                    uv4.z = textureIndex;
                    uv4.w = region.ExtrudeAmount;

                    //Если это не обновление на этапе инициализации
                    if (isInitializationUpdate == false)
                    {
                        //Обновляем координаты в списке
                        uvChunk[verticesCount] = uv4;

                        //Если требуется обновление цветов
                        if (mapGenerationData.Value.isColorUpdated == true)
                        {
                            //Обновляем цвет в списке
                            colorChunk[verticesCount] = color;
                        }
                    }
                    //Иначе
                    else
                    {
                        //Заносим координаты и цвет в списки
                        uvChunk.Add(uv4);
                        colorChunk.Add(color);
                    }
                }

                //Обновляем количество вершин
                verticesCount += uvArray.Length;
            }

            //Для каждого чанка
            for (int k = 0; k <= chunkIndex; k++)
            {
                //Заполняем данные UV-координат
                mapGenerationData.Value.uvShadedDirty[k] = false;
                mapGenerationData.Value.shadedMeshes[k].SetUVs(0, mapGenerationData.Value.uvShaded[k]);

                //Если требуется обновление цветов
                if (mapGenerationData.Value.isColorUpdated == true)
                {
                    mapGenerationData.Value.colorShadedDirty[k] = false;
                    mapGenerationData.Value.shadedMeshes[k].SetColors(mapGenerationData.Value.colorShaded[k]);
                }

                mapGenerationData.Value.shadedMFs[k].sharedMesh = mapGenerationData.Value.shadedMeshes[k];
                mapGenerationData.Value.shadedMRs[k].sharedMaterial = mapGenerationData.Value.regionMaterial;
            }

            //Если требуется обновление массива текстур
            if (mapGenerationData.Value.isTextureArrayUpdated)
            {
                //Определяем количество текстур в массиве
                int textureArrayCount = mapGenerationData.Value.texArray.Count;

                //Определяем размер текстуры
                mapGenerationData.Value.currentTextureSize = 256;

                //Если конечный массив текстур не пуст, удаляем его
                if (mapGenerationData.Value.finalTexArray != null)
                {
                    GameObject.DestroyImmediate(mapGenerationData.Value.finalTexArray);
                }

                //Создаём новый массив текстур
                mapGenerationData.Value.finalTexArray = new(
                    mapGenerationData.Value.currentTextureSize, mapGenerationData.Value.currentTextureSize,
                    textureArrayCount,
                    TextureFormat.ARGB32,
                    true);

                //Для каждой текстуры
                for (int a = 0; a < textureArrayCount; a++)
                {
                    //Если размер текстуры в массиве не соответствует стандартному
                    if (mapGenerationData.Value.texArray[a].width != mapGenerationData.Value.currentTextureSize
                        || mapGenerationData.Value.texArray[a].height != mapGenerationData.Value.currentTextureSize)
                    {
                        //Создаём текстуру и заполняем её данные
                        mapGenerationData.Value.texArray[a] = GameObject.Instantiate(mapGenerationData.Value.texArray[a]);
                        mapGenerationData.Value.texArray[a].hideFlags = HideFlags.DontSave;
                        TextureScaler.Scale(
                            mapGenerationData.Value.texArray[a],
                            mapGenerationData.Value.currentTextureSize, mapGenerationData.Value.currentTextureSize);
                    }
                    //Заносим текстуру в конечный массив
                    mapGenerationData.Value.finalTexArray.SetPixels32(
                        mapGenerationData.Value.texArray[a].GetPixels32(),
                        a);
                }
                //Применяем конечный массив и задаём его как массив текстур для материала
                mapGenerationData.Value.finalTexArray.Apply();
                mapGenerationData.Value.regionMaterial.SetTexture(
                    ShaderParameters.MainTex,
                    mapGenerationData.Value.finalTexArray);

                //Отмечаем, что массив текстур был обновлён
                mapGenerationData.Value.isTextureArrayUpdated = false;
            }

            //Отмечаем, что цвета и UV-координаты были обновлены
            mapGenerationData.Value.isColorUpdated = false;
            mapGenerationData.Value.isUVUpdatedFast = false;

            //Обновляем количество чанков UV-координат
            mapGenerationData.Value.uvChunkCount = chunkIndex + 1;
        }

        void MapUpdateShadedMaterialsFast()
        {
            //Если требуется обновление цветов
            if (mapGenerationData.Value.isColorUpdated == true)
            {
                //Для каждого чанка
                for (int a = 0; a < mapGenerationData.Value.uvChunkCount; a++)
                {
                    //Если цвета "грязны"
                    if (mapGenerationData.Value.colorShadedDirty[a] == true)
                    {
                        //Обновляем записи
                        mapGenerationData.Value.colorShadedDirty[a] = false;
                        mapGenerationData.Value.shadedMeshes[a].SetColors(mapGenerationData.Value.colorShaded[a]);
                    }
                }
            }

            //Если требуется обновление UV-координат
            if (mapGenerationData.Value.isUVUpdatedFast == true)
            {
                //Для каждого чанка
                for (int a = 0; a < mapGenerationData.Value.uvChunkCount; a++)
                {
                    //Если UV-координаты "грязны"
                    if (mapGenerationData.Value.uvShadedDirty[a] == true)
                    {
                        //Обновляем записи
                        mapGenerationData.Value.uvShadedDirty[a] = false;
                        mapGenerationData.Value.shadedMeshes[a].SetUVs(0, mapGenerationData.Value.uvShaded[a]);
                    }
                }
            }
        }

        void MapUpdateLightingMode()
        {
            //Обновляем материалы
            MapUpdateLightingMaterial(mapGenerationData.Value.regionMaterial);
            MapUpdateLightingMaterial(mapGenerationData.Value.regionColoredMaterial);

            mapGenerationData.Value.regionMaterial.EnableKeyword("HEXA_ALPHA");
        }

        void MapUpdateLightingMaterial(
            Material material)
        {
            //Заполняем данные материала
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            if (material.renderQueue >= 3000)
            {
                material.renderQueue -= 2000;
            }
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);

            material.EnableKeyword("HEXA_LIT");
        }

        void MapUpdateBevel()
        {
            //Определяем размер текстуры
            const int textureSize = 256;

            //Если текстура отсутствует или её ширина неверна
            if (mapGenerationData.Value.bevelNormals == null || mapGenerationData.Value.bevelNormals.width != textureSize)
            {
                //Создаём новую текстуру
                mapGenerationData.Value.bevelNormals = new(
                    textureSize, textureSize,
                    TextureFormat.ARGB32,
                    false);
            }

            //Определяем размер текстуры
            int textureHeight = mapGenerationData.Value.bevelNormals.height;
            int textureWidth = mapGenerationData.Value.bevelNormals.width;

            //Если массив цветов текстуры отсутствует или его длина не соответствует текстуре
            if (mapGenerationData.Value.bevelNormalsColors == null || mapGenerationData.Value.bevelNormalsColors.Length != textureHeight * textureWidth)
            {
                //Создаём новый массив
                mapGenerationData.Value.bevelNormalsColors = new Color[textureHeight * textureWidth];
            }

            //Создаём структуру для координат пикселя
            Vector2 texturePixel;

            //Определяем ширину скоса и квадрат ширины
            const float bevelWidth = 0.05f;
            float bevelWidthSqr = bevelWidth * bevelWidth;

            //Для каждого пикселя по высоте
            for (int y = 0, index = 0; y < textureHeight; y++)
            {
                //Определяем его положение по Y
                texturePixel.y = (float)y / textureHeight;

                //Для каждого пикселя по ширине
                for (int x = 0; x < textureWidth; x++)
                {
                    //Определяем его положение по X
                    texturePixel.x = (float)x / textureWidth;

                    //Обнуляем R-компонент
                    mapGenerationData.Value.bevelNormalsColors[index].r = 0f;

                    //Определяем расстояние до данного пикселя
                    float minDistSqr = float.MaxValue;

                    //Для каждого ребра шестиугольника
                    for (int a = 0; a < 6; a++)
                    {
                        //Берём индексы вершин
                        Vector2 t0 = mapGenerationData.Value.hexagonUVsExtruded[a];
                        Vector2 t1 = a < 5 ? mapGenerationData.Value.hexagonUVsExtruded[a + 1] : mapGenerationData.Value.hexagonUVsExtruded[0];

                        //Определяем длину ребра
                        float l2 = Vector2.SqrMagnitude(t0 - t1);
                        //
                        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(texturePixel - t0, t1 - t0) / l2));
                        //
                        Vector2 projection = t0 + t * (t1 - t0);
                        //
                        float distSqr = Vector2.SqrMagnitude(texturePixel - projection);

                        //Если расстояние меньше минимального, то обновляем минимальное
                        if (distSqr < minDistSqr)
                        {
                            minDistSqr = distSqr;
                        }
                    }

                    //Определяем градиент
                    float f = minDistSqr / bevelWidthSqr;
                    //Если градиент больше единицы, ограничиваем его
                    if (f > 1f)
                    {
                        f = 1f;
                    }

                    //Обновляем R-компонент
                    mapGenerationData.Value.bevelNormalsColors[index].r = f;

                    //Увеличиваем индекс пикселя
                    index++;
                }
            }

            //Задаём массив пикселей текстуре
            mapGenerationData.Value.bevelNormals.SetPixels(mapGenerationData.Value.bevelNormalsColors);

            //Применяем текстуру
            mapGenerationData.Value.bevelNormals.Apply();

            //Задаём текстуру материалу
            mapGenerationData.Value.regionMaterial.SetTexture("_BumpMask", mapGenerationData.Value.bevelNormals);
        }

        void MapUpdateMeshRenderersShadowSupport()
        {
            //Для каждого мешрендерера регионов
            for (int a = 0; a < mapGenerationData.Value.shadedMRs.Length; a++)
            {
                //Если рендерер не пуст и его имя верно
                if (mapGenerationData.Value.shadedMRs[a] != null
                    && mapGenerationData.Value.shadedMRs[a].name.Equals(MapGenerationData.shaderframeGOName))
                {
                    //Настраиваем отбрасывание принятие и отбрасывание теней
                    mapGenerationData.Value.shadedMRs[a].receiveShadows = true;
                    mapGenerationData.Value.shadedMRs[a].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }
        }

        Texture2D MapGetCachedSolidTexture(
            Color color)
        {
            Texture2D texture;

            if (mapGenerationData.Value.solidTexCache.TryGetValue(color, out texture))
            {
                return texture;
            }
            else
            {
                texture = new Texture2D(
                    mapGenerationData.Value.TileTextureSize, mapGenerationData.Value.TileTextureSize,
                    TextureFormat.ARGB32,
                    true);
                texture.hideFlags = HideFlags.DontSave;

                int length = texture.width * texture.height;
                Color32[] colors32 = new Color32[length];
                Color32 color32 = color;

                //Для каждого пикселя
                for (int a = 0; a < length; a++)
                {
                    colors32[a] = color32;
                }

                texture.SetPixels32(colors32);
                texture.Apply();

                mapGenerationData.Value.solidTexCache[color] = texture;

                return texture;
            }
        }

        GameObject MapCreateGOAndParent(
            Transform parent,
            string name)
        {
            //Создаём GO
            GameObject gO = new GameObject(name);

            //Заполняем основные данные GO
            gO.layer = parent.gameObject.layer;
            gO.transform.SetParent(parent, false);
            gO.transform.localPosition = Vector3.zero;
            gO.transform.localScale = Vector3.one;
            gO.transform.localRotation = Quaternion.Euler(0, 0, 0);

            return gO;
        }
    }
}