using AfferenceEngine.src.Core.Interfaces;
using AfferenceEngine.src.Core.Other; // BurstModel
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HapticEventPulse : AfferenceHaptic
{
    [Header("Burst Settings")]
    public bool useCustomBurst = false;
    public Vector2[] customBurst = Array.Empty<Vector2>();
    public float initialDelayMs = 0f;
    public BurstModel burstModel = BurstModel.Tap;
    public string burstNodeHint = null; // optional preferred BurstTrain node name

    [Header("Pulse Cycle Settings")]
    [Tooltip("Duration in seconds for one complete max-to-min-to-max cycle")]
    public float pulseCycleDuration = 2f;
    
    [Tooltip("Number of times to repeat the pulse (0 = infinite loop)")]
    public int pulseRepeatCount = 0;
    
    [Tooltip("Minimum intensity (0-1)")]
    [Range(0f, 1f)]
    public float minIntensity = 0f;
    
    [Tooltip("Maximum intensity (0-1)")]
    [Range(0f, 1f)]
    public float maxIntensity = 1f;

    private Coroutine _pulseCoroutine;

    public new void PlayHaptic()   // shadow base to run burst instead of continuous loop
    {
        SetEncoders();

        if (!HapticManager.Instance.stimActive) { return; }

        // Stop any existing pulse
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
        }

        // Start pulsing
        _pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    public void StopPulse()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }
    }

    private IEnumerator PulseRoutine()
    {
        var encs = GetBurstEncoders();
        if (encs.Count == 0)
        {
            Debug.LogWarning("[HapticEventPulse] Active encoders are not burst-capable (no BurstTrain found).");
            yield break;
        }

        int cyclesExecuted = 0;
        bool infiniteLoop = pulseRepeatCount == 0;

        while (infiniteLoop || cyclesExecuted < pulseRepeatCount)
        {
            // Generate pulse pattern based on cycle duration
            var pulsePattern = GeneratePulsePattern(pulseCycleDuration, minIntensity, maxIntensity);
            
            var now = DateTimeOffset.Now.AddMilliseconds(1);
            string pattern = BuildBurstPatternString(pulsePattern, cyclesExecuted == 0 ? initialDelayMs : 0f);

            foreach (var enc in encs)
            {
                var nodeName = ResolveBurstTrainName(enc, burstNodeHint);
                if (string.IsNullOrEmpty(nodeName)) continue;

                if (cyclesExecuted == 0)
                {
                    enc.ClearHapticHistory();
                }
                enc.StartBurst(nodeName, now, pattern);
            }

            cyclesExecuted++;
            
            // Wait for the cycle to complete
            yield return new WaitForSeconds(pulseCycleDuration);
        }

        _pulseCoroutine = null;
    }

    private Vector2[] GeneratePulsePattern(float duration, float minAmp, float maxAmp)
    {
        // Create a smooth pulse using sine wave
        // Number of sample points for smooth transition
        int samplePoints = Mathf.Max(10, Mathf.CeilToInt(duration * 10)); // 10 points per second
        var pattern = new Vector2[samplePoints];
        
        float timeStep = (duration * 1000f) / samplePoints; // Convert to milliseconds

        for (int i = 0; i < samplePoints; i++)
        {
            float t = (float)i / (samplePoints - 1); // 0 to 1
            
            // Sine wave for smooth pulsing: starts at max, goes to min, returns to max
            float sineValue = Mathf.Sin(t * Mathf.PI * 2f - Mathf.PI / 2f); // -1 to 1
            float normalizedValue = (sineValue + 1f) / 2f; // 0 to 1
            
            // Map to intensity range
            float intensity = Mathf.Lerp(minAmp, maxAmp, normalizedValue);
            
            pattern[i] = new Vector2(timeStep, intensity);
        }

        return pattern;
    }

    // Original single-shot burst method (can still be used if needed)
    public void PlaySingleBurst()
    {
        SetEncoders();

        if (!HapticManager.Instance.stimActive) { return; }

        var encs = GetBurstEncoders();
        if (encs.Count == 0)
        {
            Debug.LogWarning("[HapticEventPulse] Active encoders are not burst-capable (no BurstTrain found).");
            return;
        }

        var now = DateTimeOffset.Now.AddMilliseconds(1);

        if (useCustomBurst && customBurst != null && customBurst.Length > 0)
        {
            string pattern = BuildBurstPatternString(customBurst, initialDelayMs);
            foreach (var enc in encs)
            {
                var nodeName = ResolveBurstTrainName(enc, burstNodeHint);
                if (string.IsNullOrEmpty(nodeName)) continue;

                enc.ClearHapticHistory();
                enc.StartBurst(nodeName, now, pattern);
            }
        }
        else
        {
            var model = burstModel;
            foreach (var enc in encs)
            {
                var nodeName = ResolveBurstTrainName(enc, burstNodeHint);
                if (string.IsNullOrEmpty(nodeName)) continue;

                enc.ClearHapticHistory();
                enc.StartBurst(nodeName, now, model);
            }
        }
    }

    private void OnDisable()
    {
        StopPulse();
    }

    // --- local helpers (no base dependency on bursts) ---

    private static bool IsBurstCapable(IQualityEncoder enc)
    {
        var cached = HapticManager.Instance?.GetBurstTrainNames(enc.ID);
        return cached != null && cached.Count > 0;
    }

    private static List<IQualityEncoder> GetBurstEncoders()
    {
        var encs = HapticManager.Instance?.activeEncoders ?? new List<IQualityEncoder>();
        return encs.Where(IsBurstCapable).ToList();
    }

    private static string ResolveBurstTrainName(IQualityEncoder enc, string hint)
    {
        var cached = HapticManager.Instance?.GetBurstTrainNames(enc.ID);
        if (cached != null && cached.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(hint))
            {
                var hit = cached.FirstOrDefault(n => string.Equals(n, hint, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(hit)) return hit;
            }
            return cached.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).First();
        }
        return string.IsNullOrWhiteSpace(hint) ? "Burst" : hint;
    }

    private static string BuildBurstPatternString(Vector2[] points, float initialDelayMs)
    {
        if (points == null || points.Length == 0) return "[]";
        var parts = new List<string>(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            float d = points[i].x + (i == 0 ? Mathf.Max(0f, initialDelayMs) : 0f);
            float a = points[i].y;
            parts.Add("(" + d.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                            a.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
        }
        return "[" + string.Join("", parts) + "]";
    }

    // not used for bursts; keep base abstract contract happy
    protected override float Evaluate(float timeSeconds) => 0f;
    protected override float GetEndTimeSeconds() => 0f;
}


