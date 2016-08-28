using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[SerializePrivateVariables]
public class GroundBuilder : MonoBehaviour
{
    public GameObject newMonolithCrown;
    public GameObject mainMonolith;
    public Material monolithMaterial;
    public bool distort;
    public Vector3 rotation;



    public void BuildMonolithCrown()
    {
        newMonolithCrown = new GameObject();
        newMonolithCrown.name = "Monolith Crown";
        newMonolithCrown.transform.SetParent(transform);
        newMonolithCrown.transform.localPosition = Vector3.zero;
        newMonolithCrown.transform.localRotation = Quaternion.identity;

        mainMonolith = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mainMonolith.name = "MainMonolith";
        mainMonolith.GetComponent<MeshRenderer>().material = monolithMaterial;
        mainMonolith.transform.SetParent(newMonolithCrown.transform);
        mainMonolith.transform.localPosition = Vector3.zero;
        mainMonolith.transform.localRotation = Quaternion.identity;
        mainMonolith.transform.localScale
        = (new Vector3(1f, 0, 1f) * Random.Range(2f, 3f))
                + new Vector3(0f, 1f, 0f) * Random.Range(6f, 9f);

        mainMonolith.transform.
            Rotate(Random.Range(-15f, 15f),
                Random.Range(-90f, 90f),
                    Random.Range(-15f, 15f));

        BuildStakes( mainMonolith, 8, 1.111f, 0.333f, 6f, 45f, 0.333f );

        BuildStakes(mainMonolith, 6, 2.222f, 0.999f, 4f, 60f, 0.333f);

        newMonolithCrown.transform.Rotate(rotation);
    }


    private void BuildStakes(
        GameObject _surroundedMonolith,
        int _stakes,
        float _spacing,
        float _width,
        float _height,
        float _angle,
        float _variation = 0.333f)
    {
        var _generatedStakes = new List<GameObject>();

        var _stakesToBuild
        = Mathf.RoundToInt( Random.Range( _stakes * ( 1 - _variation ), _stakes ) );

        for (var j = 0; j < _stakesToBuild; j++)
        {
            var _stake = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _stake.name = "Stake" + j;
            _stake.GetComponent<MeshRenderer>().material = monolithMaterial;
            _stake.transform.SetParent(newMonolithCrown.transform);
            _stake.transform.localPosition = _surroundedMonolith.transform.localPosition;
            _stake.transform.localRotation = _surroundedMonolith.transform.localRotation;
            _stake.transform.localScale
            = (new Vector3(1f, 0, 1f) * Random.Range(_width * (1 - _variation), _width))
                    + new Vector3(0f, 1f, 0f) * Random.Range(_height * (1 - _variation), _height);

            _stake.transform.
                Rotate(0f, Random.Range(((360 / _stakesToBuild) * j) * (1 - _variation),
                                        ((360 / _stakesToBuild) * j)), 0f);

            _stake.transform.localPosition
            = _stake.transform.localPosition
                    + (_stake.transform.forward
                        * (Random.Range(-_spacing * (1 - _variation), -_spacing)
                            - (((_surroundedMonolith.transform.localScale.x / 2f)
                                - (_stake.transform.localScale.x / 2f)))));


            var _toocloser = Physics.OverlapBox( _stake.transform.position, _stake.transform.localScale * 0.5f ).Any();

            while (_toocloser )
            {
                _stake.GetComponent< BoxCollider >().enabled = false;
                _surroundedMonolith.GetComponent<BoxCollider>().enabled = false;

                var _tooCloserStackes
                = Physics.OverlapBox(_stake.transform.position, _stake.transform.localScale * 0.5f );

                for ( var i = 0; i < _tooCloserStackes.Length; i++ )
                {
                    _stake.transform.LookAt( _tooCloserStackes[i].transform.position );

                    _stake.transform.localPosition
                    = _stake.transform.localPosition
                          + ( _stake.transform.forward
                              * ( Random.Range( - _spacing * ( 1 - _variation ), - _spacing )
                                  - ( ( ( _tooCloserStackes[i].transform.localScale.x / 2f )
                                        - ( _stake.transform.localScale.x / 2f ) ) ) ) );
                }

                _toocloser
                = Physics.OverlapBox(_stake.transform.position, _stake.transform.localScale * 0.5f).Any();

                _stake.GetComponent<BoxCollider>().enabled = true;
                _surroundedMonolith.GetComponent<BoxCollider>().enabled = true;
            }

            _stake.transform.
                    LookAt(_surroundedMonolith.transform.position);

            _stake.transform.
                Rotate(Random.Range(-_angle * (1 - _variation), -_angle), 0f, 0f);

            _generatedStakes.Add( _stake );
        }

        if (distort)
        {
            _surroundedMonolith.transform.localScale
            = new Vector3(_surroundedMonolith.transform.localScale.x * Random.Range( 1 - _variation, 1f ),
                                _surroundedMonolith.transform.localScale.y * Random.Range(_variation, (1f + _variation)),
                                    _surroundedMonolith.transform.localScale.z * Random.Range(1 - _variation, 1f));

            _surroundedMonolith.transform.parent.localScale
            = new Vector3(_surroundedMonolith.transform.parent.localScale.x * Random.Range(1 - _variation, 1f),
                                _surroundedMonolith.transform.parent.localScale.y * Random.Range(1, (1f + _variation)),
                                    _surroundedMonolith.transform.parent.localScale.z * Random.Range(1 - _variation, 1f));

            foreach (var _stake in _generatedStakes)
            {
                _stake.transform.SetParent(_surroundedMonolith.transform);
            }
        }

        foreach (var _stake in _generatedStakes)
        {
            _stake.GetComponent<BoxCollider>().enabled = false;
        }
    }
}
