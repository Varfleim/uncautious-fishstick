
using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;

using SO.Map.Region;

namespace SO.Warfare.Fleet.Movement
{
    public struct TTaskForceMovement : IEcsThread<
        CTaskForceMovement, CTaskForce,
        CRegionCore>
    {
        public EcsWorld world;

        int[] tFEntities;

        CTaskForce[] tFPool;
        int[] tFIndices;

        CTaskForceMovement[] tFMovementPool;
        int[] tFMovementIndices;

        CRegionCore[] regionPool;
        int[] regionIndices;

        public void Init(
            int[] entities,
            CTaskForceMovement[] pool1, int[] indices1,
            CTaskForce[] pool2, int[] indices2,
            CRegionCore[] pool3, int[] indices3)
        {
            tFEntities = entities;

            tFMovementPool = pool1;
            tFMovementIndices = indices1;

            tFPool = pool2;
            tFIndices = indices2;

            regionPool = pool3;
            regionIndices = indices3;
        }

        public void Execute(int threadId, int fromIndex, int beforeIndex)
        {
            //��� ������ ����������� ������ � ����������� ��������
            for (int a = fromIndex; a < beforeIndex; a++)
            {
                //���� ��������� �������� � ������
                int tFEntity = tFEntities[a];
                ref CTaskForceMovement tFMovement = ref tFMovementPool[tFMovementIndices[tFEntity]];
                ref CTaskForce tF = ref tFPool[tFIndices[tFEntity]];

                //���� ���������� ������ �� ����
                if(tF.previousRegionPE.Unpack(world, out int previousRegionEntity))
                {
                    //�������� ���������� ������
                    tF.previousRegionPE = new();
                }

                //���� ������� ������ �� ����
                if(tFMovement.pathRegionPEs.Count > 0)
                {
                    //���� ��������� ������ � ��������, �� ���� ��������� ������ ����
                    tFMovement.pathRegionPEs[tFMovement.pathRegionPEs.Count - 1].Unpack(world, out int nextRegionEntity);
                    ref CRegionCore nextRegion = ref regionPool[regionIndices[nextRegionEntity]];

                    //������������ �������� � ������ ������� ����������� ������ � ������������ ���������� �������
                    float movementSpeed = 50;

                    //���������� �������� � ����������� ����������
                    tFMovement.traveledDistance += movementSpeed;

                    //���� ���������� ���������� ������ ��� ����� ���������� ����� ���������
                    if (tFMovement.traveledDistance >= RegionsData.regionDistance)
                    {
                        //�� ������ ��������� � ��������� ������

                        //��������, ��� ������ ��������� �����������
                        tFMovement.isTraveled = true;

                        //�������� ���������� ����������
                        tFMovement.traveledDistance = 0;

                        UnityEngine.Debug.LogWarning("Finish 1! " + nextRegion.Index + " ! " + tF.selfPE.Id);
                    }
                }
                //�����
                else
                {
                    //������ ��� ��������� � ������� ������� (��� �������� ������ ��� ���������� ������� ����)

                    //��������, ��� ������ ��������� ��������
                    tFMovement.isTraveled = true;

                    //�������� ���������� ����������
                    tFMovement.traveledDistance = 0;

                    UnityEngine.Debug.LogWarning("Finish 2! " + tF.selfPE.Id);
                }
            }
        }
    }
}