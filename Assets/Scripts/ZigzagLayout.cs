using UnityEngine;

class ZigzagLayout : MonoBehaviour
{
    public float Sign;

    void Start()
    {
        
    }

    void Update()
    {
        var cam = transform.parent.camera;
        var size = cam.orthographicSize * cam.aspect;

        transform.localScale = new Vector3(1.0f, 14.725f, 1.0f) / 7.3625f * cam.orthographicSize;
        transform.localPosition = new Vector3(size * Sign, 0, 12.0f);
    }
}
