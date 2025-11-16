using UnityEngine;
using Meta.WitAi.TTS.Utilities;
using System;
using MRUtilityKitSample.NavMesh;

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
    public Collider collider;
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
    
    
    

    private void Start()
    {
        Invoke("StartAudio", 1);
    }

    public void StartAudio()
    {
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
    }

    public void HandleCustomSpeak()
    {
        ttsSpeakerCustom.Speak(customMsges[msgIdx]);
    }


    public void HandleTTSDoneForCustom()
    {
        Debug.Log("Called Handle Custom TTS DONE");

        if (msgIdx == 0)
        {
            msgIdx++;
            Invoke("AudioTime", 1);
            Invoke("HelpCollider", 5);

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

    public void HelpCollider()
    {
        collider.gameObject.SetActive(true);
    }
    private void HandleTTSDone()
    {
        Debug.Log("Called Handle TTS DONE" +SceneManagerCustom.stressLevel);

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
        
        Invoke("HandleCustomSpeak", 3);
        msgIdx++;
        Debug.Log("Called Handle TTS DONE" +SceneManagerCustom.stressLevel);

        
    }

    public void AudioTime()
    {
        if (ttsAudioHandler.lastOllamaMessage == "")
        {
            SceneManagerCustom.stressLevel = 1;
            ttsAudioHandler.lastOllamaMessage = "Take deep breaths, short break recommended";
        }
        ttsAudioHandler.SpeakText();
    }
    private void HandlePathEntered()
    {
        Debug.Log("Called Handle Path");
      
        tileEncounterd++;

        if (tileEncounterd == idxCount - 1)
        {
            breathingAnimation.gameObject.SetActive(false);
            Invoke("AudioTime", 1);
            tileEncounterd = 0;
        }
           
        
        
    }

}
