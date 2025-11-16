using System;
using UnityEngine;

public class PathCreatorExample : MonoBehaviour
{
    [SerializeField] private NavMeshPathVisualizer pathVisualizer;
    [SerializeField] private Transform targetDestination;

    public void OnEnable()
    {
        Invoke("SetDesti", 1);
       
    }

    public void SetDesti()
    {
        Transform tg = FindAnyObjectByType<FindSpawendTile>()?.transform;
        pathVisualizer.SetDestinationAndCreatePath(tg.position);
    }

   
}