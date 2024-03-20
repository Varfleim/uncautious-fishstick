
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
            //��������� ����� ��������
            CacheRegionRenderer(regionHoverHighlightRenderer.renderer);

            //�������� �������� � �������� ������������ ������
            regionHoverHighlightRenderer.renderer.gameObject.SetActive(false);
            regionHoverHighlightRenderer.renderer.transform.SetParent(null);

            //������� ������ �� ��������
            regionHoverHighlightRenderer.renderer = null;
        }

        static void CacheRegionRenderer(
            GORegionRenderer regionRenderer)
        {
            //������� �������� � ������ ������������
            cachedRenderers.Add(regionRenderer);
        }

        public static void InstantiateRegionRenderer(
            ref CRegionHexasphere rHS, ref CRegionHoverHighlightRenderer regionHoverHighlightRenderer)
        {
            //��������� ����� ��������
            GORegionRenderer regionRenderer = InstantiateRegionRenderer(ref rHS);

            //������������ �������� � ���������� ��������
            regionHoverHighlightRenderer.renderer = regionRenderer;
        }

        static GORegionRenderer InstantiateRegionRenderer(
            ref CRegionHexasphere rHS)
        {
            //������ ������ ���������� ��� ���������
            GORegionRenderer regionRenderer;

            //���� ������ ������������ ���������� �� ����, �� ���� ������������
            if (cachedRenderers.Count > 0)
            {
                //���� ��������� ������ � ������ � ������� � �� ������
                regionRenderer = cachedRenderers[cachedRenderers.Count - 1];
                cachedRenderers.RemoveAt(cachedRenderers.Count - 1);
            }
            //�����
            else
            {
                //������ ����� ��������
                regionRenderer = Instantiate(regionRendererPrefab);
            }

            //���������� �������� � ������������ ��� � GO �������
            regionRenderer.gameObject.SetActive(true);
            regionRenderer.transform.SetParent(rHS.selfObject.transform);
            regionRenderer.transform.localScale = Vector3.one;

            //������ ����� ���
            Mesh mesh = new();
            mesh.hideFlags = HideFlags.DontSave;

            //���������� ������ ���������
            float extrusionAmount = rHS.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier;

            //������������ ����������� ������� �������
            Vector3[] extrudedVertices = new Vector3[rHS.vertexPoints.Length];
            //��� ������ �������
            for (int a = 0; a < rHS.vertices.Length; a++)
            {
                //������������ ��������� ����������� �������
                extrudedVertices[a] = rHS.vertices[a] * (1f + extrusionAmount);
            }
            //��������� ������� ����
            mesh.vertices = extrudedVertices;

            //���� � ������� ����� ������
            if (rHS.vertices.Length == 6)
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
            mesh.normals = rHS.vertices;
            mesh.RecalculateNormals();

            //��������� ��� ��������� � �������� ������������
            regionRenderer.meshFilter.sharedMesh = mesh;
            regionRenderer.regionRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            regionRenderer.regionRenderer.enabled = true;

            //���������� ��������
            return regionRenderer;
        }

        public static void RefreshRegionRenderer(
            ref CRegionHexasphere rHS,
            GORegionRenderer regionRenderer)
        {
            //���� ��� ���������
            Mesh mesh = regionRenderer.meshFilter.sharedMesh;

            //������������ ����� ��������� ������ ���������
            Vector3[] extrudedVertices = new Vector3[rHS.vertices.Length];
            for (int a = 0; a < rHS.vertices.Length; a++)
            {
                extrudedVertices[a] = rHS.vertices[a] * (1f + rHS.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier);
            }
            //��������� ������� ����
            mesh.vertices = extrudedVertices;

            //������������ ������� ����
            mesh.normals = rHS.vertices;
            mesh.RecalculateNormals();

            //��������� ��� ����������
            regionRenderer.meshFilter.sharedMesh = null;
            regionRenderer.meshFilter.sharedMesh = mesh;

        }
    }
}