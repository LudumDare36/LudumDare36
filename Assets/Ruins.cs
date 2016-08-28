using UnityEngine;
using System.Collections;

public class Ruins : MonoBehaviour {
  public GameObject brick;

	void Start () {
    Bounds bb = brick.GetComponent<MeshFilter>().sharedMesh.bounds;
    float offset = (bb.center.y - bb.min.y) * brick.transform.localScale.y;
    float size = bb.size.y * brick.transform.localScale.y;
    Debug.Log(size + " : " + offset);

    int maxHeight = 6;
    int maxAltitude = maxHeight * 2;
    int numberOfPiles = Random.Range(4, 20);
    for (int i = 0; i < numberOfPiles; i++)
    {
      float x = Random.Range(-1.0f, 1.0f) * numberOfPiles;
      float z = Random.Range(-1.0f, 1.0f) * numberOfPiles;

      Vector3 pos = transform.position + new Vector3(x, maxAltitude, z);
      RaycastHit hit;
      if (Physics.Raycast(pos, -Vector3.up, out hit))
      {
        pos.y = hit.point.y + offset;

        int pileHeight = Random.Range(1, maxHeight / 2) + Random.Range(1, maxHeight / 2);
        for (int y = 0; y < pileHeight; y++)
        {
          Instantiate(brick, pos, Quaternion.identity, transform);
          pos.y += size * 1.01f;
        }
      }
    }
  }

  void Update () {
	
	}
}
