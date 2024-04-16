
using System.Collections;
using System.Collections.Generic;

using SO.Map.Generation;

using UnityEngine;

namespace SO.Map.Hexasphere
{
    public class GOProvinceRenderer : MonoBehaviour
    {
        public static GOProvinceRenderer provinceRendererPrefab;
        static List<GOProvinceRenderer> cachedRenderers = new();

        public MeshRenderer provinceRenderer;
        public MeshFilter meshFilter;

        public static void CacheProvinceRenderer(
            ref CProvinceHoverHighlightRenderer provinceHoverHighlightRenderer)
        {
            //Совершаем общие действия
            CacheProvinceRenderer(provinceHoverHighlightRenderer.renderer);

            //Скрываем рендерер и обнуляем родительский объект
            provinceHoverHighlightRenderer.renderer.gameObject.SetActive(false);
            provinceHoverHighlightRenderer.renderer.transform.SetParent(null);

            //Удаляем ссылку на рендерер
            provinceHoverHighlightRenderer.renderer = null;
        }

        static void CacheProvinceRenderer(
            GOProvinceRenderer provinceRenderer)
        {
            //Заносим рендерер в список кэшированных
            cachedRenderers.Add(provinceRenderer);
        }

        public static void InstantiateProvinceRenderer(
            ref CProvinceHexasphere pHS, ref CProvinceHoverHighlightRenderer provinceHoverHighlightRenderer)
        {
            //Совершаем общие действия
            GOProvinceRenderer provinceRenderer = InstantiateProvinceRenderer(ref pHS);

            //Присоединяем рендерер к указанному родителю
            provinceHoverHighlightRenderer.renderer = provinceRenderer;
        }

        static GOProvinceRenderer InstantiateProvinceRenderer(
            ref CProvinceHexasphere pHS)
        {
            //Создаём пустую переменную для рендерера
            GOProvinceRenderer provinceRenderer;

            //Если список кэшированных рендереров не пуст, то берём кэшированную
            if (cachedRenderers.Count > 0)
            {
                //Берём последнюю группу в списке и удаляем её из списка
                provinceRenderer = cachedRenderers[cachedRenderers.Count - 1];
                cachedRenderers.RemoveAt(cachedRenderers.Count - 1);
            }
            //Иначе
            else
            {
                //Создаём новый рендерер
                provinceRenderer = Instantiate(provinceRendererPrefab);
            }

            //Отображаем рендерер и присоединяем его к GO провинции
            provinceRenderer.gameObject.SetActive(true);
            provinceRenderer.transform.SetParent(pHS.selfObject.transform);
            provinceRenderer.transform.localScale = Vector3.one;

            //Создаём новый меш
            Mesh mesh = new();
            mesh.hideFlags = HideFlags.DontSave;

            //Определяем высоту рендерера
            float extrusionAmount = pHS.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier;

            //Рассчитываем выдавленные вершины провинции
            Vector3[] extrudedVertices = new Vector3[pHS.vertexPoints.Length];
            //Для каждой вершины
            for (int a = 0; a < pHS.vertices.Length; a++)
            {
                //Рассчитываем положение выдавленной вершины
                extrudedVertices[a] = pHS.vertices[a] * (1f + extrusionAmount);
            }
            //Назначаем вершины мешу
            mesh.vertices = extrudedVertices;

            //Если у провинции шесть вершин
            if (pHS.vertices.Length == 6)
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
            mesh.normals = pHS.vertices;
            mesh.RecalculateNormals();

            //Назначаем меш рендереру и включаем визуализацию
            provinceRenderer.meshFilter.sharedMesh = mesh;
            provinceRenderer.provinceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            provinceRenderer.provinceRenderer.enabled = true;

            //Возвращаем рендерер
            return provinceRenderer;
        }

        public static void RefreshProvinceRenderer(
            ref CProvinceHexasphere pHS,
            GOProvinceRenderer provinceRenderer)
        {
            //Берём меш рендерера
            Mesh mesh = provinceRenderer.meshFilter.sharedMesh;

            //Рассчитываем новое положение вершин рендерера
            Vector3[] extrudedVertices = new Vector3[pHS.vertices.Length];
            for (int a = 0; a < pHS.vertices.Length; a++)
            {
                extrudedVertices[a] = pHS.vertices[a] * (1f + pHS.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier);
            }
            //Назначаем вершины мешу
            mesh.vertices = extrudedVertices;

            //Рассчитываем нормали меша
            mesh.normals = pHS.vertices;
            mesh.RecalculateNormals();

            //Обновляем меш мешфильтра
            provinceRenderer.meshFilter.sharedMesh = null;
            provinceRenderer.meshFilter.sharedMesh = mesh;

        }
    }
}