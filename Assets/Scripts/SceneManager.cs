using UnityEngine;
using Meta.WitAi.TTS.Utilities;
using System;

public class SceneManager : MonoBehaviour
{
    public String stressLevel;
    [SerializeField] private TTSSpeaker ttsSpeaker;

    [SerializeField] private AudioSource ttsAudioSource;

    [SerializeField] private float spawnDistance = 1f;

    [SerializeField] private float spawnHeightOffset = 0.0f;

    [Header("XR Rig Root (Instead of camera forward)")]
    [Tooltip("Root transform of the XR Rig (e.g., XR Origin). Spawn direction uses this.")]
    [SerializeField] private Transform xrRigRoot;

    [Header("Path Marker")]
    [Tooltip("Path object already placed in the scene (initially disabled). Must have a PathTrigger + Collider (Is Trigger).")]
    [SerializeField] private GameObject pathMarker;


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
        ttsSpeaker.Events.OnPlaybackComplete.AddListener((speaker, s) =>
        {
            Debug.Log($"OnPlaybackComplete: {s}");
            HandleTTSDone();
        });

        var trigger = pathMarker.GetComponent<PathTrigger>();
        trigger.xrRig = xrRigRoot;
        trigger.OnPlayerEntered += HandlePathEntered;
    }

    private void Start()
    {
        var voiceInput = "Hi, I'm here to help you move more safely around your home. We're just going to stand together for a moment. No need to do anything yet.";
        SpeakWithTTS(voiceInput, 1.0f, 1.0f);
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

    private void HandleTTSDone()
    {
        if (!pathMarker)
        {
            Debug.LogWarning("[SceneManager] pathMarker is not assigned, cannot place marker.");
            return;
        }

        if (!xrRigRoot)
        {
            Debug.LogWarning("[SceneManager] xrRigRoot not assigned — cannot spawn ahead of XR rig.");
            return;
        }

        // Use XR rig forward on the XZ plane
        Vector3 forward = xrRigRoot.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        // Position 1 unit (or spawnDistance) in front of the rig
        Vector3 spawnPos = xrRigRoot.position + forward * spawnDistance;
        spawnPos.y += spawnHeightOffset; // optional height tweak

        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

        // Move and enable existing marker instead of instantiating
        pathMarker.transform.SetPositionAndRotation(spawnPos, rot);
        pathMarker.SetActive(true);

        Debug.Log($"[SceneManager] Moved & enabled path marker at {spawnPos}");
    }

    private void HandlePathEntered()
    {
        Debug.Log("[SceneManager] Player entered path marker. Destroying it.");

        if (pathMarker != null)
        {
            Destroy(pathMarker);
            // If you want to reuse it later instead of destroying:
            // pathMarker.SetActive(false);
        }
    }

}
