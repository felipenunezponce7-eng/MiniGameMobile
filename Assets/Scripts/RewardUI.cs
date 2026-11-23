using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class RewardUI : MonoBehaviour
{
    [SerializeField] LevelManager level;
    [SerializeField] MazoDeBolas Mazo;
    [SerializeField] RewardPool pool;

    [Header("Victoria")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text titleText;
    [SerializeField] RewardCard[] cards; 

    [Header("Derrota")]
    [SerializeField] GameObject defeatPanel;

    bool picked;

    void Awake()
    {
        // Si este script está dentro del panel, ocultarlo lo apagaría a sí mismo
        if (panel != null && transform.IsChildOf(panel.transform))
        {
            Debug.LogError("RewardUI está dentro del panel de victoria. Muévelo al Canvas.", this);
            return;
        }
        if (defeatPanel != null && transform.IsChildOf(defeatPanel.transform))
        {
            Debug.LogError("RewardUI está dentro del panel de derrota. Muévelo al Canvas.", this);
            return;
        }

        if (panel != null) panel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (level == null)
        {
            Debug.LogError("RewardUI: falta asignar el LevelManager.", this);
            return;
        }
        level.OnWon.AddListener(ShowVictory);
        level.OnLost.AddListener(ShowDefeat);
    }

    void OnDisable()
    {
        if (level == null) return;
        level.OnWon.RemoveListener(ShowVictory);
        level.OnLost.RemoveListener(ShowDefeat);
    }

    void ShowVictory()
    {
        if (panel == null)
        {
            Debug.LogError("RewardUI: falta asignar el panel de victoria.", this);
            return;
        }

        List<TipoDeBola> offers;
        if (pool != null) offers = pool.Roll(cards.Length);
        else
        {
            Debug.LogError("RewardUI: falta asignar el Reward Pool.", this);
            offers = new List<TipoDeBola>();
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (i < offers.Count) cards[i].Show(offers[i], Pick);
            else cards[i].Hide();
        }

        if (titleText != null) titleText.text = $"Nivel {RunState.Level} superado";
        panel.SetActive(true);
    }

    void ShowDefeat()
    {
        if (defeatPanel == null)
        {
            Debug.LogError("RewardUI: falta asignar el panel de derrota.", this);
            return;
        }
        defeatPanel.SetActive(true);
    }

    void Pick(TipoDeBola type)
    {
        if (picked) return; // evita doble toque
        picked = true;

        // Reemplaza a UNA bola Normal; si no quedan, sube de nivel UNA bola con la misma mejora
        if (!RunState.TryUpgradeBall(type))
            Debug.Log("No se pudo aplicar la mejora: no quedan bolas normales ni una bola con esta misma mejora.");
        RunState.NextLevel();
    }

    // Conecta al botón "Saltar" si quieres permitir no llevarse nada
    public void Skip()
    {
        if (picked) return;
        picked = true;
        RunState.NextLevel();
    }

    // Conecta al botón "Reintentar" del panel de derrota
    public void Restart() => RunState.RestartRun();
}
