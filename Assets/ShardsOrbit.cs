﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShardsOrbit : MonoBehaviour
{
    public class OrbitArround : ShardsOrbit
    {
        IEnumerator Spin()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();

                float _smooth = Time.smoothDeltaTime;
                transform.Rotate(30f*_smooth,45f * _smooth, 20f * _smooth);
            }
        }
    }

	// Use this for initialization
	void Start () {
        StartCoroutine(SetUp());
    }

    IEnumerator SetUp ()
    {
        yield return new WaitForFixedUpdate();
        StartCoroutine(SpawnShards());
    }

    IEnumerator SpawnShards()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitUntil(()
            => Vector3.Distance(transform.position, 
                GameObject.FindGameObjectWithTag("Player").transform.position) < 30f);

        int _largeShardsIterations = Random.Range(20, 30);
        List<Transform> _shards = new List<Transform>();

        for (int i = 0; i < _largeShardsIterations; i++)
        {
            var _newShard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _newShard.GetComponent<MeshRenderer>().material
                = this.GetComponent<MeshRenderer>().material;

            _newShard.transform.SetParent(transform);

            _newShard.transform.localPosition
                = Vector3.zero;

            _newShard.transform.localRotation
                = Quaternion.identity;

            _newShard.transform.
                Rotate(Random.Range(-6f, 6f),
                    Random.Range(0f, (i * (360f / _largeShardsIterations))),
                        Random.Range(-90f, 90f));

            _newShard.transform.position
                = _newShard.transform.forward
                    * (Random.Range(-0.12f, -18f));

            _newShard.transform.localScale
                = new Vector3(Random.Range(6f, 54f),
                    Random.Range(6f, 54f),
                        Random.Range(6f, 54f));

            _newShard.AddComponent<OrbitArround>();
            _shards.Add(_newShard.transform);
        }
        StartCoroutine(OrbitArroundMonolith(_shards));
    }

    IEnumerator OrbitArroundMonolith(List<Transform> _shards)
    {
        yield return new WaitForFixedUpdate();

        foreach (var _shard in _shards)
        {
            GameObject _newOrbit = new GameObject();

            _newOrbit.transform.SetParent(transform);

            _newOrbit.transform.localPosition 
                = Vector3.zero;

            _newOrbit.transform.localRotation
                = Quaternion.identity;

            _shard.SetParent(_newOrbit.transform);

            StartCoroutine(TranslateOrbit(_newOrbit.transform));
        }
    }

    IEnumerator TranslateOrbit(Transform _orbit)
    {
        float _nextDisplacementSchedule 
            = Time.time + Random.Range(2,3f);

        Vector3 _currentOrbitRotationDisplacement 
            = Vector3.zero;
        Vector3 _currentOrbitScaleDisplacement
            = Vector3.zero;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            _orbit.
                Rotate(_currentOrbitRotationDisplacement*Time.smoothDeltaTime);

            _orbit.localScale
                = (Vector3.Lerp(_orbit.localScale, 
                    _currentOrbitScaleDisplacement, Time.smoothDeltaTime));

            if (Time.time > _nextDisplacementSchedule)
            {
                _nextDisplacementSchedule
                    = Time.time + Random.Range(3f, 9f);

                _currentOrbitRotationDisplacement
                    = new Vector3(Random.Range(-3f, 3f),
                        Random.Range(30f, 90f),
                            Random.Range(-3f, 3f));

                _currentOrbitScaleDisplacement
                    = new Vector3(Random.Range(0.0444f, 0.0666f),
                        Random.Range(0.0444f, 0.00666f),
                            Random.Range(0.0444f, 0.0666f));
            }
        }
    }
}

