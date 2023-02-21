using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// Blog:afoolzwt.github.io
/// Time:2023/02/21
/// Des:用来进行组件自定义绑定
/// </summary>
[InitializeOnLoad]
public class ZBinder : MonoBehaviour, IDisposable
{
    private const string GameObjectKey = "GameObject";
    private const string TagKey = "TAG_ZBINDER";

    static ZBinder()
    {
        EditorApplication.hierarchyWindowItemOnGUI += DrawMark;
    }

    //这个可以在lua层直接赋值，方便以后扩展
    public static Type[] ComponentTypes =
    {
            typeof(Image),
            typeof(Text),
    };

    // 绑定事件的方法
    private static readonly Dictionary<Type, string> TypeAliasName = new Dictionary<Type, string>
    {
            {typeof(TextMeshProUGUI), "Text"}
    };

    [SerializeField] public BindComponentContainer[] BindContainers;
    [SerializeField] public BindGameObject[] BindGameObjects;

    private void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {

    }

    /*For Lua
    public void Dispose()
    {
        m_owner?.Dispose();
        m_owner = null;
    }

    private LuaTable m_owner;
    public void BindView(LuaTable _target, LuaTable _mapping)
    {
        var _luaHelper = XLuaManager.Instance.LuaEnv;

        if (_luaHelper == null)
        {
            return;
        }

        m_owner = _target;

        if (BindContainers != null)
        {
            foreach (var _container in BindContainers)
            {
                var _table = _luaHelper.NewTable();

                foreach (var _component in _container.Components)
                {
                    _table.Set(_component.Key, _component.Target);
                }

                _mapping.Set(_container.TypeName, _table);
                _table.Dispose();
            }
        }

        if (BindGameObjects != null)
        {
            var _table = _luaHelper.NewTable();
            foreach (var _bindGameObject in BindGameObjects)
            {
                _table.Set(_bindGameObject.Name, _bindGameObject.Target);
            }

            _mapping.Set(GameObjectKey, _table);
            _table.Dispose();
        }

        _mapping.Dispose();
    }*/


    [Serializable]
    public class BindComponent
    {
        public string Key;
        public Component Target;
    }


    [Serializable]
    public class BindComponentContainer
    {
        public string TypeName;
        public BindComponent[] Components;
    }

    [Serializable]
    public class BindGameObject
    {
        public string Name;
        public GameObject Target;
    }

#if UNITY_EDITOR
    [MenuItem("CONTEXT/ZBinder/自动绑定")]
    public static void AutoBind(MenuCommand _menuCommand)
    {
        var _binder = _menuCommand.context as ZBinder;
        if (_binder == null) return;

        BindTo(_binder);
    }

    // 可以用异名绑定
    public static void BindTo(ZBinder _binder)
    {
        // 所有以_开头的组件，以及根节点上的同类型组件都会自动绑定
        var _containers = new List<BindComponentContainer>();
        foreach (var _componentType in ComponentTypes)
        {
            var _bindComponents = new List<BindComponent>();

            var _components = _binder.gameObject.GetComponentsInChildren(_componentType, true);
            foreach (var _component in _components)
            {
                var _name = _component.name;
                if (_component.gameObject != _binder.gameObject && (!_name.StartsWith("_")&&_component.tag != TagKey))
                {
                    continue;
                }

                if (_name.StartsWith("_"))
                {
                    _name = _name.Substring(1);
                }

                _bindComponents.Add(new BindComponent
                {
                    Key = _name,
                    Target = _component
                });
            }

            if (_bindComponents.Count <= 0)
            {
                continue;
            }

            var _componentTypeName = _componentType.Name;

            AddBindContainer(_containers, _componentTypeName, _bindComponents);

            if (TypeAliasName.TryGetValue(_componentType, out var _aliasName))
            {
                AddBindContainer(_containers, _aliasName, _bindComponents);
            }
        }

        var _bindGameObjects = new List<BindGameObject>();
        var _transforms = _binder.gameObject.GetComponentsInChildren<Transform>(true);
        foreach (var _transform in _transforms)
        {
            var _name = _transform.name;
            if (!_name.StartsWith("_") && _transform.tag != TagKey)
            {
                continue;
            }

            if (_name.StartsWith("_"))
            {
                _name = _name.Substring(1);
            }

            _bindGameObjects.Add(new BindGameObject
            {
                Name = _name,
                Target = _transform.gameObject
            });
        }

        _binder.BindContainers = _containers.ToArray();
        _binder.BindGameObjects = _bindGameObjects.ToArray();
    }

    private static void AddBindContainer(
        List<BindComponentContainer> _containers,
        string _typeName,
        List<BindComponent> _bindComponents)
    {
        // 如果有异名，可以多绑定一次，这主要是为了TextMeshProUGUI的特殊情况处理的
        var _container = _containers.Find(_otherContainer => _otherContainer.TypeName.Equals(_typeName));
        if (_container == null)
        {
            _container = new BindComponentContainer
            {
                Components = _bindComponents.ToArray(),
                TypeName = _typeName
            };
            _containers.Add(_container);
        }
        else
        {
            var _components = new List<BindComponent>(_container.Components);
            _components.AddRange(_bindComponents);

            _container.Components = _components.ToArray();
        }
    }

    private static void DrawMark(int instanceID, Rect selectionRect)
    {
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null)
            return;

        // 获取子节点的TAG
        if (go.tag != TagKey)
            return;

        // 绘制一个红色点
        EditorGUIUtility.SetIconSize(Vector2.one * 8);
        EditorGUIUtility.DrawColorSwatch(new Rect(selectionRect.x + selectionRect.width, selectionRect.y + 4, 8, 8), Color.red);
    }
#endif
}
