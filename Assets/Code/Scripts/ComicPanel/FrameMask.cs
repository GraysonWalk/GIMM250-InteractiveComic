using UnityEngine;

/// <summary>
///     Keeps the SpriteMask on this child object sized to match the 9-sliced SpriteRenderer on the parent Frame.
///     Because a 9-sliced SpriteRenderer uses SpriteRenderer.size (not transform.localScale) to control its
///     displayed dimensions, the SpriteMask cannot live on the same GameObject — it must be a child that reads
///     the parent's size and applies it to its own localScale.
///
///     Assign a plain 1×1 unit white square sprite to the SpriteMask (e.g. 100×100 px at 100 PPU).
///     With that sprite, localScale = (width, height, 1) maps directly to world units.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteMask))]
public class FrameMask : MonoBehaviour
{
    #region Variables

    private SpriteRenderer _frame;

    #endregion

    #region Methods

    private void Awake() => CacheAndSync();

    private void OnValidate() => CacheAndSync();

    private void CacheAndSync()
    {
        _frame = GetComponentInParent<SpriteRenderer>();
        Sync();
    }

    private void Sync()
    {
        if (_frame == null) return;
        Vector2 size = _frame.size;
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    #endregion
}
