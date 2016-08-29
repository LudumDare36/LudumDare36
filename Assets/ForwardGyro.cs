using UnityEngine;
using System.Collections;

public class ForwardGyro : MonoBehaviour
{
    public Gyroscope syncedGyro;
    
    void FixedUpdate()
    {
        transform.localPosition
            = Vector3.zero;

        transform.forward
            = syncedGyro.transform.forward;
    }
}
