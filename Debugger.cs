using UnityEngine;
using static YPipeline.RandomUtility;

public class Debugger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (uint i = 0; i < 10; i++)
        {
            Debug.Log(Halton_Inverse(i));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
