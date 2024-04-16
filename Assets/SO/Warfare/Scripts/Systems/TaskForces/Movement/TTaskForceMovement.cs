
using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;

using SO.Map.Province;

namespace SO.Warfare.Fleet.Movement
{
    public struct TTaskForceMovement : IEcsThread<
        CTaskForceMovement, CTaskForce,
        CProvinceCore>
    {
        public EcsWorld world;

        int[] tFEntities;

        CTaskForce[] tFPool;
        int[] tFIndices;

        CTaskForceMovement[] tFMovementPool;
        int[] tFMovementIndices;

        CProvinceCore[] pCPool;
        int[] pCIndices;

        public void Init(
            int[] entities,
            CTaskForceMovement[] pool1, int[] indices1,
            CTaskForce[] pool2, int[] indices2,
            CProvinceCore[] pool3, int[] indices3)
        {
            tFEntities = entities;

            tFMovementPool = pool1;
            tFMovementIndices = indices1;

            tFPool = pool2;
            tFIndices = indices2;

            pCPool = pool3;
            pCIndices = indices3;
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

                //���� ���������� ��������� �� �����
                if(tF.previousProvincePE.Unpack(world, out int previousProvinceEntity))
                {
                    //�������� ���������� ���������
                    tF.previousProvincePE = new();
                }

                //���� ������� ������ �� ����
                if(tFMovement.pathProvincePEs.Count > 0)
                {
                    //���� ��������� ��������� � ��������, �� ���� ��������� ��������� ����
                    tFMovement.pathProvincePEs[tFMovement.pathProvincePEs.Count - 1].Unpack(world, out int nextProvinceEntity);
                    ref CProvinceCore nextProvince = ref pCPool[pCIndices[nextProvinceEntity]];

                    //������������ �������� � ������ ������� ����������� ������ � ������������ ��������� ���������
                    float movementSpeed = 50;

                    //���������� �������� � ����������� ����������
                    tFMovement.traveledDistance += movementSpeed;

                    //���� ���������� ���������� ������ ��� ����� ���������� ����� �����������
                    if (tFMovement.traveledDistance >= ProvincesData.provinceDistance)
                    {
                        //�� ������ ��������� � ��������� ���������

                        //��������, ��� ������ ��������� �����������
                        tFMovement.isTraveled = true;

                        //�������� ���������� ����������
                        tFMovement.traveledDistance = 0;

                        UnityEngine.Debug.LogWarning("Finish 1! " + nextProvince.Index + " ! " + tF.selfPE.Id);
                    }
                }
                //�����
                else
                {
                    //������ ��� ��������� � ������� ��������� (��� �������� ������ ��� ���������� ������� ����)

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