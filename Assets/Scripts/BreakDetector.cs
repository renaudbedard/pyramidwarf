using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

class BreakDetector : MonoBehaviour
{
    public Action OnDetach = null;

    void OnJointBreak(float breakForce)
    {
        if (gameObject.name == "Tuque")
            return;

        Debug.Log("A joint has just been broken!, force: " + breakForce);
        transform.parent.GetComponent<Guy>().IsAttached = transform.parent.GetComponent<Guy>().IsMain;

        transform.parent.FindChild("Head").gameObject.audio.pitch = Random.Range(0.875f, 1.25f);

        foreach (var rb in transform.parent.GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject.name != "Tuque")
                rb.AddForce(rb.velocity * 4.0f + Vector3.up * 10.0f);
        }

        if (OnDetach != null)
        {
            OnDetach();
            OnDetach = null;
        }

        var guy = transform.parent.GetComponent<Guy>();
        if (!guy.IsMain && !guy.DontConnect)
        {
            transform.parent.FindChild("Head").gameObject.audio.PlayOneShot(Globals.Instance.BreakSound);
            guy.BodyRB.AddForce(Mathf.Sign(guy.BodyRB.velocity.x + 0.00001f) * 250.0f, 250.0f, 0, ForceMode.Force);
            guy.BodyRB.AddTorque((Random.value > 0.5 ? -1 : 1) * 25.0f, 0, 0, ForceMode.Force);
            guy.StartCoroutine(StopUpright(guy));
        }
    }

    IEnumerator StopUpright(Guy guy)
    {
        guy.DontConnect = true;
        guy.uprightStrength = 0.0f;
        yield return new WaitForSeconds(2.0f);
        guy.uprightStrength = 1.0f;
        guy.DontConnect = false;
    }
}
