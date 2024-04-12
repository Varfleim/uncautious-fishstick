
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using Leopotam.EcsLite.Unity.Ugui;

using SO.UI;
using SO.Time;
using SO.Map;
using SO.Map.Hexasphere;
using SO.Map.Generation;
using SO.Map.StrategicArea;
using SO.Map.Region;
using SO.Country;
using SO.Population;
using SO.Warfare.Fleet;
using SO.Warfare.Fleet.Missions;
using SO.Warfare.Fleet.Movement;

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
        public UIData uIData;
        public MapGenerationData mapGenerationData;
        public RegionsData regionsData;
        public CountriesData countriesData;
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
                //��������� �������������� ��������
                new SMapStrategicArea(),
                //��������� ��������� �������������� ��������
                new SMapStrategicAreaTerrain(),

                //���������� ��������
                new SCountryControl(),
                
                //���������� ��������������� ��������
                new SMapRegionInitializerControl(),
                
                //���������� ��������������� ���������
                new SStrategicAreaControl(),
                //���������� ���������
                new SRegionControl(),
                
                //���������� �������� ���������
                new SPopulationControl())
                //������ ����������� � SEventControl � ��� �� �����

                //��������� �����
                .Add(new SUIInput())

                //���������� ������������ ��������
                .Add(new STaskForceControl())

                //���������� �������� ����������� �����
                .Add(new STaskForceMissionsControl())

                //���������� ���������, ����������� �� ���� ������
                .Add(new SEventControl())

                //������������
                .Add(new SMapControl())
                .Add(new SUIDisplay())

                //������� �������
                .Add(new SEventClear())

                .AddWorld(new EcsWorld(), "uguiMapEventsWorld")
                .InjectUgui(uguiMapEmitter, "uguiMapEventsWorld")
                .AddWorld(new EcsWorld(), "uguiUIEventsWorld")
                .InjectUgui(uguiUIEmitter, "uguiUIEventsWorld")

                .Inject(
                staticData,
                sceneData,
                uIData,
                mapGenerationData,
                regionsData,
                countriesData,
                inputData,
                runtimeData)
                .Inject(sOUI);

            perTickSystems

                //���������� ��������������� ���������
                .Add(new SStrategicAreaControl())
                //���������� ���������
                .Add(new SRegionControl())

                //���������� �������� ���������
                .Add(new SPopulationControl())

                //���������� ������������ ��������
                .Add(new STaskForceControl())

                //���������� �������� ����������� �����
                .Add(new STaskForceMissionsControl())

                //����� ����
                .Add(new STaskForcePathfindingRequestAssign())
                .Add(new SMTTaskForcePathfinding())
                .Add(new STaskForcePathfindingSelfRequestDelete())
                //����� ����
                //�����������
                .Add(new SMTTaskForceMovement())
                .Add(new STaskForceMovementStop())
                //�����������

                //�������� ���������� ���� �������
                .Add(new STaskForceTargetCheck())


                //���������� ���������� ����� ����
                .Add(new SUITickUpdate())

                .Inject(
                staticData,
                sceneData,
                uIData,
                mapGenerationData,
                regionsData,
                countriesData,
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