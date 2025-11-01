using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

/// <summary>
/// Dùng để prewarm toàn bộ hệ thống tween của LitMotion.
/// </summary>
[DefaultExecutionOrder(-100)] // chạy sớm hơn mọi script khác
public class LMotionWarmUpController : MonoBehaviour
{
    [Header("WarmUp Settings")]
    public bool warmUpScale = true;
    public bool warmUpAlpha = true;
    public bool warmUpPosition = true;
    public bool warmUpRotation = true;
    public bool warmUpColor = true;

    [Tooltip("Tự động hủy GameObject tạm sau khi warm-up xong.")]
    public bool destroyImmediately = true;

    private void Awake()
    {
        RunWarmUp();
    }

    private void RunWarmUp()
    {
        // ⚙️ 2️⃣ Làm nóng các binder cụ thể
        var go = new GameObject("LM_WarmUp")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var t = go.transform;

        if (warmUpScale)
        {
            t.localScale = Vector3.zero;
            LMotion.Create(Vector3.zero, Vector3.one, 0.01f)
                .BindToLocalScale(t);
        }

        if (warmUpAlpha)
        {
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            LMotion.Create(0f, 1f, 0.01f)
                .BindToAlpha(cg);
        }

        if (warmUpPosition)
        {
            LMotion.Create(Vector3.zero, Vector3.one, 0.01f)
                .BindToLocalPosition(t);
        }

        if (warmUpRotation)
        {
            LMotion.Create(Quaternion.identity, Quaternion.Euler(0, 90, 0), 0.01f)
                .BindToLocalRotation(t);
        }

        if (warmUpColor)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.black;

            LMotion.Create(Color.black, Color.white, 0.01f)
                .BindToColor(sr);
        }

        // ⚙️ 3️⃣ Làm nóng các kiểu dữ liệu phổ biến khác
        LMotion.Create(0f, 1f, 0.01f);
        LMotion.Create(Vector2.zero, Vector2.one, 0.01f);
        LMotion.Create(Vector4.zero, Vector4.one, 0.01f);

        if (destroyImmediately)
            DestroyImmediate(go);
        else
            Destroy(go);
    }
}
