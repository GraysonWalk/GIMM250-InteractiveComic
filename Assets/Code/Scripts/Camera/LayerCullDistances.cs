using UnityEngine;

/// <summary>
///     Sets per-layer camera cull distances so panels far from the active camera are culled
///     without reducing the main far-clip plane (which would also cull the planet and skybox).
///
///     Unity's <see cref="Camera.layerCullDistances"/> allows each of the 32 layers to have its
///     own maximum draw distance. A value of 0 means "use the camera's global far-clip distance".
///     Any positive value overrides the far-clip for that layer only.
///
///     Designer workflow:
///     1. Create a layer named "Panels" in Project Settings → Tags &amp; Layers.
///     2. Assign all ComicPanel root GameObjects to the Panels layer.
///        (Child objects — art meshes etc. — should also use the Panels layer so they are culled too.)
///     3. Attach this component to the Main Camera GameObject (the one with CinemachineBrain).
///     4. Tune <i>Panel Cull Distance</i> in the Inspector until distant panels disappear
///        while the 1–2 panels nearest the active camera remain visible.
///     5. Leave the planet and environment geometry on the Default layer — they continue
///        to use the camera's global far-clip distance and are unaffected.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LayerCullDistances : MonoBehaviour
{
    #region Variables

    [Tooltip("Name of the layer assigned to all ComicPanel GameObjects. " +
             "Must match exactly the layer name in Project Settings → Tags & Layers.")]
    [SerializeField] private string panelLayerName = "Panels";

    [Tooltip("Panels further than this distance (in world units) from the camera are culled. " +
             "Start around 300 and tighten until background clutter disappears " +
             "but the 1–2 panels nearest the active camera remain visible.")]
    [SerializeField] private float panelCullDistance = 300f;

    private Camera _camera;

    #endregion

    #region Methods

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        ApplyCullDistances();
    }

    private void OnValidate()
    {
        // Re-apply in the Editor when values are tweaked in the Inspector during Play Mode.
        if (_camera == null) _camera = GetComponent<Camera>();
        ApplyCullDistances();
    }

    private void ApplyCullDistances()
    {
        int panelLayer = LayerMask.NameToLayer(panelLayerName);

        if (panelLayer < 0)
        {
            Debug.LogError(
                $"[LayerCullDistances] Layer '{panelLayerName}' not found. " +
                "Create it in Project Settings → Tags & Layers and assign it " +
                "to all ComicPanel root GameObjects.", this);
            return;
        }

        // Unity requires the array to have exactly 32 entries — one per layer.
        // 0 means "use the camera's global far-clip"; any positive value caps that layer.
        float[] distances = new float[32];
        distances[panelLayer] = panelCullDistance;
        _camera.layerCullDistances = distances;
    }

    #endregion
}
