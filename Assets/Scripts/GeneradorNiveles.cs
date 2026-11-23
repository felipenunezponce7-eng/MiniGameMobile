using System;
using System.Collections.Generic;
using UnityEngine;

// Genera la lista de bloques de un nivel a partir del número de nivel de la run.
// Elige un patrón (damero, pirámide, diamante, anillos...), lo hace simétrico y reparte la vida:
// más nivel = más filas ocupadas y bloques más resistentes. Con la misma semilla sale el mismo nivel.
public static class GeneradorNiveles
{
    const int VidaMaxima = 9;
    const int BloquesMinimos = 12;

    public static List<BloqueDef> Generar(int nivel, int columnas, int filas, int semilla)
    {
        var rng = new System.Random(semilla);
        nivel = Mathf.Max(1, nivel);
        columnas = Mathf.Max(3, columnas);
        filas = Mathf.Max(3, filas);

        // Más filas ocupadas con el nivel, dejando siempre espacio libre abajo para que la bola caiga
        int usadas = Mathf.Clamp(5 + nivel / 2, 4, Mathf.Max(4, filas - 2));
        usadas = Mathf.Min(usadas, filas);

        int patron = rng.Next(0, 7);
        int fase = rng.Next(0, 2);
        double densidad = 0.5 + rng.NextDouble() * 0.15;

        float cx = (columnas - 1) / 2f;
        float cy = (usadas - 1) / 2f;
        float mitadAncho = columnas / 2f;
        float mitadAlto = usadas / 2f;

        bool Ocupado(int x, int y)
        {
            switch (patron)
            {
                case 0: // damero
                    return (x + y + fase) % 2 == 0;
                case 1: // pirámide: se ensancha hacia abajo
                    return Mathf.Abs(x - cx) <= (y + 0.5f) * mitadAncho / usadas;
                case 2: // embudo: se estrecha hacia abajo
                    return Mathf.Abs(x - cx) <= (usadas - y - 0.5f) * mitadAncho / usadas;
                case 3: // diamante
                    return Mathf.Abs(x - cx) / mitadAncho + Mathf.Abs(y - cy) / mitadAlto <= 1f;
                case 4: // anillos concéntricos
                    {
                        float dx = x - cx;
                        float dy = y - cy;
                        int anillo = Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy));
                        return anillo % 2 == fase;
                    }
                case 5: // franjas diagonales
                    return (x + y) % 4 < 2;
                default: // disperso
                    return rng.NextDouble() < densidad;
            }
        }

        // Se calcula la mitad izquierda y se refleja: así el nivel siempre queda simétrico
        var celdas = new HashSet<Vector2Int>();
        int mitad = (columnas + 1) / 2;
        for (int y = 0; y < usadas; y++)
        {
            for (int x = 0; x < mitad; x++)
            {
                if (!Ocupado(x, y)) continue;
                celdas.Add(new Vector2Int(x, y));
                celdas.Add(new Vector2Int(columnas - 1 - x, y));
            }
        }

        // Si el patrón dejó muy pocos bloques, se completa con celdas al azar
        int objetivo = Mathf.Min(BloquesMinimos, columnas * usadas);
        int intentos = 0;
        while (celdas.Count < objetivo && intentos++ < 500)
            celdas.Add(new Vector2Int(rng.Next(0, columnas), rng.Next(0, usadas)));

        // Orden estable (fila y columna) para que la misma semilla dé siempre el mismo reparto de vida
        var ordenadas = new List<Vector2Int>(celdas);
        ordenadas.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

        int vidaBase = 1 + (nivel - 1) / 4;
        double probExtra = Math.Min(0.55, 0.08 + 0.04 * nivel);

        var resultado = new List<BloqueDef>(ordenadas.Count);
        foreach (var celda in ordenadas)
        {
            int vida = vidaBase;
            if (rng.NextDouble() < probExtra) vida += rng.Next(1, 3);
            resultado.Add(new BloqueDef(celda.x, celda.y, Mathf.Min(vida, VidaMaxima)));
        }

        return resultado;
    }
}