using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml.Linq;

namespace QJBT
{
    
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ContextMenuItemAttribute : Attribute
    {
        public ContextMenuItemAttribute(string name)
        {
            Name = name;
        }

        public string Name
        {
            get;
            private set;
        }
    }

    public enum Status
    {
        Success,
        Failure,
        Running
    }

    public abstract class Node
    {
        protected Node(BehaviourTreeController tree, string name)
        {
            _tree = tree;
#if UNITY_EDITOR
            Name = name;
#endif
        }
        /// <summary>
        /// Initialize is called everytime before the node gets processed.
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// This method calls Process() internally and sets the data for editor visualization
        /// </summary>
        /// <returns></returns>
        public Status ProcessNode()
        {
            var status = Process();
#if UNITY_EDITOR
            IsActive = true;
            ActiveStatus = status;
#endif
            return status;
        }
        protected abstract Status Process();
        public abstract void AddChild(Node child);
        public abstract void RemoveChild(Node child);
        public abstract List<Node> GetChildNodes();
        private BehaviourTreeController _tree;

#if UNITY_EDITOR

        public void EditorPositionChanged()
        {
            _tree.OnNodePositionChanged(this);
        }

        public virtual void ResetActiveStatus()
        {
            IsActive = false;
            foreach (var child in GetChildNodes())
            {
                child.ResetActiveStatus();
            }
        }

        public void ForceResetActiveStatus()
        {
            IsActive = false;
            foreach (var child in GetChildNodes())
            {
                child.ForceResetActiveStatus();
            }
        }

        public Vector2 EditorPosition
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public bool IsActive
        {
            get;
            set;
        }

        public Status ActiveStatus
        {
            get;
            set;
        }
#endif
        

        /// <summary>
        /// The GameObject the tree is attached to.
        /// </summary>
        protected GameObject GameObject
        {
            get
            {
                return _tree.gameObject;
            }
        }

        /// <summary>
        /// A  Dictionary<string, object> for storing data
        /// </summary>
        protected Dictionary<string, object> DataContext
        {
            get
            {
                return _tree.DataContext;
            }
        }

        public virtual XElement Serialize()
        {
            var element = new XElement(this.ToString());

#if UNITY_EDITOR
            SetAttribute(element, "name", Name);
            SetAttribute(element, "x", EditorPosition.x.ToString());
            SetAttribute(element, "y", EditorPosition.y.ToString());
#endif
            foreach (var child in GetChildNodes())
            {
                element.Add(child.Serialize());
            }
            return element;
        }

        public virtual void Deserialize(XElement element, BehaviourTreeController tree)
        {
#if UNITY_EDITOR
            Name = GetAttribute<string>(element, "name"); //element.Attribute("name").Value;
            EditorPosition = new Vector2(GetAttribute<float>(element, "x"), GetAttribute<float>(element, "y"));
#endif
            foreach (var childElem in element.Elements())
            {
                var node = NodeFactory.CreateNode(childElem.Name.ToString(), tree);
                AddChild(node);
                node.Deserialize(childElem, tree);
            }
        }

        protected void SetAttribute(XElement element, string name, string value)
        {
            element.SetAttributeValue(name, value);
        }

        protected string GetAttribute(XElement element, string name)
        {
            return element.Attribute(name).Value;
        }

        protected T GetAttribute<T>(XElement element, string name)
        {
            return (T)Convert.ChangeType(element.Attribute(name).Value, typeof(T));
        }
    }

    #region composite nodes
    public abstract class Composite : Node
    {
        protected int _currentlyProcessingNode = 0;


        protected Composite(BehaviourTreeController tree, string name) : base(tree, name)
        {
            Nodes = new List<Node>();
        }

        protected Composite(List<Node> nodes, BehaviourTreeController tree, string name) : base(tree, name)
        {
            Nodes = nodes;
        }

#if UNITY_EDITOR
        public override void ResetActiveStatus()
        {
            if (_currentlyProcessingNode == 0)
            {
                base.ResetActiveStatus();
            }
        }
#endif

        public List<Node> Nodes { get; protected set; }

        public override void Initialize()
        {
            if(Nodes.Count > 0)
            {
                Nodes[0].Initialize();
            }
        }

        public override void AddChild(Node child)
        {
            Nodes.Add(child);
        }

