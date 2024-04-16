
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
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;
        readonly EcsPoolInject<CProvinceEconomy> pEPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;


        //������
        readonly EcsCustomInject<SceneData> sceneData = default;
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<ProvincesData> provincesData = default;

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

            //��������, ��� ��������� �������� ���������, �����, UV-���������� � ������� �������
            mapGenerationData.Value.isMaterialUpdated = true;
            mapGenerationData.Value.isProvinceUpdated = true;
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
            mapGenerationData.Value.fleetProvinceHighlightMaterial.shaderKeywords = null;//new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.fleetProvinceHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            mapGenerationData.Value.hoverProvinceHighlightMaterial.shaderKeywords = null;//= new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.hoverProvinceHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            mapGenerationData.Value.currentProvinceHighlightMaterial.shaderKeywords = null;//= new string[] { ShaderParameters.SKW_HIGHLIGHT_TINT_BACKGROUND };
            mapGenerationData.Value.currentProvinceHighlightMaterial.SetFloat(ShaderParameters.ColorShift, 1f);

            //���������, ��� ����� ����������� ��� ������� ����
            provincesData.Value.needRefreshRouteMatrix = true;
            for (int a = 0; a < provincesData.Value.needRefreshPathMatrix.Length; a++)
            {
                provincesData.Value.needRefreshPathMatrix[a] = true;
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

            //������ ���������
            //���� ������ ������
            int provinceIndex = 0;
            //�������� ���� �����
            DHexaspherePoint.flag = 0;

            //���������� ������ ������� ���������
            provincesData.Value.provincePEs = new EcsPackedEntity[meshPointCount];

            //������ ������������ ������ ��� GO ���������
            Transform provincesRoot = HexasphereCreateGOAndParent(
                sceneData.Value.coreObject,
                MapGenerationData.provincesRootName).transform;

            //��� ������ ������� � �������
            foreach (DHexaspherePoint point in mapGenerationData.Value.points.Values)
            {
                //������ ���������
                ProvinceCreate(
                    provincesRoot,
                    point,
                    provinceIndex);

                //����������� ������
                provinceIndex++;
            }

            //��� ������ ���������
            for (int a = 0; a < provincesData.Value.provincePEs.Length; a++)
            {
                //���� PHS � PC
                provincesData.Value.provincePEs[a].Unpack(world.Value, out int provinceEntity);
                ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //������������ �������

                //������� ��������� ������ �������
                CProvinceCore.tempNeighbours.Clear();
                //��� ������� ������������ � ������ ������ ���������
                for (int b = 0; b < pHS.centerPoint.triangleCount; b++)
                {
                    //���� �����������
                    DHexasphereTriangle triangle = pHS.centerPoint.triangles[b];

                    //��� ������ ������� ������������
                    for (int c = 0; c < 3; c++)
                    {
                        //���� ��������� �������
                        triangle.points[c].provincePE.Unpack(world.Value, out int neighbourProvinceEntity);
                        ref CProvinceCore neighbourPC = ref pCPool.Value.Get(neighbourProvinceEntity);

                        //���� ��� �� ������� ��������� � ��������� ������ ��� �� �������� �
                        if (neighbourPC.Index != pC.Index && CProvinceCore.tempNeighbours.Contains(neighbourPC.selfPE) == false)
                        {
                            //������� PE ������ � ������
                            CProvinceCore.tempNeighbours.Add(neighbourPC.selfPE);
                        }
                    }
                }

                //������� ������� � ������ ���������
                pC.neighbourProvincePEs = CProvinceCore.tempNeighbours.ToArray();
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

        void ProvinceCreate(
            Transform parentGO,
            DHexaspherePoint centerPoint,
            int provinceIndex)
        {
            //������ ����� �������� � ��������� �� ���������� PHS, PC, PE
            int provinceEntity = world.Value.NewEntity();
            ref CProvinceHexasphere currentPHS = ref pHSPool.Value.Add(provinceEntity);
            ref CProvinceCore currentPC = ref pCPool.Value.Add(provinceEntity);
            ref CProvinceEconomy currentPE = ref pEPool.Value.Add(provinceEntity);

            //��������� �������� ������ PHS
            currentPHS = new(
                world.Value.PackEntity(provinceEntity),
                centerPoint);

            //��������� �������� ������ PC
            currentPC = new(
                currentPHS.selfPE, provinceIndex,
                currentPHS.centerPoint.ProjectedVector3);

            //��������� �������� ������ PE
            currentPE = new(
                currentPHS.selfPE);

            //������� ��������� � ������ ���������
            provincesData.Value.provincePEs[provinceIndex] = currentPHS.selfPE;

            //������ GO ��������� � ���
            GOProvince provinceGO = Object.Instantiate(uIData.Value.provincePrefab);

            //��������� �������� ������ GO
            provinceGO.gameObject.layer = parentGO.gameObject.layer;
            provinceGO.transform.SetParent(parentGO, false);
            provinceGO.transform.localPosition = Vector3.zero;
            provinceGO.transform.localScale = Vector3.one;
            provinceGO.transform.localRotation = Quaternion.Euler(0, 0, 0);

            //��� ��������� ������ �� ���� GO
            currentPHS.selfObject = provinceGO.gameObject;
        }
    }
}