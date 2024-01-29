using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ProgressBar : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Linear Progress Bar")]
    public static void AddLinearProgressBar()
    {
        GameObject gameObject = Instantiate(UnityEngine.Resources.Load<GameObject>("UI/Linear Progress Bar"));

        gameObject.transform.SetParent(Selection.activeGameObject.transform, false);
    }

    [MenuItem("GameObject/UI/Radial Progress Bar")]
    public static void AddRadialProgressBar()
    {
        GameObject gameObject = Instantiate(UnityEngine.Resources.Load<GameObject>("UI/Radial Progress Bar"));

        gameObject.transform.SetParent(Selection.activeGameObject.transform, false);
    }
#endif

    public float minimum;
    public float maximum;
    public float current;

    public Image mask;
    public Image fill;
    public Color color;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GetCurrentFill();
    }

    void GetCurrentFill()
    {
        float currentOffset = current - minimum;
        float maximumOffset = maximum - minimum;

        float fillAmount = currentOffset / maximumOffset;

        mask.fillAmount = fillAmount;

        fill.color = color;
    }
}
