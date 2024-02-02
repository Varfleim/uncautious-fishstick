using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.Unity.Ugui;

using SO.UI.Events;
using SO.UI.MainMenu;
using SO.UI.MainMenu.Events;
using SO.UI.Game;
using SO.UI.Game.Events;
using SO.UI.Game.Object.Events;
using SO.Map;
using SO.Map.Events;
using SO.Faction;
using SO.Map.Hexasphere;

namespace SO.UI
{
    public class SUIInput : IEcsInitSystem, IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;
        EcsWorld uguiMapWorld;
        EcsWorld uguiUIWorld;


        //Карта
        readonly EcsFilterInject<Inc<CRegionHexasphere>> regionFilter = default;
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //Фракции
        readonly EcsPoolInject<CFaction> factionPool = default;


        EcsFilter clickEventMapFilter;
        EcsPool<EcsUguiClickEvent> clickEventMapPool;

        EcsFilter clickEventUIFilter;
        EcsPool<EcsUguiClickEvent> clickEventUIPool;

        //Данные
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;
        readonly EcsCustomInject<InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Init(IEcsSystems systems)
        {
            uguiMapWorld = systems.GetWorld("uguiMapEventsWorld");

            clickEventMapPool = uguiMapWorld.GetPool<EcsUguiClickEvent>();
            clickEventMapFilter = uguiMapWorld.Filter<EcsUguiClickEvent>().End();

            uguiUIWorld = systems.GetWorld("uguiUIEventsWorld");

            clickEventUIPool = uguiUIWorld.GetPool<EcsUguiClickEvent>();
            clickEventUIFilter = uguiUIWorld.Filter<EcsUguiClickEvent>().End();
        }

        public void Run(IEcsSystems systems)
        {
            //Определяем состояние клавиш
            CheckButtons();

            //Для каждого события клика по интерфейсу
            foreach (int clickEventUIEntity in clickEventUIFilter)
            {
                //Берём событие
                ref EcsUguiClickEvent clickEvent = ref clickEventUIPool.Get(clickEventUIEntity);

                Debug.LogWarning("Click! " + clickEvent.WidgetName);

                //Если активно окно игры
                if(sOUI.Value.activeMainWindowType == MainWindowType.Game)
                {
                    //Проверяем клики в окне игры
                    GameClickAction(ref clickEvent);
                }
                //Иначе, если активно окно главного меню
                else if(sOUI.Value.activeMainWindowType == MainWindowType.MainMenu)
                {
                    MainMenuClickAction(ref clickEvent);
                }

                uguiUIWorld.DelEntity(clickEventUIEntity);
            }

            //Для каждого события клика по карте
            foreach (int clickEventMapEntity in clickEventMapFilter)
            {
                //Берём событие
                ref EcsUguiClickEvent clickEvent = ref clickEventMapPool.Get(clickEventMapEntity);

                Debug.LogWarning("Click! " + clickEvent.WidgetName);

                uguiMapWorld.DelEntity(clickEventMapEntity);
            }

            //Если активен какой-либо режим карты
            if (inputData.Value.mapMode != MapMode.None)
            {
                //Ввод поворота камеры
                CameraInputRotating();

                //Ввод приближения камеры
                CameraInputZoom();
            }

            //Прочий ввод
            InputOther();

            //Ввод мыши
            InputMouse();

            foreach (int regionEntity in regionFilter.Value)
            {
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //if (region.Elevation < mapGenerationData.Value.waterLevel)
                //{
                //    RegionSetColor(ref region, Color.blue);
                //}
                //else if (region.TerrainTypeIndex == 0)
                //{
                //    RegionSetColor(ref region, Color.yellow);
                //}
                //else if (region.TerrainTypeIndex == 1)
                //{
                //    RegionSetColor(ref region, Color.green);
                //}
                //else if (region.TerrainTypeIndex == 2)
                //{
                //    RegionSetColor(ref region, Color.gray);
                //}
                //else if (region.TerrainTypeIndex == 3)
                //{
                //    RegionSetColor(ref region, Color.red);
                //}
                //else if (region.TerrainTypeIndex == 4)
                //{
                //    RegionSetColor(ref region, Color.white);
                //}

                //for (int a = 0; a < rFO.factionRFOs.Length; a++)
                //{
                //    rFO.factionRFOs[a].fRFOPE.Unpack(world.Value, out int fRFOEntity);
                //    ref CExplorationFRFO exFRFO = ref exFRFOPool.Value.Get(fRFOEntity);

                //    if (exFRFO.factionPE.EqualsTo(inputData.Value.playerFactionPE))
                //    {
                //        RegionSetColor(
                //            ref region,
                //            new Color32(
                //                exFRFO.explorationLevel, exFRFO.explorationLevel, exFRFO.explorationLevel,
                //                255));
                //    }
                //}

                //if (rFO.ownerFactionPE.Unpack(world.Value, out int factionEntity))
                //{
                //    RegionSetColor(ref region, Color.magenta);

                //    List<int> regions = regionsData.Value.GetRegionIndicesWithinSteps(
                //        world.Value,
                //        regionFilter.Value, regionPool.Value,
                //        ref region, 10);

                //    for(int a = 0; a < regions.Count; a++)
                //    {
                //        regionsData.Value.regionPEs[regions[a]].Unpack(world.Value, out int neighbourRegionEntity);
                //        ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                //        RegionSetColor(ref neighbourRegion, Color.magenta);
                //    }
                //}
            }
        }

