using UnityEngine;
using System.Collections;

public class Ruins : MonoBehaviour {
  public GameObject brick;
  public GameObject archBrick;

  const int maxHeight = 8;
  const int maxAltitude = maxHeight * 2;

  void Start()
  {
    // columns
    Bounds bb = brick.GetComponent<MeshFilter>().sharedMesh.bounds;
    float offset = (bb.center.y - bb.min.y) * brick.transform.localScale.y;
    float size = bb.size.y * brick.transform.localScale.y;

    int numberOfPiles = Random.Range(4, 50);
    for (int i = 0; i < numberOfPiles; i++)
    {
      float x = Random.Range(-numberOfPiles / 3, numberOfPiles / 3) * bb.size.x * brick.transform.localScale.x;
      float z = Random.Range(-numberOfPiles / 3, numberOfPiles / 3) * bb.size.x * brick.transform.localScale.x;

      Vector3 pos = transform.position + new Vector3(x, maxAltitude, z);
      RaycastHit hit;
      if (Physics.Raycast(pos, -Vector3.up, out hit))
      {
        pos.y = hit.point.y - size * 0.25f;

        int pileHeight = Random.Range(maxHeight*3/4, maxHeight);
        for (int y = 0; y < pileHeight; y++)
        {
          GameObject go = (GameObject)Instantiate(brick, pos, Quaternion.identity, transform);
          if (y == 0) {
            go.GetComponent<Sediment>().ChangeState(Sediment.State.SEDIMENT);
          } else {
            go.GetComponent<Rigidbody>().isKinematic = false;
          }
          pos.y += size * 1.001f;

        }
        GenArch(pos);
      }
    }
  }


  void GenArch(Vector3 root)
  {
    // archs
    Bounds ab = archBrick.GetComponent<MeshFilter>().sharedMesh.bounds;
    float aOffset = (ab.center.y - ab.min.y) * archBrick.transform.localScale.y;
    float aSize = ab.size.y * archBrick.transform.localScale.y;

    Vector3 pos1 = root;
    RaycastHit hit1;
    if (Physics.Raycast(pos1, -Vector3.up, out hit1) && hit1.collider.gameObject.name.StartsWith(brick.name))
    {
      int n = 4;
      for (int i = 0; i < n; i++)
      {
        pos1.y = hit1.point.y + aOffset;
        float r = aSize * 4.0f;
        float a = Mathf.Lerp(0, Mathf.PI*2, 1/(float)n);
        Vector3 dir = new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r);
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 up = Vector3.Cross(dir, right).normalized;
        Vector3 pos2 = pos1 + dir + Vector3.up;
        RaycastHit hit2;
        if (Physics.Raycast(pos2, -Vector3.up, out hit2) && hit2.collider.gameObject.name.StartsWith(brick.name))
        {
          pos2.y = hit2.point.y + aOffset;
          if (Mathf.Abs(pos1.y - pos2.y) < aSize * 2)
          {
            Vector3 center = (pos1 + pos2) / 2.0f;
            int m = 9;
            for (float j = 0; j <= m; j++)
            {
              float a0 = Mathf.Lerp(0, Mathf.PI, j / (float)m);
              float y0 = Mathf.Sin(a0);
              float x0 = Mathf.Cos(a0);
              float x1 = 0.5f + x0 * 0.5f;
              Vector3 pos = Vector3.Lerp(pos1, pos2, x1) + up * y0 * aSize * 2.0f;
              Quaternion rot = Quaternion.LookRotation(pos - center, Vector3.up * x0);
              Instantiate(archBrick, pos, rot, transform);
            }
            return;
          }
        }
      }
    }
  }

}
