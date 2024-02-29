
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.UI;
using SO.UI.Game.Map.Events;
using SO.Map.Events;
using SO.Character;
using SO.Map.Hexasphere;
using SO.Map.StrategicArea;

namespace SO.Map
{
    public class SMapControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CRegionCore>> regionFilter = default; 
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        readonly EcsPoolInject<CExplorationRegionFractionObject> exFRFOPool = default;

        readonly EcsFilterInject<Inc<CStrategicArea>> sAFilter = default;
        readonly EcsPoolInject<CStrategicArea> sAPool = default;

        //���������
        readonly EcsPoolInject<CCharacter> characterPool = default;


        //������
        readonly EcsCustomInject<SceneData> sceneData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;
        readonly EcsCustomInject<InputData> inputData = default;

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
            mapGenerationData.Value.fleetRegionHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));
            mapGenerationData.Value.hoverRegionHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));
            mapGenerationData.Value.currentRegionHighlightMaterial.SetFloat(
                ShaderParameters.ColorShift, Mathf.PingPong(UnityEngine.Time.time * 0.25f, 1f));

            //���� ��������� ���������� ����������
            if (mapGenerationData.Value.isMaterialUpdated == true)
            {
                //��������� �������� ����������
                MapMaterialPropertiesUpdate(mapGenerationData.Value.isInitializationUpdate);

                mapGenerationData.Value.isMaterialUpdated = false;
                mapGenerationData.Value.isRegionUpdated = false;
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

            //��������� ������� ��������� ��������
            RegionHighlightRequest();
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
                //���� ������������� ����������� ������ �������������� ��������
                else if(requestComp.requestType == ChangeMapModeRequestType.StrategicArea)
                {
                    //��������� ����� ����� �������������� �������� ��� ��������
                    inputData.Value.mapMode = MapMode.StrategicArea;

                    //���������� ����� ����� �������������� ��������
                    MapDisplayModeStrategicArea();
                }
                //�����, ���� ������������� ����������� ������ ������������
                else if(requestComp.requestType == ChangeMapModeRequestType.Exploration)
                {
                    //��������� ����� ������������ ��� ��������
                    inputData.Value.mapMode = MapMode.Exploration;

                    //���������� ����� ������������
                    MapDisplayModeExploration();
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
            //�����, ���� ������� ����� ����� - ����� �������������� ��������
            else if(inputData.Value.mapMode == MapMode.StrategicArea)
            {
                //���������� ����� �������������� ��������
                MapDisplayModeStrategicArea();
            }
            //�����, ���� ������� ����� ����� - ����� ������������
            else if(inputData.Value.mapMode == MapMode.Exploration)
            {
                //���������� ����� ������������
                MapDisplayModeExploration();
            }
        }

        void MapDisplayModeTerrain()
        {
            //��� ������ �������������� �������
            foreach (int sAEntity in sAFilter.Value)
            {
                //���� �������������� �������
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                //���������� ���� �������
                Color sAColor = Color.Lerp(Color.yellow, Color.red, (float)sA.Elevation / mapGenerationData.Value.elevationMaximum);

                //��� ������� ������� �������
                for(int a = 0; a < sA.regionPEs.Length; a++)
                {
                    //���� ������
                    sA.regionPEs[a].Unpack(world.Value, out int regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //������������� ���� ������� �������������� ������ �������
                    RegionSetColor(
                        ref rC,
                        sAColor);

                }
            }
        }

        void MapDisplayModeStrategicArea()
        {
            //��� ������ �������������� �������
            foreach(int sAEntity in sAFilter.Value)
            {
                //���� �������������� �������
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                //��� ������� ������� �������
                for(int a = 0; a < sA.regionPEs.Length; a++)
                {
                    //���� ������
                    sA.regionPEs[a].Unpack(world.Value, out int regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //������������� ���� ������� 
                    RegionSetColor(
                        ref rC,
                        sA.selfColor);
                }
            }
        }

        void MapDisplayModeExploration()
        {
            //���� ��������� ������
            inputData.Value.playerCharacterPE.Unpack(world.Value, out int characterEntity);
            ref CCharacter character = ref characterPool.Value.Get(characterEntity);

            //��� ������� �������
            foreach (int regionEntity in regionFilter.Value)
            {
                //���� ������
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //���� ExFRFO ���������
                rC.rFOPEs[character.selfIndex].rFOPE.Unpack(world.Value, out int fRFOEntity);
                ref CExplorationRegionFractionObject exFRFO = ref exFRFOPool.Value.Get(fRFOEntity);

                //����
                //������������� ���� ������� �������������� ��� ������ ������������
                RegionSetColor(
                    ref rC,
                    new Color32(
                        exFRFO.explorationLevel, exFRFO.explorationLevel, exFRFO.explorationLevel,
                        255));
                //����
            }
        }

        void MapMaterialPropertiesUpdate(
            bool isInitializationUpdate = false)
        {
            //���� ����� �������� �������
            if (mapGenerationData.Value.isRegionUpdated == true)
            {
                //��������� �������
                MapRebuildTiles();

                //��������, ��� ������� ���� ���������
                mapGenerationData.Value.isRegionUpdated = false;
            }

            //��������� ��������� �������� � ����
            MapUpdateShadedMaterials(isInitializationUpdate);
            MapUpdateMeshRenderersShadowSupport();

            //��������� �������� ��������
            mapGenerationData.Value.regionMaterial.SetFloat("_GradientIntensity", 1f - mapGenerationData.Value.gradientIntensity);
            mapGenerationData.Value.regionMaterial.SetFloat("_ExtrusionMultiplier", MapGenerationData.ExtrudeMultiplier);
            mapGenerationData.Value.regionMaterial.SetColor("_Color", mapGenerationData.Value.tileTintColor);
            mapGenerationData.Value.regionMaterial.SetColor("_AmbientColor", mapGenerationData.Value.ambientColor);
            mapGenerationData.Value.regionMaterial.SetFloat("_MinimumLight", mapGenerationData.Value.minimumLight);

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

            //���������� ���������� ��������
            int tileCount = regionsData.Value.regionPEs.Length;

            //������ ������� �������� ������ � ����������
            int[] hexIndices = mapGenerationData.Value.hexagonIndicesExtruded;
            int[] pentIndices = mapGenerationData.Value.pentagonIndicesExtruded;

            //��� ������� �������
            for (int k = 0; k < tileCount; k++)
            {
                //���� ��������� �������
                regionsData.Value.regionPEs[k].Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

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

                //���� ������ ������ �������
                DHexaspherePoint[] tileVertices = rHS.vertexPoints;

                //���������� ���������� ������
                int tileVerticesCount = tileVertices.Length;

                //������ ��������� ��� UV4-���������
                Vector4 gpos = Vector4.zero;

                //��� ������ �������
                for (int b = 0; b < tileVerticesCount; b++)
                {
                    //���� ������� �������
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

                //���� ����� ������ ������� ����� �����
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


                mr.sharedMaterial = mapGenerationData.Value.regionMaterial;
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

            //���������� ���������� ��������
            int tileCount = regionsData.Value.regionPEs.Length;

            //���������� ����������� ���� �������
            Color32 color = mapGenerationData.Value.DefaultShadedColor;

            //��� ������� �������
            for (int k = 0; k < tileCount; k++)
            {
                //���� ��������� �������
                regionsData.Value.regionPEs[k].Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //���� ������������ �������������� ������� �������
                rC.ParentStrategicAreaPE.Unpack(world.Value, out int sAEntity);
                ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

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

                //���� ������ ������ �������
                DHexaspherePoint[] tileVertices = rHS.vertexPoints;

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

                //�������� ���� ������� ��� �������� � ������ �������
                Texture2D tileTexture;

                //���������� ������, ������� � �������� ��������
                int textureIndex = 0;
                Vector2 textureScale;
                Vector2 textureOffset;

                //���� ������ ����� ����������� ��������, ���� �������� ����� �������� � �������� �� �����
                if (rHS.customMaterial && rHS.customMaterial.HasProperty(ShaderParameters.MainTex) && rHS.customMaterial.mainTexture != null)
                {
                    //���� ��������� ���� ��������
                    tileTexture = (Texture2D)rHS.customMaterial.mainTexture;
                    textureIndex = mapGenerationData.Value.texArray.IndexOf(tileTexture);
                    textureScale = rHS.customMaterial.mainTextureScale;
                    textureOffset = rHS.customMaterial.mainTextureOffset;
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
                    //���� ������ ����� ����������� ��������, �� ���� ��� ����
                    if (rHS.customMaterial != null)
                    {
                        color = rHS.customMaterial.color;
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
                    //��������� ������� UV-��������� �������
                    rHS.uvShadedChunkStart = verticesCount;
                    rHS.uvShadedChunkIndex = chunkIndex;
                    rHS.uvShadedChunkLength = uvArray.Length;
                }

                //��������� ���������� ������������� �������
                if (sA.Elevation > 0)
                {
                    rHS.ExtrudeAmount = (float)sA.Elevation / mapGenerationData.Value.elevationMaximum;
                }
                else
                {
                    rHS.ExtrudeAmount = 0;
                }

                //��������� ��������� ���������� ��� �������
                RegionRefreshRenderers(ref rHS);

                //��� ������ UV-��������� � ������
                for (int b = 0; b < uvArray.Length; b++)
                {
                    //�������� UV4-����������
                    Vector4 uv4;
                    uv4.x = uvArray[b].x * textureScale.x + textureOffset.x;
                    uv4.y = uvArray[b].y * textureScale.y + textureOffset.y;
                    uv4.z = textureIndex;
                    uv4.w = rHS.ExtrudeAmount;

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
                mapGenerationData.Value.shadedMRs[k].sharedMaterial = mapGenerationData.Value.regionMaterial;
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
                mapGenerationData.Value.regionMaterial.SetTexture(
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
            MapUpdateLightingMaterial(mapGenerationData.Value.regionMaterial);
            MapUpdateLightingMaterial(mapGenerationData.Value.regionColoredMaterial);

            mapGenerationData.Value.regionMaterial.EnableKeyword("HEXA_ALPHA");
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
            mapGenerationData.Value.regionMaterial.SetTexture("_BumpMask", mapGenerationData.Value.bevelNormals);
        }

        void MapUpdateMeshRenderersShadowSupport()
        {
            //��� ������� ������������ ��������
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

        readonly EcsFilterInject<Inc<RGameShowRegionHighlight>> gameShowRegionHighlightRequestFilter = default;
        readonly EcsPoolInject<RGameShowRegionHighlight> gameShowRegionHighlightRequestPool = default;

        readonly EcsFilterInject<Inc<RGameHideRegionHighlight>> gameHideRegionHighlightRequestFilter = default;
        readonly EcsPoolInject<RGameHideRegionHighlight> gameHideRegionHighlightRequestPool = default;
        void RegionHighlightRequest()
        {
            //��� ������� ������� ��������� ��������� �������
            foreach (int requestEntity in gameShowRegionHighlightRequestFilter.Value)
            {
                //���� ������
                ref RGameShowRegionHighlight requestComp = ref gameShowRegionHighlightRequestPool.Value.Get(requestEntity);

                //���� ������������� ��������� ��������� �������
                if (requestComp.requestType == RegionHighlightRequestType.Hover)
                {
                    //�������� ��������� ��������� �������
                    RegionShowHoverHighlight(ref requestComp);
                }

                gameShowRegionHighlightRequestPool.Value.Del(requestEntity);
            }

            //��� ������� ������� ���������� ��������� �������
            foreach (int requestEntity in gameHideRegionHighlightRequestFilter.Value)
            {
                //���� ������
                ref RGameHideRegionHighlight requestComp = ref gameHideRegionHighlightRequestPool.Value.Get(requestEntity);

                //���� ������������� ��������� ��������� �������
                if (requestComp.requestType == RegionHighlightRequestType.Hover)
                {
                    //��������� ��������� ��������� �������
                    RegionHideHoverHighlight(ref requestComp);
                }

                gameHideRegionHighlightRequestPool.Value.Del(requestEntity);
            }
        }

        void RegionSetColor(
            ref CRegionCore rC,
            Color color)
        {
            //���� ������������ ��������
            Material material;

            //���� �������� ������ ����� ��� ���������� � ���� ������� ����������
            if (mapGenerationData.Value.colorCache.ContainsKey(color) == false)
            {
                //�� ������ ����� �������� � �������� ���
                material = GameObject.Instantiate(mapGenerationData.Value.regionColoredMaterial);
                mapGenerationData.Value.colorCache.Add(color, material);

                //��������� �������� ������ ���������
                material.hideFlags = HideFlags.DontSave;
                material.color = color;
                material.SetFloat(ShaderParameters.RegionAlpha, 1f);
            }
            //�����
            else
            {
                //���� �������� �� �������
                material = mapGenerationData.Value.colorCache[color];
            }

            //������������� �������� �������
            RegionSetMaterial(
                rC.Index,
                material);
        }

        bool RegionSetMaterial(
            int regionIndex,
            Material material,
            bool temporary = false)
        {
            //���� ������
            regionsData.Value.regionPEs[regionIndex].Unpack(world.Value, out int regionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

            //���� ���� �������� �������� ��� �������� �������
            if (rHS.customMaterial == material)
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
                List<Color32> colorChunk = mapGenerationData.Value.colorShaded[rHS.uvShadedChunkIndex];
                for (int k = 0; k < rHS.uvShadedChunkLength; k++)
                {
                    colorChunk[rHS.uvShadedChunkStart + k] = materialColor;
                }
                mapGenerationData.Value.colorShadedDirty[rHS.uvShadedChunkIndex] = true;
            }

            //���� �������� - �� �������� ��������� ���������
            if (material != mapGenerationData.Value.hoverRegionHighlightMaterial)
            {
                //���� ��� ��������� ��������
                if (temporary == true)
                {
                    rHS.tempMaterial = material;
                }
                //�����
                else
                {
                    rHS.customMaterial = material;
                }
            }

            return true;
        }

        readonly EcsPoolInject<CRegionHoverHighlightRenderer> regionHoverHighlightRendererPool = default;
        void RegionShowHoverHighlight(
            ref RGameShowRegionHighlight requestComp)
        {
            //���� ������
            requestComp.regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

            //���� � ������� ��� ���������� ��������� ���������
            if (regionHoverHighlightRendererPool.Value.Has(regionEntity) == false)
            {
                //��������� ���
                ref CRegionHoverHighlightRenderer regionRenderer = ref regionHoverHighlightRendererPool.Value.Add(regionEntity);

                //������ ����� �������� 
                GORegionRenderer.InstantiateRegionRenderer(ref rHS, ref regionRenderer);

                regionRenderer.renderer.regionRenderer.material = mapGenerationData.Value.hoverRegionHighlightMaterial;
            }
        }

        void RegionHideHoverHighlight(
            ref RGameHideRegionHighlight requestComp)
        {
            //���� ������
            requestComp.regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

            //���� � ������� ���� ��������� ��������� ���������
            if (regionHoverHighlightRendererPool.Value.Has(regionEntity) == true)
            {
                //���� ���
                ref CRegionHoverHighlightRenderer regionRenderer = ref regionHoverHighlightRendererPool.Value.Get(regionEntity);

                //�������� ���
                GORegionRenderer.CacheRegionRenderer(ref regionRenderer);

                //������� ��������� ���������
                regionHoverHighlightRendererPool.Value.Del(regionEntity);
            }
        }

        void RegionRefreshRenderers(
            ref CRegionHexasphere rHS)
        {
            //���� �������� �������
            rHS.selfPE.Unpack(world.Value, out int regionEntity);

            //���� � ������� ���� ��������� ��������� ���������
            if (regionHoverHighlightRendererPool.Value.Has(regionEntity) == true)
            {
                //���� ���
                ref CRegionHoverHighlightRenderer regionRenderer = ref regionHoverHighlightRendererPool.Value.Get(regionEntity);

                //��������� ���
                GORegionRenderer.RefreshRegionRenderer(
                    ref rHS,
                    regionRenderer.renderer);
            }
        }
    }
}