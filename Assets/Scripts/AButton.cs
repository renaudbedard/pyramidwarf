using InControl;
using UnityEngine;
using System.Collections;

public class AButton : MonoBehaviour
{
    public AButton OtherButton;
    public GameObject FadeQuad;
    public GameObject InstructionsQuad;

    public int ControllerId;

    public Material XboxReleased;
    public Material XboxPressed;
    public Material KeyPressed;
    public Material KeyReleased;

    bool wasPressed;

    [HideInInspector]
    public float SinceBothPressed;
    [HideInInspector]
    public bool IsPressed;

    bool wasHeld;

	void Update()
	{
        if (!wasHeld && InputCoalescer.Players[ControllerId].AttachHeld)
            IsPressed = !IsPressed;

        wasHeld = InputCoalescer.Players[ControllerId].AttachHeld;

	    renderer.material = IsPressed
	                            ? (InputCoalescer.Players[ControllerId].IsGamepad ? XboxPressed : KeyPressed)
                                : (InputCoalescer.Players[ControllerId].IsGamepad ? XboxReleased : KeyReleased);

        if (!wasPressed && IsPressed)
            audio.Play();

	    if (IsPressed)
	    {
	        transform.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.realtimeSinceStartup * 10) * 20, Vector3.forward);
            float a = Mathf.Sin(Time.realtimeSinceStartup * 12) * 0.1f + 1.0f;
            transform.localScale = new Vector3(a, a, a);
	    }
	    else
	        transform.rotation = Quaternion.identity;

	    wasPressed = IsPressed;

        if (IsPressed && OtherButton.IsPressed)
        {
            SinceBothPressed += Time.deltaTime;

            if (SinceBothPressed > 0.5f)
            {
                enabled = false;
                StartCoroutine(FadeThenWait());
            }
        }
        else
            SinceBothPressed = 0;
	}

    IEnumerator FadeThenWait()
    {
        float opacity = 0.0f;
        for (int i = 0; i < 30; i++)
        {
            opacity += 1 / 30.0f;
            FadeQuad.renderer.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, opacity));
            yield return new WaitForSeconds(1 / 60.0f);
        }

        StartCoroutine(WaitThenLoad());
    }

    IEnumerator WaitThenLoad()
    {
        float opacity = 0.0f;
        for (int i = 0; i < 30; i++)
        {
            opacity += 1 / 30.0f;
            InstructionsQuad.renderer.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, opacity));
            yield return new WaitForSeconds(1 / 60.0f);
        }

        yield return new WaitForSeconds(3);
        Application.LoadLevel(1);
    }
}
