using System.Runtime.CompilerServices;
using UnityEngine;

public static class UnityNull
{
    /// <summary>
    /// Kiểm tra object còn tồn tại (không null, không Destroy).
    /// Nhanh hơn vì check ref null trước khi dùng Unity ==.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAlive(this Object obj)
    {
        if (ReferenceEquals(obj, null)) return false;
        return obj != null; // Unity check (native)
    }

    /// <summary>
    /// Ngược lại với IsAlive().
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDestroyed(this Object obj)
    {
        return !obj.IsAlive();
    }

    /// <summary>
    /// Check null thuần .NET (không đụng Unity operator).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPureNull(this Object obj)
    {
        return ReferenceEquals(obj, null);
    }
}

/* Usage example:
 using UnityEngine;

public class ExampleUnityNull : MonoBehaviour
{
    [Header("References (Assign in Inspector)")]
    public GameObject enemy;  // Test IsAlive/IsDestroyed
    public Material material; // Test IsPureNull (để null để test)

    void Start()
    {
        // Demo IsPureNull: Check nếu chưa assign
        if (material.IsPureNull())
        {
            Debug.LogError("Material chưa assign – kiểm tra Inspector!");
            material = new Material(Shader.Find("Standard")); // Tạo tạm
        }
        else
        {
            Debug.Log("Material OK: " + material.name);
        }

        // Demo IsAlive & IsDestroyed: Log trạng thái ban đầu
        if (enemy.IsAlive())
        {
            Debug.Log("Enemy đang sống!");
        }
        else if (enemy.IsDestroyed())
        {
            Debug.LogWarning("Enemy đã bị destroy!");
        }
    }

    void Update()
    {
        // Demo IsDestroyed: Cleanup nếu enemy bị hủy (test bằng Destroy(enemy) trong Console)
        if (enemy.IsDestroyed())
        {
            enemy = Instantiate(enemy, Vector3.zero, Quaternion.identity); // Tạo lại
            Debug.Log("Enemy bị destroy, đã respawn!");
            Destroy(enemy, 5f); // Auto-destroy sau 5s để loop test
        }

        // Demo IsAlive: Di chuyển nếu còn sống
        if (enemy.IsAlive())
        {
            enemy.transform.position += Vector3.right * Time.deltaTime;
        }
    }
}
 */