using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Rendering;


public class Debugger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string scenePath = Path.ChangeExtension(SceneManager.GetActiveScene().path, null);
        if (string.IsNullOrEmpty(scenePath)) scenePath = "Assets";
        else if (Directory.Exists(scenePath) == false) Directory.CreateDirectory(scenePath);
                
        // 查找 ReflectionProbe-X
        HashSet<int> existingNumbers = new HashSet<int>();
        foreach (string filePath in Directory.GetFiles(scenePath, "ReflectionProbe-*"))
        {
            if (filePath.EndsWith(".meta")) continue;
            string fileName = Path.GetFileNameWithoutExtension(filePath);
                    
            string numberPart = fileName.Substring("ReflectionProbe-".Length);
            if (int.TryParse(numberPart, out int number))
            {
                existingNumbers.Add(number);
            }
        }
                
        int firstAvailableNumber = 0;
        while (existingNumbers.Contains(firstAvailableNumber))
        {
            firstAvailableNumber++;
        }
                
        string path = Path.Combine(scenePath, $"ReflectionProbe-{firstAvailableNumber}");
        
        Debug.Log(path);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
