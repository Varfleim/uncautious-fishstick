
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
            //��������� ����� ��������
            CacheProvinceRenderer(provinceHoverHighlightRenderer.renderer);

            //�������� �������� � �������� ������������ ������
            provinceHoverHighlightRenderer.renderer.gameObject.SetActive(false);
            provinceHoverHighlightRenderer.renderer.transform.SetParent(null);

            //������� ������ �� ��������
            provinceHoverHighlightRenderer.renderer = null;
        }

        static void CacheProvinceRenderer(
            GOProvinceRenderer provinceRenderer)
        {
            //������� �������� � ������ ������������
            cachedRenderers.Add(provinceRenderer);
        }

        public static void InstantiateProvinceRenderer(
            ref CProvinceHexasphere pHS, ref CProvinceHoverHighlightRenderer provinceHoverHighlightRenderer)
        {
            //��������� ����� ��������
            GOProvinceRenderer provinceRenderer = InstantiateProvinceRenderer(ref pHS);

            //������������ �������� � ���������� ��������
            provinceHoverHighlightRenderer.renderer = provinceRenderer;
        }

        static GOProvinceRenderer InstantiateProvinceRenderer(
            ref CProvinceHexasphere pHS)
        {
            //������ ������ ���������� ��� ���������
            GOProvinceRenderer provinceRenderer;

            //���� ������ ������������ ���������� �� ����, �� ���� ������������
            if (cachedRenderers.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                provinceRenderer = cachedRenderers[cachedRenderers.Count - 1];
                cachedRenderers.RemoveAt(cachedRenderers.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ��������
                provinceRenderer = Instantiate(provinceRendererPrefab);
            }

            //���������� �������� � ������������ ��� � GO ���������
            provinceRenderer.gameObject.SetActive(true);
            provinceRenderer.transform.SetParent(pHS.selfObject.transform);
            provinceRenderer.transform.localScale = Vector3.one;

            //������ ����� ���
            Mesh mesh = new();
            mesh.hideFlags = HideFlags.DontSave;

            //���������� ������ ���������
            float extrusionAmount = pHS.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier;

            //������������ ����������� ������� ���������
            Vector3[] extrudedVertices = new Vector3[pHS.vertexPoints.Length];
            //��� ������ �������
            for (int a = 0; a < pHS.vertices.Length; a++)
            {
                //������������ ��������� ����������� �������
                extrudedVertices[a] = pHS.vertices[a] * (1f + extrusionAmount);
            }
            //��������� ������� ����
            mesh.vertices = extrudedVertices;

            //���� � ��������� ����� ������
            if (pHS.vertices.Length == 6)
            {
                mesh.SetIndices(
                    MapGenerationData.hexagonIndices,
                    MeshTopology.Triangles,
                    0,
                    false);
                mesh.uv = MapGenerationData.hexagonUVs;
            }
            //�����
            else
            {
                mesh.SetIndices(
                    MapGenerationData.pentagonIndices,
                    MeshTopology.Triangles,
                    0,
                    false);
                mesh.uv = MapGenerationData.pentagonUVs;
            }

            //������������ ������� ����
            mesh.normals = pHS.vertices;
            mesh.RecalculateNormals();

            //��������� ��� ��������� � �������� ������������
            provinceRenderer.meshFilter.sharedMesh = mesh;
            provinceRenderer.provinceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            provinceRenderer.provinceRenderer.enabled = true;

            //���������� ��������
            return provinceRenderer;
        }

        public static void RefreshProvinceRenderer(
            ref CProvinceHexasphere pHS,
            GOProvinceRenderer provinceRenderer)
        {
            //���� ��� ���������
            Mesh mesh = provinceRenderer.meshFilter.sharedMesh;

            //������������ ����� ��������� ������ ���������
            Vector3[] extrudedVertices = new Vector3[pHS.vertices.Length];
            for (int a = 0; a < pHS.vertices.Length; a++)
            {
                extrudedVertices[a] = pHS.vertices[a] * (1f + pHS.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier);
            }
            //��������� ������� ����
            mesh.vertices = extrudedVertices;

            //������������ ������� ����
            mesh.normals = pHS.vertices;
            mesh.RecalculateNormals();

            //��������� ��� ����������
            provinceRenderer.meshFilter.sharedMesh = null;
            provinceRenderer.meshFilter.sharedMesh = mesh;

        }
    }
}