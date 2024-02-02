
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

using SO.UI.Game.Events;
using SO.Map;

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
            RFOChangeOwnerEvent();
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

                //���� ������� �������� � �������� ����� �������
                if(eventComp.objectType == ObjectNewCreatedType.Faction)
                {
                    UnityEngine.Debug.LogWarning("Faction created!");
                }
            }
        }

        readonly EcsFilterInject<Inc<ERCChangeOwner>> rFOChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERCChangeOwner> rFOChangeOwnerEventPool = default;
        void RFOChangeOwnerEvent()
        {
            //��� ������� ������� ����� ��������� RFO
            foreach (int eventEntity in rFOChangeOwnerEventFilter.Value)
            {
                //���� �������
                ref ERCChangeOwner eventComp = ref rFOChangeOwnerEventPool.Value.Get(eventEntity);

                //���� ������ �� ����������� ������, �� ��� �������� ���������� ����� �����
                if(eventComp.oldOwnerFactionPE.Unpack(world.Value, out int oldOwnerFactionEntity) == false)
                {
                    //����������� �������� ������� ������ ����� �������
                    GameCreatePanelRequest(
                        eventComp.regionPE,
                        GamePanelType.RegionMainMapPanel);
                }
                //�����
                else
                {
                    //����������� ���������� ������� �������
                    GameRefreshPanelsSelfRequest(eventComp.regionPE);
                }
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
    }
}