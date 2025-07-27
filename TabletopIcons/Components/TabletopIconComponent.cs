using AviaryModules.TabletopIcons.Entities;
using SecretHistories.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AviaryModules.TabletopIcons.Components;

public class TabletopIconComponent : MonoBehaviour
{
    
    private Image _image;
    
    public TabletopIcon IconSpec { get; set; }

    private bool _displaying;

    private bool _displayingForHover;

    public void Start()
    {
        _image = GetComponent<Image>();
    }

    public void Show(float duration = 0.5f)
    {
        _displaying = true;
        _image.CrossFadeAlpha(1f, duration, true);
    }

    public void Hide(float duration = 0.5f)
    {
        _displaying = false;
        _displayingForHover = false;
        _image.CrossFadeAlpha(0f, duration, true);
    }

    public void ShowHover(float duration = 1f)
    {
        _displayingForHover = true;
        SoundManager.PlaySfx(nameof(HighlightLocation));
        _image.CrossFadeAlpha(1f, duration, true);
    }

    public void HideHover(float duration = 0.5f)
    {
        if(!_displayingForHover)
            return;
        _displayingForHover = false;
        if(_displaying)
            return;
        _image.CrossFadeAlpha(0f, duration, true);
    }
}