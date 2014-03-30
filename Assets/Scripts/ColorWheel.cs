using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class ColorWheel : MonoBehaviour {

    List<Color32> colors = new List<Color32>
    {
        new Color32(0xff, 0xf6, 0x55, 0xff),
        new Color32(0xc2, 0xff, 0x57, 0xff),
        new Color32(0xea, 0x85, 0xff, 0xff),
        new Color32(0x52, 0xce, 0xff, 0xff),
        new Color32(0xff, 0x82, 0x82, 0xff),
        new Color32(0xff, 0xff, 0xff, 0xff),
    };

    float sinceStarted;

	void Update()
	{
	    sinceStarted += Time.deltaTime;

	    float step = sinceStarted / 1.0f;
	    int c = Mathf.RoundToInt(step * colors.Count);
	    c = c % colors.Count;

	    particleSystem.startColor = colors[c];
	}
}
