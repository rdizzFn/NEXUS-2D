using System;
using System.Collections.Generic;
using System.Linq;
using NexusTraining;

namespace NexusPOO.Game.Engine;

public sealed class EnemigoMapa
{
    public Personaje Entidad { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool Derrotado => !Entidad.EstaVivo;
    public DateTime NextAction { get; set; } = DateTime.UtcNow;
    public EnemigoMapa(Personaje entidad, int x, int y) { Entidad = entidad; X = x; Y = y; }
}

public sealed class GameWorld
{
    public NexusTraining.Nexus Estado { get; }
    public Zona[] Zonas { get; }
    public List<EnemigoMapa> Enemigos { get; } = new();
    public int ZonaActiva { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    private int previousX;
    private int previousY;
    private DateTime moveStarted = DateTime.MinValue;
    public float RenderX { get { float p=(float)Math.Clamp((DateTime.UtcNow-moveStarted).TotalMilliseconds/110.0,0,1); return previousX+(X-previousX)*p; } }
    public float RenderY { get { float p=(float)Math.Clamp((DateTime.UtcNow-moveStarted).TotalMilliseconds/110.0,0,1); return previousY+(Y-previousY)*p; } }
    public int EntidadesDerrotadas { get => Estado.EntidadesDerrotadas; }
    public bool GameOver => !Estado.Cadete.EstaVivo;
    public bool Victoria { get; private set; }
    public int Movimientos { get; private set; }
    public int Ataques { get; private set; }
    private int tiempoAnterior;
    public DateTime InicioSesion { get; } = DateTime.Now;
    public int TiempoSegundos => tiempoAnterior + (int)(DateTime.Now - InicioSesion).TotalSeconds;
    public string Mensaje { get; private set; } = "SEÑAL NEXUS ESTABLE. EXPLORA EL SECTOR.";
    public string Dificultad { get; }
    public DateTime EfectoAtaqueHasta { get; private set; } = DateTime.MinValue;
    public int EfectoAtaqueX { get; private set; }
    public int EfectoAtaqueY { get; private set; }
    public bool MostrarEfectoAtaque => DateTime.UtcNow < EfectoAtaqueHasta;
    public Personaje? ObjetivoCombate => Enemigos.Where(e => !e.Derrotado).OrderBy(e => Distancia(e.X, e.Y, X, Y)).FirstOrDefault()?.Entidad;

    public GameWorld(string nombre, int edad, string realidad, string dificultad)
    {
        Dificultad = dificultad;
        double energia = dificultad == "Difícil" ? 85 : 100;
        double estabilidad = dificultad == "Difícil" ? 85 : 100;
        var cadete = new Cadete(nombre, edad, realidad, energia, estabilidad);
        Estado = new NexusTraining.Nexus(cadete);
        Zonas = Zona.Todas();
        foreach (var zona in Zonas) zona.Preparar();
        ZonaActiva = 0;
        X = 21; Y = 4; previousX=X; previousY=Y; moveStarted=DateTime.UtcNow.AddMilliseconds(-120);
        PrepararEnemigos();
    }

    public GameWorld(NexusTraining.Nexus estado, Zona[] zonas, int zonaActiva, int x, int y, string dificultad, List<EnemigoMapa>? enemigosGuardados = null, int tiempoGuardado = 0, int movimientosGuardados = 0, int ataquesGuardados = 0)
    {
        Estado = estado; Zonas = zonas; ZonaActiva = Math.Clamp(zonaActiva, 0, zonas.Length - 1);
        Victoria = estado.MisionPrincipal.Completada;
        foreach (var zona in Zonas) if (zona.Mapa == null) zona.Preparar();
        X = Math.Clamp(x, 0, Zona.Ancho - 1); Y = Math.Clamp(y, 0, Zona.Alto - 1); previousX=X; previousY=Y; moveStarted=DateTime.UtcNow.AddMilliseconds(-120);
        Dificultad = dificultad;
        tiempoAnterior = Math.Max(0, tiempoGuardado); Movimientos = Math.Max(0, movimientosGuardados); Ataques = Math.Max(0, ataquesGuardados);
        if (enemigosGuardados == null) PrepararEnemigos();
        else Enemigos.AddRange(enemigosGuardados);
    }

    public Zona ZonaActual => Zonas[ZonaActiva];
    public char TerrenoActual => ZonaActual.Mapa[Y, X];

    private void PrepararEnemigos()
    {
        Enemigos.Clear();
        int nivel = Estado.Nivel + (Dificultad == "Difícil" ? 2 : 0);
        Enemigos.Add(new EnemigoMapa(Entidades.CrearEnemigo(nivel), 15, 12));
        var robotInicial=Entidades.CrearRobot(nivel); robotInicial.ActivarSistemaDefensivo();
        Enemigos.Add(new EnemigoMapa(robotInicial, 25, 7));
        Enemigos.Add(new EnemigoMapa(Entidades.CrearEnemigo(nivel), 8, 15));
        if (Estado.MisionPrincipal.Pendiente?.Clave == "entidad")
            Enemigos.Add(new EnemigoMapa(Entidades.CrearGuardian(nivel), 25, 6));
    }


    public void Tick()
    {
        if (GameOver || Victoria) return;
        var now = DateTime.UtcNow;
        foreach (var enemy in Enemigos.Where(e => !e.Derrotado))
        {
            if (now < enemy.NextAction) continue;
            int distance = Distancia(enemy.X, enemy.Y, X, Y);
            if (distance <= 1)
            {
                double damage = enemy.Entidad is Enemigo en ? en.Atacar(Estado.Cadete) : enemy.Entidad is Robot robot ? AtaqueRobot(robot) : 0;
                Mensaje = $"{enemy.Entidad.Nombre} TE ALCANZA. -{damage:0} ENERGÍA.";
                enemy.NextAction = now.AddMilliseconds(1200);
                if (GameOver) { Mensaje = "SEÑAL PERDIDA. EL CADETE HA CAÍDO. PULSA R PARA REINICIAR."; break; }
            }
            else if (distance <= 6)
            {
                int dx = Math.Sign(X - enemy.X), dy = Math.Sign(Y - enemy.Y);
                int nx = enemy.X + (Math.Abs(X-enemy.X) >= Math.Abs(Y-enemy.Y) ? dx : 0);
                int ny = enemy.Y + (Math.Abs(Y-enemy.Y) > Math.Abs(X-enemy.X) ? dy : 0);
                if (nx >= 0 && nx < Zona.Ancho && ny >= 0 && ny < Zona.Alto && ZonaActual.Mapa[ny,nx] is not ('?' or '▓')) { enemy.X=nx; enemy.Y=ny; }
                enemy.NextAction = now.AddMilliseconds(500);
            }
        }
    }

    public void Mover(int dx, int dy)
    {
        if (GameOver || Victoria) return;
        int nx = X + dx, ny = Y + dy;
        if (nx < 0 || nx >= Zona.Ancho || ny < 0 || ny >= Zona.Alto) return;
        char tile = ZonaActual.Mapa[ny, nx];
        if (tile is '?' or '▓') { Mensaje = "TERRENO INTRANSITABLE. BUSCA OTRA RUTA."; return; }
        int costoTerreno = tile == '^' ? 3 : tile == '~' ? 2 : tile == '#' || tile == '≈' ? 1 : 0;
        if (costoTerreno > 0 && Estado.Cadete.Energia <= costoTerreno) { Mensaje = "ENERGÍA INSUFICIENTE PARA CRUZAR ESTE TERRENO. USA UNA CÉLULA."; return; }
        if (costoTerreno > 0) Estado.Cadete.PerderEnergia(costoTerreno);
        previousX=X; previousY=Y; X = nx; Y = ny; moveStarted=DateTime.UtcNow; Movimientos++;
        if (tile == '●' || tile == '◉')
        {
            if (Enemigos.All(e => e.Derrotado || Distancia(e.X, e.Y, X, Y) > 2))
            {
                var entidad = Entidades.CrearAleatoria(Estado.Nivel);
                if(entidad is Robot robotNuevo) robotNuevo.ActivarSistemaDefensivo();
                Enemigos.Add(new EnemigoMapa(entidad, X, Y));
                ZonaActual.Mapa[Y, X] = '.';
                Mensaje = $"ALERTA: {entidad.Nombre} DETECTADO. PRESIONA ESPACIO PARA ATACAR.";
            }
        }
        else Mensaje = $"POSICIÓN {ZonaActual.Nombre} // X:{X:00} Y:{Y:00}";
    }

    public void Atacar()
    {
        if (GameOver || Victoria) return;
        Ataques++;
        var enemy = Enemigos.Where(e => !e.Derrotado && Distancia(e.X, e.Y, X, Y) <= 2)
            .OrderBy(e => Distancia(e.X, e.Y, X, Y)).FirstOrDefault();
        if (enemy == null) { Mensaje = "NO HAY ENTIDADES EN RANGO DE ATAQUE."; return; }
        EfectoAtaqueX=enemy.X; EfectoAtaqueY=enemy.Y; EfectoAtaqueHasta=DateTime.UtcNow.AddMilliseconds(360);
        double dano = Estado.Cadete.Atacar(enemy.Entidad);
        Mensaje = $"IMPACTO CONFIRMADO: {enemy.Entidad.Nombre} RECIBE {dano:0} DE DAÑO.";
        if (enemy.Derrotado)
        {
            Estado.EntidadesDerrotadas++;
            int xpGanada = enemy.Entidad is Enemigo e ? (e.EsJefe ? 60 : 20) : 15;
            GanarExperiencia(xpGanada);
            Mensaje = $"ENTIDAD ELIMINADA: {enemy.Entidad.Nombre}. +{xpGanada} XP.";
            if (Estado.MisionPrincipal.Pendiente?.Clave == "entidad" && enemy.Entidad is Enemigo { EsJefe: true })
            {
                Estado.MisionPrincipal.Notificar("entidad");
                Mensaje = "OBJETIVO COMPLETADO: ENCUENTRO SOBREVIVIDO. REGRESA A EXTRACCIÓN.";
            }
            ComprobarMision();
            IntentarBotin(enemy.Entidad is Enemigo jefeDerrotado && jefeDerrotado.EsJefe);
            return;
        }
        TurnoEnemigo(enemy);
    }

    public void HabilidadEspecial()
    {
        if(GameOver || Victoria) return;
        if(!Estado.ResonanciaActivada) { Mensaje="HABILIDAD BLOQUEADA. RECUPERA EL NÚCLEO DE RESONANCIA."; return; }
        if(Estado.Cadete.Estabilidad<20) { Mensaje="ESTABILIDAD INSUFICIENTE. SE REQUIEREN 20 PUNTOS."; return; }
        var target=Enemigos.Where(e=>!e.Derrotado && Distancia(e.X,e.Y,X,Y)<=4).OrderBy(e=>Distancia(e.X,e.Y,X,Y)).FirstOrDefault();
        if(target==null) { Mensaje="NO HAY ENTIDADES EN EL RADIO DE RESONANCIA."; return; }
        Estado.Cadete.PerderEstabilidad(20); Ataques++;
        EfectoAtaqueX=target.X;EfectoAtaqueY=target.Y;EfectoAtaqueHasta=DateTime.UtcNow.AddMilliseconds(500);
        double damage=target.Entidad.RecibirDano(35+Estado.Nivel*5);
        Mensaje=$"PULSO NEXUS: {target.Entidad.Nombre} RECIBE {damage:0} DE DAÑO. -20 ESTABILIDAD.";
        if(target.Derrotado)
        {
            Estado.EntidadesDerrotadas++;GanarExperiencia(target.Entidad is Enemigo jefe ? (jefe.EsJefe ? 60 : 20) : 15);
            if(Estado.MisionPrincipal.Pendiente?.Clave=="entidad" && target.Entidad is Enemigo { EsJefe:true }) Estado.MisionPrincipal.Notificar("entidad");
            Mensaje=$"PULSO NEXUS: {target.Entidad.Nombre} ELIMINADO.";ComprobarMision();
            IntentarBotin(target.Entidad is Enemigo jefeDerrotado && jefeDerrotado.EsJefe);
        }
    }

    private void IntentarBotin(bool esJefe)
    {
        if(!esJefe && Juego.Azar.Next(100)>=50) return;
        var botin=Objeto.Aleatorio();
        if(Estado.Inventario.Agregar(botin)) Mensaje += $"  // BOTÍN: {botin.Icono} {botin.Nombre}.";
        else Mensaje += "  // BOTÍN DETECTADO, PERO EL INVENTARIO ESTÁ LLENO.";
    }

    private double AtaqueRobot(Robot robot)
    {
        double damage=robot.Atacar(Estado.Cadete);
        if(!robot.DefensaActiva && Juego.Azar.Next(100)<55) robot.ActivarSistemaDefensivo();
        return damage;
    }

    private void TurnoEnemigo(EnemigoMapa enemy)
    {
        enemy.NextAction = DateTime.UtcNow.AddMilliseconds(1200);
        double damage;
        if (enemy.Entidad is Enemigo enemigo) damage = enemigo.Atacar(Estado.Cadete);
        else if (enemy.Entidad is Robot robot) damage = AtaqueRobot(robot);
        else damage = enemy.Entidad.RecibirDano(0);
        Mensaje += $"  // CONTRAATAQUE: -{damage:0} ENERGÍA.";
        if (GameOver) Mensaje = "SEÑAL PERDIDA. EL CADETE HA CAÍDO. PULSA R PARA REINICIAR.";
    }

    public void Interactuar()
    {
        if (GameOver || Victoria) return;
        char tile = TerrenoActual;
        if (tile == '⚠')
        {
            if (Estado.MisionPrincipal.Pendiente?.Clave == "anomalia")
            {
                Estado.Cadete.AnalizarAnomalia();
                Estado.AgregarAnomalia();
                Estado.MisionPrincipal.Notificar("anomalia");
                ZonaActual.Mapa[Y, X] = '.';
                Mensaje = "ANOMALÍA ANALIZADA. DATOS SINCRONIZADOS CON NEXUS.";
            }
            else Mensaje = "ANOMALÍA REGISTRADA. NO HAY NUEVAS ACCIONES DISPONIBLES.";
        }
        else if (tile == '◆')
        {
            if (Estado.MisionPrincipal.Pendiente?.Clave == "nucleo")
            {
                if (Estado.Inventario.Agregar(Objeto.Crear(TipoObjeto.NucleoResonancia)))
                {
                    Estado.MisionPrincipal.Notificar("nucleo");
                    Estado.ActivarResonancia();
                    ZonaActual.Mapa[Y, X] = '.';
                    Mensaje = "NÚCLEO DE RESONANCIA RECUPERADO. NUEVA SEÑAL DETECTADA.";
                    if (Enemigos.All(e => e.Entidad is not Enemigo en || !en.EsJefe))
                        Enemigos.Add(new EnemigoMapa(Entidades.CrearGuardian(Estado.Nivel), 25, 6));
                }
                else Mensaje = "INVENTARIO LLENO. LIBERA UN ESPACIO.";
            }
            else Mensaje = "RECURSO ENERGÉTICO SINCRONIZADO.";
        }
        else if (tile == '⌂')
        {
            if (Estado.MisionPrincipal.Pendiente?.Clave == "extraccion")
            {
                Estado.MisionPrincipal.Notificar("extraccion");
                ComprobarMision();
            }
            else Mensaje = "PUNTO DE EXTRACCIÓN. COMPLETA LOS OBJETIVOS ANTES DE REGRESAR.";
        }
        else if (tile == '✦')
        {
            Estado.Cadete.AgregarEnergia(15); Estado.Cadete.AumentarEstabilidad(10);
            GanarExperiencia(10); ZonaActual.Mapa[Y, X] = '.'; Mensaje = "PULSO DE RESONANCIA: +15 ENERGÍA, +10 ESTABILIDAD, +10 XP.";
        }
        else if (tile == '◇')
        {
            GanarExperiencia(10); ZonaActual.Mapa[Y, X] = '.'; Mensaje = "SEÑAL DE SEGURIDAD DESCIFRADA. +10 XP.";
        }
        else Mensaje = "NO HAY NADA CON LO QUE INTERACTUAR AQUÍ.";
        ComprobarMision();
    }

    private void GanarExperiencia(int cantidad)
    {
        int rangoAnterior=Estado.Cadete.Rango;
        Estado.Cadete.GanarXp(cantidad);
        int rangosNuevos=Estado.Cadete.Rango-rangoAnterior;
        for(int i=0;i<rangosNuevos;i++)
        {
            Estado.SubirNivel(); Estado.Cadete.AgregarEnergia(10); Estado.Cadete.AumentarEstabilidad(5);
            Mensaje=$"¡RANGO {Estado.Cadete.Rango} ALCANZADO! NIVEL {Estado.Nivel}. +10 ENERGÍA Y +5 ESTABILIDAD.";
        }
    }

    private void ComprobarMision()
    {
        if (Estado.MisionPrincipal.Completada && !Estado.MisionPrincipal.RecompensaEntregada)
        {
            Estado.MisionPrincipal.RecompensaEntregada = true;
            GanarExperiencia(Estado.MisionPrincipal.RecompensaXp);
            Victoria = true;
            Mensaje = "MISIÓN COMPLETADA. LA ANOMALÍA PERDIDA HA SIDO CONTENIDA. +100 XP.";
        }
    }

    public void UsarObjeto(int slot)
    {
        var items = Estado.Inventario.Lista;
        if (slot < 0 || slot >= items.Count) { Mensaje = "ESPACIO DE INVENTARIO VACÍO."; return; }
        var item = items[slot]; bool consumed = false;
        switch (item.Tipo)
        {
            case TipoObjeto.CelulaEnergia:
                if (Estado.Cadete.Energia < Estado.Cadete.EnergiaMaxima) { Estado.Cadete.AgregarEnergia(25); consumed = true; Mensaje = "CÉLULA UTILIZADA: +25 ENERGÍA."; }
                else Mensaje = "ENERGÍA AL MÁXIMO. NO SE CONSUMIÓ LA CÉLULA.";
                break;
            case TipoObjeto.Escudo:
                if (Estado.Cadete.Escudo < 50) { Estado.Cadete.ActivarEscudo(25); consumed = true; Mensaje = "ESCUDO ACTIVADO: +25 PROTECCIÓN."; }
                else Mensaje = "ESCUDO AL MÁXIMO.";
                break;
            case TipoObjeto.ModuloAtaque:
                if (Estado.Cadete.CargasModulo < 6) { Estado.Cadete.CargarModulo(3); consumed = true; Mensaje = "MÓDULO CARGADO: +10 DAÑO DURANTE 3 ATAQUES."; }
                break;
            case TipoObjeto.Analizador:
                var target = Enemigos.Where(e => !e.Derrotado && Distancia(e.X, e.Y, X, Y) <= 3).OrderBy(e => Distancia(e.X, e.Y, X, Y)).FirstOrDefault();
                if (target != null) { target.Entidad.PuntoDebilExpuesto = true; consumed = true; Mensaje = $"ANALIZADOR: PUNTO DÉBIL DE {target.Entidad.Nombre} EXPUESTO."; }
                else Mensaje = "NO HAY OBJETIVOS CERCANOS PARA ANALIZAR.";
                break;
            case TipoObjeto.ModuloDesconocido:
                Estado.Cadete.AgregarEnergia(30); Estado.Cadete.AumentarEstabilidad(15); consumed = true; Mensaje = "MÓDULO DESCONOCIDO: +30 ENERGÍA, +15 ESTABILIDAD.";
                break;
            case TipoObjeto.NucleoResonancia:
                Mensaje = "EL NÚCLEO DE RESONANCIA ES UN OBJETIVO DE MISIÓN.";
                break;
        }
        if (consumed) Estado.Inventario.Eliminar(item);
    }

    public void CambiarZona(int indice)
    {
        if (indice < 0 || indice >= Zonas.Length) return;
        ZonaActiva = indice; previousX=2; previousY=14; X = 2; Y = 14; moveStarted=DateTime.UtcNow.AddMilliseconds(-120); Mensaje = $"ENTRANDO EN {ZonaActual.Nombre}: {ZonaActual.Titulo}.";
    }

    public static int Distancia(int x1, int y1, int x2, int y2) => Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
}
