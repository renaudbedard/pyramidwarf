using UnityEngine;

class WobblyWobble : MonoBehaviour
{
    const float WobbleFactor = 0.6f;
    const float WobbleSpeed = 5.0f;

    float step;
    float lastWobble;

    void Start()
    {
        step = Random.Range(0, 4) / 4.0f * (Mathf.PI * 2);
    }

    void Update()
    {
        step += WobbleSpeed * Time.deltaTime;

        var sine = Mathf.Sin(step);
        var easedSine = GainBias.Gain(sine * 0.5 + 0.5, 0.1f) * 2 - 1;

        var thisWobble = easedSine * WobbleFactor;
        transform.position += Vector3.up * (-lastWobble + thisWobble);
        lastWobble = thisWobble;

        //transform.rotation = Quaternion.AngleAxis(step, Vector3.forward);
    }
}
