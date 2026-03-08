using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class WaypointManager : MonoBehaviour
{
    private List<GameObject> waypoints;
    private int currentWaypointIndex = 0;
    public static WaypointManager Instance { get; private set; }

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Start coroutine to wait for additive scene to load
        StartCoroutine(InitializeWaypointsAfterSceneLoad());
    }

    private System.Collections.IEnumerator InitializeWaypointsAfterSceneLoad()
    {
        // Wait until the "GardenWorld" scene is loaded
        while (!UnityEngine.SceneManagement.SceneManager.GetSceneByName("GardenWorld").isLoaded)
        {
            yield return null;
        }

        waypoints = FindWaypoints();
        // Sort by number in GameObject name (e.g., "WP_1", "WP_2", ...)
        waypoints = waypoints.OrderBy(wp =>
        {
            string name = wp.name;
            int idx = name.LastIndexOf("_", System.StringComparison.Ordinal);
            if (idx >= 0 && int.TryParse(name.Substring(idx + 1), out int num))
                return num;
            return int.MaxValue;
        }).ToList();
        Debug.Log("Waypoints found: " + string.Join(", ", waypoints.Select(wp => wp.name)));
    }

    private List<GameObject> FindWaypoints()
    {
        List<GameObject> foundWPs = new List<GameObject>();
        GameObject[] rootObjs = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            // Iterate through every scene to get waypoints.
            // (normal search only finds active scene objects, and waypoints are in the ground/map additive scenes)
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            GameObject[] sceneRootObjs = scene.GetRootGameObjects();
            foreach (GameObject obj in sceneRootObjs)
            {
                // get all children of this root object,
                GameObject[] children = obj.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject)
                    .Where(g => g.tag == "waypoint")
                    .ToArray();
                foundWPs.AddRange(children);
            }
        }
        return foundWPs;
    }

    public Vector3 GetCurrentWaypointPosition()
    {
        if (waypoints.Count == 0)
        {
            Debug.LogError("No waypoints found in the scene.");
            return Vector3.zero;
        }
        return waypoints[currentWaypointIndex].transform.position;
    }

    public Vector3 GetDiceWishDirection(Vector3 dicePosition)
    {
        Vector3 targetPosition = GetCurrentWaypointPosition();
        Vector3 direction = targetPosition - dicePosition;
        return direction.normalized;
    }

    public void MoveToNextWaypoint()
    {
        if (waypoints.Count == 0)
        {
            Debug.LogError("No waypoints found in the scene.");
            return;
        }
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
    }
}
