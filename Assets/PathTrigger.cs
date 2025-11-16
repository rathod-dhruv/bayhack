using UnityEngine;
using Unity.XR.CoreUtils;

public class PathTrigger : MonoBehaviour
{
    
    // Event to notify other scripts
    public System.Action OnPlayerEntered;

  

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Called OnTrigger");
        if (other.tag == "Player")
        {
            Debug.Log("Enter Trigger");
            SceneManagerCustom.triggeredPath = true;
            gameObject.SetActive(false);

        }
       
    }
}