/*
using AfferenceEngine.src.Core.Interfaces;
using AfferenceEngine.src.Core.Other; // BurstModel
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HapticEventPulse : AfferenceHaptic
{
    [Header("Burst Settings")]
    public bool useCustomBurst = false;
    public Vector2[] customBurst = Array.Empty<Vector2>();
    public float initialDelayMs = 0f;
    public BurstModel burstModel = BurstModel.Tap;
    public string burstNodeHint = null; // optional preferred BurstTrain node name

    public new void PlayHaptic()   // shadow base to run burst instead of continuous loop
    {
        SetEncoders();

        if (!HapticManager.Instance.stimActive) { return; }

        var encs = GetBurstEncoders();
        if (encs.Count == 0)
        {
            Debug.LogWarning("[HapticEventPulse] Active encoders are not burst-capable (no BurstTrain found).");
            return;
        }

        var now = DateTimeOffset.Now.AddMilliseconds(1); // ensure strictly increasing time

        if (useCustomBurst && customBurst != null && customBurst.Length > 0)
        {
            string pattern = BuildBurstPatternString(customBurst, initialDelayMs);
            foreach (var enc in encs)
            {
                var nodeName = ResolveBurstTrainName(enc, burstNodeHint);
                if (string.IsNullOrEmpty(nodeName)) continue;

                enc.ClearHapticHistory();
                enc.StartBurst(nodeName, now, pattern);
            }
        }
        else
        {
            var model = burstModel; // snapshot
            foreach (var enc in encs)
            {
                var nodeName = ResolveBurstTrainName(enc, burstNodeHint);
                if (string.IsNullOrEmpty(nodeName)) continue;

                enc.ClearHapticHistory();
                enc.StartBurst(nodeName, now, model);
            }
        }
    }

    // --- local helpers (no base dependency on bursts) ---

    private static bool IsBurstCapable(IQualityEncoder enc)
    {
        var cached = HapticManager.Instance?.GetBurstTrainNames(enc.ID);
        return cached != null && cached.Count > 0;
    }

    private static List<IQualityEncoder> GetBurstEncoders()
    {
        var encs = HapticManager.Instance?.activeEncoders ?? new List<IQualityEncoder>();
        return encs.Where(IsBurstCapable).ToList();
    }

    private static string ResolveBurstTrainName(IQualityEncoder enc, string hint)
    {
        var cached = HapticManager.Instance?.GetBurstTrainNames(enc.ID);
        if (cached != null && cached.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(hint))
            {
                var hit = cached.FirstOrDefault(n => string.Equals(n, hint, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(hit)) return hit;
            }
            return cached.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).First();
        }
        return string.IsNullOrWhiteSpace(hint) ? "Burst" : hint; // conventional fallback
    }

    private static string BuildBurstPatternString(Vector2[] points, float initialDelayMs)
    {
        if (points == null || points.Length == 0) return "[]";
        var parts = new List<string>(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            float d = points[i].x + (i == 0 ? Mathf.Max(0f, initialDelayMs) : 0f);
            float a = points[i].y;
            parts.Add("(" + d.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                            a.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
        }
        return "[" + string.Join("", parts) + "]";
    }

    // not used for bursts; keep base abstract contract happy
    protected override float Evaluate(float timeSeconds) => 0f;
    protected override float GetEndTimeSeconds() => 0f;
}
*/