using System;
using NexusTraining;
using NexusPOO.Game.Engine;
using System.Collections.Generic;

namespace NexusPOO.Game.Models;

public sealed class PartidaGuardada
{
    public int IdPartida { get; init; }
    public string Jugador { get; init; } = "";
    public int Nivel { get; init; }
    public string Estado { get; init; } = "";
    public DateTime? FechaGuardado { get; init; }
    public Nexus? Juego { get; init; }
    public int ZonaActiva { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public List<EnemigoMapa> Enemigos { get; init; } = new();
    public Zona[] Zonas { get; init; } = Array.Empty<Zona>();
    public int TiempoSegundos { get; init; }
    public int Movimientos { get; init; }
    public int Ataques { get; init; }
}
