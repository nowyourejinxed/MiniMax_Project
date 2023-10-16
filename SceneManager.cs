using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSwitcher : MonoBehaviour
{
        public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

}