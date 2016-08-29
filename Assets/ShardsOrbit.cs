using UnityEngine;
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

        yield return new WaitUntil(()
            => Vector3.Distance(transform.position, 
                GameObject.FindGameObjectWithTag("Player").transform.position)<120f);

		StartCoroutine(SpawnShards());
	}

	IEnumerator SpawnShards()
	{
		yield return new WaitForFixedUpdate();

		int _largeShardsIterations = Random.Range(20, 30);
        int _smallShardsIterations = Random.Range(60, 90);

        List<Transform> _shards = new List<Transform>();

		for (int i = 0; i < _largeShardsIterations; i++)
		{
			var _newShard = GameObject.CreatePrimitive(PrimitiveType.Cube);
			_newShard.GetComponent<MeshRenderer>().material
				= this.GetComponent<MeshRenderer>().material;

			_newShard.GetComponent<BoxCollider>().enabled = false;

			_newShard.transform.position = transform.position;
			_newShard.transform.rotation = Quaternion.identity;

			_newShard.transform.
				Rotate(Random.Range(-6f, 6f),
					Random.Range(0f, (i * (360f / _largeShardsIterations))),
						Random.Range(-90f, 90f));

			_newShard.transform.position
				= _newShard.transform.forward
					* (Random.Range(-0.12f, -15f));

			_newShard.transform.localScale
				= new Vector3(Random.Range(2f, 6f),
					Random.Range(2f, 6f),
						Random.Range(2f, 6f));

			//_newShard.AddComponent<OrbitArround>();
			_shards.Add(_newShard.transform);
		}

        for (int i = 0; i < _smallShardsIterations; i++)
        {
            var _newShard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _newShard.GetComponent<MeshRenderer>().material
                = this.GetComponent<MeshRenderer>().material;

            _newShard.GetComponent<BoxCollider>().enabled = false;

            _newShard.transform.position = transform.position;
            _newShard.transform.rotation = Quaternion.identity;

            _newShard.transform.
                Rotate(Random.Range(-3, 3f),
                    Random.Range(0f, (i * (360f / _smallShardsIterations))),
                        Random.Range(-90f, 90f));

            _newShard.transform.position
                = _newShard.transform.forward
                    * (Random.Range(-0.12f, -17f));

            _newShard.transform.localScale
                = new Vector3(Random.Range(0.333f, 0.999f),
                    Random.Range(0.333f, 0.999f),
                        Random.Range(0.333f, 0.999f));

            _shards.Add(_newShard.transform);
        }

        StartCoroutine(OrbitArroundMonolith(_shards));
	}

	IEnumerator OrbitArroundMonolith(List<Transform> _shards)
	{
		yield return new WaitForFixedUpdate();

		GameObject _newRingOfShatters
			= new GameObject();

		_newRingOfShatters.name = "RingOfShatters";

		foreach (var _shard in _shards)
		{
			GameObject _newOrbit = new GameObject();

			_newOrbit.transform.position
				= transform.position
					+ new Vector3(Random.Range(-2f, 2f), 
						Random.Range(-2f, 2f),
							Random.Range(-2f, 2f));
			
			_newOrbit.transform.localRotation
				= Quaternion.identity;

			_newOrbit.transform.SetParent(_newRingOfShatters.transform);

			_shard.SetParent(_newOrbit.transform);
			_shard.localPosition
				= _shard.localPosition * 0.0666f;

			StartCoroutine(TranslateOrbit(_newOrbit.transform));
		}
		_newRingOfShatters.transform.SetParent(transform);

	}

	IEnumerator TranslateOrbit(Transform _orbit)
	{
		float _nextDisplacementSchedule 
			= Time.time + Random.Range(2,3f);

		Vector3 _currentOrbitRotationDisplacement 
			= new Vector3(Random.Range(-3f, 3f),
						Random.Range(30f, 90f),
							Random.Range(-3f, 3f));

		Vector3 _currentOrbitScaleDisplacement
			= new Vector3(Random.Range(0.666f, 0.999f),
						Random.Range(0.666f, 0.999f),
							Random.Range(0.666f, 0.999f));

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
					= new Vector3(0f,Random.Range(20f, 60f),0f);

				_currentOrbitScaleDisplacement
					= new Vector3(Random.Range(0.888f, 0.999f),
						Random.Range(0.888f, 0.999f),
							Random.Range(0.888f, 0.999f));
			}
		}
	}
}


