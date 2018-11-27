using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QJBT;

[QJBT.ContextMenuItem("Leaf/Follow Player")] //Required for this leaf to get added to the context menu in the editor window
public class FollowPlayer : Leaf
{
    GameObject _player = null;

    Vector3? _targetOrientation = null;

    Vector3? _targetPosition = null;

    public FollowPlayer(BehaviourTreeController tree) : base(tree, "Follow Player")
    {
    }

    public override void Initialize() //Initialize() is called everytime before the node gets processed
    {
        if(_player == null)
        {
            _player = GameObject.FindWithTag("Player");
        }

        _targetOrientation = null;

        _targetPosition = null;

        if (_player != null)
        {
            var dir = _player.transform.position - GameObject.transform.position;
            if (dir.sqrMagnitude > 4.0f)
            {
                _targetOrientation = dir.normalized;
                _targetPosition = _player.transform.position;
            }
        }
    }

    protected override Status Process()
    {
        if (_player == null)
        {
            return Status.Failure;     
        }

        if (_targetOrientation != null &&  Vector3.Angle(GameObject.transform.forward, _targetOrientation.Value) > 2.0f)
        {
            GameObject.transform.rotation = Quaternion.Lerp(GameObject.transform.rotation, Quaternion.LookRotation(_targetOrientation.Value, new Vector3(0.0f, 1.0f, 0.0f)), 0.05f);
        
            return Status.Running;
        }
        else 
        if (_targetPosition != null && (GameObject.transform.position - _targetPosition.Value).sqrMagnitude > 4.0f)
        {
            GameObject.transform.position += GameObject.transform.forward * 2.0f * Time.deltaTime;
            return Status.Running;
        }

        return Status.Success;
        
    }
}
