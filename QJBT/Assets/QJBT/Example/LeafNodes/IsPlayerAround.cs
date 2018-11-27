using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QJBT;

[QJBT.ContextMenuItem("Leaf/Is Player Around")] //Required for this leaf to get added to the context menu in the editor window
public class IsPlayerAround : Leaf {

    public IsPlayerAround(BehaviourTreeController tree) : base(tree, "Is Player Around")
    {
    }

    protected override Status Process()
    {

        if (Physics.CheckSphere(GameObject.transform.position, 8.5f, LayerMask.GetMask("Player")))
        {
            return Status.Success;
        }

        return Status.Failure;
    }
}
