using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class PixelPerfectUI : MonoBehaviour
{
    public float pixelsPerUnit = 1f; // Match your project PPU (e.g. 16)
    private RectTransform rectTransform;

    void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        Vector3 pos = rectTransform.localPosition;
        rectTransform.localPosition = new Vector3(
            Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit,
            Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit,
            pos.z
        );
    }
}
