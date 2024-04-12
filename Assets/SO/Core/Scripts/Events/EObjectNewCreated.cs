
using Leopotam.EcsLite;

namespace SO
{
    public enum ObjectNewCreatedType : byte
    {
        None,
        Character,
        TaskForce,
        Population
    }

    public struct EObjectNewCreated
    {
        public EObjectNewCreated(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            this.objectPE = objectPE;
            this.objectType = objectType;
        }

        public readonly EcsPackedEntity objectPE;
        public readonly ObjectNewCreatedType objectType;
    }
}