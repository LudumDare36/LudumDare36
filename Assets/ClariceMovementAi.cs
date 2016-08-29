using UnityEngine;
using System.Collections;

public class ClariceMovementAi : MonoBehaviour {


    public GameObject followedHero;
    public float aiUpdateRate;
    public 
    float nextUpdate;

    void Start()
    {
        StartCoroutine(SetUp());
    }

    private IEnumerator SetUp()
    {
        yield return new WaitForFixedUpdate();
        StartCoroutine( StrategicPositioning());
    }

    IEnumerator StrategicPositioning()
    {

        Vector3 _strategicPoint 
            = new Vector3 (Random.Range(-3f,3f),
                Random.Range(-1f, 3f),
                    Random.Range(-1f, -3f));

        while (true)
        {
            yield return new WaitForFixedUpdate();
            transform.position
                = Vector3.Lerp(transform.position, followedHero.transform.position 
                    + (followedHero.transform.forward 
                        * _strategicPoint.z)
                            + (followedHero.transform.right
                                * _strategicPoint.x)
                                    + (followedHero.transform.up
                                        * _strategicPoint.y), 
                                            Time.smoothDeltaTime);

            transform.forward
                = Vector3.Lerp(transform.forward, 
                    followedHero.transform.forward*3f, Time.smoothDeltaTime);

            if (Time.time > nextUpdate)
            {
                nextUpdate 
                    = Time.time + Random.Range(aiUpdateRate * 0.333f, aiUpdateRate * 0.999f);
                
               _strategicPoint
                    = new Vector3(Random.Range(-3f, 3f),
                        Random.Range(1, 2f),
                            Random.Range(-2f, -3f));
            }
        }
    }
}
