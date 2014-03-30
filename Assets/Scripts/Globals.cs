using System.Collections.Generic;
using InControl;
using UnityEngine;

class Globals : MonoBehaviour
{
    public static Dictionary<int, List<Guy>> Guys = new Dictionary<int, List<Guy>> { { 0, new List<Guy>() }, { 1, new List<Guy>() } };

    public static Globals Instance { get; private set; }

    public GameObject TeleportParticle;
    public GameObject HeartParticle;
    public GameObject UnlinkParticle;
    public GameObject Winticle;

    public GameObject[] PlayerCameras = new GameObject[2];

    public AudioClip StepSound;
    public AudioClip ConnectSound;
    public AudioClip BreakSound;
    public AudioClip TeleportSound;

    void Start()
    {
        Guys[0].Clear();
        Guys[1].Clear();

        Instance = this;

        InputManager.Setup();
        InputCoalescer.Update(false);

        if (Winticle)
        {
            Winticle.renderer.enabled = false;
            Winticle.GetComponentInChildren<ParticleSystem>().enableEmission = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            GetComponentInChildren<AudioSource>().mute = !GetComponentInChildren<AudioSource>().mute;

        InputCoalescer.Update(Application.loadedLevelName == "Title");

        if (InputCoalescer.Players[0].Restart)
            Application.LoadLevel("Title");
    }
}
