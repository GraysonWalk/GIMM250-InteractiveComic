using UnityEngine;

public class UserGrabber : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public string userCred;
    void Start()
    {
        userCred = System.Environment.UserName;
        Debug.Log(userCred);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
