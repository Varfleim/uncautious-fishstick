
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Faction.Events;
using SO.Map.Events;

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
                world.Value.PackEntity(factionEntity), factionPool.Value.GetRawDenseItemsCount(),
                requestComp.factionName);

            //����
            inputData.Value.playerFactionPE = faction.selfPE;
            //����

            //����������� ������������� ���������� ������� �������
            FactionStartRegionInitializerRequest(faction.selfPE);
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
    }
}