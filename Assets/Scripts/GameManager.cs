using UnityEngine;
using System.Collections;
using System.Collections.Generic;       //Allows us to use Lists. 

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;      //Static instance of GameManager which allows it to be accessed by any other script.
    private Map mapScript;
    private Player playerScript;
    private Board boardScript;                      //Store a reference to our BoardManager which will set up the level.
    private MiniBoard miniBoardScript;
    private ItemList itemListScript;
    private GameObject canvas;

    //Start
    void Start()
    {
        canvas = GameObject.Find("Canvas");
        canvas.SetActive(false);
    }

    //Update is called every frame.
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape)) {
            if (canvas.activeSelf)
            {
                canvas.SetActive(false);
            }
            else
            {
                canvas.SetActive(true);
            }
        }
    }

    //Awake is always called before any Start functions
    void Awake()
    {
        //Check if instance already exists
        if (instance == null)

        //if not, set instance to this
        instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)

        //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
        Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);

        //Get a component reference to the attached Map script
        mapScript = GetComponent<Map>();
        playerScript = GetComponent<Player>();
        boardScript = GetComponent<Board>();
        miniBoardScript = GetComponent<MiniBoard>();
        itemListScript = GetComponent<ItemList>();

        //Call the InitGame function to initialize the first level 
        InitGame();
    }

    //Initializes the game for each level.
    public void InitGame()
    {
        //Call the SetupScene function of the BoardManager script
        mapScript.GetNewSeed();
        mapScript.InitMap();
        playerScript.InitPlayer();
        boardScript.SetupScene();
        miniBoardScript.SetupScene();

        //leaving this out for now -- WIP!
        //itemListScript.Init();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}