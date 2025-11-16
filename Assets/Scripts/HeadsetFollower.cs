using UnityEngine;


public class HeadsetFollower : MonoBehaviour
{
  public Transform playerCamera;
  Vector3 lookAtPosition;
  
  void Update() {
    if (playerCamera != null)
    {
      lookAtPosition = new Vector3(playerCamera.position.x, transform.position.y, playerCamera.position.z);
      
      transform.LookAt(lookAtPosition);
      
      transform.forward = -transform.forward; // ensure that the front of the breath control is being shown to the user
    }
  }
  
}