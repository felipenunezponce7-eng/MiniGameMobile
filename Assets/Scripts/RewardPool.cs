using System.Collections.Generic;
using UnityEngine;

// Conjunto de bolas que pueden ofrecerse como recompensa.
[CreateAssetMenu(menuName = "Roguelike/Reward Pool")]
public class RewardPool : ScriptableObject
{
    public List<TipoDeBola> candidates = new();

    // Elige 'count' bolas distintas, con probabilidad proporcional a BallType.weight
    public List<TipoDeBola> Roll(int count)
    {
        var pool = new List<TipoDeBola>(candidates);
        var result = new List<TipoDeBola>();
        count = Mathf.Min(count, pool.Count);

        for (int n = 0; n < count; n++)
        {
            float total = 0f;
            foreach (var t in pool) total += t.weight;

            float r = Random.value * total;
            int pick = pool.Count - 1;
            for (int i = 0; i < pool.Count; i++)
            {
                r -= pool[i].weight;
                if (r <= 0f)
                {
                    pick = i;
                    break;
                }
            }

            result.Add(pool[pick]);
            pool.RemoveAt(pick); // sin repetir dentro de una misma oferta
        }

        return result;
    }
}
