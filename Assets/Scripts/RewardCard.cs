using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Una carta de recompensa: un Button con nombre, descripción e icono.
public class RewardCard : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Image iconImage;
    [SerializeField] Button button;

    public void Show(TipoDeBola type, Action<TipoDeBola> onPicked)
    {
        gameObject.SetActive(true);

        nameText.text = type.displayName;
        descriptionText.text = type.description;

        iconImage.sprite = type.icon;
        iconImage.color = type.icon != null ? Color.white : type.color;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onPicked(type));
    }

    public void Hide() => gameObject.SetActive(false);
}
