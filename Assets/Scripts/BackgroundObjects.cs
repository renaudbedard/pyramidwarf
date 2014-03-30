using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

class BackgroundObjects : MonoBehaviour
{
    public Material[] Textures;
    public Transform QuadPrefab;
    public float AmountToCreate;
    public float Spacing;
    public float RandomSpacing;
    public float ScaleFactor;

    void Start()
    {
        List<int> indexPool = Enumerable.Range(0, Textures.Length).ToList();

        Vector3 basePosition = Vector3.zero;

        for (int i = 0; i < AmountToCreate; i++)
        {
            var go = (Instantiate(QuadPrefab, Vector3.zero, Quaternion.identity) as Transform).gameObject;
            go.transform.parent = transform;

            var toGet = Random.Range(0, indexPool.Count);
            var textureId = indexPool[toGet];
            indexPool.RemoveAt(toGet);
            if (indexPool.Count == 0)
            {
                indexPool.AddRange(Enumerable.Range(0, Textures.Length));
                indexPool.Remove(textureId);
            }

            var mat = Textures[textureId];
            var tex = mat.GetTexture(0);

            go.GetComponentInChildren<Renderer>().material = mat;

            go.transform.localScale = new Vector3(tex.width / 64.0f * ScaleFactor, tex.height / 64.0f * ScaleFactor, 1.0f);

            var rndSpace = Random.Range(0.0f, RandomSpacing);
            //Debug.Log("rndspace " + rndSpace);

            basePosition += Vector3.right * (go.transform.localScale.x / 2.0f);

            go.transform.localPosition = basePosition;

            basePosition += Vector3.right * (go.transform.localScale.x / 2.0f + Spacing + rndSpace);
        }
    }
}
