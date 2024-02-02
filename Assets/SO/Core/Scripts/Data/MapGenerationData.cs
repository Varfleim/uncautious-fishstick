
using System.Collections.Generic;

using SO.Map.Hexasphere;

using UnityEngine;

namespace SO.Map
{
    public class MapGenerationData : MonoBehaviour
    {
        //Карта
        public const float PHI = 1.61803399f;
        public const string shaderframeName = "ShadedFrame";
        public const string shaderframeGOName = "Shade";
        public const string regionsRootName = "TilesRoot";
        const int maxHexasphereParts = 50;
        public const int maxVertexCountPerChunk = 65500;
        const int vertexArraySize = 65530;

        public readonly int[] hexagonIndices = new int[] {
            0, 1, 5,
            1, 2, 5,
            4, 5, 2,
            3, 4, 2
        };
        public readonly int[] hexagonIndicesExtruded = new int[]
        {
            0, 1, 6,
            5, 0, 6,
            1, 2, 5,
            4, 5, 2,
            2, 3, 7,
            3, 4, 7
        };
        public readonly Vector2[] hexagonUVs = new Vector2[] {
            new Vector2 (0, 0.5f),
            new Vector2 (0.25f, 1f),
            new Vector2 (0.75f, 1f),
            new Vector2 (1f, 0.5f),
            new Vector2 (0.75f, 0f),
            new Vector2 (0.25f, 0f)
        };
        public readonly Vector2[] hexagonUVsExtruded = new Vector2[]
        {
            new Vector2 (0, 0.5f),
            new Vector2 (0.25f, 1f),
            new Vector2 (0.75f, 1f),
            new Vector2 (1f, 0.5f),
            new Vector2 (0.75f, 0f),
            new Vector2 (0.25f, 0f),
            new Vector2 (0.25f, 0.5f),
            new Vector2 (0.75f, 0.5f)
        };
        public readonly int[] pentagonIndices = new int[] {
            0, 1, 4,
            1, 2, 4,
            3, 4, 2
        };
        public readonly int[] pentagonIndicesExtruded = new int[]
        {
            0, 1, 5,
            4, 0, 5,
            1, 2, 4,
            2, 3, 6,
            3, 4, 6
        };
        public readonly Vector2[] pentagonUVs = new Vector2[] {
            new Vector2 (0, 0.33f),
            new Vector2 (0.25f, 1f),
            new Vector2 (0.75f, 1f),
            new Vector2 (1f, 0.33f),
            new Vector2 (0.5f, 0f),
        };
        public readonly Vector2[] pentagonUVsExtruded = new Vector2[]
        {
            new Vector2 (0, 0.33f),
            new Vector2 (0.25f, 1f),
            new Vector2 (0.75f, 1f),
            new Vector2 (1f, 0.33f),
            new Vector2 (0.5f, 0f),
            new Vector2 (0.375f, 0.5f),
            new Vector2 (0.625f, 0.5f)
        };

        public int subdivisions;
        public static float ExtrudeMultiplier;
        public float extrudeMultiplier;
        public float hexasphereScale;

        public int currentTextureSize;
        public float gradientIntensity;
        public Color tileTintColor;
        public Color ambientColor;
        public float minimumLight;

        public int uvChunkCount;
        public Texture2DArray finalTexArray;

        public Texture2D bevelNormals;
        public Color[] bevelNormalsColors;


        public bool isInitializationUpdate;
        public bool isMaterialUpdated;
        public bool isRegionUpdated;
        public bool isColorUpdated;
        public bool isTextureArrayUpdated;
        public bool isUVUpdatedFast;

        public readonly Dictionary<DHexaspherePoint, DHexaspherePoint> points = new();
        public readonly Dictionary<DHexaspherePoint, int> verticesIndices = new();
        public readonly List<Vector3>[] verticesShaded = new List<Vector3>[maxHexasphereParts];
        public readonly List<int>[] indicesShaded = new List<int>[maxHexasphereParts];
        public readonly List<Vector4>[] uvShaded = new List<Vector4>[maxHexasphereParts];
        public readonly List<Vector4>[] uv2Shaded = new List<Vector4>[maxHexasphereParts];
        public readonly List<Color32>[] colorShaded = new List<Color32>[maxHexasphereParts];

