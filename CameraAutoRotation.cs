using UnityEngine;

public class CameraAutoRotation : MonoBehaviour
{
    public Vector3 rotationSpeed;
    private Camera m_Camera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        m_Camera.transform.Rotate(Time.deltaTime * rotationSpeed.x, Time.deltaTime * rotationSpeed.y, Time.deltaTime * rotationSpeed.z);
    }
}
