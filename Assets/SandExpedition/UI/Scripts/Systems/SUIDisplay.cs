
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
        //����
        readonly EcsWorldInject world = default;

        //����� �������
        readonly EcsFilterInject<Inc<RGeneralAction>> generalActionRequestFilter = default;
        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;

        //������

        readonly EcsCustomInject<SCMUI> sCMUI = default;

        public void Init(IEcsSystems systems)
        {
            //��������� ���� �������� ����
            MainMenuOpenWindow();
        }

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������ �������
            foreach (int requestEntity in generalActionRequestFilter.Value)
            {
                //���� ������
                ref RGeneralAction requestComp = ref generalActionRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ����
                if(requestComp.actionType == GeneralActionType.QuitGame)
                {
                    Debug.LogError("����� �� ����!");

                    //��������� ����
                    Application.Quit();
                }

                world.Value.DelEntity(requestEntity);
            }

            //���� ������� ���� ����
            if(sCMUI.Value.activeMainWindowType == MainWindowType.Game)
            {
                //��������� ������� � ���� ����
                EventCheckGame();
            }
            //�����, ���� ������� ���� �������� ����
            else if(sCMUI.Value.activeMainWindowType == MainWindowType.MainMenu)
            {
                //��������� ������� � ���� �������� ����
                EventCheckMainMenu();
            }
        }

        void CloseMainWindow()
        {
            //���� �����-���� ������� ���� ���� ��������
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
            //��� ������� ������� �������� � ������� ����
            foreach (int requestEntity in mainMenuActionRequestFilter.Value)
            {
                //���� ������
                ref RMainMenuAction requestComp = ref mainMenuActionRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ���� ����
                if(requestComp.actionType == MainMenuActionType.OpenGame)
                {
                    //������ ������ �������� ����� ����
                    NewGameMenuStartNewGame();

                    //��������� ���� ����
                    GameOpenWindow();
                }

                world.Value.DelEntity(requestEntity);
            }
        }

        void MainMenuOpenWindow()
        {
            //��������� �������� ������� ����
            CloseMainWindow();

            //���� ������ �� ���� �������� ����
            UIMainMenuWindow mainMenuWindow = sCMUI.Value.mainMenuWindow;

            //������ ��� �������� � ��������� ��� �������� � SCMUI
            mainMenuWindow.gameObject.SetActive(true);
            sCMUI.Value.activeMainWindow = sCMUI.Value.mainMenuWindow;

            //���������, ��� ������� ���� �������� ����
            sCMUI.Value.activeMainWindowType = MainWindowType.MainMenu;

        }
        #endregion

        #region NewGame
        readonly EcsPoolInject<RStartNewGame> startNewGameRequestPool = default;
        void NewGameMenuStartNewGame()
        {
            //������ ����� �������� � �������� �� ������ ������ ����� ����
            int requestEntity = world.Value.NewEntity();
            ref RStartNewGame requestComp = ref startNewGameRequestPool.Value.Add(requestEntity);

            //����������� ��������� ������ ������ "NewGame"
            EcsGroupSystemStateEvent("NewGame", true);
        }
        #endregion

        #region Game
        void EventCheckGame()
        {
            //���� ���� ����
            UIGameWindow gameWindow = sCMUI.Value.gameWindow;
        }

        void GameOpenWindow()
        {
            //��������� �������� ������� ����
            CloseMainWindow();

            //���� ������ �� ���� ����
            UIGameWindow gameWindow = sCMUI.Value.gameWindow;

            //������ ��� �������� � ��������� ��� �������� � SCMUI
            gameWindow.gameObject.SetActive(true);
            sCMUI.Value.activeMainWindow = sCMUI.Value.gameWindow;

            //���������, ��� ������� ���� ����
            sCMUI.Value.activeMainWindowType = MainWindowType.Game;
        }
        #endregion

        void EcsGroupSystemStateEvent(
            string systemGroupName,
            bool systemGroupState)
        {
            //������ ����� �������� � ��������� �� ������� ����� ��������� ������ ������
            int eventEntity = world.Value.NewEntity();
            ref EcsGroupSystemState eventComp = ref ecsGroupSystemStatePool.Value.Add(eventEntity);

            //��������� �������� ������ ������ � ������ ���������
            eventComp.Name = systemGroupName;
            eventComp.State = systemGroupState;
        }
    }
}