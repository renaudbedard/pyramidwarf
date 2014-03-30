using System.Linq;
using UnityEngine;
using System.Collections;
using System;

public class CameraMotion : MonoBehaviour
{
    public int ForPlayer;

    bool firstUpdate;
    Vector3 lastLookDir;
    Vector3 lastLookDirSmoothed;

    void Start()
    {
        firstUpdate = true;
    }

	void FixedUpdate()
	{
	    int gnomeCount = 0;
	    Vector3 center = Vector3.zero;

	    foreach (var g in Globals.Guys[ForPlayer])
	        if (g.IsAttached || Guy.MainGuys[ForPlayer] == g)
	        {
	            center += g.HeadRB.transform.position;
	            gnomeCount++;
	        }

	    float interpolationStep = firstUpdate ? 1.0f : 0.01f;

	    float nextSize = 7 + Mathf.Pow(gnomeCount + 0.5f, 1.3f);
        camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, nextSize, interpolationStep);

        interpolationStep = firstUpdate ? 1.0f : 0.05f;

        var destinationLook = center / gnomeCount - Vector3.forward * 50 + Vector3.up * 1.5f;

        // look in move direction
        lastLookDir = new Vector3(Guy.MainGuys[ForPlayer].LastWalkSign * 5, 0.0f, 0.0f);

        lastLookDirSmoothed = Vector3.Lerp(lastLookDirSmoothed, lastLookDir, interpolationStep / 2.0f);
        destinationLook += lastLookDirSmoothed;

        camera.transform.position = Vector3.Lerp(camera.transform.position, destinationLook, interpolationStep);

        //Debug.Log(string.Format("Found {0} gnomes for player {1}, position avg = {2}", gnomeCount, ForPlayer, center / gnomeCount));

	    firstUpdate = false;
	}
}
