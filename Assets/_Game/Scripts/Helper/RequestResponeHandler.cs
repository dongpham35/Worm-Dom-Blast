using MemoryPack;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[MemoryPackable]
public partial class PayloadData
{
    public string DeviceId { get; set; }
    public string Json { get; set; }
}

public class RequestResponeHandler
{
    private readonly string baseAddress = "";

    #region API Payload Class
    // Generic method for API calls
    public async Task<T> MakeApiRequest<T>(string endpoint, string deviceId, string jsonData = "") where T : class
    {
        if (string.IsNullOrEmpty(baseAddress))
        {
            return null;
        }
        var payload = new PayloadData { DeviceId = deviceId, Json = jsonData };
        byte[] bytes = MemoryPackSerializer.Serialize(payload);

        using var uwr = new UnityWebRequest(baseAddress + endpoint, "POST");
        uwr.uploadHandler = new UploadHandlerRaw(bytes);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("X-API-Key", "b2c3d4e5-f6g7-8901-bcde-f23456789012");
        uwr.SetRequestHeader("Content-Type", "application/x-memorypack");
        uwr.SetRequestHeader("Accept", "application/json");

        try
        {
            await SendWebRequestAsync(uwr);

#if UNITY_2020_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                Debug.LogError($"[MakeApiRequest] [{endpoint}] Error: {uwr.error} | Status: {uwr.responseCode} | Body: {uwr.downloadHandler.text}");
                return null;
            }

            string json = uwr.downloadHandler.text;
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MakeApiRequest] [{endpoint}] Error processing response: {e}");
            return null;
        }
    }

    // Generic method for saving data to API
    public async Task<bool> SaveToApi(string endpoint, string deviceId, string jsonData)
    {
        try
        {
            if (string.IsNullOrEmpty(baseAddress))
            {
                return false;
            }

            var payload = new PayloadData { DeviceId = deviceId, Json = jsonData };
            byte[] bytes = MemoryPackSerializer.Serialize(payload);

            using var uwr = new UnityWebRequest(baseAddress + endpoint, "POST");
            uwr.uploadHandler = new UploadHandlerRaw(bytes);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("X-API-Key", "b2c3d4e5-f6g7-8901-bcde-f23456789012");
            uwr.SetRequestHeader("Content-Type", "application/x-memorypack");

            await SendWebRequestAsync(uwr);

#if UNITY_2020_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                Debug.LogError($"[SaveToApi] [{endpoint}] Save Error: {uwr.error}");
                return false;
            }

            Debug.Log($"[SaveToApi] [{endpoint}] Save successful");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveToApi] [{endpoint}] Error saving data: {e}");
            return false;
        }
    }

#if UNITY_EDITOR
    public async Task<bool> RemovePlayerOnServer(string deviceId)
    {
        // Tạo payload đúng định dạng JSON
        var payload = new PayloadData
        {
            DeviceId = deviceId,
            Json = "7btfNlxK0BY0vqRmvMRvVatG7puHcc0z"
        };
        string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

        using var uwr = new UnityWebRequest(baseAddress + "api/player/remove", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        uwr.SetRequestHeader("X-API-Key", "b2c3d4e5-f6g7-8901-bcde-f23456789012");
        uwr.SetRequestHeader("Content-Type", "application/json");
        uwr.SetRequestHeader("Accept", "application/json");

        try
        {
            await SendWebRequestAsync(uwr);
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[RemovePlayerOnServer] Success: " + uwr.downloadHandler.text);
                return true;
            }
            else
            {
                Debug.LogError("[RemovePlayerOnServer] Error: " + uwr.error + " | " + uwr.downloadHandler.text);
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[RemovePlayerOnServer] Exception: " + e);
            return false;
        }
    }
#endif
    #endregion

    private Task<UnityWebRequest> SendWebRequestAsync(UnityWebRequest uwr)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();
        var operation = uwr.SendWebRequest();
        operation.completed += _ => tcs.SetResult(uwr);
        return tcs.Task;
    }
}
