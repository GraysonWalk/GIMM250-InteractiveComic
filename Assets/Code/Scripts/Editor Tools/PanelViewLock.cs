using Unity.Cinemachine;
using UnityEngine;

/// <summary>
///     Editor-only workflow helper. Add this component to a panel to lock the Game View
///     to that panel's CinemachineCamera, regardless of what you select in the Inspector.
///     Remove (or disable) the component to release the lock.
///
///     All logic is stripped in builds via #if UNITY_EDITOR — zero runtime cost.
/// </summary>
[ExecuteAlways]
public class PanelViewLock : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private CinemachineCamera cam;

    private void OnEnable()
    {
        if (cam != null)
            CinemachineCore.SoloCamera = cam;
    }

    private void OnDisable()
    {
        // Only clear the solo if we own it — another PanelViewLock may have taken over.
        if (cam != null && (CinemachineCamera)CinemachineCore.SoloCamera == cam)
            CinemachineCore.SoloCamera = null;
    }

    private void OnValidate()
    {
        // Auto-find the sibling CinemachineCamera when first added, so the field
        // is usually populated without any manual drag-and-drop.
        if (cam == null)
            cam = GetComponentInChildren<CinemachineCamera>(true);

        if (enabled && cam != null)
            CinemachineCore.SoloCamera = cam;
    }
#endif
}
