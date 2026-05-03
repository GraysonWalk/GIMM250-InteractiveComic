using UnityEngine;
using UnityEngine.InputSystem;

public class MouseAttach : MonoBehaviour
{
    [SerializeField] GameObject mask;
    private Vector3 mousePos;
    private Vector3 worldPos;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        mousePos = Mouse.current.position.ReadValue();
        float distance = Mathf.Abs(Camera.main.transform.position.z - mask.transform.position.z);
        mousePos.z = distance;
        worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        mask.transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);

    }
}
