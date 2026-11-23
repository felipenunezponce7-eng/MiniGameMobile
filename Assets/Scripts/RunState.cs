using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Una posición del mazo: el tipo de bola y su nivel de mejora.
// level 0 = bola Normal, 1 en adelante = nivel de la mejora (1 = recién mejorada).
// (Usa el nombre TipoDeBola, tu clase renombrada.)
public readonly struct BallSlot
{
    public readonly TipoDeBola type;
    public readonly int level;

    public BallSlot(TipoDeBola type, int level)
    {
        this.type = type;
        this.level = level;
    }
}

// Estado de la run que sobrevive a los cambios de escena: mazo, nivel y semilla.
public static class RunState
{
    // Nombre de la escena del menú. La crea Roguelike > Crear menú principal
    public const string EscenaMenu = "MenuPrincipal";
    const string ClaveMejorNivel = "MejorNivel";

    public static int Level { get; private set; } = 1;
    public static List<BallSlot> Deck { get; private set; } // null = run aún no iniciada

    // Semilla de la run: los niveles generados son distintos en cada run, pero estables dentro de ella
    static int seed;
    public static int Seed
    {
        get
        {
            if (seed == 0) seed = new System.Random().Next(1, int.MaxValue);
            return seed;
        }
    }

    // Mejor nivel alcanzado en cualquier run (se guarda en el dispositivo)
    public static int MejorNivel => PlayerPrefs.GetInt(ClaveMejorNivel, 0);

    // Evita arrastrar datos viejos si desactivas Domain Reload en el editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Level = 1;
        Deck = null;
        seed = 0;
    }

    // Empieza una run limpia: nivel 1, mazo inicial y semilla nueva (no cambia de escena)
    public static void NewRun()
    {
        Level = 1;
        Deck = null;
        seed = new System.Random().Next(1, int.MaxValue);
        RegistrarNivel();
    }

    // La primera vez crea el mazo con las bolas iniciales; después no hace nada
    public static void EnsureDeck(IEnumerable<TipoDeBola> starting)
    {
        if (Deck != null) return;

        Deck = new List<BallSlot>();
        foreach (var type in starting)
        {
            if (type == null) continue;
            Deck.Add(new BallSlot(type, type.isUpgrade ? 1 : 0));
        }
    }

    // 1) La mejora reemplaza a UNA bola Normal (nunca pisa otra mejora).
    // 2) Si no quedan Normales, sube de nivel UNA bola que ya tenga esa misma mejora
    //    (la de menor nivel, para repartir). Las demás no cambian.
    // Devuelve false si no se pudo aplicar.
    public static bool TryUpgradeBall(TipoDeBola upgrade)
    {
        if (Deck == null || upgrade == null || !upgrade.isUpgrade) return false;

        for (int i = 0; i < Deck.Count; i++)
        {
            if (Deck[i].type.isUpgrade) continue;
            Deck[i] = new BallSlot(upgrade, 1);
            return true;
        }

        int best = -1;
        for (int i = 0; i < Deck.Count; i++)
        {
            if (Deck[i].type != upgrade) continue;
            if (best < 0 || Deck[i].level < Deck[best].level) best = i;
        }
        if (best < 0) return false;

        Deck[best] = new BallSlot(upgrade, Deck[best].level + 1);
        return true;
    }

    public static void NextLevel()
    {
        Level++;
        RegistrarNivel();
        Recargar();
    }

    // Reintentar después de perder: run nueva en la misma escena de juego
    public static void RestartRun()
    {
        NewRun();
        Recargar();
    }

    public static void GoToMenu()
    {
        Level = 1;
        Deck = null;
        FundidoEscena.Cargar(EscenaMenu);
    }

    static void RegistrarNivel()
    {
        if (Level <= MejorNivel) return;
        PlayerPrefs.SetInt(ClaveMejorNivel, Level);
        PlayerPrefs.Save();
    }

    // Recarga la escena de juego; el CargadorNivel genera el tablero según Level y Seed
    static void Recargar() => FundidoEscena.Cargar(SceneManager.GetActiveScene().name);
}