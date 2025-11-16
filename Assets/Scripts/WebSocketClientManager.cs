using System;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

[System.Serializable]
public class EEGMessage
{
    public float timestamp;
    public AverageData average;
    public int buffer_size;
    public string ollama_response;
}

[System.Serializable]
public class AverageData
{
    public float Delta;
    public float Theta;
    public float Alpha;
    public float Beta;
    public float Gamma;
}

public class WebSocketClientManager : MonoBehaviour
{
    [Header("WebSocket")]
    [SerializeField] private string serverIp = "192.168.1.42";   // laptop IP
    [SerializeField] private int serverPort = 8080;
    public bool connectOnStart = true;

    private WebSocket websocket;

    public event Action<string> OnMessageReceived;
    public event Action<string> OnOllamaResponseReceived;  // New event for ollama status
    public event Action OnConnected;
    public event Action<WebSocketCloseCode> OnDisconnected;

    private async void Start()
    {
        if (connectOnStart)
        {
            await Connect();
        }
    }

    public async Task Connect()
    {
        string url = $"ws://{serverIp}:{serverPort}";
        Debug.Log($"[WS] Connecting to {url}");

        websocket = new WebSocket(url);

        websocket.OnOpen += () =>
        {
            Debug.Log("[WS] Connection open");
            OnConnected?.Invoke();
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("[WS] Error: " + e);
        };

        websocket.OnClose += (code) =>
        {
            Debug.LogWarning("[WS] Closed: " + code);
            OnDisconnected?.Invoke(code);
        };

        websocket.OnMessage += (bytes) =>
        {
            string msg = Encoding.UTF8.GetString(bytes);
            
            // Parse JSON and extract ollama_response
            try
            {
                EEGMessage data = JsonUtility.FromJson<EEGMessage>(msg);
                
                if (!string.IsNullOrEmpty(data.ollama_response))
                {
                    Debug.Log($"[WS] Ollama Status: {data.ollama_response}");
                    
                    // Trigger TTS function with the ollama response
                    OnOllamaResponseReceived?.Invoke(data.ollama_response);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WS] Failed to parse message: {ex.Message}");
            }
            
            // Still invoke the raw message event if needed
            OnMessageReceived?.Invoke(msg);
        };

        try
        {
            await websocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError("[WS] Exception while connecting: " + ex);
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private void OnApplicationQuit()
    {
        if (websocket != null)
        {
            websocket.Close();
        }
    }

    public async void SendString(string text)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("[WS] Cannot send, socket not open");
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        await websocket.Send(bytes);
    }

    public void SendJson(object obj)
    {
        string json = JsonUtility.ToJson(obj);
        SendString(json);
    }
}