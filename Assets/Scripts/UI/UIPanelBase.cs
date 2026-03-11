using UnityEngine;

public abstract class UIPanelBase : MonoBehaviour
{
    public virtual void Show() => gameObject.SetActive(true);
    public virtual void Hide() => gameObject.SetActive(false);
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}