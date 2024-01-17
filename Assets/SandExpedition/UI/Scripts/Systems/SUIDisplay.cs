
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

using SCM.UI;
using SCM.UI.Events;
using SCM.UI.MainMenu;
using SCM.UI.MainMenu.Events;
using SCM.UI.Game;
using SCM.UI.Game.Events;

namespace SCM.UI
{
    public class SUIDisplay : IEcsInitSystem, IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //Общие события
        readonly EcsFilterInject<Inc<RGeneralAction>> generalActionRequestFilter = default;
        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;

        //Данные

        readonly EcsCustomInject<SCMUI> sCMUI = default;

        public void Init(IEcsSystems systems)
        {
            //Открываем окно главного меню
            MainMenuOpenWindow();
        }

        public void Run(IEcsSystems systems)
        {
            //Для каждого общего запроса
            foreach (int requestEntity in generalActionRequestFilter.Value)
            {
                //Берём запрос
                ref RGeneralAction requestComp = ref generalActionRequestPool.Value.Get(requestEntity);

                //Если запрашивается закрытие игры
                if(requestComp.actionType == GeneralActionType.QuitGame)
                {
                    Debug.LogError("Выход из игры!");

                    //Закрываем игру
                    Application.Quit();
                }

                world.Value.DelEntity(requestEntity);
            }

            //Если активно окно игры
            if(sCMUI.Value.activeMainWindowType == MainWindowType.Game)
            {
                //Проверяем события в окне игры
                EventCheckGame();
            }
            //Иначе, если активно окно главного меню
            else if(sCMUI.Value.activeMainWindowType == MainWindowType.MainMenu)
            {
                //Проверяем события в окне главного меню
                EventCheckMainMenu();
            }
        }

        void CloseMainWindow()
        {
            //Если какое-либо главное окно было активным
            if(sCMUI.Value.activeMainWindowType != MainWindowType.None)
            {
                sCMUI.Value.activeMainWindow.gameObject.SetActive(false);
                sCMUI.Value.activeMainWindow = null;
                sCMUI.Value.activeMainWindowType = MainWindowType.None;
            }
        }

        #region MainMenu
        readonly EcsFilterInject<Inc<RMainMenuAction>> mainMenuActionRequestFilter = default;
        readonly EcsPoolInject<RMainMenuAction> mainMenuActionRequestPool = default;
        void EventCheckMainMenu()
        {
            //Для каждого запроса действия в главном меню
            foreach (int requestEntity in mainMenuActionRequestFilter.Value)
            {
                //Берём запрос
                ref RMainMenuAction requestComp = ref mainMenuActionRequestPool.Value.Get(requestEntity);

                //Если запрашивается открытие окна игры
                if(requestComp.actionType == MainMenuActionType.OpenGame)
                {
                    //Создаём запрос создания новой игры
                    NewGameMenuStartNewGame();

                    //Открываем окно игры
                    GameOpenWindow();
                }

                world.Value.DelEntity(requestEntity);
            }
        }

        void MainMenuOpenWindow()
        {
            //Закрываем открытое главное окно
            CloseMainWindow();

            //Берём ссылку на окно главного меню
            UIMainMenuWindow mainMenuWindow = sCMUI.Value.mainMenuWindow;

            //Делаем его активным и указываем как активное в SCMUI
            mainMenuWindow.gameObject.SetActive(true);
            sCMUI.Value.activeMainWindow = sCMUI.Value.mainMenuWindow;

            //Указываем, что активно окно главного меню
            sCMUI.Value.activeMainWindowType = MainWindowType.MainMenu;

        }
        #endregion

        #region NewGame
        readonly EcsPoolInject<RStartNewGame> startNewGameRequestPool = default;
        void NewGameMenuStartNewGame()
        {
            //Создаём новую сущность и назначем ей запрос начала новой игры
            int requestEntity = world.Value.NewEntity();
            ref RStartNewGame requestComp = ref startNewGameRequestPool.Value.Add(requestEntity);

            //Запрашиваем включение группы систем "NewGame"
            EcsGroupSystemStateEvent("NewGame", true);
        }
        #endregion

        #region Game
        void EventCheckGame()
        {
            //Берём окно игры
            UIGameWindow gameWindow = sCMUI.Value.gameWindow;
        }

        void GameOpenWindow()
        {
            //Закрываем открытое главное окно
            CloseMainWindow();

            //Берём ссылку на окно игры
            UIGameWindow gameWindow = sCMUI.Value.gameWindow;

            //Делаем его активным и указываем как активное в SCMUI
            gameWindow.gameObject.SetActive(true);
            sCMUI.Value.activeMainWindow = sCMUI.Value.gameWindow;

            //Указываем, что активно окно игры
            sCMUI.Value.activeMainWindowType = MainWindowType.Game;
        }
        #endregion

        void EcsGroupSystemStateEvent(
            string systemGroupName,
            bool systemGroupState)
        {
            //Создаём новую сущность и назначаем ей событие смены состояния группы систем
            int eventEntity = world.Value.NewEntity();
            ref EcsGroupSystemState eventComp = ref ecsGroupSystemStatePool.Value.Add(eventEntity);

            //Указываем название группы систем и нужное состояние
            eventComp.Name = systemGroupName;
            eventComp.State = systemGroupState;
        }
    }
}