
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using Leopotam.EcsLite.Unity.Ugui;

using SCM.UI;
using SCM.Time;
using SCM.Map;

namespace SCM
{
    public class SandColonyManagementStartup : MonoBehaviour
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
        public InputData inputData;

        public RuntimeData runtimeData;

        public SCMUI sCMUI;

        void Start()
        {
            world = new EcsWorld();
            perFrameSystems = new EcsSystems(world);
            perTickSystems = new EcsSystems(world);
            RuntimeData runtimeData = new();

            Random.InitState(sceneData.seed);
            Formulas.random = new System.Random(sceneData.seed);

            perFrameSystems

                //Группа, отвечающая за начало новой игры
                //Группа включается при нажатии на кнопку "начать новую игру"
                .AddGroup("NewGame", false, null,
                new SNewGameInitializationMain(),
                new SMapHexasphere(),
                new SMapTerrain(),
                new SMapClimate())
                //Группа выключается в SEventControl в том же кадре

                //Обработка ввода
                .Add(new SUIInput())

                //Управление событиями, приходящими со всех систем
                .Add(new SEventControl())

                //Визуализация
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
                inputData,
                runtimeData)
                .Inject(sCMUI);

            perTickSystems

                .Inject(
                staticData,
                sceneData,
                mapGenerationData,
                regionsData,
                inputData,
                runtimeData)
                .Inject(sCMUI);

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