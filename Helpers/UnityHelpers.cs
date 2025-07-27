using UnityEngine;
using UnityEngine.UI;

namespace AviaryModules.Helpers;

public static class UnityHelpers
{
    public static GameObject CreatePseudoPrefab(this Transform gameObject)
    {
        gameObject.gameObject.SetActive(false);
        gameObject.SetParent(null);
        gameObject.localPosition = Vector3.zero;
        return gameObject.gameObject;
    }

    public static void SetUISprite(this Image image, string spriteName, Color? color = null)
    {
        image.SetSprite("ui", spriteName, color ?? Color.white);
    }
    
    public static void SetLocalizedUISprite(this Image image, string cultureId, string spriteName, Color? color = null)
    {
        image.SetLocalizedSprite(cultureId, "ui", spriteName, color ?? Color.white);
    }

    public static void SetSprite(this Image image, string spriteType, string spriteName, Color? color = null)
    {
        image.sprite = ResourcesManager.GetSprite(spriteType, spriteName);
        image.color = color ?? Color.white;
        image.GetComponent<RectTransform>().sizeDelta = image.sprite.rect.size;
    }

    public static void SetLocalizedSprite(this Image image,
        string cultureId, string spriteType, string spriteName, Color? color = null)
    {
        image.sprite = ResourcesManager.GetSpriteLocalised(spriteType, spriteName, cultureId);
        image.color = color ?? Color.white;
        image.GetComponent<RectTransform>().sizeDelta = image.sprite.rect.size;
    }
}