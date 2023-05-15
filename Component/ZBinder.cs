using System;
using System.Collections.Generic;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XLua;
using Common.Utils;
using MoreMountains.Feedbacks;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Author:ZWT
/// Time:2023/01/30
/// Des:用来进行组件自定义绑定
/// </summary>
[InitializeOnLoad]
#endif
public class ZBinder : MonoBehaviour, IDisposable
{
    private const string GameObjectKey = "GameObject";

    static ZBinder()
    {
#if UNITY_EDITOR
        EditorApplication.hierarchyWindowItemOnGUI += DrawMark;
#endif
    }

    //这个可以在lua层直接赋值，方便以后扩展
    public static Type[] ComponentTypes =
    {
            typeof(Image),
            typeof(Button),
            typeof(RawImage),
            typeof(Text),
            typeof(CanvasGroup),
            typeof(Canvas),
            typeof(Slider),
            typeof(InputField),
            typeof(TMP_InputField),
            typeof(SkeletonGraphic),
            typeof(ScrollRect),
            typeof(Dropdown),
            typeof(TMP_Dropdown),
            typeof(TMP_DropdownEx),
            typeof(Toggle),
            typeof(EventTrigger),
            typeof(VerticalLayoutGroup),
            typeof(HorizontalLayoutGroup),
            typeof(GridLayoutGroup),
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            //ZM自定义组件
            typeof(ZSlider),
            typeof(ButtonEx),
            typeof(ScrollRectEx),
            typeof(TMPLocalization),
            typeof(MMFeedbacks)
    };

    // 绑定事件的方法
    private static readonly Dictionary<Type, string> TypeAliasName = new Dictionary<Type, string>
        {
            {typeof(TextMeshProUGUI), "Text"}
        };

    [SerializeField] public BindComponentContainer[] BindContainers;
    [SerializeField] public BindGameObject[] BindGameObjects;

    private LuaTable m_owner;

    private void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (XLuaManager.Instance.LuaEnv != null)
        {
            m_owner?.Dispose();
        }
        m_owner = null;
    }

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
    }


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
    public Dictionary<RectTransform, bool> editorToggle;

    public void FormatEditorToggle()
    {
        if (editorToggle == null)
            editorToggle = new Dictionary<RectTransform, bool>();
        else
            editorToggle.Clear();

        foreach (BindGameObject go in BindGameObjects)
        {
            if (go.Target == null)
            {
                continue;
            }
            editorToggle.Add(go.Target.transform as RectTransform, true);
        }
    }

    [MenuItem("CONTEXT/ZBinder/自动绑定")]
    [BlackList]
    public static void AutoBind(MenuCommand _menuCommand)
    {
        var _binder = _menuCommand.context as ZBinder;
        if (_binder == null) return;

        BindTo(_binder);
        /*
        EditorUtility.SetDirty(_binder.gameObject);
        AssetDatabase.SaveAssets();
        EditorUtility.ClearDirty(_binder.gameObject);*/
    }

    public void BindTo(GameObject go)
    {
        var _containers = new List<BindComponentContainer>(BindContainers);
        foreach (var _componentType in ComponentTypes)
        {
            var components = _containers.Find(o => o.TypeName.Equals(_componentType.Name))?.Components;
            var _bindComponents = components != null ? new List<BindComponent>(components) : new List<BindComponent>();
            var _components = go.GetComponents(_componentType);
            foreach (var _component in _components)
            {
                var existComp = _bindComponents.Find(o => o.Key.Equals(_component.name));
                if (existComp != null)
                    _bindComponents.Remove(existComp);
                else
                {
                    _bindComponents.Add(new BindComponent
                    {
                        Key = _component.name,
                        Target = _component
                    });
                }
            }

            if (_bindComponents.Count <= 0)
            {
               var existContainer = _containers.Find(o => o.TypeName.Equals(_componentType.Name));
                if (existContainer != null)
                    _containers.Remove(existContainer);
                continue;
            }

            var _componentTypeName = _componentType.Name;

            AddBindContainer(_containers, _componentTypeName, _bindComponents);

            if (TypeAliasName.TryGetValue(_componentType, out var _aliasName))
            {
                AddBindContainer(_containers, _aliasName, _bindComponents);
            }
        }

        var _bindGameObjects = new List<BindGameObject>(BindGameObjects);
        var existGo = _bindGameObjects.Find(o=> o.Name.Equals(go.name));
        if (existGo != null)
            _bindGameObjects.Remove(existGo);
        else
        { 
            _bindGameObjects.Add(new BindGameObject
            {
                Name = go.name,
                Target = go
            });
        }

        BindContainers = _containers.ToArray();
        BindGameObjects = _bindGameObjects.ToArray();
        FormatEditorToggle();
    }


    // 可以用异名绑定
    [BlackList]
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
                bool oldBindRule = _component.gameObject != _binder.gameObject && (_name.StartsWith("_") || _component.tag == "TAG_ZBINDER");
                _binder.editorToggle.TryGetValue(_component.transform as RectTransform, out bool newBindRule);
                if (!(oldBindRule||newBindRule))
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
            bool oldBindRule = _name.StartsWith("_") || _transform.tag == "TAG_ZBINDER";
            _binder.editorToggle.TryGetValue(_transform as RectTransform, out bool newBindRule);
            if (!(oldBindRule || newBindRule))
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
            var _components = new List<BindComponent>();//_container.Components
            _components.AddRange(_bindComponents);

            _container.Components = _components.ToArray();
        }
    }

    private static void DrawMark(int instanceID, Rect selectionRect)
    {
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        Transform trans = go.transform;
        if (go == null)
            return;

        // 获取根节点的ZBinder组件
        Transform root = go.transform;
        while (root.parent != null && root.parent.parent != null)//顶层UI上一个Canvas(Environment)
        {
            root = root.parent;
        }
        ZBinder binder = root.GetComponent<ZBinder>();
        bool newBindRule = false;
        if (binder != null && binder.editorToggle != null)
        {
            binder.editorToggle.TryGetValue(trans as RectTransform, out newBindRule);
        }

        if (!(newBindRule ||go.tag == "TAG_ZBINDER"))
            return;

        // 绘制一个红色点
        EditorGUIUtility.SetIconSize(Vector2.one * 8);
        EditorGUIUtility.DrawColorSwatch(new Rect(selectionRect.x + selectionRect.width, selectionRect.y + 4, 8, 8), Color.red);
    }
#endif
}
