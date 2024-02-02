
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
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //������
        readonly EcsCustomInject<SceneData> sceneData = default;
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //���� ������
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //������ �����
                HexasphereCreate(ref requestComp);
            }
        }

        void HexasphereCreate(
            ref RMapGenerating requestComp)
        {
            //�������������� ������ �����
            HexasphereDataInitialization(ref requestComp);

            //���������� ������� ����������
            HexasphereGenerate();

            //��������, ��� ��������� ��������� ����������
            mapGenerationData.Value.isInitializationUpdate = true;

            //��������, ��� ��������� �������� �������, �����, UV-���������� � ������� �������
            mapGenerationData.Value.isMaterialUpdated = true;
            mapGenerationData.Value.isRegionUpdated = true;
            mapGenerationData.Value.isColorUpdated = true;
            mapGenerationData.Value.isUVUpdatedFast = true;
        }

        void HexasphereDataInitialization(
            ref RMapGenerating requestComp)
        {
            //������������ ����� �������������
            mapGenerationData.Value.subdivisions = Mathf.Max(1, requestComp.subdivisions);

            //��������� ��������� ������������
            MapGenerationData.ExtrudeMultiplier = mapGenerationData.Value.extrudeMultiplier;

            //��������� ������ ����������
            SceneData.HexashpereGO = sceneData.Value.hexashpereGO;

            //����� ��������� ��������
            mapGenerationData.Value.fleetRegionHighlightMaterial.shaderKeywords = null;//new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.fleetRegionHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            mapGenerationData.Value.hoverRegionHighlightMaterial.shaderKeywords = null;//= new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.hoverRegionHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            mapGenerationData.Value.currentRegionHighlightMaterial.shaderKeywords = null;//= new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.currentRegionHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            //���������, ��� ����� ����������� ��� ������� ����
            regionsData.Value.needRefreshRouteMatrix = true;
            for (int a = 0; a < regionsData.Value.needRefreshPathMatrix.Length; a++)
            {
                regionsData.Value.needRefreshPathMatrix[a] = true;
            }
        }

        void HexasphereGenerate()
        {
            //���������� ������� ������������ ���������
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

            //���������� ������������ ������������ ���������
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

            //������� ������� �����
            mapGenerationData.Value.points.Clear();

            //������� ������� ��������� � �������
            for (int a = 0; a < corners.Length; a++)
            {
                mapGenerationData.Value.points[corners[a]] = corners[a];
            }

            //������ ������ ����� ������� ����� ������������
            List<DHexaspherePoint> bottom = new();
            //���������� ���������� �������������
            int triangleCount = triangles.Length;
            //��� ������� ������������
            for (int f = 0; f < triangleCount; f++)
            {
                //������ ������ ������ �����
                List<DHexaspherePoint> previous = null;

                //���� ������ ������� ������������
                DHexaspherePoint point0 = triangles[f].points[0];

                //������� ��������� ������ �����
                bottom.Clear();

                //������� � ������ ������ ������� ������������
                bottom.Add(point0);

                //������ ������ ����� ������ ����� ������������
                List<DHexaspherePoint> left = PointSubdivide(
                    point0, triangles[f].points[1],
                    mapGenerationData.Value.subdivisions);
                //������ ������ ����� ������� ����� ������������
                List<DHexaspherePoint> right = PointSubdivide(
                    point0, triangles[f].points[2],
                    mapGenerationData.Value.subdivisions);

                //��� ������� �������������
                for (int i = 1; i <= mapGenerationData.Value.subdivisions; i++)
                {
                    //��������� ������ ����� ������� �����
                    previous = bottom;

                    //������������ ��������� � ������ ����� �� �������
                    bottom = PointSubdivide(
                        left[i], right[i],
                        i);

                    //������ ����� �����������
                    new DHexasphereTriangle(previous[0], bottom[0], bottom[1]);

                    //��� ������� ...
                    for (int j = 1; j < i; j++)
                    {
                        //������ ��� ����� ������������
                        new DHexasphereTriangle(previous[j], bottom[j], bottom[j + 1]);
                        new DHexasphereTriangle(previous[j - 1], previous[j], bottom[j]);
                    }
                }
            }

            //���������� ���������� �����
            int meshPointCount = mapGenerationData.Value.points.Values.Count;

            //������ �������
            //���� ������ ������� �������
            int regionIndex = 0;
            //�������� ���� �����
            DHexaspherePoint.flag = 0;

            //���������� ������ ������� ��������
            regionsData.Value.regionPEs = new EcsPackedEntity[meshPointCount];

            //������ ������������ ������ ��� GO ��������
            Transform regionsRoot = HexasphereCreateGOAndParent(
                sceneData.Value.coreObject,
                MapGenerationData.regionsRootName).transform;

            //��� ������ ������� � �������
            foreach (DHexaspherePoint point in mapGenerationData.Value.points.Values)
            {
                //������ ������
                RegionCreate(
                    regionsRoot,
                    point,
                    regionIndex);

                //����������� ������
                regionIndex++;
            }

            //��� ������� �������
            for (int a = 0; a < regionsData.Value.regionPEs.Length; a++)
            {
                //���� RHS � RC
                regionsData.Value.regionPEs[a].Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //������������ ������� �������

                //������� ��������� ������ �������
                CRegionCore.tempNeighbours.Clear();
                //��� ������� ������������ � ������ ������ �������
                for (int b = 0; b < rHS.centerPoint.triangleCount; b++)
                {
                    //���� �����������
                    DHexasphereTriangle triangle = rHS.centerPoint.triangles[b];

                    //��� ������ ������� ������������
                    for (int c = 0; c < 3; c++)
                    {
                        //���� ������ �������
                        triangle.points[c].regionPE.Unpack(world.Value, out int neighbourRegionEntity);
                        ref CRegionCore neighbourRC = ref rCPool.Value.Get(neighbourRegionEntity);

                        //���� ��� �� ������� ������ � ��������� ������ ��� �� �������� ���
                        if (neighbourRC.Index != rC.Index && CRegionCore.tempNeighbours.Contains(neighbourRC.selfPE) == false)
                        {
                            //������� PE ������ � ������
                            CRegionCore.tempNeighbours.Add(neighbourRC.selfPE);
                        }
                    }
                }

                //������� ������� � ������ �������
                rC.neighbourRegionPEs = CRegionCore.tempNeighbours.ToArray();
            }
        }

        GameObject HexasphereCreateGOAndParent(
            Transform parent,
            string name)
        {
            //������ GO
            GameObject gO = new(name);

            //��������� �������� ������ GO
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
            //������ ������ �����, ������������ �������� �����, � ������� � ���� ������� �������
            List<DHexaspherePoint> segments = new(count + 1);
            segments.Add(startPoint);

            //������������ ���������� �����
            double dx = endPoint.x - startPoint.x;
            double dy = endPoint.y - startPoint.y;
            double dz = endPoint.z - startPoint.z;
            double doublex = startPoint.x;
            double doubley = startPoint.y;
            double doublez = startPoint.z;
            double doubleCount = count;

            //��� ������� �������������
            for (int a = 1; a < count; a++)
            {
                //������ ����� �������
                DHexaspherePoint newPoint = new(
                    (float)(doublex + dx * a / doubleCount),
                    (float)(doubley + dy * a / doubleCount),
                    (float)(doublez + dz * a / doubleCount));

                //��������� �������
                newPoint = PointGetCached(newPoint);

                //������� ������� � ������
                segments.Add(newPoint);
            }

            //������� � ������ �������� �������
            segments.Add(endPoint);

            //���������� ������ ������
            return segments;
        }

        DHexaspherePoint PointGetCached(
            DHexaspherePoint point)
        {
            DHexaspherePoint thePoint;

            //���� ����������� ������� ���������� � �������
            if (mapGenerationData.Value.points.TryGetValue(point, out thePoint))
            {
                //���������� �������
                return thePoint;
            }
            //�����
            else
            {
                //��������� ������� � �������
                mapGenerationData.Value.points[point] = point;

                //���������� �������
                return point;
            }
        }

        void RegionCreate(
            Transform parentGO,
            DHexaspherePoint centerPoint,
            int regionIndex)
        {
            //������ ����� �������� � ��������� �� ��������� RHS � RC
            int regionEntity = world.Value.NewEntity();
            ref CRegionHexasphere currentRHS = ref rHSPool.Value.Add(regionEntity);
            ref CRegionCore currentRC = ref rCPool.Value.Add(regionEntity);

            //��������� �������� ������ RHS
            currentRHS = new(
                world.Value.PackEntity(regionEntity),
                centerPoint);

            //��������� �������� ������ RC
            currentRC = new(
                currentRHS.selfPE, regionIndex,
                currentRHS.centerPoint.ProjectedVector3);

            //������� ������ � ������ ��������
            regionsData.Value.regionPEs[regionIndex] = currentRHS.selfPE;

            //������ GO ������� � ���
            GORegion regionGO = Object.Instantiate(uIData.Value.regionPrefab);

            //��������� �������� ������ GO
            regionGO.gameObject.layer = parentGO.gameObject.layer;
            regionGO.transform.SetParent(parentGO, false);
            regionGO.transform.localPosition = Vector3.zero;
            regionGO.transform.localScale = Vector3.one;
            regionGO.transform.localRotation = Quaternion.Euler(0, 0, 0);

            //��� ������� ������ �� ���� GO
            currentRHS.selfObject = regionGO.gameObject;

            //������ ��������� �������
            currentRHS.fleetRenderer = RegionCreateRenderer(ref currentRHS, regionGO.fleetRenderer);
            currentRHS.hoverRenderer = RegionCreateRenderer(ref currentRHS, regionGO.hoverRenderer);
            currentRHS.currentRenderer = RegionCreateRenderer(ref currentRHS, regionGO.currentRenderer);
        }

        MeshRenderer RegionCreateRenderer(
            ref CRegionHexasphere rHS, MeshRenderer renderer)
        {
            //������ ����� ��������� � ���
            MeshFilter meshFilter = renderer.gameObject.AddComponent<MeshFilter>();
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
                    mapGenerationData.Value.hexagonIndices,
                    MeshTopology.Triangles,
                    0,
                    false);
                mesh.uv = mapGenerationData.Value.hexagonUVs;
            }
            //�����
            else
            {
                mesh.SetIndices(
                    mapGenerationData.Value.pentagonIndices,
                    MeshTopology.Triangles,
                    0,
                    false);
                mesh.uv = mapGenerationData.Value.pentagonUVs;
            }

            //������������ ������� ����
            mesh.normals = rHS.vertices;
            mesh.RecalculateNormals();

            //��������� ��� ��������� � ��������� ������������
            meshFilter.sharedMesh = mesh;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.enabled = false;

            //���������� ��������
            return renderer;
        }
    }
}