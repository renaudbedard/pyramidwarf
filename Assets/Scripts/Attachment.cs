using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InControl;
using UnityEngine;
using Random = UnityEngine.Random;

public class Attachment : MonoBehaviour
{
    Rigidbody ArmRB;
    Quaternion target;
    Guy Guy;
    bool isLeft;

    List<Transform> AttachPossibilities = new List<Transform>();

    void Start()
    {
        ArmRB = transform.parent.rigidbody;
        isLeft = transform.parent.gameObject.name == "Left Arm";
        target = ArmRB.transform.rotation;
        Guy = transform.parent.parent.gameObject.GetComponent<Guy>();
    }

    void Update()
    {
        foreach (var target in AttachPossibilities)
        {
            var armRoot = transform.position;

            if (!(Guy.IsAttached || target.parent.gameObject.GetComponent<Guy>().IsAttached))
                continue;

            var guyToAttach = target.parent.gameObject.GetComponent<Guy>();
            if (guyToAttach.DontConnect || guyToAttach.AboveGround)
                continue;

            Vector3 targetDelta = target.parent.FindChild("Body").transform.position - armRoot;

            //get the angle between transform.forward and target delta
            float angleDiff = Vector3.Angle(transform.right, targetDelta);

            // get its cross product, which is the axis of rotation to
            // get from one vector to the other
            Vector3 cross = Vector3.Cross(transform.right, targetDelta);

            // apply torque along that axis according to the magnitude of the angle.
            ArmRB.AddTorque(cross * angleDiff * 0.5f * (isLeft ? -0.1f : 1));

            var controls = InputCoalescer.Players[Guy.PlayerId];

            if (Guy.IsAttached && !guyToAttach.IsAttached && controls.AttachPressed)
            {
                // reset button for this update
                controls.AttachPressed = false;

                const float GuyHeight = 4.0f;

                // result!
                ConnectionTypes attacheeConnectionA, attacheeConnectionB = default(ConnectionTypes), attacheeConnectionC = default(ConnectionTypes),
                                hostAConnection, hostBConnection = default(ConnectionTypes), hostCConnection = default(ConnectionTypes);
                Guy hostA = null, hostB = null, hostC = null;

                var closestAttachedGuy = Guy;

                ConnectionTypes toIterate;
                if (closestAttachedGuy.BodyRB.transform.position.x < guyToAttach.BodyRB.transform.position.x)
                    toIterate = ConnectionTypes.LeftArm;
                else
                    toIterate = ConnectionTypes.RightArm;

                Guy iterator = closestAttachedGuy;
                Guy lastLevelOrigin = closestAttachedGuy;

                int graphHeight = 0;
                int inARow = 1;
                while (true)
                {
                    // iterate
                    if (!iterator.Connections.TryGetValue(toIterate, out iterator))
                    {
                        // how many in a row have we found?
                        if (inARow == 1)
                        {
                            // we can't check for a higher connection because there's no space for one
                            // immediately attach laterally and break
                            hostA = closestAttachedGuy; // this assumes the base 
                            hostAConnection = toIterate.GetOpposite();
                            attacheeConnectionA = toIterate;
                            break;
                        }

                        // we had filled-up couples but we're done : go one level higher and test
                        var headDirection = toIterate == ConnectionTypes.RightArm ? ConnectionTypes.HeadRight : ConnectionTypes.HeadLeft;
                        if (!lastLevelOrigin.Connections.TryGetValue(headDirection, out iterator))
                        {
                            // HMMM?
                            throw new InvalidOperationException();
                        }

                        graphHeight++;
                        lastLevelOrigin = iterator;
                        inARow = 0;
                    }

                    // continue on
                    inARow++;

                    // do we have a couple?
                    if (inARow >= 2)
                    {
                        // we need to check up!
                        var headDirection = toIterate == ConnectionTypes.RightArm ? ConnectionTypes.HeadLeft : ConnectionTypes.HeadRight;
                        Guy upwardsConnectee;
                        if (iterator.Connections.TryGetValue(headDirection, out upwardsConnectee))
                        {
                            // that's fine, we'll check the next couple then
                        }
                        else
                        {
                            if (graphHeight == 2)
                            {
                                //var go = Instantiate(Globals.Instance.Winticle, Vector3.zero, Quaternion.identity) as GameObject;

                                Globals.Instance.Winticle.transform.parent = Globals.Instance.PlayerCameras[Guy.PlayerId].transform;
                                Globals.Instance.Winticle.transform.localPosition = Vector3.forward * 10;

                                Globals.Instance.Winticle.renderer.enabled = true;
                                Globals.Instance.Winticle.GetComponentInChildren<ParticleSystem>().enableEmission = true;

                                StartCoroutine(Restart());
                            }

                            Debug.Log("Graph Height = " + graphHeight);

                            // there's a spot! GIT IT
                            hostA = iterator;
                            hostB = iterator.Connections[toIterate.GetOpposite()];
                            hostAConnection = headDirection;
                            hostBConnection = headDirection.GetSibling();
                            attacheeConnectionA = hostAConnection.GetOpposite();
                            attacheeConnectionB = hostBConnection.GetOpposite();

                            // wait a minute... is there a lateral connection as well?
                            if (hostA.Connections.ContainsKey(hostBConnection))
                            {
                                hostC = hostA.Connections[hostBConnection];
                                hostCConnection = hostBConnection == ConnectionTypes.HeadRight
                                                      ? ConnectionTypes.LeftArm
                                                      : ConnectionTypes.RightArm;
                                attacheeConnectionC = hostCConnection.GetOpposite();
                            }
                            if (hostB.Connections.ContainsKey(hostAConnection))
                            {
                                hostC = hostB.Connections[hostAConnection];
                                hostCConnection = hostAConnection == ConnectionTypes.HeadRight
                                                      ? ConnectionTypes.LeftArm
                                                      : ConnectionTypes.RightArm;
                                attacheeConnectionC = hostCConnection.GetOpposite();
                            }
                            break;
                        }
                    }
                }

                // attach!
                if (hostA == closestAttachedGuy)
                {
                    // simple lateral connection (in-place, no warping)
                    var hj = transform.parent.gameObject.AddComponent<HingeJoint>();
                    hj.connectedBody = target.rigidbody;
                    hj.anchor = new Vector3(0.35f, 0, 0);
                    hj.axis = new Vector3(0, 0, -1);
                    StartCoroutine(DelayBrekability(hj, 5.0f));

                    guyToAttach.ShouldWalk = true;
                    guyToAttach.IsAttached = true;

                    hostA.Connections.Add(hostAConnection, guyToAttach);
                    guyToAttach.Connections.Add(attacheeConnectionA, hostA);

                    transform.parent.gameObject.GetComponent<BreakDetector>().OnDetach = () =>
                    {
                        StartCoroutine(FireParticle(Globals.Instance.UnlinkParticle, (hostA.BodyRB.transform.position + guyToAttach.BodyRB.transform.position) / 2));

                        hostA.Connections.Remove(hostAConnection);
                        guyToAttach.Connections.Remove(attacheeConnectionA);
                        guyToAttach.IsAttached = guyToAttach.ShouldWalk = guyToAttach.IsMain;
                    };

                    StartCoroutine(FireParticle(Globals.Instance.HeartParticle, (hostA.BodyRB.transform.position + guyToAttach.BodyRB.transform.position) / 2));

                    guyToAttach.HeadRB.gameObject.audio.pitch = Random.Range(0.875f, 1.25f);
                    guyToAttach.HeadRB.gameObject.audio.PlayOneShot(Globals.Instance.ConnectSound);
                }
                else
                {
                    // DO TELEPORT

                    // get the midpoint
                    var midpoint = (hostA.BodyRB.transform.position.x + hostB.BodyRB.transform.position.x) / 2.0f;
                    var targetPos = new Vector3(midpoint, hostA.BodyRB.transform.position.y + GuyHeight, hostA.BodyRB.transform.position.z);
                    var delta = targetPos - guyToAttach.BodyRB.transform.position;

                    // test if there's already a guy there!
                    Bounds b = guyToAttach.BodyRB.renderer.bounds;
                    b.center += delta;
                    bool intersects = false;
                    foreach (var g in guyToAttach.transform.parent.GetComponentsInChildren<Guy>())
                    {
                        if (g != guyToAttach)
                        {
                            foreach (var r in g.GetComponentsInChildren<Renderer>())
                                if (r.gameObject.name != "Tuque" && r.bounds.Intersects(b))
                                {
                                    intersects = true;
                                    break;
                                }
                        }
                        if (intersects)
                            break;
                    }
                    if (intersects)
                    {
                        Debug.Log("Intersected!");
                        return;
                    }

                    // reset velocity on other guy
                    foreach (var rb in GetComponentsInChildren<Rigidbody>(target.parent.gameObject))
                        rb.velocity = Vector3.zero;

                    // pyramid-style
                    guyToAttach.HeadRB.gameObject.audio.pitch = Random.Range(0.875f, 1.25f);
                    guyToAttach.HeadRB.gameObject.audio.PlayOneShot(Globals.Instance.TeleportSound);
                    StartCoroutine(FireParticle(Globals.Instance.TeleportParticle, guyToAttach.BodyRB.transform.position));

                    foreach (var rb in guyToAttach.GetComponentsInChildren<Rigidbody>())
                        rb.transform.position += delta;

                    // who's the right/left host?
                    var rightGuy = attacheeConnectionA == ConnectionTypes.RightFoot ? hostA : hostB;
                    var leftGuy = attacheeConnectionA == ConnectionTypes.LeftFoot ? hostA : hostB;
                    
                    // create two (fragile) hinges at the feet
                    var hj = guyToAttach.RightLegRB.gameObject.AddComponent<HingeJoint>();
                    hj.connectedBody = rightGuy.HeadRB;
                    hj.axis = new Vector3(0, 0, -1);
                    hj.anchor = new Vector3(0, -0.5f, 0);
                    StartCoroutine(DelayBrekability(hj, 1.6f));

                    var hj2 = guyToAttach.LeftLegRB.gameObject.AddComponent<HingeJoint>();
                    hj2.connectedBody = leftGuy.HeadRB;
                    hj2.axis = new Vector3(0, 0, -1);
                    hj2.anchor = new Vector3(0, -0.5f, 0);
                    StartCoroutine(DelayBrekability(hj2, 1.6f));

                    HingeJoint hj3 = null;

                    // and one more lateral if it applies
                    if (hostC)
                    {
                        var attachingArm = attacheeConnectionC == ConnectionTypes.LeftArm
                                               ? guyToAttach.LeftArmRB
                                               : guyToAttach.RightArmRB;

                        var attachToArm = hostCConnection == ConnectionTypes.LeftArm
                                               ? hostC.LeftArmRB
                                               : hostC.RightArmRB;

                        // rotate the arms to make sure they'll align
                        attachingArm.rigidbody.rotation = attacheeConnectionC == ConnectionTypes.LeftArm
                                                              ? Quaternion.Euler(0, 0, Mathf.PI)
                                                              : Quaternion.Euler(0, 0, 0);

                        attachToArm.rigidbody.rotation = hostCConnection == ConnectionTypes.LeftArm
                                      ? Quaternion.Euler(0, 0, Mathf.PI)
                                      : Quaternion.Euler(0, 0, 0);

                        hj3 = attachingArm.gameObject.AddComponent<HingeJoint>();
                        hj3.connectedBody = attachToArm;
                        hj3.anchor = new Vector3(0.35f, 0, 0);
                        hj3.axis = new Vector3(0, 0, -1);
                        StartCoroutine(DelayBrekability(hj3, 1.5f));

                        attachingArm.GetComponent<BreakDetector>().OnDetach = () =>
                        {
                            StartCoroutine(FireParticle(Globals.Instance.UnlinkParticle, (attachingArm.transform.position + attachToArm.transform.position) / 2));

                            foreach (var hinge in guyToAttach.Connections.Values.SelectMany(x => x.GetComponentsInChildren<HingeJoint>().Where(y => y.connectedBody.transform.parent == guyToAttach.transform)))
                                hinge.breakForce = 0.1f;
                            foreach (var hinge in guyToAttach.GetComponentsInChildren<HingeJoint>().Where(y => y.connectedBody.transform.parent != guyToAttach.transform))
                                hinge.breakForce = 0.1f;

                            hostC.Connections.Remove(hostCConnection);
                            guyToAttach.Connections.Remove(attacheeConnectionC);

                            guyToAttach.IsAttached = guyToAttach.IsMain;
                        };
                    }

                    guyToAttach.RightLegRB.GetComponent<BreakDetector>().OnDetach = () =>
                    {
                        StartCoroutine(FireParticle(Globals.Instance.UnlinkParticle, (guyToAttach.RightLegRB.transform.position + rightGuy.HeadRB.transform.position) / 2));

                        foreach (var hinge in guyToAttach.Connections.Values.SelectMany(x => x.GetComponentsInChildren<HingeJoint>().Where(y => y.connectedBody.transform.parent == guyToAttach.transform)))
                            hinge.breakForce = 0.1f;
                        foreach (var hinge in guyToAttach.GetComponentsInChildren<HingeJoint>().Where(y => y.connectedBody.transform.parent != guyToAttach.transform))
                            hinge.breakForce = 0.1f;

                        hostA.Connections.Remove(hostAConnection);
                        hostB.Connections.Remove(hostBConnection);
                        guyToAttach.Connections.Remove(attacheeConnectionA);
                        guyToAttach.Connections.Remove(attacheeConnectionB);

                        guyToAttach.IsAttached = guyToAttach.IsMain;
                    };

                    guyToAttach.LeftLegRB.GetComponent<BreakDetector>().OnDetach = () =>
                    {
                        StartCoroutine(FireParticle(Globals.Instance.UnlinkParticle, (guyToAttach.LeftLegRB.transform.position + leftGuy.HeadRB.transform.position) / 2));

                        foreach (var hinge in guyToAttach.Connections.Values.SelectMany(x => x.GetComponentsInChildren<HingeJoint>().Where(y => y.connectedBody.transform.parent == guyToAttach.transform)))
                            hinge.breakForce = 0.1f;
                        foreach (var hinge in guyToAttach.GetComponentsInChildren<HingeJoint>().Where(y => y.connectedBody.transform.parent != guyToAttach.transform))
                            hinge.breakForce = 0.1f;

                        hostA.Connections.Remove(hostAConnection);
                        hostB.Connections.Remove(hostBConnection);
                        guyToAttach.Connections.Remove(attacheeConnectionA);
                        guyToAttach.Connections.Remove(attacheeConnectionB);

                        guyToAttach.IsAttached = guyToAttach.IsMain;
                    };

                    //guyToAttach.ShouldWalk = true;
                    guyToAttach.IsAttached = true;

                    hostA.Connections.Add(hostAConnection, guyToAttach);
                    hostB.Connections.Add(hostBConnection, guyToAttach);
                    guyToAttach.Connections.Add(attacheeConnectionA, hostA);
                    guyToAttach.Connections.Add(attacheeConnectionB, hostB);

                    if (hostC != null)
                    {
                        hostC.Connections.Add(hostCConnection, guyToAttach);
                        guyToAttach.Connections.Add(attacheeConnectionC, hostC);
                    }
                }

                Guy.MainGuys[Guy.PlayerId].TotalAttachedGuys.Add(guyToAttach);

                Debug.Log("Connected by " + attacheeConnectionA + " to guy with id = " + hostA.GuyId);
                if (hostB)
                    Debug.Log("Also by " + attacheeConnectionB + " to guy with id = " + hostB.GuyId);
                if (hostC)
                    Debug.Log("Also by " + attacheeConnectionC + " to guy with id = " + hostC.GuyId);
            }

            break;
        }
    }

    IEnumerator DelayBrekability(HingeJoint hj, float to)
    {
        hj.breakForce = float.PositiveInfinity;
        yield return new WaitForSeconds(1);
        if (hj)
            hj.breakForce = to;
    }

    IEnumerator Restart()
    {
        yield return new WaitForSeconds(5);
        Application.LoadLevel(0);
    }

    IEnumerator FireParticle(GameObject particle, Vector3 at)
    {
        var t = Instantiate(particle, at, Quaternion.identity);
        yield return new WaitForSeconds(3);
        Destroy(t);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "AttachDetector")
            AttachPossibilities.Add(other.transform.parent);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "AttachDetector")
            AttachPossibilities.Remove(other.transform.parent);
    }
}