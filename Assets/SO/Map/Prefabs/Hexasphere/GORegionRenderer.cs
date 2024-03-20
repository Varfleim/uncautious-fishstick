
using System.Collections;
using System.Collections.Generic;

using SO.Map.Generation;

using UnityEngine;

namespace SO.Map.Hexasphere
{
    public class GORegionRenderer : MonoBehaviour
    {
        public static GORegionRenderer regionRendererPrefab;
        static List<GORegionRenderer> cachedRenderers = new();

        public MeshRenderer regionRenderer;
        public MeshFilter meshFilter;

        public static void CacheRegionRenderer(
            ref CRegionHoverHighlightRenderer regionHoverHighlightRenderer)
        {
            //Совершаем общие действия
            CacheRegionRenderer(regionHoverHighlightRenderer.renderer);

            //Скрываем рендерер и обнуляем родительский объект
            regionHoverHighlightRenderer.renderer.gameObject.SetActive(false);
            regionHoverHighlightRenderer.renderer.transform.SetParent(null);

            //Удаляем ссылку на рендерер
            regionHoverHighlightRenderer.renderer = null;
        }

        static void CacheRegionRenderer(
            GORegionRenderer regionRenderer)
        {
            //Заносим рендерер в список кэшированных
            cachedRenderers.Add(regionRenderer);
        }

        public static void InstantiateRegionRenderer(
            ref CRegionHexasphere rHS, ref CRegionHoverHighlightRenderer regionHoverHighlightRenderer)
        {
            //Совершаем общие действия
            GORegionRenderer regionRenderer = InstantiateRegionRenderer(ref rHS);

            //Присоединяем рендерер к указанному родителю
            regionHoverHighlightRenderer.renderer = regionRenderer;
        }

        static GORegionRenderer InstantiateRegionRenderer(
            ref CRegionHexasphere rHS)
        {
            //Создаём пустую переменную для рендерера
            GORegionRenderer regionRenderer;

            //Если список кэшированных рендереров не пуст, то берём кэшированную
            if (cachedRenderers.Count > 0)
            {
                //Берём последнюю группу в списке и удаляем её из списка
                regionRenderer = cachedRenderers[cachedRenderers.Count - 1];
                cachedRenderers.RemoveAt(cachedRenderers.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новый рендерер
                regionRenderer = Instantiate(regionRendererPrefab);
            }

            //Отображаем рендерер и присоединяем его к GO региона
            regionRenderer.gameObject.SetActive(true);
            regionRenderer.transform.SetParent(rHS.selfObject.transform);
            regionRenderer.transform.localScale = Vector3.one;

            //Создаём новый меш
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
                    MapGenerationData.hexagonIndices,
                    MeshTopology.Triangles,
                    0,
                    false);
                mesh.uv = MapGenerationData.hexagonUVs;
            }
            //Иначе
            else
            {
                mesh.SetIndices(
                    MapGenerationData.pentagonIndices,
                    MeshTopology.Triangles,
                    0,
                    false);
                mesh.uv = MapGenerationData.pentagonUVs;
            }

            //Рассчитываем нормали меша
            mesh.normals = rHS.vertices;
            mesh.RecalculateNormals();

            //Назначаем меш рендереру и включаем визуализацию
            regionRenderer.meshFilter.sharedMesh = mesh;
            regionRenderer.regionRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            regionRenderer.regionRenderer.enabled = true;

            //Возвращаем рендерер
            return regionRenderer;
        }

        public static void RefreshRegionRenderer(
            ref CRegionHexasphere rHS,
            GORegionRenderer regionRenderer)
        {
            //Берём меш рендерера
            Mesh mesh = regionRenderer.meshFilter.sharedMesh;

            //Рассчитываем новое положение вершин рендерера
            Vector3[] extrudedVertices = new Vector3[rHS.vertices.Length];
            for (int a = 0; a < rHS.vertices.Length; a++)
            {
                extrudedVertices[a] = rHS.vertices[a] * (1f + rHS.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier);
            }
            //Назначаем вершины мешу
            mesh.vertices = extrudedVertices;

            //Рассчитываем нормали меша
            mesh.normals = rHS.vertices;
            mesh.RecalculateNormals();

            //Обновляем меш мешфильтра
            regionRenderer.meshFilter.sharedMesh = null;
            regionRenderer.meshFilter.sharedMesh = mesh;

        }
    }
}