        public readonly List<Texture2D> texArray = new List<Texture2D>(255);
        public readonly Dictionary<Color, Texture2D> solidTexCache = new Dictionary<Color, Texture2D>();
        public readonly Mesh[] shadedMeshes = new Mesh[maxHexasphereParts];
        public readonly MeshFilter[] shadedMFs = new MeshFilter[maxHexasphereParts];
        public readonly MeshRenderer[] shadedMRs = new MeshRenderer[maxHexasphereParts];
        public readonly bool[] colorShadedDirty = new bool[maxHexasphereParts];
        public readonly bool[] uvShadedDirty = new bool[maxHexasphereParts];
        public readonly Dictionary<Color, Material> colorCache = new Dictionary<Color, Material>();

        public Color DefaultShadedColor
        {
            get
            {
                return defaultShadedColor;
            }
        }
        [SerializeField]
        [ColorUsage(true, true)]
        Color defaultShadedColor = new Color(0.56f, 0.71f, 0.54f);

        public int TileTextureSize
        {
            get
            {
                return tileTextureSize;
            }
        }
        int tileTextureSize = 256;

        public Texture2D whiteTex;
        public Material regionMaterial;
        public Material regionColoredMaterial;

        public Material fleetRegionHighlightMaterial;
        public Material hoverRegionHighlightMaterial;
        public Material currentRegionHighlightMaterial;

        public List<T> CheckList<T>(
            ref List<T> l)
        {
            //Если список пуст
            if (l == null)
            {
                //Создаём новый с максимальным размером массива вершин
                l = new List<T>(vertexArraySize);
            }
            //Иначе
            else
            {
                //Очищаем список
                l.Clear();
            }

            //Возвращаем список
            return l;
        }

        [Range(0f, 0.5f)]
        public float jitterProbability = 0.25f;
        [Range(20, 200)]
        public int chunkSizeMin = 30;
        [Range(20, 200)]
        public int chunkSizeMax = 100;

        [Range(0, 100)]
        public int landPercentage = 50;
        [Range(0, 5)]
        public int waterLevel = 3;
        [Range(0f, 1f)]
        public float highRiseProbability = 0.25f;
        [Range(0f, 0.4f)]
        public float sinkProbability = 0.2f;

        [Range(-4, 0)]
        public int elevationMinimum = -2;
        [Range(6, 10)]
        public int elevationMaximum = 8;

        [Range(0, 100)]
        public int erosionPercentage = 50;

        [Range(0f, 1f)]
        public float startingMoisture = 0.1f;
        [Range(0f, 1f)]
        public float evaporationFactor = 0.5f;
        [Range(0f, 1f)]
        public float precipitationFactor = 0.25f;
        [Range(0f, 1f)]
        public float runoffFactor = 0.25f;
        [Range(0f, 1f)]
        public float seepageFactor = 0.125f;
        [Range(1f, 10f)]
        public float windStrength = 4f;

        [Range(0f, 1f)]
        public float lowTemperature = 0f;
        [Range(0f, 1f)]
        public float highTemperature = 1f;
        [Range(0f, 1f)]
        public float temperatureJitter = 0.1f;

        public float[] temperatureBands = { 0.1f, 0.3f, 0.6f };
        public float[] moistureBands = { 0.12f, 0.28f, 0.85f };
        public DRegionBiome[] biomes =
        {
        new DRegionBiome(0, 0), new DRegionBiome(4, 0), new DRegionBiome(4, 0), new DRegionBiome(4, 0),
        new DRegionBiome(0, 0), new DRegionBiome(2, 0), new DRegionBiome(2, 1), new DRegionBiome(2, 2),
        new DRegionBiome(0, 0), new DRegionBiome(1, 0), new DRegionBiome(1, 1), new DRegionBiome(1, 2),
        new DRegionBiome(0, 0), new DRegionBiome(1, 1), new DRegionBiome(1, 2), new DRegionBiome(1, 3)
        };
    }
}