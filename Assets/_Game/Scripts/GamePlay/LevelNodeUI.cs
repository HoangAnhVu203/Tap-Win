using UnityEngine;
using UnityEngine.UI;

public class LevelNodeUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private CanvasGroup canvasGroup;

    public bool IsDisabled { get; private set; }
    public RectTransform Rect => transform as RectTransform;

    private void Awake()
    {
        if (icon == null)
            icon = GetComponent<Image>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Reset()
    {
        icon = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetDisabled(bool value)
    {
        IsDisabled = value;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = value ? 0.25f : 1f;
            canvasGroup.interactable = !value;
            canvasGroup.blocksRaycasts = !value;
        }
        else if (icon != null)
        {
            Color c = icon.color;
            c.a = value ? 0.25f : 1f;
            icon.color = c;
        }
    }
}