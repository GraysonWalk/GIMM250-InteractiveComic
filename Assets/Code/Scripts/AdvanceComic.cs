using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class AdvanceComic : MonoBehaviour
{
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private GameObject cameraHolder;
    private Dictionary<int, CinemachineCamera> _cameras = new Dictionary<int, CinemachineCamera>();
    private InputAction _advanceAction;
    private int _activeCameraKey = 0;
    private int _lastCamera;

    void Awake()
    {
        int i = 0;
        foreach (CinemachineCamera camera in cameraHolder.GetComponentsInChildren<CinemachineCamera>())
        {
            _cameras.Add(i, camera);
            i++;
        }
        _lastCamera = i-1;
        foreach (var camera in _cameras)
        {
            if (camera.Key != _activeCameraKey)
            {
                camera.Value.enabled = false;
            }
        }
        _advanceAction = InputSystem.actions.FindAction("Advance");
    }

    // Update is called once per frame
    void Update()
    {
        if (_advanceAction.triggered)
        {
            Debug.Log("Advance");
            if (_activeCameraKey == _lastCamera)
            {
                _activeCameraKey = 0;
            }
            else
            {
                _activeCameraKey++;
            }
            _cameras[_activeCameraKey].enabled = true;
             foreach (var camera in _cameras)
            {
                if (camera.Key != _activeCameraKey)
                {
                    camera.Value.enabled = false;
                }
            }
        }
    }
}
