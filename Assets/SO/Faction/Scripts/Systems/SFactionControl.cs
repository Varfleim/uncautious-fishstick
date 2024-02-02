
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Faction.Events;

namespace SO.Faction
{
    public class SFactionControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //�������
        readonly EcsPoolInject<CFaction> factionPool = default;


        //������� �������
        readonly EcsFilterInject<Inc<RFactionCreating>> factionCreatingRequestFilter = default;
        readonly EcsPoolInject<RFactionCreating> factionCreatingRequestPool = default;


        //������
        readonly EcsCustomInject<UI.InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� �������� �������
            foreach (int requestEntity in factionCreatingRequestFilter.Value)
            {
                //���� ������
                ref RFactionCreating requestComp = ref factionCreatingRequestPool.Value.Get(requestEntity);

                //������ ����� �������
                FactionCreating(
                    ref requestComp);

                factionCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        void FactionCreating(
            ref RFactionCreating requestComp)
        {
            //������ ����� �������� � ��������� �� ��������� �������
            int factionEntity = world.Value.NewEntity();
            ref CFaction faction = ref factionPool.Value.Add(factionEntity);

            //��������� �������� ������ �������
            faction = new(
                world.Value.PackEntity(factionEntity), runtimeData.Value.factionsCount++,
                requestComp.factionName);

            //����
            inputData.Value.playerFactionPE = faction.selfPE;
            //����

            //����������� �������� ExFRFO ������ �������
            FactionExFRFOsCreatingSelfRequest(factionEntity);

            //����������� ������������� ���������� ������� �������
            FactionStartRegionInitializerRequest(faction.selfPE);

            //������ �������, ���������� � �������� ����� �������
            ObjectNewCreatedEvent(faction.selfPE, ObjectNewCreatedType.Faction);
        }

        readonly EcsPoolInject<RRegionInitializer> regionInitializerRequestPool = default;
        readonly EcsPoolInject<RRegionInitializerOwner> regionInitializerOwnerRequestPool = default;
        void FactionStartRegionInitializerRequest(
            EcsPackedEntity factionPE)
        {
            //������ ����� �������� � ��������� �� ������ ���������� ��������������
            int requestEntity = world.Value.NewEntity();
            ref RRegionInitializer requestCoreComp = ref regionInitializerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestCoreComp = new();
            
            //��������� ��������� ���������
            ref RRegionInitializerOwner requestOwnerComp = ref regionInitializerOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestOwnerComp = new(factionPE);
        }

        readonly EcsPoolInject<SRExRFOsCreating> exFRFOsCreatingSelfRequestPool = default;
        void FactionExFRFOsCreatingSelfRequest(
            int factionEntity)
        {
            //��������� ������� ���������� �������� ExFRFO
            ref SRExRFOsCreating selfRequestComp = ref exFRFOsCreatingSelfRequestPool.Value.Add(factionEntity);

            //��������� ������ �������
            selfRequestComp = new();
        }

        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            //������ ����� �������� � ��������� �� ������� �������� ������ �������
            int eventEntity = world.Value.NewEntity();
            ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(objectPE, objectType);
        }
    }
}