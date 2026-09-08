using UnityEngine;

/// <summary>
/// 2D 正交相机渲染像素对齐。
///
/// Cinemachine 可以继续平滑移动真实 Transform；渲染前仅修正 worldToCameraMatrix，
/// 把最终画面吸附到屏幕像素网格，避免直接修改 Transform 后与 Cinemachine 相互覆盖。
/// </summary>
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class CameraPixelSnap : MonoBehaviour
{
    [Tooltip("移动端目标帧率。提高到 60 可减轻低帧率下相机移动产生的拖影感。")]
    [SerializeField, Min(30)] private int targetMobileFrameRate = 60;

    private Camera cachedCamera;
    private bool matrixOverridden;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();

        if (Application.isMobilePlatform)
            Application.targetFrameRate = targetMobileFrameRate;
    }

    private void OnPreCull()
    {
        if (cachedCamera == null || !cachedCamera.orthographic)
            return;

        int renderHeight = cachedCamera.pixelHeight;
        if (renderHeight <= 0)
            return;

        float pixelWorldSize = (2f * cachedCamera.orthographicSize) / renderHeight;
        if (pixelWorldSize <= 0f)
            return;

        Vector3 cameraPosition = transform.position;
        Vector3 snappedPosition = cameraPosition;
        snappedPosition.x = Mathf.Round(cameraPosition.x / pixelWorldSize) * pixelWorldSize;
        snappedPosition.y = Mathf.Round(cameraPosition.y / pixelWorldSize) * pixelWorldSize;

        Vector3 offset = snappedPosition - cameraPosition;
        Matrix4x4 offsetMatrix = Matrix4x4.TRS(
            -offset,
            Quaternion.identity,
            new Vector3(1f, 1f, -1f));

        cachedCamera.worldToCameraMatrix = offsetMatrix * transform.worldToLocalMatrix;
        matrixOverridden = true;
    }

    private void OnPostRender()
    {
        ResetCameraMatrix();
    }

    private void OnDisable()
    {
        ResetCameraMatrix();
    }

    private void ResetCameraMatrix()
    {
        if (!matrixOverridden || cachedCamera == null)
            return;

        cachedCamera.ResetWorldToCameraMatrix();
        matrixOverridden = false;
    }
}