        void CheckButtons()
        {
            //Определяем состояние ЛКМ
            inputData.Value.leftMouseButtonClick = Input.GetMouseButtonDown(0);
            inputData.Value.leftMouseButtonPressed = inputData.Value.leftMouseButtonClick || Input.GetMouseButton(0);
            inputData.Value.leftMouseButtonRelease = Input.GetMouseButtonUp(0);

            //Определяем состояние ПКМ
            inputData.Value.rightMouseButtonClick = Input.GetMouseButtonDown(1);
            inputData.Value.rightMouseButtonPressed = inputData.Value.leftMouseButtonClick || Input.GetMouseButton(1);
            inputData.Value.rightMouseButtonRelease = Input.GetMouseButtonUp(1);

            //Определяем состояние LeftShift
            inputData.Value.leftShiftKeyPressed = Input.GetKey(KeyCode.LeftShift);
        }

        void CameraInputRotating()
        {
            //Берём Y-вращение
            float yRotationDelta = Input.GetAxis("Horizontal");
            //Если оно не равно нулю, то применяем
            if (yRotationDelta != 0f)
            {
                CameraAdjustYRotation(yRotationDelta);
            }

            //Берём X-вращение
            float xRotationDelta = Input.GetAxis("Vertical");
            //Если оно не равно нулю, то применяем
            if (xRotationDelta != 0f)
            {
                CameraAdjustXRotation(xRotationDelta);
            }
        }

        void CameraAdjustYRotation(
            float rotationDelta)
        {
            //Рассчитываем угол вращения
            inputData.Value.rotationAngleY -= rotationDelta * inputData.Value.rotationSpeed * UnityEngine.Time.deltaTime;

            //Выравниваем угол
            if (inputData.Value.rotationAngleY < 0f)
            {
                inputData.Value.rotationAngleY += 360f;
            }
            else if (inputData.Value.rotationAngleY >= 360f)
            {
                inputData.Value.rotationAngleY -= 360f;
            }

            //Применяем вращение
            inputData.Value.mapCamera.localRotation = Quaternion.Euler(
                0f, inputData.Value.rotationAngleY, 0f);
        }

        void CameraAdjustXRotation(
            float rotationDelta)
        {
            //Рассчитываем угол вращения
            inputData.Value.rotationAngleX += rotationDelta * inputData.Value.rotationSpeed * UnityEngine.Time.deltaTime;

            //Выравниваем угол
            inputData.Value.rotationAngleX = Mathf.Clamp(
                inputData.Value.rotationAngleX, inputData.Value.minAngleX, inputData.Value.maxAngleX);

            //Применяем вращение
            inputData.Value.swiwel.localRotation = Quaternion.Euler(
                inputData.Value.rotationAngleX, 0f, 0f);
        }

