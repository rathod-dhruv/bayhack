using System;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;   // TTSSpeaker

public class TTSAudioHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WebSocketClientManager wsClient;
    [SerializeField] private TTSSpeaker ttsSpeaker;
    [SerializeField] private AudioSource ttsAudioSource;

    [Serializable]
    private class SpeakCommand
    {
        public string intent;   // "high", "medium", "low"
        public string text;     // text to speak
        // optional: public string type; // e.g. "tts" if you want to route different message types
    }

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
        ttsSpeaker.Events.OnLoadSuccess.AddListener((speaker, s) => Debug.Log($"OnLoadSuccess: {s}"));
    }

    private void OnEnable()
    {
        if (wsClient != null)
            wsClient.OnMessageReceived += HandleWebSocketMessage;
    }

    private void OnDisable()
    {
        if (wsClient != null)
            wsClient.OnMessageReceived -= HandleWebSocketMessage;
    }

    private void HandleWebSocketMessage(string raw)
    {
        // Here you can optionally filter by message type if you add one.
        // For now we just assume anything coming in is a SpeakCommand.
        try
        {
            SpeakCommand cmd = JsonUtility.FromJson<SpeakCommand>(raw);
            if (cmd == null || string.IsNullOrWhiteSpace(cmd.text))
            {
                Debug.LogWarning("[TTS] Invalid SpeakCommand payload: " + raw);
                return;
            }

            HandleSpeakIntent(cmd.intent, cmd.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TTS] Failed to parse SpeakCommand: " + e + " | raw=" + raw);
        }
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
        // "medium" -> moderate calm
        // "low"    -> normal
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

        Debug.Log($"[TTS] intent-based speak | vol={volume}, pitch={pitch} | text={text}");

        // This triggers Wit TTS via the TTSSpeaker component
        ttsSpeaker.Speak(text);
    }
}