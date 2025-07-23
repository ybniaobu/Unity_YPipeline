using UnityEngine;

public class AutoRotation : MonoBehaviour
{
    public Vector3 rotationSpeed;
    public Vector3 moveSpeed;
    public float moveDistance = 1.0f;
    public bool stop;
    private Transform m_Transform;
    private Vector3 m_StartPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Application.targetFrameRate = 60;
        m_Transform = GetComponent<Transform>();
        m_StartPosition = m_Transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!stop)
        {
            m_Transform.Rotate(Time.deltaTime * rotationSpeed.x, Time.deltaTime * rotationSpeed.y,
                Time.deltaTime * rotationSpeed.z);
            m_Transform.Translate(Time.deltaTime * moveSpeed, Space.World);
            if (Vector3.Distance(m_StartPosition, m_Transform.position) > moveDistance)
            {
                moveSpeed = -moveSpeed;
            }
        }
    }
}
