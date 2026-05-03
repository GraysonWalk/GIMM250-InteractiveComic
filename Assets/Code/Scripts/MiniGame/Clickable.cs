using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Clickable : MonoBehaviour
{
    [SerializeField] UnityEvent onClick;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void Click()
    {
        Debug.Log("Clicked: " + gameObject.name);
        onClick?.Invoke();
    
    }
}
