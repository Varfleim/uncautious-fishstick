
using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;
using Leopotam.EcsLite.Unity.Ugui;

using SO.UI;
using SO.Time;
using SO.Map;
using SO.Map.Hexasphere;
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
        public UIData uIData;
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

                //Группа, отвечающая за начало новой игры
                //Группа включается при нажатии на кнопку "начать новую игру"
                .AddGroup("NewGame", false, null,
                //Основная инициализация
                new SNewGameInitializationMain(),
                //Генерация гексасферы
                new SMapHexasphere(),
                //Генерация ландшафта
                new SMapTerrain(),
                //Генерация климата
                new SMapClimate(),

                //Управление фракциями
                new SFactionControl(),
                
                //Применение инициализаторов регионов
                new SMapRegionInitializerControl(),
                
                //Управление RC
                new SRCControl())
                //Группа выключается в SEventControl в том же кадре

                //Обработка ввода
                .Add(new SUIInput())

                //Управление событиями, приходящими со всех систем
                .Add(new SEventControl())

                //Визуализация
                .Add(new SMapControl())
                .Add(new SUIDisplay())

                //Очитска событий
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
                factionsData,
                inputData,
                runtimeData)
                .Inject(sOUI);

            perTickSystems

                //Управление RC
                .Add(new SRCControl())

                //Подсчёт исследования от наблюдателей
                .Add(new SMTObserverExplorationCalc())

                //Обновление интерфейса после тика
                .Add(new SUITickUpdate())

                .Inject(
                staticData,
                sceneData,
                uIData,
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