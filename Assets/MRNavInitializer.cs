using Meta.XR.MRUtilityKit;
using UnityEngine;

public class MRNavInitializer : MonoBehaviour
{
    public SceneNavigation sceneNavigation;

    private void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogError("[MRNavInitializer] MRUK.Instance is null.");
            return;
        }

        if (!sceneNavigation)
            sceneNavigation = FindObjectOfType<SceneNavigation>();

        if (!sceneNavigation)
        {
            Debug.LogError("[MRNavInitializer] SceneNavigation not found in scene.");
            return;
        }

        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
    }

    private void OnSceneLoaded()
    {
        Debug.Log("[MRNavInitializer] MR scene loaded, building Scene NavMesh…");

        // Simplest: use SceneNavigation to build a Unity NavMesh
        sceneNavigation.BuildSceneNavMesh();
        // Or scoped: sceneNavigation.BuildSceneNavMeshForRoom(MRUK.Instance.GetCurrentRoom());
    }
}