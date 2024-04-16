
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.UI;
using SO.UI.Game.Map.Events;
using SO.Country;
using SO.Map.MapArea;
using SO.Map.Hexasphere;
using SO.Map.Generation;
using SO.Map.Events;
using SO.Map.Province;

namespace SO.Map
{
    public class SMapControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        readonly EcsFilterInject<Inc<CMapArea>> mAFilter = default;
        readonly EcsPoolInject<CMapArea> mAPool = default;

        //������
        readonly EcsFilterInject<Inc<CCountry>> countryFilter = default;
        readonly EcsPoolInject<CCountry> countryPool = default;


        //������
        readonly EcsCustomInject<SceneData> sceneData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<ProvincesData> provincesData = default;
        readonly EcsCustomInject<InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //���� ������ �������� ����� ����� �� ����
            if(changeMapModeRequestFilter.Value.GetEntitiesCount() > 0)
            {
                //����� ������� �����
                MapChangeMapMode();
            }
            //�����
            else
            {
                //���������� ������� ����� �����
                MapRefreshMapMode();
            }

            //��������� ������� ���������� ���������
            mapGenerationData.Value.fleetProvinceHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));
            mapGenerationData.Value.hoverProvinceHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));
            mapGenerationData.Value.currentProvinceHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));

            //���� ��������� ���������� ����������
            if (mapGenerationData.Value.isMaterialUpdated == true)
            {
                //��������� �������� ����������
                MapMaterialPropertiesUpdate(mapGenerationData.Value.isInitializationUpdate);

                mapGenerationData.Value.isMaterialUpdated = false;
                mapGenerationData.Value.isProvinceUpdated = false;
                mapGenerationData.Value.isInitializationUpdate = false;
            }

            //���� ��������� ���������� ������� �������
            if (mapGenerationData.Value.isTextureArrayUpdated == true)
            {
                //��������� ���������
                MapUpdateShadedMaterials(mapGenerationData.Value.isInitializationUpdate);

                mapGenerationData.Value.isInitializationUpdate = false;
                mapGenerationData.Value.isTextureArrayUpdated = false;
            }
            //�����, ���� ��������� ���������� UV-��������� ��� ������
            else if (mapGenerationData.Value.isUVUpdatedFast == true
                || mapGenerationData.Value.isColorUpdated == true)
            {
                //��������� ��������� ���������
                MapUpdateShadedMaterialsFast();

                mapGenerationData.Value.isColorUpdated = false;
                mapGenerationData.Value.isUVUpdatedFast = false;
            }

            //��������� ������� ��������� ���������
            ProvinceHighlightRequest();
        }

        readonly EcsFilterInject<Inc<RChangeMapMode>> changeMapModeRequestFilter = default;
        readonly EcsPoolInject<RChangeMapMode> changeMapModeRequestPool = default;
        void MapChangeMapMode()
        {
            //��� ������� ������� ����� ������ �����
            foreach(int requestEntity in changeMapModeRequestFilter.Value)
            {
                //���� ������
                ref RChangeMapMode requestComp = ref changeMapModeRequestPool.Value.Get(requestEntity);

                //���� ������������� ����������� ������ ����� ���������
                if(requestComp.requestType == ChangeMapModeRequestType.Terrain)
                {
                    //��������� ����� ����� ��������� ��� ��������
                    inputData.Value.mapMode = MapMode.Terrain;

                    //���������� ����� ����� ���������
                    MapDisplayModeTerrain();
                }
                //���� ������������� ����������� ������ �������� �����
                else if (requestComp.requestType == ChangeMapModeRequestType.MapArea)
                {
                    //��������� ����� ����� �������� ��� ��������
                    inputData.Value.mapMode = MapMode.MapArea;

                    //���������� ����� ����� ��������
                    MapDisplayModeMapArea();
                }
                //�����, ���� ������������� ����������� �����
                else if (requestComp.requestType == ChangeMapModeRequestType.Country)
                {
                    //��������� ����� ����� ��� ��������
                    inputData.Value.mapMode = MapMode.Country;

                    //���������� ����� ����� �����
                    MapDisplayModeCountry();
                }

                changeMapModeRequestPool.Value.Del(requestEntity);
            }
        }

        void MapRefreshMapMode()
        {
            //���� ������� ����� ����� - ����� ����� ���������
            if (inputData.Value.mapMode == MapMode.Terrain)
            {
                //���������� ����� ����� ���������
                MapDisplayModeTerrain();
            }
            //�����, ���� ������� ����� ����� - ����� �������� �����
            else if (inputData.Value.mapMode == MapMode.MapArea)
            {
                //���������� ����� ��������
                MapDisplayModeMapArea();
            }
        }

        void MapDisplayModeTerrain()
        {
            //��� ������ ������� �����
            foreach (int mAEntity in mAFilter.Value)
            {
                //���� �������
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //���������� ���� �������
                Color mAColor = Color.Lerp(Color.yellow, Color.red, (float)mA.Elevation / mapGenerationData.Value.elevationMaximum);

                //��� ������ ��������� �������
                for (int a = 0; a < mA.provincePEs.Length; a++)
                {
                    //���� ���������
                    mA.provincePEs[a].Unpack(world.Value, out int provinceEntity);
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                    //������������� ���� ��������� �������������� ������ �������
                    ProvinceSetColor(
                        ref pC,
                        mAColor);

                }
            }
        }

        void MapDisplayModeMapArea()
        {
            //��� ������ ������� �����
            foreach (int mAEntity in mAFilter.Value)
            {
                //���� �������
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //��� ������ ��������� �������
                for (int a = 0; a < mA.provincePEs.Length; a++)
                {
                    //���� ���������
                    mA.provincePEs[a].Unpack(world.Value, out int provinceEntity);
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                    //������������� ���� ���������
                    ProvinceSetColor(
                        ref pC,
                        mA.selfColor);
                }
            }
        }

        void MapDisplayModeCountry()
        {
            //��� ������ ������
            foreach (int countryEntity in countryFilter.Value)
            {
                //���� ������
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //��� ������ ������� ����� ������
                for (int a = 0; a < country.ownedMAPEs.Count; a++)
                {
                    //���� �������
                    country.ownedMAPEs[a].Unpack(world.Value, out int mAEntity);
                    ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                    //��� ������ ��������� �������
                    for (int b = 0; b < mA.provincePEs.Length; b++)
                    {
                        //���� ���������
                        mA.provincePEs[b].Unpack(world.Value, out int provinceEntity);
                        ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                        //����
                        //������������� ���� ���������
                        ProvinceSetColor(
                            ref pC,
                            runtimeData.Value.playerColor);
                        //����
                    }
                }
            }
        }

        void MapMaterialPropertiesUpdate(
            bool isInitializationUpdate = false)
        {
            //���� ����� �������� ���������
            if (mapGenerationData.Value.isProvinceUpdated == true)
            {
                //��������� ���������
                MapRebuildTiles();

                //��������, ��� ��������� ���� ���������
                mapGenerationData.Value.isProvinceUpdated = false;
            }

            //��������� ��������� ��������� � ����
            MapUpdateShadedMaterials(isInitializationUpdate);
            MapUpdateMeshRenderersShadowSupport();

            //��������� �������� ���������
            mapGenerationData.Value.provinceMaterial.SetFloat("_GradientIntensity", 1f - mapGenerationData.Value.gradientIntensity);
            mapGenerationData.Value.provinceMaterial.SetFloat("_ExtrusionMultiplier", MapGenerationData.ExtrudeMultiplier);
            mapGenerationData.Value.provinceMaterial.SetColor("_Color", mapGenerationData.Value.tileTintColor);
            mapGenerationData.Value.provinceMaterial.SetColor("_AmbientColor", mapGenerationData.Value.ambientColor);
            mapGenerationData.Value.provinceMaterial.SetFloat("_MinimumLight", mapGenerationData.Value.minimumLight);

            //��������� ������ ����������
            sceneData.Value.hexashpereCollider.radius = 0.5f * (1.0f + MapGenerationData.ExtrudeMultiplier);

            //��������� ����
            MapUpdateLightingMode();

            //��������� ����
            MapUpdateBevel();
        }

        void MapRebuildTiles()
        {
            //������� ��������
            GameObject placeholder = GameObject.Find(MapGenerationData.shaderframeName);
            if (placeholder != null)
            {
                GameObject.DestroyImmediate(placeholder);
            }

            //������ ��� ������
            MapBuildTiles();
        }

        void MapBuildTiles()
        {
            //���� ������ ������� �����
            int chunkIndex = 0;

            //������ ������ ��� ������, �������� � UV-���������
            List<Vector3> vertexChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.verticesShaded[chunkIndex]);
            List<int> indicesChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.indicesShaded[chunkIndex]);
            List<Vector4> uv2Chunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.uv2Shaded[chunkIndex]);

            //������ ������� ������
            int verticesCount = 0;

            //���������� ���������� ���������
            int tileCount = provincesData.Value.provincePEs.Length;

            //������ ������� �������� ������ � ����������
            int[] hexIndices = mapGenerationData.Value.hexagonIndicesExtruded;
            int[] pentIndices = mapGenerationData.Value.pentagonIndicesExtruded;

            //��� ������� ���������
            for (int k = 0; k < tileCount; k++)
            {
                //���� ��������� ���������
                provincesData.Value.provincePEs[k].Unpack(world.Value, out int provinceEntity);
                ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

                //���� ���������� ������ ������ ������������� ���������� ������ �� ����
                if (verticesCount > MapGenerationData.maxVertexCountPerChunk)
                {
                    //����������� ������ �����
                    chunkIndex++;

                    //��������� ������ ������, �������� � UV-���������
                    vertexChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.verticesShaded[chunkIndex]);
                    indicesChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.indicesShaded[chunkIndex]);
                    uv2Chunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.uv2Shaded[chunkIndex]);

                    //�������� ������� ������
                    verticesCount = 0;
                }

                //���� ������ ������ ���������
                DHexaspherePoint[] tileVertices = pHS.vertexPoints;

                //���������� ���������� ������
                int tileVerticesCount = tileVertices.Length;

                //������ ��������� ��� UV4-���������
                Vector4 gpos = Vector4.zero;

                //��� ������ �������
                for (int b = 0; b < tileVerticesCount; b++)
                {
                    //���� ������� ���������
                    DHexaspherePoint point = tileVertices[b];

                    //���� ���������� ������ ������� � ������� � � ������ ������
                    Vector3 vertex = point.ProjectedVector3;
                    vertexChunk.Add(vertex);

                    //��������� gpos
                    gpos.x += vertex.x;
                    gpos.y += vertex.y;
                    gpos.z += vertex.z;
                }

                //������������ gpos
                gpos.x /= tileVerticesCount;
                gpos.y /= tileVerticesCount;
                gpos.z /= tileVerticesCount;

                //������ ������ ��������
                int[] indicesArray;

                //���� ����� ������ ��������� ����� �����
                if (tileVerticesCount == 6)
                {
                    //������� ������� ��������� ����� � ������
                    vertexChunk.Add((tileVertices[1].ProjectedVector3 + tileVertices[5].ProjectedVector3) * 0.5f);
                    vertexChunk.Add((tileVertices[2].ProjectedVector3 + tileVertices[4].ProjectedVector3) * 0.5f);

                    //��������� ������� ������
                    tileVerticesCount += 2;

                    //��������� ������� 
                    indicesArray = hexIndices;
                }
                //����� ��� ��������
                else
                {
                    //������� ������� ��������� ��������� � ������
                    vertexChunk.Add((tileVertices[1].ProjectedVector3 + tileVertices[4].ProjectedVector3) * 0.5f);
                    vertexChunk.Add((tileVertices[2].ProjectedVector3 + tileVertices[4].ProjectedVector3) * 0.5f);

                    //��������� ������� ������
                    tileVerticesCount += 2;

                    //�������� bevel ��� ����������
                    gpos.w = 1.0f;

                    //��������� ������� 
                    indicesArray = pentIndices;
                }

                //��� ������ �������
                for (int b = 0; b < tileVerticesCount; b++)
                {
                    //������� gpos � ������ UV-���������
                    uv2Chunk.Add(gpos);
                }

                //��� ������� �������
                for (int b = 0; b < indicesArray.Length; b++)
                {
                    //������� ������ ������� � ������ ��������
                    indicesChunk.Add(verticesCount + indicesArray[b]);
                }

                //��������� ������� ������
                verticesCount += tileVerticesCount;
            }

            //������ ������������ GO
            GameObject partsRoot = MapCreateGOAndParent(
                sceneData.Value.coreObject,
                MapGenerationData.shaderframeName);

            //��� ������� �����
            for (int k = 0; k <= chunkIndex; k++)
            {
                //������ ������ ������
                GameObject go = MapCreateGOAndParent(
                    partsRoot.transform,
                    MapGenerationData.shaderframeGOName);

                //��������� ��� ��������� ���������� � ��������� ������
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

                //��������� ��� ��������� ������������ � ��������� ������
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mapGenerationData.Value.shadedMRs[k] = mr;


                mr.sharedMaterial = mapGenerationData.Value.provinceMaterial;
            }
        }

        void MapUpdateShadedMaterials(
            bool isInitializationUpdate = false)
        {
            //���� ������ ������� �����
            int chunkIndex = 0;

            //������ ������ ������ � ��������
            List<Vector4> uvChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.uvShaded[chunkIndex]);
            List<Color32> colorChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.colorShaded[chunkIndex]);

            //���� ����� �������� �� ����������, ������ �
            if (mapGenerationData.Value.whiteTex == null)
            {
                mapGenerationData.Value.whiteTex = MapGetCachedSolidTexture(Color.white);
            }

            //������� ������ ������� � ������� � ���� ����� ��������
            mapGenerationData.Value.texArray.Clear();
            mapGenerationData.Value.texArray.Add(mapGenerationData.Value.whiteTex);

            //������ ������� ������
            int verticesCount = 0;

            //���������� ���������� ���������
            int tileCount = provincesData.Value.provincePEs.Length;

            //���������� ����������� ���� ���������
            Color32 color = mapGenerationData.Value.DefaultShadedColor;

            //��� ������ ���������
            for (int k = 0; k < tileCount; k++)
            {
                //���� ��������� ���������
                provincesData.Value.provincePEs[k].Unpack(world.Value, out int provinceEntity);
                ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //���� ������������ ������� ����� ���������
                pC.ParentMapAreaPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //���� ���������� ������ ������ ������������� ���������� ������ �� ����
                if (verticesCount > MapGenerationData.maxVertexCountPerChunk)
                {
                    //����������� ������ �����
                    chunkIndex++;

                    //��������� ������ ������ � ��������
                    uvChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.uvShaded[chunkIndex]);
                    colorChunk = mapGenerationData.Value.CheckList(ref mapGenerationData.Value.colorShaded[chunkIndex]);

                    //�������� ������� ������
                    verticesCount = 0;
                }

                //���� ������ ������ ���������
                DHexaspherePoint[] tileVertices = pHS.vertexPoints;

                //���������� ���������� ������
                int tileVerticesCount = tileVertices.Length;

                //������ ������ UV-���������
                Vector2[] uvArray;

                //���� ���������� ������ ����� �����
                if (tileVerticesCount == 6)
                {
                    //��������� �������
                    uvArray = mapGenerationData.Value.hexagonUVsExtruded;
                }
                //����� ��� ��������
                else
                {
                    //��������� �������
                    uvArray = mapGenerationData.Value.pentagonUVsExtruded;
                }

                //�������� ���� ��������� ��� �������� � ������ �������
                Texture2D tileTexture;

                //���������� ������, ������� � �������� ��������
                int textureIndex = 0;
                Vector2 textureScale;
                Vector2 textureOffset;

                //���� ��������� ����� ����������� ��������, ���� �������� ����� �������� � �������� �� �����
                if (pHS.customMaterial && pHS.customMaterial.HasProperty(ShaderParameters.MainTex) && pHS.customMaterial.mainTexture != null)
                {
                    //���� ��������� ���� ��������
                    tileTexture = (Texture2D)pHS.customMaterial.mainTexture;
                    textureIndex = mapGenerationData.Value.texArray.IndexOf(tileTexture);
                    textureScale = pHS.customMaterial.mainTextureScale;
                    textureOffset = pHS.customMaterial.mainTextureOffset;
                }
                //�����
                else
                {
                    //���� ��������� ����������� ��������
                    tileTexture = mapGenerationData.Value.whiteTex;
                    textureScale = Vector2.one;
                    textureOffset = Vector2.zero;
                }

                //���� ������ �������� ������ ����
                if (textureIndex < 0)
                {
                    //������� �������� � ������ � ��������� ������
                    mapGenerationData.Value.texArray.Add(tileTexture);
                    textureIndex = mapGenerationData.Value.texArray.Count - 1;
                }

                //���� ��������� ���������� ������
                if (mapGenerationData.Value.isColorUpdated == true)
                {
                    //���� ��������� ����� ����������� ��������, �� ���� ��� ����
                    if (pHS.customMaterial != null)
                    {
                        color = pHS.customMaterial.color;
                    }
                    //����� ���� ����������� ����
                    else
                    {
                        color = mapGenerationData.Value.DefaultShadedColor;
                    }
                }

                //���� ��� ���������� �� ����� �������������
                if (isInitializationUpdate == true)
                {
                    //��������� ������� UV-��������� ���������
                    pHS.uvShadedChunkStart = verticesCount;
                    pHS.uvShadedChunkIndex = chunkIndex;
                    pHS.uvShadedChunkLength = uvArray.Length;
                }

                //��������� ���������� ������������� ���������
                if (mA.Elevation > 0)
                {
                    pHS.ExtrudeAmount = (float)mA.Elevation / mapGenerationData.Value.elevationMaximum;
                }
                else
                {
                    pHS.ExtrudeAmount = 0;
                }

                //��������� ��������� ���������� ��� ���������
                ProvinceRefreshRenderers(ref pHS);

                //��� ������ UV-��������� � ������
                for (int b = 0; b < uvArray.Length; b++)
                {
                    //�������� UV4-����������
                    Vector4 uv4;
                    uv4.x = uvArray[b].x * textureScale.x + textureOffset.x;
                    uv4.y = uvArray[b].y * textureScale.y + textureOffset.y;
                    uv4.z = textureIndex;
                    uv4.w = pHS.ExtrudeAmount;

                    //���� ��� �� ���������� �� ����� �������������
                    if (isInitializationUpdate == false)
                    {
                        //��������� ���������� � ������
                        uvChunk[verticesCount] = uv4;

                        //���� ��������� ���������� ������
                        if (mapGenerationData.Value.isColorUpdated == true)
                        {
                            //��������� ���� � ������
                            colorChunk[verticesCount] = color;
                        }
                    }
                    //�����
                    else
                    {
                        //������� ���������� � ���� � ������
                        uvChunk.Add(uv4);
                        colorChunk.Add(color);
                    }
                }

                //��������� ���������� ������
                verticesCount += uvArray.Length;
            }

            //��� ������� �����
            for (int k = 0; k <= chunkIndex; k++)
            {
                //��������� ������ UV-���������
                mapGenerationData.Value.uvShadedDirty[k] = false;
                mapGenerationData.Value.shadedMeshes[k].SetUVs(0, mapGenerationData.Value.uvShaded[k]);

                //���� ��������� ���������� ������
                if (mapGenerationData.Value.isColorUpdated == true)
                {
                    mapGenerationData.Value.colorShadedDirty[k] = false;
                    mapGenerationData.Value.shadedMeshes[k].SetColors(mapGenerationData.Value.colorShaded[k]);
                }

                mapGenerationData.Value.shadedMFs[k].sharedMesh = mapGenerationData.Value.shadedMeshes[k];
                mapGenerationData.Value.shadedMRs[k].sharedMaterial = mapGenerationData.Value.provinceMaterial;
            }

            //���� ��������� ���������� ������� �������
            if (mapGenerationData.Value.isTextureArrayUpdated)
            {
                //���������� ���������� ������� � �������
                int textureArrayCount = mapGenerationData.Value.texArray.Count;

                //���������� ������ ��������
                mapGenerationData.Value.currentTextureSize = 256;

                //���� �������� ������ ������� �� ����, ������� ���
                if (mapGenerationData.Value.finalTexArray != null)
                {
                    GameObject.DestroyImmediate(mapGenerationData.Value.finalTexArray);
                }

                //������ ����� ������ �������
                mapGenerationData.Value.finalTexArray = new(
                    mapGenerationData.Value.currentTextureSize, mapGenerationData.Value.currentTextureSize,
                    textureArrayCount,
                    TextureFormat.ARGB32,
                    true);

                //��� ������ ��������
                for (int a = 0; a < textureArrayCount; a++)
                {
                    //���� ������ �������� � ������� �� ������������� ������������
                    if (mapGenerationData.Value.texArray[a].width != mapGenerationData.Value.currentTextureSize
                        || mapGenerationData.Value.texArray[a].height != mapGenerationData.Value.currentTextureSize)
                    {
                        //������ �������� � ��������� � ������
                        mapGenerationData.Value.texArray[a] = GameObject.Instantiate(mapGenerationData.Value.texArray[a]);
                        mapGenerationData.Value.texArray[a].hideFlags = HideFlags.DontSave;
                        TextureScaler.Scale(
                            mapGenerationData.Value.texArray[a],
                            mapGenerationData.Value.currentTextureSize, mapGenerationData.Value.currentTextureSize);
                    }
                    //������� �������� � �������� ������
                    mapGenerationData.Value.finalTexArray.SetPixels32(
                        mapGenerationData.Value.texArray[a].GetPixels32(),
                        a);
                }
                //��������� �������� ������ � ����� ��� ��� ������ ������� ��� ���������
                mapGenerationData.Value.finalTexArray.Apply();
                mapGenerationData.Value.provinceMaterial.SetTexture(
                    ShaderParameters.MainTex,
                    mapGenerationData.Value.finalTexArray);

                //��������, ��� ������ ������� ��� �������
                mapGenerationData.Value.isTextureArrayUpdated = false;
            }

            //��������, ��� ����� � UV-���������� ���� ���������
            mapGenerationData.Value.isColorUpdated = false;
            mapGenerationData.Value.isUVUpdatedFast = false;

            //��������� ���������� ������ UV-���������
            mapGenerationData.Value.uvChunkCount = chunkIndex + 1;
        }

        void MapUpdateShadedMaterialsFast()
        {
            //���� ��������� ���������� ������
            if (mapGenerationData.Value.isColorUpdated == true)
            {
                //��� ������� �����
                for (int a = 0; a < mapGenerationData.Value.uvChunkCount; a++)
                {
                    //���� ����� "������"
                    if (mapGenerationData.Value.colorShadedDirty[a] == true)
                    {
                        //��������� ������
                        mapGenerationData.Value.colorShadedDirty[a] = false;
                        mapGenerationData.Value.shadedMeshes[a].SetColors(mapGenerationData.Value.colorShaded[a]);
                    }
                }
            }

            //���� ��������� ���������� UV-���������
            if (mapGenerationData.Value.isUVUpdatedFast == true)
            {
                //��� ������� �����
                for (int a = 0; a < mapGenerationData.Value.uvChunkCount; a++)
                {
                    //���� UV-���������� "������"
                    if (mapGenerationData.Value.uvShadedDirty[a] == true)
                    {
                        //��������� ������
                        mapGenerationData.Value.uvShadedDirty[a] = false;
                        mapGenerationData.Value.shadedMeshes[a].SetUVs(0, mapGenerationData.Value.uvShaded[a]);
                    }
                }
            }
        }

        void MapUpdateLightingMode()
        {
            //��������� ���������
            MapUpdateLightingMaterial(mapGenerationData.Value.provinceMaterial);
            MapUpdateLightingMaterial(mapGenerationData.Value.provinceColoredMaterial);

            mapGenerationData.Value.provinceMaterial.EnableKeyword("HEXA_ALPHA");
        }

        void MapUpdateLightingMaterial(
            Material material)
        {
            //��������� ������ ���������
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
            //���������� ������ ��������
            const int textureSize = 256;

            //���� �������� ����������� ��� � ������ �������
            if (mapGenerationData.Value.bevelNormals == null || mapGenerationData.Value.bevelNormals.width != textureSize)
            {
                //������ ����� ��������
                mapGenerationData.Value.bevelNormals = new(
                    textureSize, textureSize,
                    TextureFormat.ARGB32,
                    false);
            }

            //���������� ������ ��������
            int textureHeight = mapGenerationData.Value.bevelNormals.height;
            int textureWidth = mapGenerationData.Value.bevelNormals.width;

            //���� ������ ������ �������� ����������� ��� ��� ����� �� ������������� ��������
            if (mapGenerationData.Value.bevelNormalsColors == null || mapGenerationData.Value.bevelNormalsColors.Length != textureHeight * textureWidth)
            {
                //������ ����� ������
                mapGenerationData.Value.bevelNormalsColors = new Color[textureHeight * textureWidth];
            }

            //������ ��������� ��� ��������� �������
            Vector2 texturePixel;

            //���������� ������ ����� � ������� ������
            const float bevelWidth = 0.05f;
            float bevelWidthSqr = bevelWidth * bevelWidth;

            //��� ������� ������� �� ������
            for (int y = 0, index = 0; y < textureHeight; y++)
            {
                //���������� ��� ��������� �� Y
                texturePixel.y = (float)y / textureHeight;

                //��� ������� ������� �� ������
                for (int x = 0; x < textureWidth; x++)
                {
                    //���������� ��� ��������� �� X
                    texturePixel.x = (float)x / textureWidth;

                    //�������� R-���������
                    mapGenerationData.Value.bevelNormalsColors[index].r = 0f;

                    //���������� ���������� �� ������� �������
                    float minDistSqr = float.MaxValue;

                    //��� ������� ����� ��������������
                    for (int a = 0; a < 6; a++)
                    {
                        //���� ������� ������
                        Vector2 t0 = mapGenerationData.Value.hexagonUVsExtruded[a];
                        Vector2 t1 = a < 5 ? mapGenerationData.Value.hexagonUVsExtruded[a + 1] : mapGenerationData.Value.hexagonUVsExtruded[0];

                        //���������� ����� �����
                        float l2 = Vector2.SqrMagnitude(t0 - t1);
                        //
                        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(texturePixel - t0, t1 - t0) / l2));
                        //
                        Vector2 projection = t0 + t * (t1 - t0);
                        //
                        float distSqr = Vector2.SqrMagnitude(texturePixel - projection);

                        //���� ���������� ������ ������������, �� ��������� �����������
                        if (distSqr < minDistSqr)
                        {
                            minDistSqr = distSqr;
                        }
                    }

                    //���������� ��������
                    float f = minDistSqr / bevelWidthSqr;
                    //���� �������� ������ �������, ������������ ���
                    if (f > 1f)
                    {
                        f = 1f;
                    }

                    //��������� R-���������
                    mapGenerationData.Value.bevelNormalsColors[index].r = f;

                    //����������� ������ �������
                    index++;
                }
            }

            //����� ������ �������� ��������
            mapGenerationData.Value.bevelNormals.SetPixels(mapGenerationData.Value.bevelNormalsColors);

            //��������� ��������
            mapGenerationData.Value.bevelNormals.Apply();

            //����� �������� ���������
            mapGenerationData.Value.provinceMaterial.SetTexture("_BumpMask", mapGenerationData.Value.bevelNormals);
        }

        void MapUpdateMeshRenderersShadowSupport()
        {
            //��� ������� ������������ ���������
            for (int a = 0; a < mapGenerationData.Value.shadedMRs.Length; a++)
            {
                //���� �������� �� ���� � ��� ��� �����
                if (mapGenerationData.Value.shadedMRs[a] != null
                    && mapGenerationData.Value.shadedMRs[a].name.Equals(MapGenerationData.shaderframeGOName))
                {
                    //����������� ������������ �������� � ������������ �����
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

                //��� ������� �������
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
            //������ GO
            GameObject gO = new GameObject(name);

            //��������� �������� ������ GO
            gO.layer = parent.gameObject.layer;
            gO.transform.SetParent(parent, false);
            gO.transform.localPosition = Vector3.zero;
            gO.transform.localScale = Vector3.one;
            gO.transform.localRotation = Quaternion.Euler(0, 0, 0);

            return gO;
        }

        readonly EcsFilterInject<Inc<RGameShowProvinceHighlight>> gameShowProvinceHighlightRequestFilter = default;
        readonly EcsPoolInject<RGameShowProvinceHighlight> gameShowProvinceHighlightRequestPool = default;

        readonly EcsFilterInject<Inc<RGameHideProvinceHighlight>> gameHideProvinceHighlightRequestFilter = default;
        readonly EcsPoolInject<RGameHideProvinceHighlight> gameHideProvinceHighlightRequestPool = default;
        void ProvinceHighlightRequest()
        {
            //��� ������� ������� ��������� ��������� ���������
            foreach (int requestEntity in gameShowProvinceHighlightRequestFilter.Value)
            {
                //���� ������
                ref RGameShowProvinceHighlight requestComp = ref gameShowProvinceHighlightRequestPool.Value.Get(requestEntity);

                //���� ������������� ��������� ��������� ���������
                if (requestComp.requestType == ProvinceHighlightRequestType.Hover)
                {
                    //�������� ��������� ��������� ���������
                    ProvinceShowHoverHighlight(ref requestComp);
                }

                gameShowProvinceHighlightRequestPool.Value.Del(requestEntity);
            }

            //��� ������� ������� ���������� ��������� ���������
            foreach (int requestEntity in gameHideProvinceHighlightRequestFilter.Value)
            {
                //���� ������
                ref RGameHideProvinceHighlight requestComp = ref gameHideProvinceHighlightRequestPool.Value.Get(requestEntity);

                //���� ������������� ��������� ��������� ���������
                if (requestComp.requestType == ProvinceHighlightRequestType.Hover)
                {
                    //��������� ��������� ��������� ���������
                    ProvinceHideHoverHighlight(ref requestComp);
                }

                gameHideProvinceHighlightRequestPool.Value.Del(requestEntity);
            }
        }

        void ProvinceSetColor(
            ref CProvinceCore pC,
            Color color)
        {
            //���� ������������ ��������
            Material material;

            //���� �������� ������ ����� ��� ���������� � ���� ������� ����������
            if (mapGenerationData.Value.colorCache.ContainsKey(color) == false)
            {
                //�� ������ ����� �������� � �������� ���
                material = GameObject.Instantiate(mapGenerationData.Value.provinceColoredMaterial);
                mapGenerationData.Value.colorCache.Add(color, material);

                //��������� �������� ������ ���������
                material.hideFlags = HideFlags.DontSave;
                material.color = color;
                material.SetFloat(ShaderParameters.ProvinceAlpha, 1f);
            }
            //�����
            else
            {
                //���� �������� �� �������
                material = mapGenerationData.Value.colorCache[color];
            }

            //������������� �������� ���������
            ProvinceSetMaterial(
                pC.Index,
                material);
        }

        bool ProvinceSetMaterial(
            int provinceIndex,
            Material material,
            bool temporary = false)
        {
            //���� ���������
            provincesData.Value.provincePEs[provinceIndex].Unpack(world.Value, out int provinceEntity);
            ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);
            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

            //���� ���� �������� �������� ��� �������� ���������
            if (pHS.customMaterial == material)
            {
                //�� ������ �� ��������
                return false;
            }

            //���� ���� ���������
            Color32 materialColor = Color.white;
            if (material.HasProperty(ShaderParameters.Color) == true)
            {
                materialColor = material.color;
            }
            else if (material.HasProperty(ShaderParameters.BaseColor))
            {
                materialColor = material.GetColor(ShaderParameters.BaseColor);
            }

            //��������, ��� ��������� ���������� ������
            mapGenerationData.Value.isColorUpdated = true;

            //���� �������� ���������
            Texture materialTexture = null;
            if (material.HasProperty(ShaderParameters.MainTex))
            {
                materialTexture = material.mainTexture;
            }
            else if (material.HasProperty(ShaderParameters.BaseMap))
            {
                materialTexture = material.GetTexture(ShaderParameters.BaseMap);
            }

            //���� �������� �� �����
            if (materialTexture != null)
            {
                //��������, ��� ��������� ���������� ������� �������
                mapGenerationData.Value.isTextureArrayUpdated = true;
            }
            //�����
            else
            {
                List<Color32> colorChunk = mapGenerationData.Value.colorShaded[pHS.uvShadedChunkIndex];
                for (int k = 0; k < pHS.uvShadedChunkLength; k++)
                {
                    colorChunk[pHS.uvShadedChunkStart + k] = materialColor;
                }
                mapGenerationData.Value.colorShadedDirty[pHS.uvShadedChunkIndex] = true;
            }

            //���� �������� - �� �������� ��������� ���������
            if (material != mapGenerationData.Value.hoverProvinceHighlightMaterial)
            {
                //���� ��� ��������� ��������
                if (temporary == true)
                {
                    pHS.tempMaterial = material;
                }
                //�����
                else
                {
                    pHS.customMaterial = material;
                }
            }

            return true;
        }

        readonly EcsPoolInject<CProvinceHoverHighlightRenderer> provinceHoverHighlightRendererPool = default;
        void ProvinceShowHoverHighlight(
            ref RGameShowProvinceHighlight requestComp)
        {
            //���� ���������
            requestComp.provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

            //���� � ��������� ��� ���������� ��������� ���������
            if (provinceHoverHighlightRendererPool.Value.Has(provinceEntity) == false)
            {
                //��������� ���
                ref CProvinceHoverHighlightRenderer provinceRenderer = ref provinceHoverHighlightRendererPool.Value.Add(provinceEntity);

                //������ ����� �������� 
                GOProvinceRenderer.InstantiateProvinceRenderer(ref pHS, ref provinceRenderer);

                provinceRenderer.renderer.provinceRenderer.material = mapGenerationData.Value.hoverProvinceHighlightMaterial;
            }
        }

        void ProvinceHideHoverHighlight(
            ref RGameHideProvinceHighlight requestComp)
        {
            //���� ���������
            requestComp.provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

            //���� � ��������� ���� ��������� ��������� ���������
            if (provinceHoverHighlightRendererPool.Value.Has(provinceEntity) == true)
            {
                //���� ���
                ref CProvinceHoverHighlightRenderer provinceRenderer = ref provinceHoverHighlightRendererPool.Value.Get(provinceEntity);

                //�������� ���
                GOProvinceRenderer.CacheProvinceRenderer(ref provinceRenderer);

                //������� ��������� ���������
                provinceHoverHighlightRendererPool.Value.Del(provinceEntity);
            }
        }

        void ProvinceRefreshRenderers(
            ref CProvinceHexasphere pHS)
        {
            //���� �������� ���������
            pHS.selfPE.Unpack(world.Value, out int provinceEntity);

            //���� � ��������� ���� ��������� ��������� ���������
            if (provinceHoverHighlightRendererPool.Value.Has(provinceEntity) == true)
            {
                //���� ���
                ref CProvinceHoverHighlightRenderer provinceRenderer = ref provinceHoverHighlightRendererPool.Value.Get(provinceEntity);

                //��������� ���
                GOProvinceRenderer.RefreshProvinceRenderer(
                    ref pHS,
                    provinceRenderer.renderer);
            }
        }
    }
}