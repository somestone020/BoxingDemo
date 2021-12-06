using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MySceneManager : MonoBehaviour
{
    public static MySceneManager instance;

    void Start()
    {
        instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    void Update()
    {
        
    }
}
