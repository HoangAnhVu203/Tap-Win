using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PanelPrize : UICanvas, IPointerClickHandler
{
    [SerializeField] private Text txtReward;
    [SerializeField] private string rewardFormat = "+{0}";

    public bool IsShowing { get; private set; }

    private Action onClosed;

    public override void SetUp()
    {
        AutoAssignReferences();
    }

    public void ShowReward(int rewardAmount, Action onClose = null)
    {
        AutoAssignReferences();

        onClosed = onClose;
        IsShowing = true;

        if (txtReward != null)
            txtReward.text = string.Format(rewardFormat, rewardAmount);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        CloseDirectly();
    }

    public override void CloseDirectly()
    {
        if (!IsShowing && !gameObject.activeSelf)
            return;

        IsShowing = false;

        Action callback = onClosed;
        onClosed = null;

        base.CloseDirectly();
        callback?.Invoke();
    }

    private void AutoAssignReferences()
    {
        if (txtReward != null)
            return;

        Text[] texts = GetComponentsInChildren<Text>(true);

        if (texts.Length > 0)
            txtReward = texts[0];
    }

    
}
