using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InControl;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

internal enum ConnectionTypes
{
    LeftArm, RightArm, LeftFoot, RightFoot, HeadRight, HeadLeft
}

static class ConnectionTypeExtensions
{
    public static ConnectionTypes GetOpposite(this ConnectionTypes t)
    {
        switch (t)
        {
            case ConnectionTypes.RightArm: return ConnectionTypes.LeftArm;
            case ConnectionTypes.LeftArm: return ConnectionTypes.RightArm;
            case ConnectionTypes.LeftFoot: return ConnectionTypes.HeadRight;
            case ConnectionTypes.RightFoot: return ConnectionTypes.HeadLeft;
            case ConnectionTypes.HeadRight: return ConnectionTypes.LeftFoot;
            case ConnectionTypes.HeadLeft: return ConnectionTypes.RightFoot;
        }
        throw new InvalidOperationException();
    }

    public static ConnectionTypes GetSibling(this ConnectionTypes t)
    {
        switch (t)
        {
            case ConnectionTypes.LeftFoot: return ConnectionTypes.RightFoot;
            case ConnectionTypes.RightFoot: return ConnectionTypes.LeftFoot;
            case ConnectionTypes.HeadRight: return ConnectionTypes.HeadLeft;
            case ConnectionTypes.HeadLeft: return ConnectionTypes.HeadRight;
        }
        throw new InvalidOperationException();
    }
}

class Guy : MonoBehaviour
{
    public static Dictionary<int, Guy> MainGuys = new Dictionary<int, Guy> { {0, null}, {1, null} };

    public bool IsAttached;
    public bool ShouldWalk;
    public float LastWalkSign;

    public bool DoNotChangeColourDummy;

    public Dictionary<ConnectionTypes, Guy> Connections = new Dictionary<ConnectionTypes, Guy>();

    [HideInInspector]
    public Rigidbody BodyRB, HeadRB, TuqueRB;
    [HideInInspector]
    public Rigidbody RightLegRB, LeftLegRB;
    [HideInInspector]
    public Rigidbody RightArmRB, LeftArmRB;

    GameObject HeadGO;

    public HashSet<Guy> TotalAttachedGuys;

    public int PlayerId;
    public int GuyId;
    static List<int> Pool = Enumerable.Range(0, 12).ToList();

    public Material[] BodyTypes = new Material[6];
    public Material[] FaceTypes = new Material[6];
    public Material[] ChantingFaceTypes = new Material[6];
    public Material[] LegTypes = new Material[6];
    public Material[] ArmTypes = new Material[6];
    public Material[] TuqueTypes = new Material[6];

    float SinceFaceChanged;
    Material nextFace;

    object activeCo2;
    object activeCoroutine;
    float activeCoroutineDirection;
    float movementForce;
    public float uprightStrength;
    float baseHeight;

    public bool DontConnect;
    public bool AboveGround;

    public bool IsMain;

    bool stabilize;

    void Start()
    {
        if (!DoNotChangeColourDummy)
            Globals.Guys[PlayerId].Add(this);

        if (DoNotChangeColourDummy|| IsMain)
        {
            TotalAttachedGuys = new HashSet<Guy>();
            MainGuys[PlayerId] = this;
            IsAttached = true;
            ShouldWalk = true;
        }

        BodyRB = transform.FindChild("Body").rigidbody;
        RightLegRB = transform.FindChild("Right Leg").rigidbody;
        LeftLegRB = transform.FindChild("Left Leg").rigidbody;
        LeftArmRB = transform.FindChild("Left Arm").rigidbody;
        RightArmRB = transform.FindChild("Right Arm").rigidbody;
        TuqueRB = transform.FindChild("Tuque").rigidbody;

        HeadGO = transform.Find("Head").gameObject;
        HeadRB = HeadGO.rigidbody;

        stabilize = true;

        var toGet = Random.Range(0, Pool.Count);
        GuyId = Pool[toGet];
        Pool.RemoveAt(toGet);
        if (Pool.Count == 0)
        {
            Pool.AddRange(Enumerable.Range(0, 12).ToList());
            Pool.Remove(GuyId);
        }

        nextFace = ChantingFaceTypes[GuyId];

        // retexture
        if (!DoNotChangeColourDummy)
        {
            BodyRB.renderer.material = BodyTypes[GuyId];
            RightLegRB.renderer.material = LegTypes[GuyId];
            LeftLegRB.renderer.material = LegTypes[GuyId];
            HeadGO.renderer.material = FaceTypes[GuyId];
            RightArmRB.renderer.material = ArmTypes[GuyId];
            LeftArmRB.renderer.material = ArmTypes[GuyId];
            TuqueRB.renderer.material = TuqueTypes[GuyId];
        }

        uprightStrength = 1.0f;

        StartCoroutine(RecordBaseHeight());
    }

    IEnumerator RecordBaseHeight()
    {
        yield return new WaitForSeconds(0.5f);

        baseHeight = BodyRB.position.y;
        //Debug.Log("Base height is " + baseHeight);
    }

    float sincePushedUp;

