
using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;

namespace SO.UI
{
    public enum MapMode : byte
    {
        None,
        Default,
        Terrain,
        StrategicArea,
        Exploration
    }

    public class InputData : MonoBehaviour
    {
        public MapMode mapMode = MapMode.Terrain;

        public bool leftMouseButtonClick;
        public bool leftMouseButtonPressed;
        public bool leftMouseButtonRelease;

        public bool rightMouseButtonClick;
        public bool rightMouseButtonPressed;
        public bool rightMouseButtonRelease;

        public bool leftShiftKeyPressed;
        public bool LMBAndLeftShift
        {
            get
            {
                return (leftMouseButtonClick || leftMouseButtonPressed) && leftShiftKeyPressed;
            }
        }
        public bool RMBAndLeftShift
        {
            get
            {
                return (rightMouseButtonClick || rightMouseButtonPressed) && leftShiftKeyPressed;
            }
        }

        public Transform mapCamera;
        public Transform swiwel;
        public Transform stick;
        public UnityEngine.Camera camera;

        public float movementSpeedMinZoom;
        public float movementSpeedMaxZoom;

        public float rotationSpeed;
        public float rotationAngleY;
        public float rotationAngleX;
        public float minAngleX;
        public float maxAngleX;

        public float zoom;
        public float stickMinZoom;
        public float stickMaxZoom;
        public float swiwelMinZoom;
        public float swiwelMaxZoom;


        public bool isMouseOver;
        public int lastHitRegionIndex;
        public EcsPackedEntity lastHighlightedRegionPE;

        public EcsPackedEntity searchFromRegion;
        public EcsPackedEntity searchToRegion;


        public EcsPackedEntity playerFactionPE;


        public List<EcsPackedEntity> activeTaskForcePEs = new();
    }
}