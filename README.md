# ����ֱ�۵�Unity����󶨹���
## ʹ�ò���
* 1.�������ZBinder.cs����Ŀ·��
* 2.����Ҫ�󶨵�UI�Ϲ���ZBinder���
* 3.������Ҫ�󶨵Ľڵ�tagΪTAG_ZBINGDER
�˴������ڽű����Զ���
```csharp
private const string TagKey = "TAG_ZBINDER";
```
![](Pic/1.png)
* 4.���ZBinder������Զ���

![](Pic/2.png)

* 5.��������Զ�����Ҫ�������GameObject�������� �Թ�ʹ��
�˴������ڽű����Զ�����Ҫ�󶨵��������
```csharp
    public static Type[] ComponentTypes =
    {
            typeof(Image),
            typeof(Text),
    };
```
![](Pic/3.png)
## Lua��
��ZBinder lua��ش����ע��
����ͨ������BindView��������� ���� ��������-��� �ĸ�ʽ �󶨵�lua��table��

�ο�����
��
```Lua
--��memberName��Ӧ��gameObject��table
function UIBaseView:BindViewComponentsByMemberName(view)
    view.ZBinder = view.ZBinder or {}
    local componentsBinder = view.m_uiObj:GetComponent("ZBinder")
    if componentsBinder then
        componentsBinder:BindView(view, view.ZBinder)
    end
end
```
��ȡGameObject�ο�
```Lua
self.ZBinder.GameObject.HistoryOrderPanel
```
��ȡComponent�ο�
```Lua
self.ZBinder.ButtonEx.infoItemBtn
```
## ��ע
������Ҫ�޸ĺʹ����Ƶĵط�������ϵ����QQ��848832649��
��л�����е�����ͽ��顣