        void CameraInputZoom()
        {
            //Берём вращение колёсика мыши
            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");

            //Если оно не равно нулю
            if (zoomDelta != 0)
            {
                //Применяем приближение
                CameraAdjustZoom(zoomDelta);
            }
        }

        void CameraAdjustZoom(
            float zoomDelta)
        {
            //Рассчитываем приближение камеры
            inputData.Value.zoom = Mathf.Clamp01(inputData.Value.zoom + zoomDelta);

            //Рассчитываем расстояние приближения и применяем его
            float zoomDistance = Mathf.Lerp(
                inputData.Value.stickMinZoom, inputData.Value.stickMaxZoom, inputData.Value.zoom);
            inputData.Value.stick.localPosition = new(0f, 0f, zoomDistance);
        }

        void InputOther()
        {
            //Пауза тиков
            if(Input.GetKeyUp(KeyCode.Space))
            {
                //Если игра активна
                if (runtimeData.Value.isGameActive == true)
                {
                    //Запрашиваем включение паузы
                    GameActionRequest(GameActionType.PauseOn);
                }
                //Иначе
                else
                {
                    //Запрашиваем выключение паузы
                    GameActionRequest(GameActionType.PauseOff);
                }
            }

            //Закрытие игры
            if(Input.GetKeyUp(KeyCode.Escape))
            {
                //Запрашиваем выход из игры
                GeneralActionRequest(GeneralActionType.QuitGame);
            }

            if(Input.GetKeyUp(KeyCode.Y))
            {
                Debug.LogWarning("Y!");

                MapChangeMapModeRequest(
                    ChangeMapModeRequestType.Exploration);
            }
        }

