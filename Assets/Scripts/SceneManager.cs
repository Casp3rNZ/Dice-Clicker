using UnityEngine;

public class SceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // additively load the FlatWorldTest1 scene, only if not in editor mode
#if !UNITY_EDITOR
        UnityEngine.SceneManagement.SceneManager.LoadScene("GardenWorld", UnityEngine.SceneManagement.LoadSceneMode.Additive);
#endif
    }
}
