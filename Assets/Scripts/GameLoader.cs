using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLoader : MonoBehaviour
{
    public GameObject gameManager;              //GameManager prefab to instantiate.
    //public GameObject soundManager;           //SoundManager prefab to instantiate.

    //Start
    void Start()
    {

    }

    //Update is called every frame.
    void Update()
    {

    }

    //Awake is always called before any Start functions
    void Awake()
    {
        //Check if a GameManager has already been assigned to static variable GameManager.instance or if it's still null
        if (GameManager.instance == null)

            //Instantiate gameManager prefab
            Instantiate(gameManager);

        /*
        //Check if a SoundManager has already been assigned to static variable GameManager.instance or if it's still null
        if (SoundManager.instance == null)

            //Instantiate SoundManager prefab
            Instantiate(soundManager);
        */
    }
}