    void Update()
    {
        var device = InputCoalescer.Players[PlayerId];

        if (!IsAttached && BodyRB.position.y > baseHeight + 2.5f && !DontConnect)
        {
            AboveGround = true;
            BodyRB.AddForce(Mathf.Sign(BodyRB.velocity.x + 0.00001f) * 4.0f, 1.5f, 0, ForceMode.Force);
            sincePushedUp += Time.deltaTime;
            if (sincePushedUp > 2)
            {
                BodyRB.AddForce(Mathf.Sign(BodyRB.velocity.x + 0.00001f) * 10.0f, 500.0f, 0, ForceMode.Force);
                BodyRB.velocity = new Vector3(-1 * BodyRB.velocity.x, BodyRB.velocity.y, BodyRB.velocity.z);
                sincePushedUp -= 2;
            }
        }
        else
            AboveGround = false;

        if (ShouldWalk && IsAttached)
        {
            if (device.MovingRight && activeCoroutineDirection != 1)
            {
                LastWalkSign = 1.0f;
                var token = new Object();
                activeCoroutineDirection = 1;
                activeCoroutine = token;
                StartCoroutine(LiftLeg(token, 1));
            }

            if (device.MovingLeft && activeCoroutineDirection != -1)
            {
                LastWalkSign = -1.0f;
                var token = new Object();
                activeCoroutineDirection = -1;
                activeCoroutine = token;
                StartCoroutine(LiftLeg(token, -1));
            }

            if (!device.MovingLeft && !device.MovingRight)
            {
                if (LastWalkSign != 0 && activeCo2 == null)
                {
                    var token = new Object();
                    StartCoroutine(ResetWalkSign(token));
                    activeCo2 = token;
                }
                activeCoroutineDirection = 0;
                activeCoroutine = null;
                stabilize = true;
            }

            movementForce = device.MovementSpeed;
//            if (movementForce != 0)
//                Debug.Log("Movement force = " + movementForce);
        }

        var target = BodyRB.rotation.z;
        BodyRB.AddTorque(0, 0, uprightStrength * -target * (IsAttached ? 500 : 100));

        if (stabilize && uprightStrength == 1)
        {
            target = RightLegRB.rotation.z;
            RightLegRB.AddTorque(0, 0, -target * 50);
            target = LeftLegRB.rotation.z;
            LeftLegRB.AddTorque(0, 0, -target * 50);
        }

        if (device.DetachPressed && Application.loadedLevelName != "Title")
        {
            foreach (var hinge in transform.parent.GetComponentsInChildren<HingeJoint>())
            {
                if (!hinge.useLimits || Input.GetKey(KeyCode.LeftShift))
                    hinge.breakForce = 0.00001f;
            }

            // safe guard : check for guys overlapping other guys and respawn them
            foreach (var g in Globals.Guys[PlayerId])
            {
                foreach (var gg in Globals.Guys[PlayerId])
                {
                    if (g != gg && g.BodyRB.collider.bounds.Intersects(gg.BodyRB.collider.bounds))
                    {
                        // reset both
                        var delta = (g.BodyRB.transform.position - gg.BodyRB.transform.position) * 10.0f;
                        foreach (var rb in g.transform.parent.GetComponentsInChildren<Rigidbody>())
                            rb.transform.position -= delta;
                    }
                }
            }

            MainGuys[PlayerId].ShouldWalk = true;
            MainGuys[PlayerId].IsAttached = true;

            if (TotalAttachedGuys != null)
            {
                TotalAttachedGuys.Clear();
                TotalAttachedGuys.Add(MainGuys[PlayerId]);
            }
        }
    }

    IEnumerator ResetWalkSign(object token)
    {
        yield return new WaitForSeconds(1.5f);
        if (activeCo2 == token)
        {
            LastWalkSign = 0.0f;
            activeCo2 = null;
        }
    }

    IEnumerator LiftLeg(object token, int direction)
    {
        var str = MainGuys[PlayerId].TotalAttachedGuys.Count / 10.0f + 1.0f;

        StartCoroutine(PlayStep());

        float t = 0;
        while (activeCoroutine == token && t < 0.125)
        {
            (direction == 1 ? RightLegRB : LeftLegRB).AddTorque(0, 0, 100 * direction * movementForce * str);
            (direction == 1 ? RightLegRB : LeftLegRB).AddForce(0, 15 * movementForce * str, 0);
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }

        if (activeCoroutine == token)
            yield return StartCoroutine(HoldUpAndMove(token, direction));
    }

    IEnumerator PlayStep()
    {
        yield return new WaitForSeconds(Random.Range(0.25f, 0.75f));
        HeadGO.audio.pitch = Random.Range(1.875f, 2.25f);
        HeadGO.audio.PlayOneShot(Globals.Instance.StepSound);
    }

    IEnumerator HoldUpAndMove(object token, int direction)
    {
        var str = MainGuys[PlayerId].TotalAttachedGuys.Count / 10.0f + 1.0f;

        float t = 0;
        while (activeCoroutine == token && t < 0.25)
        {
            (direction == 1 ? RightLegRB : LeftLegRB).AddTorque(0, 0, 50 * direction * movementForce * str);
            (direction == -1 ? RightLegRB : LeftLegRB).AddForce(0, 10 * movementForce * str, 0);
            BodyRB.AddForce(40 * direction * movementForce * str, 0, 0);
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }

        stabilize = true;

        t = 0;
        while (activeCoroutine == token && t < 0.4)
        {
            BodyRB.AddForce(-7.5f * -direction * movementForce * str, -10 * movementForce * str, 0);
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }

        activeCoroutine = null;
        activeCoroutineDirection = 0;
    }
}