using UnityEngine;
using TMPro;

public class FPSTest : MonoBehaviour
{
    public TMP_Text text;
    
    private int count;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //textMesh = gameObject.GetComponent<TextMesh>();
    }

    // Update is called once per frame
    void Update()
    {
        count += 1;
        if (count % 100 == 0)
        {
            var fps = Time.deltaTime * 1000;
            text.text = $"ms: {fps}";
        }
    }
}
