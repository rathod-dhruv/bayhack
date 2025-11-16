using UnityEngine;

/// <summary>
/// Follows camera position on X and Z axis, keeps Y at 0
/// Attach this to your OVRCameraRig
/// </summary>
public class FollowCameraXZ : MonoBehaviour
{
    public Transform camerat;
    void Update()
    {
        // Get camera position
        Vector3 cameraPos = camerat.position;
        
        // Set this object to camera's X and Z, but Y = 0
        transform.position = new Vector3(cameraPos.x, 0, cameraPos.z);
    }
}