        void InputMouse()
        {
            //Если курсор не находится над объектом интерфейса
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() == false)
            {
                //Проверяем взаимодействие мыши со сферой
                HexasphereCheckMouseInteraction();
            }
            //Иначе
            else
            {
                //Курсор точно не находится над гексасферой
                inputData.Value.isMouseOver = false;
            }
        }

        #region MainMenu
        void MainMenuClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Если запрашивается открытие окна игры
            if(clickEvent.WidgetName == "MainMenuNewGame")
            {
                //Создаём запрос открытия окна игры
                MainMenuActionRequest(MainMenuActionType.OpenGame);
            }
            //Иначе, если запрашивается выход из игры
            else if(clickEvent.WidgetName == "QuitGame")
            {
                //Создаём запрос выхода из игры
                GeneralActionRequest(GeneralActionType.QuitGame);
            }
        }

        readonly EcsPoolInject<RMainMenuAction> mainMenuActionRequestPool = default;
        void MainMenuActionRequest(
            MainMenuActionType actionType)
        {
            //Создаём новую сущность и назначаем ей запрос действия в главном меню
            int requestEntity = world.Value.NewEntity();
            ref RMainMenuAction requestComp = ref mainMenuActionRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(actionType);
        }
        #endregion

        #region Game
        void GameClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём окно игры
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //Берём фракцию игрока
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //Если щелчок по флагу фракции игрока
            if (clickEvent.WidgetName == "PlayerFactionFlag")
            {
                //Запрашиваем открытие панели фракции
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.Faction,
                    faction.selfPE);
            }

            //Иначе, если активна панель объекта
            else if (gameWindow.activeMainPanelType == MainPanelType.Object)
            {
                //Проверяем клики в панели объекта
                ObjPnClickAction(ref clickEvent);
            }
        }

        readonly EcsPoolInject<RGameAction> gameActionRequestPool = default;
        void GameActionRequest(
            GameActionType actionType)
        {
            //Создаём новую сущность и назначаем ей запрос действия в игре
            int requestEntity = world.Value.NewEntity();
            ref RGameAction requestComp = ref gameActionRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(actionType);
        }

        readonly EcsPoolInject<RChangeMapMode> changeMapModeRequestTypePool = default;
        void MapChangeMapModeRequest(
            ChangeMapModeRequestType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос смены режима карты
            int requestEntity = world.Value.NewEntity();
            ref RChangeMapMode requestComp = ref changeMapModeRequestTypePool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                requestType);
        }

        #region ObjectPanel
        void ObjPnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём фракцию игрока
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //Если нажата кнопка закрытия панели объекта
            if(clickEvent.WidgetName == "ClosePanelObjPn")
            {
                //Запрашиваем открытие панели объекта
                ObjPnActionRequest(ObjectPanelActionRequestType.Close);
            }

            //Иначе, если активна подпанель фракции
            else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Faction)
            {
                //Проверяем клики в подпанели фракции
                FactionSbpnClickAction(ref clickEvent);
            }
            //Иначе, если активна подпанель региона
            else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Region)
            {
                //Проверяем клики в подпанели региона
                RegionSbpnClickAction(ref clickEvent);
            }
        }

        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjPnActionRequest(
            ObjectPanelActionRequestType requestType,
            EcsPackedEntity objectPE = new(),
            bool isRefresh = false)
        {
            //Создаём новую сущность и назначаем ей запрос действия панели объекта
            int requestEntity = world.Value.NewEntity();
            ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                requestType,
                objectPE,
                isRefresh);
        }

        #region FactionSubpanel
        void FactionSbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель фракции

            //Если нажата кнопка обзорной вкладки
            if(clickEvent.WidgetName == "OverviewTabFactionSbpn")
            {
                //Запрашиваем отображение обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.FactionOverview,
                    objectPanel.activeObjectPE);
            }
        }
        #endregion

        #region RegionSubpanel
        void RegionSbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель региона

            //Если нажата кнопка обзорной вкладки
            if (clickEvent.WidgetName == "OverviewTabRegionSbpn")
            {
                //Запрашиваем отображение обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.RegionOverview,
                    objectPanel.activeObjectPE);
            }
        }
        #endregion
        #endregion

        #region Hexasphere
        void HexasphereCheckMouseInteraction()
        {
            //Если курсор находится над гексасферой
            if(HexasphereCheckMousePosition(out Vector3 position, out Ray ray, out EcsPackedEntity regionPE))
            {
                //Берём регион под курсором
                regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere currentRHS = ref rHSPool.Value.Get(regionEntity);

                //Если нажата ЛКМ
                if(inputData.Value.leftMouseButtonClick)
                {
                    //Запрашиваем отображение подпанели региона
                    ObjPnActionRequest(
                        ObjectPanelActionRequestType.Region,
                        currentRHS.selfPE);
                }
            }
        }

        bool HexasphereCheckMousePosition(
            out Vector3 position, out Ray ray,
            out EcsPackedEntity regionPE)
        {
            //Проверяем, находится ли курсор над гексасферой
            inputData.Value.isMouseOver = HexasphereGetHitPoint(out position, out ray);

            regionPE = new();

            //Если курсор находится над гексасферой
            if(inputData.Value.isMouseOver == true)
            {
                //Определяем индекс региона, над которым находится курсор
                int regionIndex = RegionGetInRayDirection(
                    ray, position,
                    out Vector3 hitPosition);

                //Если индекс региона больше нуля
                if (regionIndex >= 0)
                {
                    //Берём регион
                    regionsData.Value.regionPEs[regionIndex].Unpack(world.Value, out int regionEntity);
                    ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //Если индекс региона не равен индексу последнего подсвеченного
                    if (rC.Index != inputData.Value.lastHighlightedRegionIndex)
                    {
                        //Если индекс последнего подсвеченного региона больше нуля
                        if (inputData.Value.lastHighlightedRegionIndex > 0)
                        {
                            //Скрываем подсветку наведения
                            RegionHideHoverHighlight();
                        }

                        //Обновляем подсвеченный регион
                        inputData.Value.lastHighlightedRegionPE = rC.selfPE;
                        inputData.Value.lastHighlightedRegionIndex = rC.Index;

                        //Подсвечиваем регион
                        RegionShowHoverHighlight(ref rHS);
                    }

                    regionPE = rC.selfPE;

                    return true;
                }
                //Иначе, если индекс региона меньше нуля и индекс последнего подсвеченного региона больше нуля
                else if (regionIndex < 0 && inputData.Value.lastHighlightedRegionIndex >= 0)
                {
                    //Скрываем подсветку наведения
                    RegionHideHoverHighlight();
                }
            }

            return false;
        }

        bool HexasphereGetHitPoint(
            out Vector3 position,
            out Ray ray)
        {
            //Берём луч из положения мыши
            ray = inputData.Value.camera.ScreenPointToRay(Input.mousePosition);

            //Вызываем функцию
            return HexapshereGetHitPoint(ray, out position);
        }

        bool HexapshereGetHitPoint(
            Ray ray,
            out Vector3 position)
        {
            //Если луч касается объекта
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                //Если луч касается объекта гексасферы
                if (hit.collider.gameObject == SceneData.HexashpereGO)
                {
                    //Определяем точку касания
                    position = hit.point;

                    //И возвращаем true
                    return true;
                }
            }

            position = Vector3.zero;
            return false;
        }

        int RegionGetInRayDirection(
            Ray ray,
            Vector3 worldPosition,
            out Vector3 hitPosition)
        {
            hitPosition = worldPosition;

            //Определяем итоговую точку касания
            Vector3 minPoint = worldPosition;
            Vector3 maxPoint = worldPosition + 0.5f * mapGenerationData.Value.hexasphereScale * ray.direction;

            float rangeMin = mapGenerationData.Value.hexasphereScale * 0.5f;
            rangeMin *= rangeMin;

            float rangeMax = worldPosition.sqrMagnitude;

            float distance;
            Vector3 bestPoint = maxPoint;

            //Уточняем точку
            for (int a = 0; a < 10; a++)
            {
                Vector3 midPoint = (minPoint + maxPoint) * 0.5f;

                distance = midPoint.sqrMagnitude;

                if (distance < rangeMin)
                {
                    maxPoint = midPoint;
                    bestPoint = midPoint;
                }
                else if (distance > rangeMax)
                {
                    maxPoint = midPoint;
                }
                else
                {
                    minPoint = midPoint;
                }
            }

            //Берём индекс региона 
            int nearestRegionIndex = RegionGetAtLocalPosition(SceneData.HexashpereGO.transform.InverseTransformPoint(worldPosition));
            //Если индекс меньше нуля, выходим из функции
            if (nearestRegionIndex < 0)
            {
                return -1;
            }

            //Определяем индекс региона
            Vector3 currentPoint = worldPosition;

            //Берём ближайший регион
            regionsData.Value.regionPEs[nearestRegionIndex].Unpack(world.Value, out int nearestRegionEntity);
            ref CRegionHexasphere nearestRHS = ref rHSPool.Value.Get(nearestRegionEntity);

            //Определяем верхнюю точку региона
            Vector3 regionTop = SceneData.HexashpereGO.transform.TransformPoint(
                nearestRHS.center * (1.0f + nearestRHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

            //Определяем высоту региона и высоту луча
            float regionHeight = regionTop.sqrMagnitude;
            float rayHeight = currentPoint.sqrMagnitude;

            float minDistance = 1e6f;
            distance = minDistance;

            //Определяем индекс региона-кандидата
            int candidateRegionIndex = -1;

            const int NUM_STEPS = 10;
            //Уточняем точку
            for (int a = 1; a <= NUM_STEPS; a++)
            {
                distance = Mathf.Abs(rayHeight - regionHeight);

                //Если расстояние меньше минимального
                if (distance < minDistance)
                {
                    //Обновляем минимальное расстояние и кандидата
                    minDistance = distance;
                    candidateRegionIndex = nearestRegionIndex;
                    hitPosition = currentPoint;
                }

                if (rayHeight < regionHeight)
                {
                    return candidateRegionIndex;
                }

                float t = a / (float)NUM_STEPS;

                currentPoint = worldPosition * (1f - t) + bestPoint * t;

                nearestRegionIndex = RegionGetAtLocalPosition(SceneData.HexashpereGO.transform.InverseTransformPoint(currentPoint));

                if (nearestRegionIndex < 0)
                {
                    break;
                }

                //Обновляем ближайший регион
                regionsData.Value.regionPEs[nearestRegionIndex].Unpack(world.Value, out nearestRegionEntity);
                nearestRHS = ref rHSPool.Value.Get(nearestRegionEntity);

                regionTop = SceneData.HexashpereGO.transform.TransformPoint(
                    nearestRHS.center * (1.0f + nearestRHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

                //Определяем высоту региона и высоту луча
                regionHeight = regionTop.sqrMagnitude;
                rayHeight = currentPoint.sqrMagnitude;
            }

            //Если расстояние меньше минимального
            if (distance < minDistance)
            {
                //Обновляем минимальное расстояние и кандидата
                minDistance = distance;
                candidateRegionIndex = nearestRegionIndex;
                hitPosition = currentPoint;
            }

            if (rayHeight < regionHeight)
            {
                return candidateRegionIndex;
            }
            else
            {
                return -1;
            }
        }

        int RegionGetAtLocalPosition(
            Vector3 localPosition)
        {
            //Проверяем, не последний ли это регион
            if (inputData.Value.lastHitRegionIndex >= 0 && inputData.Value.lastHitRegionIndex < regionsData.Value.regionPEs.Length)
            {
                //Берём последний регион
                regionsData.Value.regionPEs[inputData.Value.lastHitRegionIndex].Unpack(world.Value, out int lastHitRegionEntity);
                ref CRegionCore lastHitRC = ref rCPool.Value.Get(lastHitRegionEntity);

                //Определяем расстояние до центра региона
                float distance = Vector3.SqrMagnitude(lastHitRC.center - localPosition);

                bool isValid = true;

                //Для каждого соседнего региона
                for (int a = 0; a < lastHitRC.neighbourRegionPEs.Length; a++)
                {
                    //Берём соседний регион
                    lastHitRC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegionHexasphere neighbourRHS = ref rHSPool.Value.Get(neighbourRegionEntity);

                    //Определяем расстояние до центре региона
                    float otherDistance = Vector3.SqrMagnitude(neighbourRHS.center - localPosition);

                    //Если оно меньше расстояния до последнего региона
                    if (otherDistance < distance)
                    {
                        //Отмечаем это и выходим из цикла
                        isValid = false;
                        break;
                    }
                }

                //Если это последний регион
                if (isValid == true)
                {
                    return inputData.Value.lastHitRegionIndex;
                }
            }
            //Иначе
            else
            {
                //Обнуляем индекс последнего региона
                inputData.Value.lastHitRegionIndex = 0;
            }

            //Следуем кратчайшему пути к минимальному расстоянию
            regionsData.Value.regionPEs[inputData.Value.lastHitRegionIndex].Unpack(world.Value, out int nearestRegionEntity);
            ref CRegionCore nearestRC = ref rCPool.Value.Get(nearestRegionEntity);

            float minDist = 1e6f;

            //Для каждого региона
            for (int a = 0; a < regionsData.Value.regionPEs.Length; a++)
            {
                //Берём ближайший регион 
                RegionGetNearestToPosition(
                    ref nearestRC.neighbourRegionPEs,
                    localPosition,
                    out float regionDistance).Unpack(world.Value, out int newNearestRegionEntity);

                //Если расстояние меньше минимального
                if (regionDistance < minDist)
                {
                    //Обновляем регион и минимальное расстояние 
                    nearestRC = ref rCPool.Value.Get(newNearestRegionEntity);

                    minDist = regionDistance;
                }
                //Иначе выходим из цикла
                else
                {
                    break;
                }
            }

            //Индекс последнего региона - это индекс ближайшего
            inputData.Value.lastHitRegionIndex = nearestRC.Index;

            return inputData.Value.lastHitRegionIndex;
        }

        EcsPackedEntity RegionGetNearestToPosition(
            ref EcsPackedEntity[] regionPEs,
            Vector3 localPosition,
            out float minDistance)
        {
            minDistance = float.MaxValue;

            EcsPackedEntity nearestRegionPE = new();

            //Для каждого региона в массиве
            for (int a = 0; a < regionPEs.Length; a++)
            {
                //Берём регион
                regionPEs[a].Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

                //Берём центр региона
                Vector3 center = rHS.center;

                //Рассчитываем расстояние
                float distance
                    = (center.x - localPosition.x) * (center.x - localPosition.x)
                    + (center.y - localPosition.y) * (center.y - localPosition.y)
                    + (center.z - localPosition.z) * (center.z - localPosition.z);

                //Если расстояние меньше минимального
                if (distance < minDistance)
                {
                    //Обновляем регион и минимальное расстояние
                    nearestRegionPE = rHS.selfPE;
                    minDistance = distance;
                }
            }

            return nearestRegionPE;
        }

        void RegionShowHoverHighlight(
            ref CRegionHexasphere rHS)
        {
            //Активируем рендерер наведения
            rHS.hoverRenderer.enabled = true;
        }

        void RegionHideHoverHighlight()
        {
            //Если подсвеченный регион существует
            if (inputData.Value.lastHighlightedRegionPE.Unpack(world.Value, out int highlightedRegionEntity))
            {
                //Берём регион
                ref CRegionHexasphere highlightedRHS = ref rHSPool.Value.Get(highlightedRegionEntity);

                //Отключаем рендерер наведения
                highlightedRHS.hoverRenderer.enabled = false;
            }

            //Удаляем последний подсвеченный регион
            inputData.Value.lastHighlightedRegionPE = new();
            inputData.Value.lastHighlightedRegionIndex = -1;
        }

        void RegionSetColor(
            ref CRegionCore rC,
            Color color)
        {
            //Берём кэшированный материал
            Material material;

            //Если материал такого цвета уже существует в кэше цветных материалов
            if (mapGenerationData.Value.colorCache.ContainsKey(color) == false)
            {
                //То создаём новый материал и кэшируем его
                material = GameObject.Instantiate(mapGenerationData.Value.regionColoredMaterial);
                mapGenerationData.Value.colorCache.Add(color, material);

                //Заполняем основные данные материала
                material.hideFlags = HideFlags.DontSave;
                material.color = color;
                material.SetFloat(ShaderParameters.RegionAlpha, 1f);
            }
            //Иначе
            else
            {
                //Берём материал из словаря
                material = mapGenerationData.Value.colorCache[color];
            }

            //Устанавливаем материал региона
            RegionSetMaterial(
                rC.Index,
                material);
        }

        bool RegionSetMaterial(
            int regionIndex,
            Material material,
            bool temporary = false)
        {
            //Берём регион
            regionsData.Value.regionPEs[regionIndex].Unpack(world.Value, out int regionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

            //Если назначается временный материал
            if (temporary == true)
            {
                //Если этот временный материал уже назначен региону
                if (rHS.tempMaterial == material)
                {
                    //То ничего не меняется
                    return false;
                }

                //Назначаем временный материал рендереру региона
                rHS.hoverRenderer.sharedMaterial = material;
                rHS.hoverRenderer.enabled = true;
            }
            //Иначе
            else
            {
                //Если этот основной материал уже назначен региону
                if (rHS.customMaterial == material)
                {
                    //То ничего не меняется
                    return false;
                }

                //Берём цвет материала
                Color32 materialColor = Color.white;
                if (material.HasProperty(ShaderParameters.Color) == true)
                {
                    materialColor = material.color;
                }
                else if (material.HasProperty(ShaderParameters.BaseColor))
                {
                    materialColor = material.GetColor(ShaderParameters.BaseColor);
                }

                //Отмечаем, что требуется обновление цветов
                mapGenerationData.Value.isColorUpdated = true;

                //Берём текстуру материала
                Texture materialTexture = null;
                if (material.HasProperty(ShaderParameters.MainTex))
                {
                    materialTexture = material.mainTexture;
                }
                else if (material.HasProperty(ShaderParameters.BaseMap))
                {
                    materialTexture = material.GetTexture(ShaderParameters.BaseMap);
                }

                //Если текстура не пуста
                if (materialTexture != null)
                {
                    //Отмечаем, что требуется обновление массива текстур
                    mapGenerationData.Value.isTextureArrayUpdated = true;
                }
                //Иначе
                else
                {
                    List<Color32> colorChunk = mapGenerationData.Value.colorShaded[rHS.uvShadedChunkIndex];
                    for (int k = 0; k < rHS.uvShadedChunkLength; k++)
                    {
                        colorChunk[rHS.uvShadedChunkStart + k] = materialColor;
                    }
                    mapGenerationData.Value.colorShadedDirty[rHS.uvShadedChunkIndex] = true;
                }
            }

            //Если материал - не материал подсветки наведения
            if (material != mapGenerationData.Value.hoverRegionHighlightMaterial)
            {
                //Если это временный материал
                if (temporary == true)
                {
                    rHS.tempMaterial = material;
                }
                //Иначе
                else
                {
                    rHS.customMaterial = material;
                }
            }

            //Если материал подсветки не пуст и регион - это последний подсвеченный регион
            if (mapGenerationData.Value.hoverRegionHighlightMaterial != null && inputData.Value.lastHighlightedRegionIndex == rC.Index)
            {
                //Задаём рендереру материал подсветки наведения
                rHS.hoverRenderer.sharedMaterial = mapGenerationData.Value.hoverRegionHighlightMaterial;

                //Берём исходный материал 
                Material sourceMaterial = null;
                if (rHS.tempMaterial != null)
                {
                    sourceMaterial = rHS.tempMaterial;
                }
                else if (rHS.customMaterial != null)
                {
                    sourceMaterial = rHS.customMaterial;
                }

                //Если исходный материал не пуст
                if (sourceMaterial != null)
                {
                    //Берём цвет исходного материала
                    Color32 color = Color.white;
                    if (sourceMaterial.HasProperty(ShaderParameters.Color) == true)
                    {
                        color = sourceMaterial.color;
                    }
                    else if (sourceMaterial.HasProperty(ShaderParameters.BaseColor))
                    {
                        color = sourceMaterial.GetColor(ShaderParameters.BaseColor);
                    }
                    //Устанавливаем вторичный цвет материалу подсветки наведения
                    mapGenerationData.Value.hoverRegionHighlightMaterial.SetColor(ShaderParameters.Color2, color);

                    //Берём текстуру исходного материала
                    Texture tempMaterialTexture = null;
                    if (sourceMaterial.HasProperty(ShaderParameters.MainTex))
                    {
                        tempMaterialTexture = sourceMaterial.mainTexture;
                    }
                    else if (sourceMaterial.HasProperty(ShaderParameters.BaseMap))
                    {
                        tempMaterialTexture = sourceMaterial.GetTexture(ShaderParameters.BaseMap);
                    }

                    //Если текстура не пуста
                    if (tempMaterialTexture != null)
                    {
                        mapGenerationData.Value.hoverRegionHighlightMaterial.mainTexture = tempMaterialTexture;
                    }
                }
            }

            return true;
        }
        #endregion
        #endregion

        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;
        void GeneralActionRequest(
            GeneralActionType actionType)
        {
            //Создаём новую сущность и назначаем ей запрос общего действия
            int requestEntity = world.Value.NewEntity();
            ref RGeneralAction requestComp = ref generalActionRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(actionType);
        }
    }
}