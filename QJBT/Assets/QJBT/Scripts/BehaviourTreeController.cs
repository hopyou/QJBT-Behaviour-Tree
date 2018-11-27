using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using System;
using System.Reflection;
using System.Linq;

namespace QJBT
{
    public static class TypeLoaderExtensions
    {
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
    }

    [CreateAssetMenu(menuName ="QJBT/Behaviour Tree Controller")]
    public class BehaviourTreeController : ScriptableObject, ISerializationCallbackReceiver
    {

        Dictionary<Node, List<Node>> _nodesHierarchy = new Dictionary<Node, List<Node>>();

        Dictionary<string, object> _dataContext = new Dictionary<string, object>();

        Node _rootNode;    

        [SerializeField] [HideInInspector]
        string _serializedTree;

#if UNITY_EDITOR
        public event Action TreeStatusChanged = delegate { };
#endif

        static BehaviourTreeController()
        {
           IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany<Assembly,Type>(GetTypesWithBaseClassNode);
            foreach (var type in types)
            {
                NodeFactory.RegisterNode(type.ToString(), type);
#if UNITY_EDITOR
                var attris = type.GetCustomAttributes(false);
                foreach(var attri in attris)
                {
                    if (attri is ContextMenuItemAttribute)
                    {
                        var att = attri as ContextMenuItemAttribute;
                        NodeFactory.RegisterNodeMenuName(type.ToString(), att.Name);
                        break;
                    }
                }
#endif
            }
        }

        private static IEnumerable<Type> GetTypesWithBaseClassNode(Assembly asm)
        {
            var baseType = typeof(Node);
            return asm.GetLoadableTypes().Where(p =>
            {
                return baseType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract;
            }).ToList();
        }

        // Use this for initializationn
        private void OnEnable()
        {
            if(_rootNode == null)
            {
               _rootNode = new Repeater(this);
            }
        }

        public GameObject gameObject { get; set; }

        public Dictionary<string, object> DataContext {
            get
            {
                return _dataContext;
            }
        }

        public void Tick()
        {
#if UNITY_EDITOR
            _rootNode.ResetActiveStatus();
#endif
            _rootNode.Initialize();
            _rootNode.ProcessNode();
#if UNITY_EDITOR
            TreeStatusChanged();
#endif
        }

#if UNITY_EDITOR
        public void ForceResetActiveStatus()
        {
            _rootNode.ForceResetActiveStatus();
            TreeStatusChanged();
        }

        public Dictionary<Node, List<Node>> GetNodesHierarchy()
        {
            _nodesHierarchy.Clear();
            BuildHierarchy(_rootNode);
            return _nodesHierarchy;
        }

        void BuildHierarchy(Node node)
        {
            if (node != null)
            {
                var childNodes = node.GetChildNodes();
                if (childNodes != null)
                {
                    _nodesHierarchy.Add(node, childNodes);
                    foreach (var childNode in childNodes)
                    {
                        BuildHierarchy(childNode);
                    }
                }
            }
        }

        public void DeleteNode(Node node)
        {
            var parent = GetParent(node);
            if (parent != null)
            {
                parent.RemoveChild(node);
            }
        }

        public void SetNewParent(Node child, Node newParent)
        {
            var currentParent = GetParent(child);
            if (currentParent == newParent)
            {
                return;
            }

            if (newParent is Leaf)
            {
                return;
            }
            currentParent.RemoveChild(child);
            newParent.AddChild(child);
        }

        public Node GetParent(Node node)
        {
            foreach (var pair in _nodesHierarchy)
            {
                if (pair.Value.Contains(node))
                {
                    return pair.Key;
                }
            }
            return null;
        }

        public void OnNodePositionChanged(Node node)
        {
            var parent = GetParent(node);
            if (parent != null && parent is Composite)
            {
                var childNodes = parent.GetChildNodes();
                childNodes.Sort((a, b) => (int)(a.EditorPosition.x - b.EditorPosition.x));
            }
        }
#endif

        public void OnBeforeSerialize()
        {
            var tree = new XElement("Tree");
            if(_rootNode != null)
            {
                tree.Add(_rootNode.Serialize());
            }
            _serializedTree = tree.ToString();
        }

        public void OnAfterDeserialize()
        {
            var tree = XElement.Parse(_serializedTree);
            int count = 0;
            foreach (var childElem in tree.Elements())
            {
                _rootNode = NodeFactory.CreateNode(childElem.Name.ToString(), this);
                _rootNode.Deserialize(childElem, this);
                count++;
                if (count > 1)
                {
                    throw new Exception("Inverter node can not have more than 1 child.");
                }
            }
        }
    }
}
