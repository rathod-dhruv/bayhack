using System;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;   // TTSSpeaker

public class TTSAudioHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WebSocketClientManager wsClient;
    [SerializeField] private TTSSpeaker ttsSpeaker;
    [SerializeField] private TTSSpeaker ttsSpeaker2;
    [SerializeField] private AudioSource ttsAudioSource;

    // Track last message to avoid repeats
    public string lastOllamaMessage = "";

    private void Awake()
    {
        // Fallback auto-wiring
        if (!ttsSpeaker)
            ttsSpeaker = GetComponent<TTSSpeaker>();

        if (!ttsAudioSource && ttsSpeaker)
            ttsAudioSource = ttsSpeaker.GetComponentInChildren<AudioSource>();

        ttsSpeaker.Events.OnLoadBegin.AddListener((speaker, s) => Debug.Log($"OnLoadBegin: {s}"));
        ttsSpeaker.Events.OnLoadSuccess.AddListener((speaker, s) => Debug.Log($"OnLoadSuccess: {s}"));
        ttsSpeaker.Events.OnPlaybackStart.AddListener((speaker, s) => Debug.Log($"OnPlaybackStart: {s}"));
        ttsSpeaker.Events.OnPlaybackComplete.AddListener((speaker, s) => Debug.Log($"OnPlaybackComplete: {s}"));
    }

    private void OnEnable()
    {
        if (wsClient != null)
            wsClient.OnOllamaResponseReceived += HandleOllamaResponse;
    }

    private void OnDisable()
    {
        if (wsClient != null)
            wsClient.OnOllamaResponseReceived -= HandleOllamaResponse;
    }

    private void HandleOllamaResponse(string ollamaText)
    {
        // ollamaText is plain text like: "State: stressed. Take deep breaths, short break recommended"
        if (string.IsNullOrWhiteSpace(ollamaText))
        {
            Debug.LogWarning("[TTS] Empty ollama response received");
            return;
        }
        lastOllamaMessage = ollamaText;
        
        Debug.Log($"[TTS] New message received: {ollamaText}");

       
    }

    public void SpeakText()
    {
        
        // Parse the state from the text to determine intent
        string intent = ParseStateIntent(lastOllamaMessage);
        
        // Speak the full ollama response text
        HandleSpeakIntent(intent, lastOllamaMessage);
    }
    private string ParseStateIntent(string ollamaText)
    {
        // Parse state from text like "State: relaxed. ..." or "State: stressed. ..."
        string textLower = ollamaText.ToLower();

        if (textLower.Contains("state: relaxed") || textLower.Contains("state: calm"))
        {
            return "high";  // very calm voice
        }
        else if (textLower.Contains("state: focused") || textLower.Contains("state: moderate"))
        {
            return "mild";  // moderate voice
        }
        else if (textLower.Contains("state: stressed") || textLower.Contains("state: alert") || textLower.Contains("state: active"))
        {
            return "calm";  // normal voice (ironically named, but keeps your existing logic)
        }
        
        // Default to calm/normal
        return "calm";
    }

    private void HandleSpeakIntent(string intent, string text)
    {
        if (ttsSpeaker == null || ttsAudioSource == null)
        {
            Debug.LogWarning("[TTS] Missing TTSSpeaker or AudioSource reference.");
            return;
        }

        // defaults = normal
        float volume = 1.0f;
        float pitch = 1.0f;

        // Your mapping:
        // "high"   -> very calm (slow, gentle but not deep)
        // "mild"   -> moderate calm
        // "calm"   -> normal
        switch ((intent ?? "").ToLowerInvariant())
        {
            case "high":
                // Very calm: softer, slightly deeper and slower-feeling
                volume = 0.55f;
                pitch = 0.90f;   // slow & deep
                break;

            case "mild":
                // Moderate: between calm & normal
                volume = 0.75f;
                pitch = 0.93f;   // slightly slower
                break;

            case "calm":
                // Normal tone
                volume = 1.0f;
                pitch = 1.0f;    // normal speed
                break;

            default:
                // Unknown intent -> treat as normal
                volume = 1.0f;
                pitch = 1.0f;
                break;
        }

        SpeakWithTTS(text, volume, pitch);
    }

    private void SpeakWithTTS(string text, float volume, float pitch)
    {
        if (ttsAudioSource != null)
        {
            ttsAudioSource.volume = volume;
            ttsAudioSource.pitch = pitch;
        }

        Debug.Log($"[TTS] Playing new message | vol={volume}, pitch={pitch} | text={text}");

        // This triggers Wit TTS via the TTSSpeaker component
        ttsSpeaker.Speak(text);
    }
    
    
    private void SpeakWithTTS_Custom(string text, float volume, float pitch)
    {
        if (ttsAudioSource != null)
        {
            ttsAudioSource.volume = volume;
            ttsAudioSource.pitch = pitch;
        }

        Debug.Log($"[TTS] Playing new message | vol={volume}, pitch={pitch} | text={text}");

        // This triggers Wit TTS via the TTSSpeaker component
        ttsSpeaker2.Speak(text);
    }


    // Optional: Public method to reset the last message (useful for testing or manual resets)
    public void ResetLastMessage()
    {
        lastOllamaMessage = "";
        Debug.Log("[TTS] Last message cache cleared");
    }
}