
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using Leopotam.EcsLite.Unity.Ugui;

using SO.UI;
using SO.Time;
using SO.Map;
using SO.Map.RFO;
using SO.Faction;

namespace SO
{
    public class SOStartup : MonoBehaviour
    {
        EcsWorld world;
        EcsSystems perFrameSystems;
        EcsSystems perTickSystems;

        [SerializeField] EcsUguiEmitter uguiMapEmitter;
        [SerializeField] EcsUguiEmitter uguiUIEmitter;

        public StaticData staticData;

        public SceneData sceneData;
        public MapGenerationData mapGenerationData;
        public RegionsData regionsData;
        public FactionsData factionsData;
        public InputData inputData;

        public RuntimeData runtimeData;

        public SOUI sOUI;

        void Start()
        {
            world = new EcsWorld();
            perFrameSystems = new EcsSystems(world);
            perTickSystems = new EcsSystems(world);
            RuntimeData runtimeData = new();

            Random.InitState(sceneData.seed);
            Formulas.random = new System.Random(sceneData.seed);

            perFrameSystems

                //������, ���������� �� ������ ����� ����
                //������ ���������� ��� ������� �� ������ "������ ����� ����"
                .AddGroup("NewGame", false, null,
                //�������� �������������
                new SNewGameInitializationMain(),
                //��������� ����������
                new SMapHexasphere(),
                //��������� ���������
                new SMapTerrain(),
                //��������� �������
                new SMapClimate(),

                //������������� RFO
                new SRFOInitialization(),

                //���������� ���������
                new SFactionControl(),
                
                //���������� ��������������� ��������
                new SMapRegionInitializerControl(),
                
                //���������� RFO
                new SRFOControl())
                //������ ����������� � SEventControl � ��� �� �����

                //��������� �����
                .Add(new SUIInput())

                //���������� ���������, ����������� �� ���� ������
                .Add(new SEventControl())

                //������������
                .Add(new SUIDisplay())
                .Add(new SMapControl())

                .AddWorld(new EcsWorld(), "uguiMapEventsWorld")
                .InjectUgui(uguiMapEmitter, "uguiMapEventsWorld")
                .AddWorld(new EcsWorld(), "uguiUIEventsWorld")
                .InjectUgui(uguiUIEmitter, "uguiUIEventsWorld")

                .Inject(
                staticData,
                sceneData,
                mapGenerationData,
                regionsData,
                factionsData,
                inputData,
                runtimeData)
                .Inject(sOUI);

            perTickSystems

                //���������� RFO
                .Add(new SRFOControl())

                //������� ������������ �� ������������
                .Add(new SMTObserverExplorationCalc())

                //���������� ���������� ����� ����
                .Add(new SUITickUpdate())

                .Inject(
                staticData,
                sceneData,
                mapGenerationData,
                regionsData,
                factionsData,
                inputData,
                runtimeData)
                .Inject(sOUI);

            perFrameSystems.Init();
            perTickSystems.Init();

            TimeTickSystem.Create();

            TimeTickSystem.OnTick += delegate (object sender, TimeTickSystem.OnTickEventArgs e)
            {
                if (runtimeData.isGameActive == true)
                {
                    Debug.Log("Stage 1 " + System.DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss:fff"));
                    perTickSystems.Run();
                    Debug.Log("Stage 2 " + System.DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss:fff"));
                }
            };
        }

        void Update()
        {
            perFrameSystems?.Run();
        }

        void OnDestroy()
        {
            if (perFrameSystems != null)
            {
                perFrameSystems.Destroy();
                perFrameSystems = null;
            }
            if (perTickSystems != null)
            {
                perTickSystems.Destroy();
                perTickSystems = null;
            }

            if (world != null)
            {
                world.Destroy();
                world = null;
            }
        }
    }
}