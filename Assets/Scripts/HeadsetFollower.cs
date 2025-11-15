using UnityEngine;

public class HeadsetFollower : MonoBehaviour
{
    [Header("Headset / Camera")]
    [Tooltip("Transform of the VR headset camera (e.g., Main Camera under XR Origin/OVRCameraRig).")]
    public Transform headsetCamera;

    [Header("Position Settings")]
    [Tooltip("Distance in meters in front of the headset.")]
    public float distanceInFront = 1.5f;

    [Tooltip("Extra positional offset in the camera's local space (x = right, y = up, z = forward).")]
    public Vector3 localOffset = Vector3.zero;

    [Header("Rotation Settings")]
    [Tooltip("If true, this object will always face the headset (billboard).")]
    public bool faceCamera = true;

    [Tooltip("If true, match the camera's Y rotation so it feels anchored in front of view.")]
    public bool matchCameraYaw = false;

    private void Reset()
    {
        // Try auto-find a camera if not set
        if (headsetCamera == null && Camera.main != null)
        {
            headsetCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (headsetCamera == null)
        {
            // Try to grab main camera at runtime if missing
            if (Camera.main != null)
            {
                headsetCamera = Camera.main.transform;
            }
            else
            {
                return;
            }
        }

        // 1. Base position: distanceInFront along camera forward
        Vector3 targetPos = headsetCamera.position + headsetCamera.forward * distanceInFront;

        // 2. Apply local offset in camera space (e.g., slightly down or up)
        targetPos += headsetCamera.TransformVector(localOffset);

        transform.position = targetPos;

        // 3. Rotation options
        if (faceCamera)
        {
            // Billboard: always look at the camera
            Vector3 lookTarget = headsetCamera.position;
            lookTarget.y = transform.position.y; // keep upright if you want
            transform.LookAt(lookTarget);
            transform.Rotate(0f, 180f, 0f); // flip to face the user, if needed
        }
        else if (matchCameraYaw)
        {
            // Match only Yaw (Y rotation), keep object upright
            Vector3 euler = transform.eulerAngles;
            euler.y = headsetCamera.eulerAngles.y;
            transform.eulerAngles = euler;
        }
        // else: keep whatever rotation you set in the prefab
    }
}