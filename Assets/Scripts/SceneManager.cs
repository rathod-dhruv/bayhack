using UnityEngine;
using Meta.WitAi.TTS.Utilities;
using System;
using MRUtilityKitSample.NavMesh;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class SceneManagerCustom : MonoBehaviour
{
    public static int stressLevel;
    [SerializeField] private TTSSpeaker ttsSpeaker;
    [SerializeField] private TTSSpeaker ttsSpeakerCustom;
    [SerializeField] private TTSAudioHandler ttsAudioHandler;
    [SerializeField] private AudioSource ttsAudioSource;

    [SerializeField] private float spawnDistance = 1f;

    [SerializeField] private float spawnHeightOffset = 0.0f;

    [Header("XR Rig Root (Instead of camera forward)")]
    [Tooltip("Root transform of the XR Rig (e.g., XR Origin). Spawn direction uses this.")]
    [SerializeField] private Transform xrRigRoot;

    [Header("Path Marker")]
    
    public static bool triggeredPath = false;
    public BreathMovement breathingAnimation;
    public GameObject[] halo;
    public string[] customMsges;


    public int msgIdx = 0;

    public int idxTile = 0;
    public int idxCount = 1;
    public int tileEncounterd = 0;
    
    public NavMeshAgentController _AgentController;
    public HapticEventPulse _HapticEventPulse;
    public float lastAudioCalledTime = 0;
    public bool customCall = false;
    public bool audioCall = false;
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
        ttsSpeakerCustom.Events.OnPlaybackComplete.AddListener((speaker, s) =>
        {
            Debug.Log($"OnPlaybackComplete For Custom: {s}");
            HandleTTSDoneForCustom();
        });
        
        
    }


    public void Start()
    {
        
    }

    public void StartIntro()
    {
        lastAudioCalledTime = Time.time;
        audioCall = true;
        Invoke("StartAudio", 1);
    }

    public void StartAudio()
    {
        lastAudioCalledTime = Time.time;
        audioCall = true;
        
        HandleCustomSpeak();
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
        ttsSpeakerCustom.Speak(text);
    }
    

    private void Update()
    {
        if (SceneManagerCustom.triggeredPath)
        {
            SceneManagerCustom.triggeredPath = false;
            HandlePathEntered();
        }

        if (audioCall && Time.time - lastAudioCalledTime > 25)
        {
            audioCall = false;
            if(customCall)
                HandleTTSDoneForCustom();
            else
            {
                HandleTTSDone();
            }
        }
    }

    public void HandleCustomSpeak()
    {
        audioCall = true;
        customCall = true;
        lastAudioCalledTime = Time.time;
        ttsSpeakerCustom.Speak(customMsges[msgIdx]);
    }


    public void HandleTTSDoneForCustom()
    {
        audioCall = false;
        customCall = false;
        Debug.Log("Called Handle Custom TTS DONE");

        if (msgIdx == 0)
        {
            msgIdx++;
            Invoke("AudioTime", 4);

        }
        else
        {
            Debug.Log("CALLED going to Spawned Tile :: ");

            _AgentController.DisableAll();
            int i = idxTile;
            int cnt = 0;
            while (i+1 < _AgentController.currentPathMarkers.Count && cnt < idxCount)
            {
                Debug.Log(" CALLED Spawned Tile :: "+i);
                _AgentController.currentPathMarkers[i+1].SetActive(true);
                i++;
                cnt++;
            }

            idxTile += idxCount;
            idxCount++;
        }
        
       
    }

    
    private void HandleTTSDone()
    {
        audioCall = false;
        customCall = false;
        Debug.Log("Called Handle TTS DONE" +SceneManagerCustom.stressLevel);
        _HapticEventPulse.PlayHaptic();
        switch (SceneManagerCustom.stressLevel)
        {
            case 1:
                breathingAnimation.SetMovementSpeed(0);
                halo[2].SetActive(true);
                halo[0].SetActive(false);
                halo[1].SetActive(false);
                break;
            case 2:
                breathingAnimation.SetMovementSpeed(0.5f);
                halo[1].SetActive(true);
                halo[0].SetActive(false);
                halo[2].SetActive(false);
                break;
            default:
                breathingAnimation.SetMovementSpeed(1);
                halo[0].SetActive(true);
                halo[1].SetActive(false);
                halo[2].SetActive(false);
                break;
        }
        

        breathingAnimation.gameObject.SetActive(true);
        
        Invoke("HandleCustomSpeak", 5);
        msgIdx++;
        Debug.Log("Called Handle TTS DONE" +SceneManagerCustom.stressLevel);

        
    }

    public void AudioTime()
    {
        audioCall = true;
        customCall = false;
        lastAudioCalledTime = Time.time;
        if (ttsAudioHandler.lastOllamaMessage == "")
        {
            SceneManagerCustom.stressLevel = Random.Range(1, 3 + 1);
            if(SceneManagerCustom.stressLevel  == 1)
            ttsAudioHandler.lastOllamaMessage = "Take deep breaths, short break recommended";
            else if (SceneManagerCustom.stressLevel == 2)
            {
                ttsAudioHandler.lastOllamaMessage = "You’re doing really well.";
            }
            else
            {
                ttsAudioHandler.lastOllamaMessage = "Let’s pause together.You’re doing perfectly";
            }
        }
        ttsAudioHandler.SpeakText();
    }
    private void HandlePathEntered()
    {
        Debug.Log("Called Handle Path");
      
        tileEncounterd++;

        if (tileEncounterd == idxCount - 1)
        {
            _HapticEventPulse.StopPulse();
            breathingAnimation.gameObject.SetActive(false);
            Invoke("AudioTime", 4);
            tileEncounterd = 0;
        }
           
        
        
    }

}
