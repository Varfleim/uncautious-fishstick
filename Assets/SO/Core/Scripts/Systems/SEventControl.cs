
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

using SO.UI.Game.Events;
using SO.UI.Game.Map.Events;
using SO.Warfare.Fleet.Movement;
using SO.Map.Events;

namespace SO
{
    public class SEventControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //����� �������
        readonly EcsFilterInject<Inc<RStartNewGame>> startNewGameRequestFilter = default;
        readonly EcsPoolInject<RStartNewGame> startNewGameRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ������ ����� ����
            foreach(int requestEntity in startNewGameRequestFilter.Value)
            {
                //����������� ���������� ������ ������ "NewGame"
                EcsGroupSystemStateEvent("NewGame", false);

                UnityEngine.Debug.LogWarning("��������� ������� ����� ����");

                startNewGameRequestPool.Value.Del(requestEntity);
            }

            //��������� ������� �������� ����� ��������
            ObjectNewCreatedEvent();

            //��������� ������� ����� ��������� RFO
            ProvinceCoreChangeOwnerEvent();

            //��������� ������� ����� ��������� �������� �����
            MapAreaChangeOwnerEvent();

            //��������� ������� ����� ��������� ����������� �������
            TaskForceChangeProvinceEvent();
        }

        readonly EcsFilterInject<Inc<EObjectNewCreated>> objectNewCreatedEventFilter = default;
        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent()
        {
            //��� ������� ������� �������� ������ �������
            foreach(int eventEntity in objectNewCreatedEventFilter.Value)
            {
                //���� �������
                ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Get(eventEntity);

                //���� ������� �������� � �������� ����� ������
                if (eventComp.objectType == ObjectNewCreatedType.Country)
                {
                    UnityEngine.Debug.LogWarning("Country created!");
                }
                //�����, ���� ������� �������� � �������� ����� ����������� ������
                else if(eventComp.objectType == ObjectNewCreatedType.TaskForce)
                {
                    UnityEngine.Debug.LogWarning("Task Force created!");

                    //����������� �������� �������� ������ ������ �� ������� ������
                    GameCreatePanelRequest(
                        eventComp.objectPE,
                        GamePanelType.TaskForceSummaryPanelFMSbpnFleetsTab);

                    //����������� �������� ������� ������ ����� ����������� ������
                    GameCreatePanelRequest(
                        eventComp.objectPE,
                        GamePanelType.TaskForceMainMapPanel);
                }
                //�����, ���� ������� �������� � �������� ����� ������ ���������
                if(eventComp.objectType == ObjectNewCreatedType.Population)
                {
                    UnityEngine.Debug.LogWarning("Population created!");
                }
            }
        }

        readonly EcsFilterInject<Inc<EProvinceChangeOwner>> provinceChangeOwnerEventFilter = default;
        readonly EcsPoolInject<EProvinceChangeOwner> provinceChangeOwnerEventPool = default;
        void ProvinceCoreChangeOwnerEvent()
        {
            //��� ������� ������� ����� ��������� ���������
            foreach (int eventEntity in provinceChangeOwnerEventFilter.Value)
            {
                //���� �������
                ref EProvinceChangeOwner eventComp = ref provinceChangeOwnerEventPool.Value.Get(eventEntity);

                //���� ��������� �� ������������ ������, �� �������� ��������� ���������� ����� �����
                if (eventComp.oldOwnerCountryPE.Unpack(world.Value, out int oldOwnerCountryEntity) == false)
                {
                    //����������� �������� ������� ������ �����
                    GameCreatePanelRequest(
                        eventComp.provincePE,
                        GamePanelType.ProvinceMainMapPanel);
                }
                //�����
                else
                {
                    //����������� ���������� �������
                    GameRefreshPanelsSelfRequest(eventComp.provincePE);
                }
            }
        }

        readonly EcsFilterInject<Inc<EMapAreaChangeOwner>> mAChangeOwnerEventFilter = default;
        readonly EcsPoolInject<EMapAreaChangeOwner> mAChangeOwnerEventPool = default;
        void MapAreaChangeOwnerEvent()
        {
            //��� ������� ������� ����� ��������� ������� �����
            foreach (int eventEntity in mAChangeOwnerEventFilter.Value)
            {
                //���� ������� 
                ref EMapAreaChangeOwner eventComp = ref mAChangeOwnerEventPool.Value.Get(eventEntity);

                //���� ������� �� ������������ ������, �� �������� ������ ���������� ����� �����
                if(eventComp.oldOwnerCountryPE.Unpack(world.Value, out int oldOwnerCountryEntity) == false)
                {
                    //����������� �������� ������� ������ ���������� �������
                }
                //�����
                else
                {
                    //����������� ���������� ������� �������
                }
            }
        }

        readonly EcsFilterInject<Inc<ETaskForceChangeProvince>> tFChangeProvinceEventFilter = default;
        readonly EcsPoolInject<ETaskForceChangeProvince> tFChangeProvinceEventPool = default;
        void TaskForceChangeProvinceEvent()
        {
            //��� ������� ������� ����� ��������� ����������� �������
            foreach (int eventEntity in tFChangeProvinceEventFilter.Value)
            {
                //���� ������� 
                ref ETaskForceChangeProvince eventComp = ref tFChangeProvinceEventPool.Value.Get(eventEntity);

                //����������� ���������� �������� ������� �����
                GameRefreshMapPanelParentSelfRequest(eventComp.tFPE);
            }
        }

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;
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

        readonly EcsPoolInject<RGameCreatePanel> gameCreatePanelRefreshPool = default;
        void GameCreatePanelRequest(
            EcsPackedEntity objectPE,
            GamePanelType panelType)
        {
            //������ ����� �������� � ��������� �� ������ �������� ������ � ����
            int requestEntity = world.Value.NewEntity();
            ref RGameCreatePanel requestComp = ref gameCreatePanelRefreshPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                objectPE,
                panelType);
        }

        readonly EcsPoolInject<SRGameRefreshPanels> gameRefreshPanelsSelfRequestPool = default;
        void GameRefreshPanelsSelfRequest(
            EcsPackedEntity objectPE)
        {
            //���� �������� �������
            objectPE.Unpack(world.Value, out int objectEntity);

            //���� � ������� ��� ����������� ���������� �������
            if(gameRefreshPanelsSelfRequestPool.Value.Has(objectEntity) == false)
            {
                //��������� �������� ����������
                ref SRGameRefreshPanels selfRequestComp = ref gameRefreshPanelsSelfRequestPool.Value.Add(objectEntity);
            }
        }

        readonly EcsPoolInject<SRRefreshMapPanelsParent> refreshMapPanelsParentSelfRequestPool = default;
        void GameRefreshMapPanelParentSelfRequest(
            EcsPackedEntity objectPE)
        {
            //���� �������� �������
            objectPE.Unpack(world.Value, out int objectEntity);

            //���� � ������� ��� ����������� ���������� �������� ������� �����
            if (refreshMapPanelsParentSelfRequestPool.Value.Has(objectEntity) == false)
            {
                //��������� �������� ����������
                ref SRRefreshMapPanelsParent selfRequestComp = ref refreshMapPanelsParentSelfRequestPool.Value.Add(objectEntity);
            }
        }
    }
}