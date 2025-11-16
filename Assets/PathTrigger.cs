using UnityEngine;
using Unity.XR.CoreUtils;

public class PathTrigger : MonoBehaviour
{
    [Header("XR Rig")]
    [Tooltip("Assign the XR Rig root here. Usually the GameObject containing XROrigin.")]
    public Transform xrRig;

    // Event to notify other scripts
    public System.Action OnPlayerEntered;

    private void Awake()
    {
        if (xrRig == null)
        {
            Debug.LogWarning("[PathTrigger] XR Rig not assigned. Drag your XROrigin object here.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (xrRig != null)
        {
            // If the XR Rig or its children entered this trigger
            if (other.transform == xrRig || other.transform.IsChildOf(xrRig))
            {
                Debug.Log("[PathTrigger] XR Rig entered the trigger!");
                OnPlayerEntered?.Invoke();
            }
        }
        else
        {
            // Fallback: look for Player-tagged collider
            if (other.CompareTag("Player"))
            {
                Debug.Log("[PathTrigger] Player entered the trigger!");
                OnPlayerEntered?.Invoke();
            }
        }
    }
}