        public override void RemoveChild(Node child)
        {
            if (Nodes.Contains(child))
            {
                Nodes.Remove(child);
            }
        }

        public override List<Node> GetChildNodes()
        {
            return Nodes;
        }

        // public override Status Process();
    }

    [ContextMenuItem("Composite/Sequence")]
    public class Sequence : Composite
    {
        public Sequence(List<Node> nodes, BehaviourTreeController tree) : base(nodes, tree, "Sequence") {
        }

        public Sequence(BehaviourTreeController tree) : base(tree, "Sequence")
        {
        }


        protected override Status Process()
        {
            for (int i = _currentlyProcessingNode; i < Nodes.Count; i++)
            {
                var status = Nodes[i].ProcessNode();
                if (status == Status.Running)
                {
                    _currentlyProcessingNode = i;
                    return status;
                }
                else if (status == Status.Failure)
                {
                    _currentlyProcessingNode = 0;
                    return status;
                }
                else
                {
                    if((i + 1) < Nodes.Count)
                    {
                        Nodes[i + 1].Initialize();
                    }
                }
            }
            _currentlyProcessingNode = 0;
            return Status.Success;
        }

    }

    [ContextMenuItem("Composite/Random Sequence")]
    public class RandomSequence : Composite
    {

        List<int> _indices = new List<int>();
        public RandomSequence(List<Node> nodes, BehaviourTreeController tree, string name) : base(nodes, tree, "Random Sequence")
        {
        }

        public RandomSequence(BehaviourTreeController tree, string name) : base(tree, "Random Sequence")
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            if(_indices.Count != Nodes.Count)
            {
                _indices.Clear();
                for(int i = 0; i < Nodes.Count; i++)
                {
                    _indices.Add(i);
                }
                _indices.Shuffle();
            }
            else if(_indices.Count > 1)
            {
                _indices.Shuffle();
            }
        }

