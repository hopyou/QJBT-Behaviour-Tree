using System.Collections;
using System.Collections.Generic;
using QJBT;
using UnityEngine;

[QJBT.ContextMenuItem("Leaf/Are Enemies Around")] //Required for this leaf to get added to the context menu in the editor window
public class AreEnemiesAround : Leaf {

    public AreEnemiesAround(BehaviourTreeController tree) : base(tree, "Are Enemies Around")
    {
    }

    protected override Status Process()
    {
        var colliders = Physics.OverlapSphere(GameObject.transform.position, 10.0f, LayerMask.GetMask("Enemy")); 
        if(colliders.Length > 0)
        {
            DataContext["Enemies"] = colliders; //store results in datacontext for use in next node 
            return Status.Success;
        }
        return Status.Failure;
    }
}
