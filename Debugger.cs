using UnityEngine;
using static YPipeline.RandomUtility;

public class Debugger : MonoBehaviour
{
    public Material material;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(material.GetShaderPassEnabled("MotionVectors"));
        Debug.Log(material.IsKeywordEnabled("_ADD_PRECOMPUTED_VELOCITY"));
    }
}
