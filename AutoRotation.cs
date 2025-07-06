using UnityEngine;

public class AutoRotation : MonoBehaviour
{
    public Vector3 rotationSpeed;
    private Transform m_Transform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Transform = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        m_Transform.Rotate(Time.deltaTime * rotationSpeed.x, Time.deltaTime * rotationSpeed.y, Time.deltaTime * rotationSpeed.z);
    }
}
