using System;
using System.Collections.Generic;
using UnityEngine;

// Un bloque de un nivel: en qué celda de la cuadrícula está y cuánta vida tiene.
[Serializable]
public struct BloqueDef
{
    [Tooltip("x = columna (0 = izquierda), y = fila (0 = la de más arriba)")]
    public Vector2Int celda;
    [Min(1)] public int vida;

    public BloqueDef(int x, int y, int vida)
    {
        celda = new Vector2Int(x, y);
        this.vida = vida;
    }
}

// Un nivel hecho a mano (o producido por una herramienta, como el futuro "dibujar tu mapa").
// Crear: clic derecho > Create > Roguelike > Nivel. Se asigna en CargadorNivel > Niveles Fijos.
[CreateAssetMenu(menuName = "Roguelike/Nivel")]
public class NivelData : ScriptableObject
{
    [Min(1)] public int columnas = 10;
    [Min(1)] public int filas = 10;
    public List<BloqueDef> bloques = new();
}