using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    // [CustomEditor(typeof(SimpleCopy))]
    // public class SimpleCopyEditor : VolumeComponentEditor
    // {
    //     private SerializedDataParameter m_Activate;
    //
    //     public override void OnEnable()
    //     {
    //         var o = new PropertyFetcher<SimpleCopy>(serializedObject);
    //         
    //         m_Activate = Unpack(o.Find(x => x.activate));
    //     }
    //
    //     public override void OnInspectorGUI()
    //     {
    //         PropertyField(m_Activate);
    //     }
    // }
}