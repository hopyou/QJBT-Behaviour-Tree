using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QJBT;

[QJBT.ContextMenuItem("Leaf/Shoot")]
public class Shoot : Leaf
{
    public Shoot(BehaviourTreeController tree) : base(tree, "Shoot")
    {

    }

    protected override Status Process()
    {
        var gun = GameObject.GetComponent<Gun>();
        if(gun != null)
        {
            gun.Shoot();

            return Status.Success;
        }
        return Status.Failure;
    }
}
