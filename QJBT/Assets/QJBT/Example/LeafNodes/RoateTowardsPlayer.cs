using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QJBT;

[QJBT.ContextMenuItem("Leaf/Rotate Towrads Player")] //Required for this leaf to get added to the context menu in the editor window
public class RotateTowardsPlayer : Leaf {

    Vector3? _targetOrientation;

    GameObject _player;

    public RotateTowardsPlayer(BehaviourTreeController tree) : base(tree, "Rotate Towards Player")
    {
    }

    public override void Initialize() //Initialize() is called everytime before the node gets processed
    {
        if(_player == null)
        {
            _player = GameObject.FindWithTag("Player");
        }
        _targetOrientation = null;
        
        if(_player != null)
        {
            _targetOrientation = (_player.transform.position - GameObject.transform.position).normalized;
        }

    }

    protected override Status Process()
    {
        if(_player == null)
        {
            return Status.Failure;
        }

        if (_targetOrientation != null &&Vector3.Angle(GameObject.transform.forward, _targetOrientation.Value) > 1.0f)
        {
            GameObject.transform.rotation = Quaternion.Lerp(GameObject.transform.rotation, Quaternion.LookRotation(_targetOrientation.Value, new Vector3(0.0f, 1.0f, 0.0f)), 0.05f);
            return Status.Running;
        }
        return Status.Success;

    }
}
