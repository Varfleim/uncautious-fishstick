
using UnityEngine;

namespace SO
{
    public class RuntimeData
    {
        public bool isGameActive = false;

        public int countriesCount = 0;

        public Color playerColor = new Color32((byte)(Random.value * 255), (byte)(Random.value * 255), (byte)(Random.value * 255), 255);
    }
}