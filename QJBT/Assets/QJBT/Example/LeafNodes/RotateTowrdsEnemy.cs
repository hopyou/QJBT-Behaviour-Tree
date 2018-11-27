using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QJBT;

[QJBT.ContextMenuItem("Leaf/Rotate Towrads Enemy")] //Required for this leaf to get added to the context menu in the editor window
public class RotateTowardsEnemy : Leaf
{

    Vector3? _targetOrientation;

    public RotateTowardsEnemy(BehaviourTreeController tree) : base(tree, "Rotate Towards Enemy")
    {

    }

    public override void Initialize() //Initialize() is called everytime before the node gets processed
    {
        _targetOrientation = null;
        if(DataContext.ContainsKey("Enemies"))
        {
            var enemies = DataContext["Enemies"] as Collider[];
            var closestEnemy = GetClosetEnemy(enemies);
            if (closestEnemy != null)
            {
                _targetOrientation = (closestEnemy.transform.position - GameObject.transform.position).normalized;
            }
        }

    }

    protected override Status Process()
    {

        if (_targetOrientation == null)
        {
            return Status.Failure;
        }

        if (Vector3.Angle(GameObject.transform.forward, _targetOrientation.Value) > 2.0f)
        {
            GameObject.transform.rotation = Quaternion.Lerp(GameObject.transform.rotation, Quaternion.LookRotation(_targetOrientation.Value, new Vector3(0.0f, 1.0f, 0.0f)), 0.05f);      return Status.Running;
        }

        return Status.Success;

    }

    Collider GetClosetEnemy(Collider[] enemies)
    {
        Collider retVal = null;
        if(enemies.Length > 0)
        {
            retVal = enemies[0];
            float minDist = float.MaxValue;
            foreach(var enemy in enemies)
            {
                var dist = (GameObject.transform.position - enemy.transform.position).sqrMagnitude;
                if(dist < minDist)
                {
                    retVal = enemy;
                    minDist = dist;
                }
            }
        }
        return retVal;
    }
}