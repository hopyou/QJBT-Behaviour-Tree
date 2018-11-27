using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;

namespace QJBT
{
    public static class NodeFactory
    {     
       static Dictionary<string, Type> _registeredNodes = new Dictionary<string, Type>();

        public static Node CreateNode(string typeKey, BehaviourTreeController tree)
        {
            if (_registeredNodes.ContainsKey(typeKey))
            {
                return (Node)Activator.CreateInstance(_registeredNodes[typeKey], new object[1] {tree});
            }
            throw new System.ArgumentException("Node of type \"" + typeKey + "\" is not registered.");
        }

        public static void RegisterNode(string key, Type node)
        {
            _registeredNodes.Add(key, node);  
        }

#if UNITY_EDITOR
        static Dictionary<string, string> _registeredNodeMenuNames = new Dictionary<string, string>();

        public static void RegisterNodeMenuName(string key, string menuName)
        {
            _registeredNodeMenuNames.Add(key, menuName);
        }
        /// <summary>
        /// Returns a list of registered nodes as KeyValuePairs, Key = registered key,  Value = menu display name
        /// </summary>
        public static ReadOnlyCollection<KeyValuePair<string, string>> RegisteredNodes
        {
            get
            {
                var nodes = new List<KeyValuePair<string, string>>();
                foreach (var pair in _registeredNodeMenuNames)
                {
                    nodes.Add(pair);
                }
                return nodes.AsReadOnly();
            }
        }
#endif
    }

}