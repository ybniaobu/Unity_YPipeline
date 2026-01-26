using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    public class Debugger : MonoBehaviour
    {
        public ReflectionProbe probe;
        
        void Start()
        {
           
        }
        
        void Update()
        {

        }

        public void DebugEntry()
        {
            YPipelineReflectionProbe yProbe = probe.GetYPipelineReflectionProbe();
            Debug.Log(yProbe.isOctahedralAtlasBaked);
            Debug.Log(yProbe.octahedralAtlasLow != null);
            Debug.Log(yProbe.octahedralAtlasMedium != null);
            Debug.Log(yProbe.octahedralAtlasHigh != null);
        }
    }
}
