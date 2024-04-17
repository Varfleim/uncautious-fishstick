
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.State;
using SO.Population.Events;

namespace SO.Population
{
    public class SPopulationControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CState>> stateFilter = default;
        readonly EcsPoolInject<CState> statePool = default;

        //���������
        readonly EcsPoolInject<CPopulation> pOPPool = default;

        public void Run(IEcsSystems systems)
        {
            //�������� ����� ���������
            PopulationCreating();
        }

        void PopulationCreating()
        {
            //��� ������ �������
            foreach (int currentStateEntity in stateFilter.Value)
            {
                //���� �������
                ref CState currentState = ref statePool.Value.Get(currentStateEntity);

                //���� � ������� ���� ���������� ������ ���������
                for (int a = 0; a < currentState.orderedPOPs.Count; a++)
                {
                    //���� ���������� ���
                    DROrderedPopulation orderedPOP = currentState.orderedPOPs[a];

                    //���������� �������, � ������� �������� ���
                    int targetStateEntity;

                    //���� � ���� ��� ������� ������� 
                    if (orderedPOP.targetStatePE.Unpack(world.Value, out targetStateEntity) == false)
                    {
                        //��� �������� � ������� �������

                        //��������� � �������� ������� �������
                        targetStateEntity = currentStateEntity;
                    }
                    //����� ��� �������� � ������� �������

                    //���� ������� �������
                    ref CState targetState = ref statePool.Value.Get(targetStateEntity);

                    //���� � ������� ��� ���� ���������� ���
                    if (FindIdenticalPopulation(
                        ref targetState,
                        ref orderedPOP,
                        out int oldPOPEntity))
                    {
                        //���� ���
                        ref CPopulation oldPOP = ref pOPPool.Value.Get(oldPOPEntity);

                        //��������� � ���� ���������
                        PopulationAddToOld(
                            ref orderedPOP,
                            ref oldPOP);
                    }
                    //�����
                    else
                    {
                        //������ ����� ��� � �������
                        PopulationCreateNew(
                            ref orderedPOP,
                            ref targetState);
                    }
                }
                currentState.orderedPOPs.Clear();
            }
        }

        bool FindIdenticalPopulation(
            ref CState state,
            ref DROrderedPopulation orderedPOP,
            out int oldPOPEntity)
        {
            //����� �������������� ������
            oldPOPEntity = -1;

            //��� ������ ������ ��������� � �������
            for (int a = 0; a < state.pOPPEs.Count; a++)
            {
                //���� ���
                state.pOPPEs[a].Unpack(world.Value, out oldPOPEntity);
                ref CPopulation oldPOP = ref pOPPool.Value.Get(oldPOPEntity);

                //���� ������������ ��� ��������� ������������
                if(PopulationFunctions.IsPopulationIdentical(
                    ref orderedPOP, 
                    ref oldPOP))
                {
                    //����������, ��� ��� ���������� ���
                    return true;
                }
            }

            //����������, ��� ����������� ���� ���
            return false;
        }

        void PopulationCreateNew(
            ref DROrderedPopulation orderedPOP,
            ref CState state)
        {
            //������ ����� �������� � ��������� �� ��������� ����
            int pOPEntity = world.Value.NewEntity();
            ref CPopulation pOP = ref pOPPool.Value.Add(pOPEntity);

            //��������� �������� ������ ����
            pOP = new(
                world.Value.PackEntity(pOPEntity),
                orderedPOP.socialClassIndex);

            //��������� ���� ��� ���������
            pOP.UpdatePopulation(ref orderedPOP);

            //������� PE ���� � ������ �������
            state.pOPPEs.Add(pOP.selfPE);

            //������ �������, ���������� � �������� ������ ����
            ObjectNewCreatedEvent(pOP.selfPE, ObjectNewCreatedType.Population);
        }

        void PopulationAddToOld(
            ref DROrderedPopulation orderedPOP,
            ref CPopulation oldPOP)
        {
            //��������� ���� ����� ���������
            oldPOP.UpdatePopulation(ref orderedPOP);
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