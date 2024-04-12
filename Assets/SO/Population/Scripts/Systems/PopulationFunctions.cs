
using SO.Population.Events;

namespace SO.Population
{
    public static class PopulationFunctions
    {
        public static bool IsPopulationIdentical(
            ref CPopulation firstPOP, ref CPopulation secondPOP)
        {
            if(firstPOP.socialClassIndex == secondPOP.socialClassIndex)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public static bool IsPopulationIdentical(
            ref DROrderedPopulation firstPOP,
            ref CPopulation secondPOP)
        {
            if(firstPOP.socialClassIndex == secondPOP.socialClassIndex)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}