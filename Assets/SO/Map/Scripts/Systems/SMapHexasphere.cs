
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Map.RFO.Events;

namespace SO.Map
{
    public class SMapHexasphere : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegion> regionPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        //������� ���������
        readonly EcsPoolInject<SRRFOCreating> rFOCreatingSelfRequestPool = default;


        //������
        readonly EcsCustomInject<SceneData> sceneData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach(int requestEntity in mapGeneratingRequestFilter.Value)
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
            for(int a = 0; a < regionsData.Value.needRefreshPathMatrix.Length; a++)
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
                //���� ������
                regionsData.Value.regionPEs[a].Unpack(world.Value, out int regionEntity);
                ref CRegion region = ref regionPool.Value.Get(regionEntity);

                //������������ ������� �������

                //������� ��������� ������ �������
                CRegion.tempNeighbours.Clear();
                //��� ������� ������������ � ������ ������ �������
                for (int b = 0; b < region.centerPoint.triangleCount; b++)
                {
                    //���� �����������
                    DHexasphereTriangle triangle = region.centerPoint.triangles[b];

                    //��� ������ ������� ������������
                    for (int c = 0; c < 3; c++)
                    {
                        //���� ������ �������
                        triangle.points[c].regionPE.Unpack(world.Value, out int neighbourRegionEntity);
                        ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                        //���� ��� �� ������� ������ � ��������� ������ ��� �� �������� ���
                        if (neighbourRegion.Index != region.Index && CRegion.tempNeighbours.Contains(neighbourRegion.selfPE) == false)
                        {
                            //������� PE ������ � ������
                            CRegion.tempNeighbours.Add(neighbourRegion.selfPE);
                        }
                    }
                }

                //������� ������� � ������ �������
                region.neighbourRegionPEs = CRegion.tempNeighbours.ToArray();
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
            double doublex = (double)startPoint.x;
            double doubley = (double)startPoint.y;
            double doublez = (double)startPoint.z;
            double doubleCount = (double)count;

            //��� ������� �������������
            for (int a = 1; a < count; a++)
            {
                //������ ����� �������
                DHexaspherePoint newPoint = new(
                    (float)(doublex + dx * (double)a / doubleCount),
                    (float)(doubley + dy * (double)a / doubleCount),
                    (float)(doublez + dz * (double)a / doubleCount));

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
            //������ ����� �������� � ��������� �� ��������� �������
            int regionEntity = world.Value.NewEntity();
            ref CRegion currentRegion = ref regionPool.Value.Add(regionEntity);

            //��������� �������� ������ �������
            currentRegion = new(
                world.Value.PackEntity(regionEntity), regionIndex,
                centerPoint);

            //������� ������ � ������ ��������
            regionsData.Value.regionPEs[regionIndex] = currentRegion.selfPE;

            //������ GO ������� � ���
            GORegionPrefab regionGO = GameObject.Instantiate(mapGenerationData.Value.regionPrefab);

            //��������� �������� ������ GO
            regionGO.gameObject.layer = parentGO.gameObject.layer;
            regionGO.transform.SetParent(parentGO, false);
            regionGO.transform.localPosition = Vector3.zero;
            regionGO.transform.localScale = Vector3.one;
            regionGO.transform.localRotation = Quaternion.Euler(0, 0, 0);

            //������ ��������� �������
            currentRegion.fleetRenderer = RegionCreateRenderer(ref currentRegion, regionGO.fleetRenderer);
            currentRegion.hoverRenderer = RegionCreateRenderer(ref currentRegion, regionGO.hoverRenderer);
            currentRegion.currentRenderer = RegionCreateRenderer(ref currentRegion, regionGO.currentRenderer);

            //����������� �������� RFO
            RegionFOCreatingSelfRequest(regionEntity);
        }

        MeshRenderer RegionCreateRenderer(
            ref CRegion region, MeshRenderer renderer)
        {
            //������ ����� ��������� � ���
            MeshFilter meshFilter = renderer.gameObject.AddComponent<MeshFilter>();
            Mesh mesh = new();
            mesh.hideFlags = HideFlags.DontSave;

            //���������� ������ ���������
            float extrusionAmount = region.ExtrudeAmount * MapGenerationData.ExtrudeMultiplier;

            //������������ ����������� ������� �������
            Vector3[] extrudedVertices = new Vector3[region.vertexPoints.Length];
            //��� ������ �������
            for (int a = 0; a < region.vertices.Length; a++)
            {
                //������������ ��������� ����������� �������
                extrudedVertices[a] = region.vertices[a] * (1f + extrusionAmount);
            }
            //��������� ������� ����
            mesh.vertices = extrudedVertices;

            //���� � ������� ����� ������
            if (region.vertices.Length == 6)
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
            mesh.normals = region.vertices;
            mesh.RecalculateNormals();

            //��������� ��� ��������� � ��������� ������������
            meshFilter.sharedMesh = mesh;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.enabled = false;

            //���������� ��������
            return renderer;
        }

        void RegionFOCreatingSelfRequest(
            int regionEntity)
        {
            //��������� �������� ������� ���������� �������� RFO
            ref SRRFOCreating selfRequestComp = ref rFOCreatingSelfRequestPool.Value.Add(regionEntity);

            //��������� ������ �����������
            selfRequestComp = new(0);
        }
    }
}