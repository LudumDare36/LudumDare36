using UnityEngine;
using System.Collections.Generic;

//#define DEBUG_TRIGGER_VOLUMES


/// <summary>
/// A wrapper class for triggers that has public events
/// that other scripts can subscribe to, a public method 
/// to set the trigger's layer mask, and public methods 
/// to enable or disable the trigger.
/// </summary>
[RequireComponent(typeof (Collider))]
public class TriggerVolume : MonoBehaviour
{
    private new Collider collider;

    private List<Collider> containing = new List<Collider>();

    public int ContainCount
    {
        get { return containing.Count; }
    }

    public delegate void TriggerEventDelegate(TriggerVolume volume, Collider collider);

    [SerializeField]
    private LayerMask collisionMask = -1;

    private void Awake()
    {
        collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    public event TriggerEventDelegate OnTriggerEnterEvent;

    private void DispatchOnTriggerEnterEvent(Collider collider)
    {
        if (OnTriggerEnterEvent != null)
            OnTriggerEnterEvent(this, collider);
    }

    public event TriggerEventDelegate OnTriggerStayEvent;
    private void DispatchOnTriggerStayEvent(Collider collider)
    {
        if (OnTriggerStayEvent != null)
            OnTriggerStayEvent(this, collider);
    }

    public event TriggerEventDelegate OnTriggerExitEvent;
    private void DispatchOnTriggerExitEvent(Collider collider)
    {
        if (OnTriggerExitEvent != null)
            OnTriggerExitEvent(this, collider);
    }

    public void SetCollisionMask(LayerMask targetLayerMask)
    {
        collisionMask = targetLayerMask;
    }

    public void EnableVolume(bool enable)
    {
        if (enable)
            collider.enabled = true;
        else
            Disable();
    }

    private void Disable()
    {
        collider.enabled = false;

        if (OnTriggerExitEvent == null)
            return;

        for (int i = 0; i < containing.Count; i++)
        {
            Collider target = containing[i];
            containing.RemoveAt(i);
            i--;

            DispatchOnTriggerExitEvent(target);
        }
    }

    public Bounds GetBounds()
    {
        return collider.bounds;
    }

    public int GetContainingCount()
    {
        return containing.Count;
    }

    public Collider GetContaining(int index)
    {
        return containing[index];
    }

    public bool Contains(Collider other)
    {
        return containing.Contains(other);
    }

    public void ClearContainingList()
    {
        if (containing == null)
            return;

        for (int i = 0; i < containing.Count; i++)
        {
            Collider collider = containing[i];

            if (collider == null)
                continue;

            OnTriggerExit(collider);
        }
        containing.Clear();
    }

    public void OnTriggerEnter(Collider other)
    {
		if (!other.gameObject.IsInLayerMask(collisionMask))
            return;

        if (!containing.Contains(other))
        {
            containing.Add(other);
            DispatchOnTriggerEnterEvent(other);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.IsInLayerMask(collisionMask))
            return;

        containing.Remove(other);
        DispatchOnTriggerExitEvent(other);
    }

    public void OnTriggerStay(Collider other)
    {
        if (!other.gameObject.IsInLayerMask(collisionMask))
            return;

        if (!containing.Contains(other))
            return;

        DispatchOnTriggerStayEvent(other);
    }

#if UNITY_EDITOR && DEBUG_TRIGGER_VOLUMES
    private void OnDrawGizmos()
    {
        if (containing != null)
        {
            for (int i = 0; i < containing.Count; i++)
            {
                if (containing[i] != null)
                {
                    Gizmos.DrawSphere(containing[i].transform.position, 0.1f);
                    Gizmos.DrawLine(transform.position, containing[i].transform.position);
                }
            }
        }
    }
#endif

}