        protected override Status Process()
        {

            for (int i = _currentlyProcessingNode; i < _indices.Count; i++)
            {
                var status = Nodes[_indices[i]].ProcessNode();
                if (status == Status.Running)
                {
                    _currentlyProcessingNode = i;
                    return status;
                }
                else if (status == Status.Failure)
                {
                    _currentlyProcessingNode = 0;
                    return status;
                }
                else
                {
                    if ((i + 1) < _indices.Count)
                    {
                        Nodes[_indices[i + 1]].Initialize();
                    }
                }
            }
            _currentlyProcessingNode = 0;
            return Status.Success;
        }

    }

    [ContextMenuItem("Composite/Selector")]
    public class Selector : Composite
    {

        public Selector(List<Node> nodes, BehaviourTreeController tree) : base(tree, "Selector") { }

            public Selector(BehaviourTreeController tree) : base(tree, "Selector") {}

        protected override Status Process()
        {

            for (int i = _currentlyProcessingNode; i < Nodes.Count; i++)
            {
                var status = Nodes[i].ProcessNode();
                if (status == Status.Running)
                {
                    _currentlyProcessingNode = i;
                    return status;
                }
                else if (status == Status.Success)
                {
                    _currentlyProcessingNode = 0;
                    return status;
                }
                else
                {
                    if ((i + 1) < Nodes.Count)
                    {
                        Nodes[i + 1].Initialize();
                    }
                }
            }

            _currentlyProcessingNode = 0;
            return Status.Failure;
        }

    }

    [ContextMenuItem("Composite/Random Selector")]
    public class RandomSelector : Composite
    {

        List<int> _indices = new List<int>();
        public RandomSelector(List<Node> nodes, BehaviourTreeController tree) : base(tree, "Random Selector")
        {
        }

        public RandomSelector(BehaviourTreeController tree) : base(tree, "Random Selector")
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            if (_indices.Count != Nodes.Count)
            {
                _indices.Clear();
                for (int i = 0; i < Nodes.Count; i++)
                {
                    _indices.Add(i);
                }
                _indices.Shuffle();
            }
            else if (_indices.Count > 1)
            {
                _indices.Shuffle();
            }
        }

        protected override Status Process()
        {

            for (int i = _currentlyProcessingNode; i < _indices.Count; i++)
            {
                var status = Nodes[_indices[i]].ProcessNode();
                if (status == Status.Running)
                {
                    _currentlyProcessingNode = i;
                    return status;
                }
                else if (status == Status.Success)
                {
                    _currentlyProcessingNode = 0;
                    return status;
                }
                else
                {
                    if ((i + 1) < _indices.Count)
                    {
                        Nodes[_indices[i + 1]].Initialize();
                    }
                }
            }

            _currentlyProcessingNode = 0;
            return Status.Failure;
        }

    }
    #endregion

    #region decorators nodes

    public abstract class Decorator : Node
    {
        protected Decorator(Node child, BehaviourTreeController tree, string name) : base(tree, name)
        {
            Child = child;
        }

        public Node Child
        {
            get;
            set;
        }

        public override void Initialize()
        {
            Child.Initialize();
        }

        protected override abstract Status Process();

        public override List<Node> GetChildNodes()
        {
            return Child == null ? new List<Node>(0) : new List<Node> { Child };
        }

        public override void AddChild(Node child)
        {
            Child = child;
        }

        public override void RemoveChild(Node child)
        {
            if (Child == child)
            {
                Child = null;
            }
        }
    }

    [ContextMenuItem("Decorator/Inverter")]
    public class Inverter : Decorator
    {
        public Inverter(BehaviourTreeController tree) : base(null, tree, "Inverter")
        {
        }

        public Inverter(Node child, BehaviourTreeController tree, string name) : base(child, tree, name)
        {
        }
  

        protected override Status Process()
        {
            var status = Child.ProcessNode();
            status = status == Status.Running ? Status.Running : status == Status.Failure ? Status.Success : Status.Failure;
            return status;
        }
    }

    [ContextMenuItem("Decorator/Succeeder")]
    public class Succeeder : Decorator
    {
        public Succeeder(BehaviourTreeController tree) : base(null, tree, "Succeeder")
        {
        }

        public Succeeder(Node child, BehaviourTreeController tree) : base(child, tree, "Succeeder")
        {
        }


        protected override Status Process()
        {
            Child.ProcessNode();
            return Status.Success;
        }

    }

    [ContextMenuItem("Decorator/Repeater")]
    public class Repeater : Decorator
    {
        public Repeater(BehaviourTreeController tree) : base(null, tree, "Repeater")
        {
            Loops = -1;
        }

        public Repeater(Node child, BehaviourTreeController tree) : base(child, tree, "Repeater")
        {
            Loops = -1;
        }

        int _loops; // -1 for infinite loops
        int _counter = 0;

        protected override Status Process()
        {
            Child.ProcessNode();

            if (Loops < 0)
            {
                return Status.Running;
            }
            else
            {
                _counter++;
                if (_counter >= Loops)
                {
                    _counter = 0;
                    return Status.Success;
                }
                return Status.Running;
            }
        }

        public int Loops
        {
            get
            {
                return _loops;
            }
            set
            {
                _loops = value;
                _counter = 0;
            }
        }

        public override XElement Serialize()
        {
            var element = base.Serialize();
            SetAttribute(element, "loops", _loops.ToString());
            return element;
        }

        public override void Deserialize(XElement element, BehaviourTreeController tree)
        {
            _loops = GetAttribute<int>(element, "loops");
            base.Deserialize(element, tree);
        }
    }

    [ContextMenuItem("Decorator/Repeat Until Fail")]
    public class RepeatUntilFail : Decorator
    {
        public RepeatUntilFail(BehaviourTreeController tree) : base(null, tree, "Repeat Until Fail")
        {
        }

        public RepeatUntilFail(Node child, BehaviourTreeController tree) : base(child, tree, "Repeat Until Fail")
        {
        }


        protected override Status Process()
        {
            var status = Child.ProcessNode();
            return status != Status.Failure ? Status.Running : Status.Success;
        }
    }

    #endregion

    #region leaf Node
    public abstract class Leaf : Node
    {

        protected Leaf(BehaviourTreeController tree, string name) : base(tree, name) { }

        public object[] Params
        {
            get;
            set;
        }

        public override List<Node> GetChildNodes()
        {
            return new List<Node>(0);
        }

        public override void AddChild(Node child)
        {
            throw new System.Exception("Can not add child to a leaf node.");
        }

        public override void RemoveChild(Node child)
        {
            throw new System.Exception("Leaf node does not have a child.");
        }
    }
    #endregion

    public static class ExtentionMethods
    {
        private static System.Random rng = new System.Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
