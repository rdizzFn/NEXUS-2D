using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using NexusPOO.Game.Models;
using NexusPOO.Game.Engine;
using NexusTraining;
using System.Data;

namespace NexusPOO.Game.Data;

public sealed class PartidaRepository
{
    public int Guardar(string nombreJugador, NexusTraining.Nexus juego, Zona[] zonas, int zonaActiva, int x, int y, int idPartida = 0, int tiempoSegundos = 0, int movimientos = 0, int ataques = 0, List<EnemigoMapa>? enemigos = null)
    {
        using var cn = ConexionBD.CrearConexion();
        cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            int idPerfil = ObtenerOCrearPerfil(cn, tx, nombreJugador);
            int idMision = ObtenerMision(cn, tx, juego.MisionPrincipal.Titulo);
            string estadoPartida = !juego.Cadete.EstaVivo ? "GAME_OVER" : juego.MisionPrincipal.Completada ? "COMPLETADA" : "PAUSADA";
            if (idPartida <= 0)
            {
                using var cmd = new SqlCommand(@"INSERT INTO Partida
                    (id_perfil,id_mision,nivel,anomalias,entidades_derrotadas,resonancia_activada,recompensa_entregada,fecha_guardado,estado)
                    OUTPUT INSERTED.id_partida
                    VALUES (@perfil,@mision,@nivel,@anomalias,@derrotadas,@resonancia,@recompensa,SYSDATETIME(),@estado);", cn, tx);
                cmd.Parameters.AddWithValue("@perfil", idPerfil);
                cmd.Parameters.AddWithValue("@mision", idMision);
                cmd.Parameters.AddWithValue("@nivel", juego.Nivel);
                cmd.Parameters.AddWithValue("@anomalias", juego.Anomalias);
                cmd.Parameters.AddWithValue("@derrotadas", juego.EntidadesDerrotadas);
                cmd.Parameters.AddWithValue("@resonancia", juego.ResonanciaActivada);
                cmd.Parameters.AddWithValue("@recompensa", juego.MisionPrincipal.RecompensaEntregada);
                cmd.Parameters.Add("@estado", SqlDbType.VarChar, 20).Value = estadoPartida;
                idPartida = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                using var cmd = new SqlCommand(@"UPDATE Partida SET id_mision=@mision,nivel=@nivel,anomalias=@anomalias,
                    entidades_derrotadas=@derrotadas,resonancia_activada=@resonancia,recompensa_entregada=@recompensa,
                    fecha_guardado=SYSDATETIME(),estado=@estado WHERE id_partida=@partida AND id_perfil=@perfil;", cn, tx);
                cmd.Parameters.AddWithValue("@mision", idMision);
                cmd.Parameters.AddWithValue("@nivel", juego.Nivel);
                cmd.Parameters.AddWithValue("@anomalias", juego.Anomalias);
                cmd.Parameters.AddWithValue("@derrotadas", juego.EntidadesDerrotadas);
                cmd.Parameters.AddWithValue("@resonancia", juego.ResonanciaActivada);
                cmd.Parameters.AddWithValue("@recompensa", juego.MisionPrincipal.RecompensaEntregada);
                cmd.Parameters.Add("@estado", SqlDbType.VarChar, 20).Value = estadoPartida;
                cmd.Parameters.AddWithValue("@partida", idPartida);
                cmd.Parameters.AddWithValue("@perfil", idPerfil);
                if (cmd.ExecuteNonQuery() == 0) throw new InvalidOperationException("La partida seleccionada no existe o no pertenece a este perfil.");
            }

            GuardarCadete(cn, tx, idPartida, juego.Cadete);
            GuardarInventario(cn, tx, idPartida, juego.Inventario);
            GuardarObjetivos(cn, tx, idPartida, juego.MisionPrincipal);
            GuardarZonas(cn, tx, idPartida, zonas, zonaActiva, x, y);
            GuardarEnemigos(cn, tx, idPartida, enemigos ?? new List<EnemigoMapa>());
            GuardarEstadisticas(cn, tx, idPartida, juego, tiempoSegundos, movimientos, ataques);
            GuardarLogros(cn, tx, idPerfil, juego);
            tx.Commit();
            return idPartida;
        }
        catch { tx.Rollback(); throw; }
    }

    public List<PartidaGuardada> Listar(string nombreJugador)
    {
        var partidas = new List<PartidaGuardada>();
        using var cn = ConexionBD.CrearConexion(); cn.Open();
        using var cmd = new SqlCommand(@"SELECT p.id_partida,p.nivel,p.estado,p.fecha_guardado
            FROM Partida p INNER JOIN PerfilJugador j ON j.id_perfil=p.id_perfil
            WHERE j.nombre=@nombre ORDER BY p.fecha_guardado DESC,p.id_partida DESC;", cn);
        cmd.Parameters.Add("@nombre", SqlDbType.NVarChar, 100).Value = nombreJugador;
        using var rd = cmd.ExecuteReader();
        while (rd.Read()) partidas.Add(new PartidaGuardada {
            IdPartida = rd.GetInt32(0), Nivel = rd.GetInt32(1), Estado = rd.GetString(2),
            FechaGuardado = rd.IsDBNull(3) ? null : rd.GetDateTime(3), Jugador = nombreJugador
        });
        return partidas;
    }

    public PartidaGuardada Cargar(int idPartida)
    {
        using var cn = ConexionBD.CrearConexion(); cn.Open();
        string jugador; int nivel, anomalias, derrotadas; bool resonancia, recompensa; string estado;
        using (var cmd = new SqlCommand(@"SELECT j.nombre,p.nivel,p.anomalias,p.entidades_derrotadas,p.resonancia_activada,
            p.recompensa_entregada,p.estado FROM Partida p INNER JOIN PerfilJugador j ON j.id_perfil=p.id_perfil
            WHERE p.id_partida=@id;", cn))
        {
            cmd.Parameters.AddWithValue("@id", idPartida);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) throw new InvalidOperationException("No se encontró la partida seleccionada.");
            jugador = rd.GetString(0); nivel = rd.GetInt32(1); anomalias = rd.GetInt32(2); derrotadas = rd.GetInt32(3);
            resonancia = rd.GetBoolean(4); recompensa = rd.GetBoolean(5); estado = rd.GetString(6);
        }

        string nombre; int edad, xp, cargas; string realidad; double energia, estabilidad, escudo;
        using (var cmd = new SqlCommand(@"SELECT nombre,edad,realidad,energia,estabilidad,xp,escudo,cargas_modulo
            FROM EstadoCadete WHERE id_partida=@id;", cn))
        {
            cmd.Parameters.AddWithValue("@id", idPartida); using var rd = cmd.ExecuteReader();
            if (!rd.Read()) throw new InvalidOperationException("La partida no contiene un estado de cadete.");
            nombre=rd.GetString(0); edad=rd.GetInt32(1); realidad=rd.GetString(2); energia=Convert.ToDouble(rd.GetDecimal(3));
            estabilidad=Convert.ToDouble(rd.GetDecimal(4)); xp=rd.GetInt32(5); escudo=Convert.ToDouble(rd.GetDecimal(6)); cargas=rd.GetInt32(7);
        }
        var cadete = new Cadete(nombre, edad, realidad, energia, estabilidad);
        cadete.GanarXp(xp); cadete.ActivarEscudo(escudo); cadete.CargarModulo(cargas);
        var juego = new NexusTraining.Nexus(cadete);
        for (int i=1;i<nivel;i++) juego.SubirNivel();
        for (int i=0;i<anomalias;i++) juego.AgregarAnomalia();
        juego.EntidadesDerrotadas=derrotadas;
        if (resonancia) juego.ActivarResonancia();
        juego.MisionPrincipal.RecompensaEntregada=recompensa;

        foreach (var oldItem in juego.Inventario.Lista) juego.Inventario.Eliminar(oldItem);
        foreach (var item in CargarObjetosInventario(cn, idPartida)) juego.Inventario.Agregar(item);
        using (var cmd = new SqlCommand(@"SELECT o.clave,p.completado FROM ProgresoObjetivo p
            INNER JOIN ObjetivoMision o ON o.id_objetivo=p.id_objetivo WHERE p.id_partida=@id;", cn))
        {
            cmd.Parameters.AddWithValue("@id", idPartida); using var rd=cmd.ExecuteReader();
            while(rd.Read()) { var objetivo=juego.MisionPrincipal.Objetivos.FirstOrDefault(o=>o.Clave==rd.GetString(0)); if(objetivo!=null) objetivo.Completado=rd.GetBoolean(1); }
        }
        var zonas=Zona.Todas(); foreach(var z in zonas) z.Preparar();
        int zonaActiva=0, px=21, py=4;
        using (var cmd = new SqlCommand(@"SELECT z.nombre,p.fila_actual,p.columna_actual,p.descubierta,p.mapa_estado FROM ProgresoZona p
            INNER JOIN Zona z ON z.id_zona=p.id_zona WHERE p.id_partida=@id;", cn))
        {
            cmd.Parameters.AddWithValue("@id", idPartida); using var rd=cmd.ExecuteReader();
            while(rd.Read())
            {
                int idx=Array.FindIndex(zonas,z=>z.Nombre==rd.GetString(0)); if(idx<0) continue;
                if(!rd.IsDBNull(4)) RestaurarMapa(zonas[idx],rd.GetString(4));
                if(rd.GetBoolean(3)) { zonaActiva=idx; py=rd.GetInt32(1); px=rd.GetInt32(2); }
            }
        }
        var enemigos = new List<EnemigoMapa>();
        using (var cmd = new SqlCommand(@"SELECT nombre,tipo,energia_actual,energia_maxima,ataque,habilidad,es_jefe,defensa_activa,punto_debil,x,y
            FROM EnemigoPartida WHERE id_partida=@id ORDER BY id_enemigo_partida;", cn))
        {
            cmd.Parameters.AddWithValue("@id", idPartida); using var rd=cmd.ExecuteReader();
            while(rd.Read())
            {
                string enemyName=rd.GetString(0), tipo=rd.GetString(1); double hp=Convert.ToDouble(rd.GetDecimal(2)), maxHp=Convert.ToDouble(rd.GetDecimal(3));
                int attack=rd.GetInt32(4); string ability=rd.GetString(5); bool boss=rd.GetBoolean(6), defense=rd.GetBoolean(7), weak=rd.GetBoolean(8); int ex=rd.GetInt32(9), ey=rd.GetInt32(10);
                Personaje? entity = tipo=="ROBOT" ? new Robot(enemyName,hp,maxHp,attack,defense) : new Enemigo(enemyName,hp,maxHp,attack,ability,boss,weak);
                if(entity!=null) { entity.PuntoDebilExpuesto=weak; enemigos.Add(new EnemigoMapa(entity,ex,ey)); }
            }
        }
        int tiempo=0,movimientos=0,ataques=0;
        using (var cmd = new SqlCommand("SELECT tiempo_segundos,movimientos,ataques FROM EstadisticaPartida WHERE id_partida=@id;",cn))
        {
            cmd.Parameters.AddWithValue("@id",idPartida);using var rd=cmd.ExecuteReader();
            if(rd.Read()){tiempo=rd.GetInt32(0);movimientos=rd.GetInt32(1);ataques=rd.GetInt32(2);}
        }
        return new PartidaGuardada { IdPartida=idPartida, Jugador=jugador, Nivel=nivel, Estado=estado, Juego=juego, ZonaActiva=zonaActiva, X=px, Y=py, Enemigos=enemigos, Zonas=zonas, TiempoSegundos=tiempo, Movimientos=movimientos, Ataques=ataques };
    }

    public List<(string Titulo, string Descripcion, DateTime Fecha)> ListarLogros(string nombreJugador)
    {
        var result=new List<(string Titulo,string Descripcion,DateTime Fecha)>();
        using var cn=ConexionBD.CrearConexion();cn.Open();
        using var cmd=new SqlCommand(@"SELECT l.titulo,l.descripcion,lj.fecha_obtencion FROM LogroJugador lj
            INNER JOIN Logro l ON l.id_logro=lj.id_logro INNER JOIN PerfilJugador p ON p.id_perfil=lj.id_perfil
            WHERE p.nombre=@nombre ORDER BY lj.fecha_obtencion DESC;",cn);
        cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,100).Value=nombreJugador;
        using var rd=cmd.ExecuteReader();while(rd.Read()) result.Add((rd.GetString(0),rd.GetString(1),rd.GetDateTime(2)));
        return result;
    }

    public void Eliminar(int idPartida, string nombreJugador)
    {
        using var cn=ConexionBD.CrearConexion(); cn.Open();
        using var cmd=new SqlCommand(@"DELETE p FROM Partida p INNER JOIN PerfilJugador j ON j.id_perfil=p.id_perfil
            WHERE p.id_partida=@id AND j.nombre=@nombre;",cn);
        cmd.Parameters.AddWithValue("@id",idPartida); cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,100).Value=nombreJugador;
        if(cmd.ExecuteNonQuery()==0) throw new InvalidOperationException("No se eliminó ninguna partida. Comprueba el perfil y el identificador.");
    }

    private static int ObtenerOCrearPerfil(SqlConnection cn, SqlTransaction tx, string nombre)
    {
        using var cmd=new SqlCommand("SELECT id_perfil FROM PerfilJugador WHERE nombre=@nombre;",cn,tx);
        cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,100).Value=nombre;
        var id=cmd.ExecuteScalar(); if(id!=null) return Convert.ToInt32(id);
        using var ins=new SqlCommand("INSERT INTO PerfilJugador(nombre) OUTPUT INSERTED.id_perfil VALUES(@nombre);",cn,tx);
        ins.Parameters.Add("@nombre",SqlDbType.NVarChar,100).Value=nombre; return Convert.ToInt32(ins.ExecuteScalar());
    }
    private static int ObtenerMision(SqlConnection cn, SqlTransaction tx, string titulo)
    {
        using var cmd=new SqlCommand("SELECT id_mision FROM Mision WHERE titulo=@titulo;",cn,tx);
        cmd.Parameters.Add("@titulo",SqlDbType.NVarChar,150).Value=titulo;
        return Convert.ToInt32(cmd.ExecuteScalar() ?? throw new InvalidOperationException("No existe la misión principal en la base de datos. Ejecuta primero los datos iniciales."));
    }
    private static void GuardarCadete(SqlConnection cn,SqlTransaction tx,int id,Cadete c)
    {
        using var cmd=new SqlCommand(@"IF EXISTS(SELECT 1 FROM EstadoCadete WHERE id_partida=@id)
            UPDATE EstadoCadete SET nombre=@nombre,edad=@edad,realidad=@realidad,energia=@energia,energia_maxima=@max,
            estabilidad=@estabilidad,xp=@xp,escudo=@escudo,cargas_modulo=@cargas WHERE id_partida=@id
            ELSE INSERT INTO EstadoCadete(id_partida,nombre,edad,realidad,energia,energia_maxima,estabilidad,xp,escudo,cargas_modulo)
            VALUES(@id,@nombre,@edad,@realidad,@energia,@max,@estabilidad,@xp,@escudo,@cargas);",cn,tx);
        cmd.Parameters.AddWithValue("@id",id); cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,100).Value=c.Nombre;
        cmd.Parameters.AddWithValue("@edad",c.Edad); cmd.Parameters.Add("@realidad",SqlDbType.NVarChar,100).Value=c.Realidad;
        cmd.Parameters.AddWithValue("@energia",c.Energia); cmd.Parameters.AddWithValue("@max",c.EnergiaMaxima);
        cmd.Parameters.AddWithValue("@estabilidad",c.Estabilidad); cmd.Parameters.AddWithValue("@xp",c.Xp);
        cmd.Parameters.AddWithValue("@escudo",c.Escudo); cmd.Parameters.AddWithValue("@cargas",c.CargasModulo); cmd.ExecuteNonQuery();
    }
    private static void GuardarInventario(SqlConnection cn,SqlTransaction tx,int id,Inventario inv)
    {
        using(var del=new SqlCommand("DELETE FROM InventarioPartida WHERE id_partida=@id;",cn,tx)){del.Parameters.AddWithValue("@id",id);del.ExecuteNonQuery();}
        int slot=1; foreach(var item in inv.Lista)
        {
            using var cmd=new SqlCommand(@"INSERT INTO InventarioPartida(id_partida,id_objeto,numero_espacio)
                SELECT @partida,id_objeto,@slot FROM Objeto WHERE tipo=@tipo;",cn,tx);
            cmd.Parameters.AddWithValue("@partida",id);cmd.Parameters.AddWithValue("@slot",slot++);cmd.Parameters.Add("@tipo",SqlDbType.VarChar,30).Value=item.Tipo.ToString();
            if(cmd.ExecuteNonQuery()==0) throw new InvalidOperationException($"No existe en SQL el objeto {item.Tipo}. Ejecuta los datos iniciales.");
        }
    }
    private static void GuardarObjetivos(SqlConnection cn,SqlTransaction tx,int id,Mision m)
    {
        foreach(var o in m.Objetivos)
        {
            using var cmd=new SqlCommand(@"UPDATE ProgresoObjetivo SET completado=@hecho WHERE id_partida=@partida
                AND id_objetivo=(SELECT id_objetivo FROM ObjetivoMision WHERE id_mision=(SELECT id_mision FROM Partida WHERE id_partida=@partida) AND clave=@clave);
                IF @@ROWCOUNT=0 INSERT INTO ProgresoObjetivo(id_partida,id_objetivo,completado)
                SELECT @partida,id_objetivo,@hecho FROM ObjetivoMision WHERE clave=@clave
                AND id_mision=(SELECT id_mision FROM Partida WHERE id_partida=@partida);",cn,tx);
            cmd.Parameters.AddWithValue("@partida",id);cmd.Parameters.AddWithValue("@hecho",o.Completado);cmd.Parameters.Add("@clave",SqlDbType.VarChar,50).Value=o.Clave;cmd.ExecuteNonQuery();
        }
    }
    private static void GuardarZonas(SqlConnection cn,SqlTransaction tx,int id,Zona[] zonas,int activa,int x,int y)
    {
        for(int i=0;i<zonas.Length;i++)
        {
            var z=zonas[i]; int zx=i==activa?x:0, zy=i==activa?y:0;
            using var cmd=new SqlCommand(@"UPDATE ProgresoZona SET fila_actual=@fila,columna_actual=@col,cartografia_actual=@cart,
                lecturas_realizadas=@lecturas,senales_encontradas=@senales,estado_actual=@estado,descubierta=@desc,mapa_estado=@mapa
                WHERE id_partida=@partida AND id_zona=(SELECT id_zona FROM Zona WHERE nombre=@nombre);
                IF @@ROWCOUNT=0 INSERT INTO ProgresoZona(id_partida,id_zona,fila_actual,columna_actual,cartografia_actual,
                lecturas_realizadas,senales_encontradas,estado_actual,descubierta,mapa_estado)
                SELECT @partida,id_zona,@fila,@col,@cart,@lecturas,@senales,@estado,@desc,@mapa FROM Zona WHERE nombre=@nombre;",cn,tx);
            cmd.Parameters.AddWithValue("@fila",zy);cmd.Parameters.AddWithValue("@col",zx);cmd.Parameters.AddWithValue("@cart",z.Cartografia);
            cmd.Parameters.AddWithValue("@lecturas",z.Lecturas);cmd.Parameters.AddWithValue("@senales",z.Senales);
            cmd.Parameters.Add("@estado",SqlDbType.NVarChar,30).Value=z.Estado;cmd.Parameters.AddWithValue("@desc",i==activa);
            cmd.Parameters.Add("@mapa",SqlDbType.NVarChar,-1).Value=SerializarMapa(z);
            cmd.Parameters.AddWithValue("@partida",id);cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,30).Value=z.Nombre;
            if(cmd.ExecuteNonQuery()==0) throw new InvalidOperationException($"La zona {z.Nombre} no existe en SQL Server.");
        }
    }
    private static string SerializarMapa(Zona zona)
    {
        var filas=new string[Zona.Alto];
        for(int y=0;y<Zona.Alto;y++) { var chars=new char[Zona.Ancho]; for(int x=0;x<Zona.Ancho;x++) chars[x]=zona.Mapa[y,x]; filas[y]=new string(chars); }
        return string.Join("|",filas);
    }
    private static void RestaurarMapa(Zona zona,string datos)
    {
        var filas=datos.Split('|');
        for(int y=0;y<Math.Min(filas.Length,Zona.Alto);y++)
            for(int x=0;x<Math.Min(filas[y].Length,Zona.Ancho);x++) zona.Mapa[y,x]=filas[y][x];
    }

    private static void GuardarEnemigos(SqlConnection cn,SqlTransaction tx,int id,List<EnemigoMapa> enemigos)
    {
        using(var del=new SqlCommand("DELETE FROM EnemigoPartida WHERE id_partida=@id;",cn,tx)){del.Parameters.AddWithValue("@id",id);del.ExecuteNonQuery();}
        foreach(var item in enemigos.Where(e=>!e.Derrotado))
        {
            string tipo; int ataque; string habilidad; bool jefe=false,defensa=false;
            if(item.Entidad is Robot robot) { tipo="ROBOT"; ataque=robot.Descarga; habilidad="NINGUNA"; defensa=robot.DefensaActiva; }
            else if(item.Entidad is Enemigo enemigo) { tipo="ENEMIGO"; ataque=enemigo.Ataque; habilidad=enemigo.Habilidad; jefe=enemigo.EsJefe; }
            else continue;
            using var cmd=new SqlCommand(@"INSERT INTO EnemigoPartida(id_partida,nombre,tipo,energia_actual,energia_maxima,ataque,habilidad,es_jefe,defensa_activa,punto_debil,x,y)
                VALUES(@partida,@nombre,@tipo,@energia,@max,@ataque,@habilidad,@jefe,@defensa,@debil,@x,@y);",cn,tx);
            cmd.Parameters.AddWithValue("@partida",id);cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,100).Value=item.Entidad.Nombre;
            cmd.Parameters.Add("@tipo",SqlDbType.VarChar,10).Value=tipo;cmd.Parameters.AddWithValue("@energia",item.Entidad.Energia);
            cmd.Parameters.AddWithValue("@max",item.Entidad.EnergiaMaxima);cmd.Parameters.AddWithValue("@ataque",ataque);
            cmd.Parameters.Add("@habilidad",SqlDbType.VarChar,30).Value=habilidad;cmd.Parameters.AddWithValue("@jefe",jefe);
            cmd.Parameters.AddWithValue("@defensa",defensa);cmd.Parameters.AddWithValue("@debil",item.Entidad.PuntoDebilExpuesto);
            cmd.Parameters.AddWithValue("@x",item.X);cmd.Parameters.AddWithValue("@y",item.Y);cmd.ExecuteNonQuery();
        }
    }

    public void GuardarConfiguracion(string nombreJugador, string dificultad, bool sonido, bool pantallaCompleta)
    {
        using var cn=ConexionBD.CrearConexion();cn.Open(); using var tx=cn.BeginTransaction();
        try
        {
            int idPerfil=ObtenerOCrearPerfil(cn,tx,nombreJugador);
            using var cmd=new SqlCommand(@"MERGE ConfiguracionJugador AS destino
                USING (SELECT @perfil AS id_perfil) AS origen ON destino.id_perfil=origen.id_perfil
                WHEN MATCHED THEN UPDATE SET dificultad=@dificultad,sonido_activo=@sonido,pantalla_completa=@pantalla,fecha_actualizacion=SYSDATETIME()
                WHEN NOT MATCHED THEN INSERT(id_perfil,dificultad,sonido_activo,pantalla_completa)
                VALUES(origen.id_perfil,@dificultad,@sonido,@pantalla);",cn,tx);
            cmd.Parameters.AddWithValue("@perfil",idPerfil);cmd.Parameters.Add("@dificultad",SqlDbType.NVarChar,20).Value=dificultad;
            cmd.Parameters.AddWithValue("@sonido",sonido);cmd.Parameters.AddWithValue("@pantalla",pantallaCompleta);cmd.ExecuteNonQuery();tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public (string Dificultad, bool Sonido, bool PantallaCompleta)? CargarConfiguracion(string nombreJugador)
    {
        using var cn=ConexionBD.CrearConexion();cn.Open();
        using var cmd=new SqlCommand(@"SELECT c.dificultad,c.sonido_activo,c.pantalla_completa FROM ConfiguracionJugador c
            INNER JOIN PerfilJugador p ON p.id_perfil=c.id_perfil WHERE p.nombre=@nombre;",cn);
        cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,100).Value=nombreJugador;
        using var rd=cmd.ExecuteReader(); if(!rd.Read()) return null;
        return (rd.GetString(0),rd.GetBoolean(1),rd.GetBoolean(2));
    }

    private static void GuardarEstadisticas(SqlConnection cn,SqlTransaction tx,int id,NexusTraining.Nexus juego,int segundos,int movimientos,int ataques)
    {
        using var cmd=new SqlCommand(@"MERGE EstadisticaPartida AS destino
            USING (SELECT @partida AS id_partida) AS origen ON destino.id_partida=origen.id_partida
            WHEN MATCHED THEN UPDATE SET tiempo_segundos=@tiempo,movimientos=@movimientos,ataques=@ataques,
                entidades_derrotadas=@derrotadas,xp_total=@xp,nivel_alcanzado=@nivel,fecha_actualizacion=SYSDATETIME()
            WHEN NOT MATCHED THEN INSERT(id_partida,tiempo_segundos,movimientos,ataques,entidades_derrotadas,xp_total,nivel_alcanzado)
                VALUES(@partida,@tiempo,@movimientos,@ataques,@derrotadas,@xp,@nivel);",cn,tx);
        cmd.Parameters.AddWithValue("@partida",id);cmd.Parameters.AddWithValue("@tiempo",segundos);cmd.Parameters.AddWithValue("@movimientos",movimientos);
        cmd.Parameters.AddWithValue("@ataques",ataques);cmd.Parameters.AddWithValue("@derrotadas",juego.EntidadesDerrotadas);
        cmd.Parameters.AddWithValue("@xp",juego.Cadete.Xp);cmd.Parameters.AddWithValue("@nivel",juego.Nivel);cmd.ExecuteNonQuery();
    }

    private static void GuardarLogros(SqlConnection cn,SqlTransaction tx,int idPerfil,NexusTraining.Nexus juego)
    {
        var desbloqueados=new List<string>();
        if(juego.Anomalias>0) desbloqueados.Add("ANALISTA_ANOMALIAS");
        if(juego.EntidadesDerrotadas>0) desbloqueados.Add("PRIMER_CONTACTO");
        if(juego.ResonanciaActivada) desbloqueados.Add("NUCLEO_RESONANCIA");
        if(juego.MisionPrincipal.Completada) desbloqueados.Add("ANOMALIA_PERDIDA");
        foreach(var codigo in desbloqueados)
        {
            using var cmd=new SqlCommand(@"IF NOT EXISTS(SELECT 1 FROM LogroJugador lj INNER JOIN Logro l ON l.id_logro=lj.id_logro
                WHERE lj.id_perfil=@perfil AND l.codigo=@codigo)
                INSERT INTO LogroJugador(id_perfil,id_logro) SELECT @perfil,id_logro FROM Logro WHERE codigo=@codigo;",cn,tx);
            cmd.Parameters.AddWithValue("@perfil",idPerfil);cmd.Parameters.Add("@codigo",SqlDbType.VarChar,50).Value=codigo;cmd.ExecuteNonQuery();
        }
    }

    private static IEnumerable<Objeto> CargarObjetosInventario(SqlConnection cn,int id)
    {
        var result=new List<Objeto>(); using var cmd=new SqlCommand(@"SELECT o.tipo FROM InventarioPartida i INNER JOIN Objeto o ON o.id_objeto=i.id_objeto
            WHERE i.id_partida=@id ORDER BY i.numero_espacio;",cn);cmd.Parameters.AddWithValue("@id",id);
        using var rd=cmd.ExecuteReader();while(rd.Read()) if(Enum.TryParse<TipoObjeto>(rd.GetString(0),out var tipo)) result.Add(Objeto.Crear(tipo)); return result;
    }
}
