using AfferenceEngine.src.Core.Entities;
using AfferenceEngine.src.Core.Other;
using AfferenceEngine.src.Core.StimulationLogic;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class AutoConnectAndCalibrate : MonoBehaviour
{
    [Header("Device Settings")]
    [Tooltip("Your ring device ID (e.g., '4df3')")]
    public string deviceID = "4df3";
    
    [Header("Auto Connect Settings")]
    [Tooltip("Enable automatic connection on start")]
    public bool autoConnectOnStart = true;
    
    [Tooltip("Skip all setup panels and go directly to main app")]
    public bool skipSetupPanels = true;
    
    [Header("Default Calibration Values")]
    [Tooltip("Use these fixed calibration values instead of manual calibration")]
    public bool useFixedCalibration = true;
    
    [Tooltip("Pulse Amplitude Threshold (0-10)")]
    public double PA_Threshold = 2.5;
    
    [Tooltip("Pulse Amplitude Maximum (0-10)")]
    public double PA_Maximum = 7.0;
    
    [Tooltip("Pulse Width Threshold (0-255)")]
    public double PW_Threshold = 50;
    
    [Tooltip("Pulse Width Maximum (0-255)")]
    public double PW_Maximum = 200;
    
    [Header("Calibration Constants")]
    public double calFrequency = 20;
    public double PA_CalibrationPW = 250;
    public double PW_CalibrationPA = 9;
    
    [Header("Panel Settings")]
    [Tooltip("Panel Manager reference (assign from scene)")]
    public PanelManager panelManager;
    
    [Tooltip("Which panel to show after auto-setup (usually the main app panel)")]
    public int mainAppPanelIndex = 4;
    
    [Header("User Settings")]
    [Tooltip("Load existing user - leave empty to use first available user")]
    public string userFileName = "";
    
    [Tooltip("If true, automatically load the first user found in Users folder")]
    public bool useFirstAvailableUser = true;
    
    [Header("Status")]
    [SerializeField] private string connectionStatus = "Not Connected";
    [SerializeField] private bool isConnecting = false;

    private async void Start()
    {
        if (autoConnectOnStart)
        {
            await AutoConnectSequenceAsync();
        }
    }

    private async Task AutoConnectSequenceAsync()
    {
        isConnecting = true;
        connectionStatus = "Starting auto-connect sequence...";
        
        // Step 1: Set device ID
        connectionStatus = "Setting device ID...";
        HapticManager.Instance.SetCommType("ble");
        HapticManager.Instance.SetDevice(deviceID);
        await Task.Delay(100);
        
        // Step 2: Load user
        connectionStatus = "Loading user...";
        
        string userToLoad = userFileName;
        
        // If no user specified, try to find the first available user
        if (string.IsNullOrEmpty(userToLoad) && useFirstAvailableUser)
        {
            userToLoad = GetFirstAvailableUser();
            if (string.IsNullOrEmpty(userToLoad))
            {
                connectionStatus = "No existing users found!";
                Debug.LogError("[AutoConnect] No user files found in Users directory. Please create a user first.");
                isConnecting = false;
                return;
            }
        }
        
        try
        {
            HapticManager.Instance.LoadUser(userToLoad);
            Debug.Log($"[AutoConnect] User loaded successfully: {userToLoad}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AutoConnect] Failed to load user {userToLoad}: {e.Message}");
            connectionStatus = $"Failed to load user: {userToLoad}";
            isConnecting = false;
            return;
        }
        await Task.Delay(500);
        
#if UNITY_ANDROID && !UNITY_EDITOR
        // Step 3: Request BLE permissions (Android only)
        connectionStatus = "Requesting BLE permissions...";
        bool hasPermissions = await HapticManager.Instance.EnsureBlePermissionsAsync();
        
        if (!hasPermissions)
        {
            connectionStatus = "BLE permissions denied!";
            Debug.LogError("[AutoConnect] BLE permissions were denied");
            isConnecting = false;
            return;
        }
        
        // Step 4: Wait for Android to stabilize
        await HapticManager.Instance.WaitForAndroidFocusAndStabilityAsync();
        await Task.Delay(500);
#endif
        
        // Step 5: Connect to device
        connectionStatus = "Connecting to device...";
        bool connected = await HapticManager.Instance.ConnectCurrentUserAsync(
            maxAttempts: 5,
            initialDelaySeconds: 1.0f
        );
        
        if (!connected)
        {
            connectionStatus = "Connection failed!";
            Debug.LogError("[AutoConnect] Failed to connect to device");
            isConnecting = false;
            return;
        }
        
        connectionStatus = "Connected successfully!";
        Debug.Log("[AutoConnect] Device connected successfully");
        await Task.Delay(500);
        
        // Step 6: Apply fixed calibration values
        if (useFixedCalibration)
        {
            connectionStatus = "Applying calibration values...";
            ApplyFixedCalibrationValues();
            await Task.Delay(500);
        }
        
        // Step 7: Start stimulation
        connectionStatus = "Starting stimulation...";
        if (!HapticManager.Instance.stimActive)
        {
            HapticManager.Instance.ToggleStim();
        }
        
        // Activate the quality encoders (index 2 and 3 are typically the main encoders)
        HapticManager.Instance.ActivateEncoders(2, 3);
        await Task.Delay(500);
        
        // Step 8: Skip to main app panel
        if (skipSetupPanels && panelManager != null)
        {
            connectionStatus = "Loading main app...";
            panelManager.SetActivePanel(mainAppPanelIndex);
        }
        
        connectionStatus = "Ready!";
        isConnecting = false;
        Debug.Log("[AutoConnect] Auto-connect sequence completed successfully!");
    }

    private void ApplyFixedCalibrationValues()
    {
        Debug.Log($"[AutoConnect] Applying fixed calibration: PA({PA_Threshold}-{PA_Maximum}), PW({PW_Threshold}-{PW_Maximum})");
        
        StimPoint[] stimpoints = new StimPoint[4];
        
        // Apply to LATERAL bounder
        stimpoints[0] = new StimPoint(PA_Threshold, PA_CalibrationPW);
        HapticManager.Instance.lateralBounder.AddCalibrationData(Waveform.Sine.ToString(), calFrequency, stimpoints);
        
        stimpoints[1] = new StimPoint(PA_Maximum, PA_CalibrationPW);
        HapticManager.Instance.lateralBounder.AddCalibrationData(Waveform.Sine.ToString(), calFrequency, stimpoints);
        
        stimpoints[2] = new StimPoint(PW_CalibrationPA, PW_Threshold);
        HapticManager.Instance.lateralBounder.AddCalibrationData(Waveform.Sine.ToString(), calFrequency, stimpoints);
        
        stimpoints[3] = new StimPoint(PW_CalibrationPA, PW_Maximum);
        HapticManager.Instance.lateralBounder.AddCalibrationData(Waveform.Sine.ToString(), calFrequency, stimpoints);
        
        // Apply to MEDIAL bounder
        stimpoints[0] = new StimPoint(PA_Threshold, PA_CalibrationPW);
        HapticManager.Instance.medialBounder.AddCalibrationData(Waveform.Sine.ToString(), calFrequency, stimpoints);
        
        stimpoints[1] = new StimPoint(PA_Maximum, PA_CalibrationPW);
        HapticManager.Instance.medialBounder.AddCalibrationData(Waveform.Sine.ToString(), calFrequency, stimpoints);
        
        stimpoints[2] = new StimPoint(PW_CalibrationPA, PW_Threshold);
        HapticManager.Instance.medialBounder.AddCalibrationData(Waveform.Sine.ToString(), calFrequency, stimpoints);
        
        stimpoints[3] = new StimPoint(PW_CalibrationPA, PW_Maximum);
        HapticManager.Instance.medialBounder.AddCalibrationData(Waveform.Sine.ToString(), calFrequency, stimpoints);
        
        Debug.Log("[AutoConnect] Fixed calibration values applied successfully");
    }

    // Public method to manually trigger connection
    public async void ManualConnect()
    {
        if (!isConnecting)
        {
            await AutoConnectSequenceAsync();
        }
    }

    // Public method to update calibration values at runtime
    public void UpdateCalibrationValues(double paThresh, double paMax, double pwThresh, double pwMax)
    {
        PA_Threshold = paThresh;
        PA_Maximum = paMax;
        PW_Threshold = pwThresh;
        PW_Maximum = pwMax;
        
        ApplyFixedCalibrationValues();
        Debug.Log("[AutoConnect] Calibration values updated");
    }

    private void OnGUI()
    {
        // Simple debug display in top-left corner
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = isConnecting ? Color.yellow : Color.green;
        GUI.Label(new Rect(10, 10, 500, 30), $"Status: {connectionStatus}", style);
    }

    private string GetFirstAvailableUser()
    {
        string usersDir = System.IO.Path.Combine(Application.persistentDataPath, "Users");
        
        if (!System.IO.Directory.Exists(usersDir))
        {
            Debug.LogWarning($"[AutoConnect] Users directory not found: {usersDir}");
            return null;
        }
        
        var userFiles = System.IO.Directory.GetFiles(usersDir, "*.json");
        
        if (userFiles.Length == 0)
        {
            Debug.LogWarning("[AutoConnect] No user files found in Users directory");
            return null;
        }
        
        // Get the first user file without the .json extension
        string firstUserFile = System.IO.Path.GetFileNameWithoutExtension(userFiles[0]);
        Debug.Log($"[AutoConnect] Found user: {firstUserFile}");
        return firstUserFile;
    }
    
    // Helper method to list all available users
    public string[] GetAllAvailableUsers()
    {
        string usersDir = System.IO.Path.Combine(Application.persistentDataPath, "Users");
        
        if (!System.IO.Directory.Exists(usersDir))
            return new string[0];
        
        var userFiles = System.IO.Directory.GetFiles(usersDir, "*.json");
        var userNames = new string[userFiles.Length];
        
        for (int i = 0; i < userFiles.Length; i++)
        {
            userNames[i] = System.IO.Path.GetFileNameWithoutExtension(userFiles[i]);
        }
        
        return userNames;
    }
}