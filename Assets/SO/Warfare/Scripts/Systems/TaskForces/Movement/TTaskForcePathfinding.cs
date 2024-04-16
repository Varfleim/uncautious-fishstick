
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;

using SO.Map.Province;
using SO.Warfare.Fleet.Movement.Events;

namespace SO.Warfare.Fleet.Movement
{
    public struct TTaskForcePathfinding : IEcsThread<
        SRTaskForceFindPath,
        CTaskForce, CTaskForceMovement,
        CProvinceCore>
    {
        public EcsWorld world;

        public ProvincesData provincesData;

        int[] tFEntities;

        SRTaskForceFindPath[] tFFindPathSelfRequestPool;
        int[] tFFindPathSelfRequestIndices;

        CTaskForce[] tFPool;
        int[] tFIndices;

        CTaskForceMovement[] tFMovementPool;
        int[] tFMovementIndices;

        CProvinceCore[] pCPool;
        int[] pCIndices;

        public void Init(
            int[] entities,
            SRTaskForceFindPath[] pool1, int[] indices1,
            CTaskForce[] pool2, int[] indices2,
            CTaskForceMovement[] pool3, int[] indices3,
            CProvinceCore[] pool4, int[] indices4)
        {
            tFEntities = entities;

            tFFindPathSelfRequestPool = pool1;
            tFFindPathSelfRequestIndices = indices1;

            tFPool = pool2;
            tFIndices = indices2;

            tFMovementPool = pool3;
            tFMovementIndices = indices3;

            pCPool = pool4;
            pCIndices = indices4;
        }

        public void Execute(int threadId, int fromIndex, int beforeIndex)
        {
            //��� ������ ����������� ������ � ������������ ������ ����
            for(int a = fromIndex; a < beforeIndex; a++)
            {
                //���� ����������, ������ � ��������� ��������
                int tFEntity = tFEntities[a];
                ref SRTaskForceFindPath selfRequestComp = ref tFFindPathSelfRequestPool[tFFindPathSelfRequestIndices[tFEntity]];
                ref CTaskForce tF = ref tFPool[tFIndices[tFEntity]];
                ref CTaskForceMovement tFMovement = ref tFMovementPool[tFMovementIndices[tFEntity]];

                //������� ������ ��������
                tFMovement.pathProvincePEs.Clear();

                //���� ������� ��������� ������
                tF.currentProvincePE.Unpack(world, out int startProvinceEntity);
                ref CProvinceCore startPC = ref pCPool[pCIndices[startProvinceEntity]];

                //���� ������� ���������
                selfRequestComp.targetProvincePE.Unpack(world, out int endProvinceEntity);
                ref CProvinceCore endPC = ref pCPool[pCIndices[endProvinceEntity]];

                //������� ����
                List<int> path = provincesData.PathFindThreads(
                    world,
                    ref pCPool, ref pCIndices,
                    threadId,
                    ref startPC, ref endPC);

                //���� ���� �� ����
                if(path != null)
                {
                    //��� ������ ��������� � ����
                    for (int b = 0; b < path.Count; b++)
                    {
                        //������� ��������� � ������ PE
                        tFMovement.pathProvincePEs.Add(provincesData.provincePEs[path[b]]);

                        UnityEngine.Debug.LogWarning(path[b]);
                    }
                }

                //���������� ������ � ���
                ListPool<int>.Add(
                    threadId,
                    path);
            }
        }
    }
}