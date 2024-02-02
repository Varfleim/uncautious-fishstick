
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;

namespace SO.Map.Hexasphere
{
    /// <summary>
    /// ���������, �������� ������ �������, ��������������� ��������� � �����������
    /// </summary>
    public struct CRegionHexasphere
    {
        public CRegionHexasphere(
            EcsPackedEntity selfPE,
            DHexaspherePoint centerPoint)
        {
            this.selfPE = selfPE;

            this.centerPoint = centerPoint;
            center = this.centerPoint.ProjectedVector3;
            this.centerPoint.regionPE = this.selfPE;

            int facesCount = centerPoint.GetOrderedTriangles(tempTriangles);
            vertexPoints = new DHexaspherePoint[facesCount];
            //��� ������ �������
            for (int a = 0; a < facesCount; a++)
            {
                vertexPoints[a] = tempTriangles[a].GetCentroid();
            }
            //�����������������, ���� �������� �������
            if (facesCount == 6)
            {
                Vector3 p0 = (Vector3)vertexPoints[0];
                Vector3 p1 = (Vector3)vertexPoints[1];
                Vector3 p5 = (Vector3)vertexPoints[5];
                Vector3 v0 = p1 - p0;
                Vector3 v1 = p5 - p0;
                Vector3 cp = Vector3.Cross(v0, v1);
                float dp = Vector3.Dot(cp, p1);
                if (dp < 0)
                {
                    DHexaspherePoint aux;
                    aux = vertexPoints[0];
                    vertexPoints[0] = vertexPoints[5];
                    vertexPoints[5] = aux;
                    aux = vertexPoints[1];
                    vertexPoints[1] = vertexPoints[4];
                    vertexPoints[4] = aux;
                    aux = vertexPoints[2];
                    vertexPoints[2] = vertexPoints[3];
                    vertexPoints[3] = aux;
                }
            }
            else if (facesCount == 5)
            {
                Vector3 p0 = (Vector3)vertexPoints[0];
                Vector3 p1 = (Vector3)vertexPoints[1];
                Vector3 p4 = (Vector3)vertexPoints[4];
                Vector3 v0 = p1 - p0;
                Vector3 v1 = p4 - p0;
                Vector3 cp = Vector3.Cross(v0, v1);
                float dp = Vector3.Dot(cp, p1);
                if (dp < 0)
                {
                    DHexaspherePoint aux;
                    aux = vertexPoints[0];
                    vertexPoints[0] = vertexPoints[4];
                    vertexPoints[4] = aux;
                    aux = vertexPoints[1];
                    vertexPoints[1] = vertexPoints[3];
                    vertexPoints[3] = aux;
                }
            }

            vertices = new Vector3[vertexPoints.Length];
            //��� ������ �������
            for (int a = 0; a < vertexPoints.Length; a++)
            {
                vertices[a] = vertexPoints[a].ProjectedVector3;
            }

            selfObject = null;

            fleetRenderer = null;
            hoverRenderer = null;
            currentRenderer = null;

            customMaterial = null;
            tempMaterial = null;

            extrudeAmount = 0f;

            uvShadedChunkIndex = 0;
            uvShadedChunkStart = 0;
            uvShadedChunkLength = 0;
        }

        static readonly DHexasphereTriangle[] tempTriangles = new DHexasphereTriangle[20];

        public readonly EcsPackedEntity selfPE;


        public DHexaspherePoint centerPoint;
        public Vector3 center;
        public DHexaspherePoint[] vertexPoints;
        public Vector3[] vertices;

        public GameObject selfObject;
        public Renderer fleetRenderer;
        public Renderer hoverRenderer;
        public Renderer currentRenderer;

        public Material customMaterial;
        public Material tempMaterial;

        /// <summary>
        /// ������ �������� � ������
        /// </summary>
        public float ExtrudeAmount
        {
            get
            {
                return extrudeAmount;
            }
            set
            {
                extrudeAmount = value;

                //��������� �������� ��������� ����� �������
                RefreshRenderer(fleetRenderer);

                //��������� �������� ��������� ��������� �������
                RefreshRenderer(hoverRenderer);

                //��������� �������� ��������� ���������� �������
                RefreshRenderer(currentRenderer);
            }
        }
        float extrudeAmount;

        public int uvShadedChunkIndex;
        public int uvShadedChunkStart;
        public int uvShadedChunkLength;

        void RefreshRenderer(
            Renderer renderer)
        {
            //���� ��������� � ���
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;

            //������������ ����� ��������� ������ ���������
            Vector3[] extrudedVertices = new Vector3[vertices.Length];
            for (int a = 0; a < vertices.Length; a++)
            {
                extrudedVertices[a] = vertices[a] * (1f + extrudeAmount * MapGenerationData.ExtrudeMultiplier);
            }
            //��������� ������� ����
            mesh.vertices = extrudedVertices;

            //������������ ������� ����
            mesh.normals = vertices;
            mesh.RecalculateNormals();

            //��������� ��� ����������
            meshFilter.sharedMesh = null;
            meshFilter.sharedMesh = mesh;
        }

        public Vector3 GetRegionCenter()
        {
            Vector3 vertice = center;

            vertice *= 1.0f + extrudeAmount * MapGenerationData.ExtrudeMultiplier;

            return vertice;
        }
    }
}