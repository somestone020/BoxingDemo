using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InitSceen : MonoBehaviour
{
    private float speed = 0.01f;
    private float currentValue = 0;
    private Slider slider;
    private string loadSceneName = "00_MainMenu";

    void Start()
    {

        Init();
    }


    private void Init()
    {
        slider = GameObject.Find("Canvas/Slider").GetComponent<Slider>();
        //create UI
        //if (!GameObject.FindObjectOfType<UIManager>()) GameObject.Instantiate(Resources.Load("UI"), Vector3.zero, Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        currentValue += speed * Time.fixedTime;
        slider.value = Mathf.Clamp(currentValue, 0, 1);
        if (currentValue >= 0.9)
        {
            LoadScene(loadSceneName);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return new WaitForSeconds(0.1f);
        //Load new scene
        SceneManager.LoadScene(sceneName);
        
    }
}
