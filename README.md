# NEXUS 2D — The Lost Anomaly

NEXUS evoluciona desde su simulador de consola a una experiencia 2D de escritorio. Se conserva el núcleo original de C# (Cadete, Personaje, Enemigo, Robot, Inventario, Misión, Objetivos, Nexus y los mapas IRIS) y se incorpora una capa gráfica de exploración en tiempo real.

## Estado de esta versión

Incluye una primera versión jugable 2D con:

- Menú principal: nueva partida, continuar, cargar, configuración, estadísticas, logros, créditos y salir.
- Exploración top-down con los mapas originales IRIS, movimiento por teclado y colisiones de terreno.
- Personaje y enemigos dibujados en tiempo real, cámara que sigue al cadete, barras de energía y animaciones de movimiento.
- Combate con reutilización del daño, escudo, módulos, puntos débiles, enemigos y guardián.
- Interacción con anomalías, núcleo de resonancia, señales y punto de extracción.
- Inventario con ocho espacios y uso rápido de objetos.
- Misión principal con objetivos, XP, niveles, victoria y derrota.
- Menú de pausa y atajos para guardar/cargar.
- Audio WAV original del proyecto.
- Persistencia real en SQL Server mediante ADO.NET y consultas parametrizadas.

El renderizador 2D está implementado sobre Windows Forms/GDI+ para mantener el proyecto de escritorio C# sin añadir un runtime de motor externo. No es una escena de Unity/MonoGame: el movimiento, colisiones, enemigos y combate sí se ejecutan como sistemas del juego.

## Requisitos

- Windows 10/11.
- Visual Studio con soporte para .NET 10, o el SDK de .NET 10.
- SQL Server.
- La base de datos `Nexus` creada por el usuario.

## Abrir y ejecutar

1. Extrae el ZIP en una carpeta local.
2. Abre `NexusPOO.sln` (también se incluye `NexusPOO.slnx`) o directamente `NexusPOO/NexusPOO.csproj` en Visual Studio.
3. Espera a que NuGet restaure las dependencias.
4. Configura la conexión en `NexusPOO/Game/Data/ConexionBD.cs`.
5. Ejecuta en SQL Server `NexusPOO/SQL/01_Esquema_Base_Nexus.sql` dentro de la base `Nexus` (verifica/crea las 10 tablas base y sus catálogos sin crear la base de datos).
6. Ejecuta `NexusPOO/SQL/02_Extensiones_Persistencia.sql` para estadísticas, configuraciones, enemigos activos, estado del mapa y logros.
7. Compila y ejecuta el proyecto.

## Configuración SQL Server

Edita únicamente la constante `cadenaConexion` de `Game/Data/ConexionBD.cs`. Ejemplo de formato (ajusta servidor, instancia y autenticación a tu instalación):

```csharp
private static readonly string cadenaConexion =
    "Server=localhost;Database=Nexus;Trusted_Connection=True;TrustServerCertificate=True;";
```

La aplicación centraliza las conexiones en `ConexionBD`. Los guardados usan transacciones SQL, parámetros y las tablas base existentes: `PerfilJugador`, `Partida`, `EstadoCadete`, `InventarioPartida`, `Mision`, `ObjetivoMision`, `ProgresoObjetivo`, `Zona`, `ProgresoZona` y `Objeto`.

El script adicional crea `ConfiguracionJugador`, `EstadisticaPartida`, `EnemigoPartida`, `Logro` y `LogroJugador`. No crea, elimina ni restaura la base de datos `Nexus`.

Si la conexión no está configurada o SQL Server no está disponible, la interfaz informa del error y no muestra un guardado ficticio.

## Controles

| Tecla | Acción |
|---|---|
| WASD / Flechas | Mover al cadete |
| Shift + movimiento | Correr |
| Espacio | Atacar a una entidad cercana |
| E | Interactuar con anomalía, núcleo, señales o extracción |
| Q | Pulso especial de resonancia (tras recuperar el núcleo) |
| I | Mostrar/ocultar inventario |
| 1–8 | Usar el objeto correspondiente del inventario |
| F1–F3 | Cambiar entre sectores IRIS |
| F5 | Guardar en SQL Server |
| F9 | Cargar una partida guardada |
| Esc | Abrir/cerrar pausa |
| R | Reiniciar tras victoria o derrota |

## Organización

- `Program.cs`: mantiene las clases originales del simulador y arranca la nueva interfaz gráfica.
- `Game/Engine`: estado de mundo, movimiento, colisiones, combate e interacciones.
- `Game/UI`: formulario principal, menús, HUD y renderizado 2D.
- `Game/Data`: conexión SQL Server y repositorio de partidas.
- `Game/Models`: modelos de selección/carga de partidas.
- `SQL`: extensiones del esquema SQL Server.
- `Resources`: imágenes originales.
- `Sonidos`: recursos WAV originales.
- `frames`: fotogramas de arte ASCII conservados del simulador de consola original.
- `docs`: notas de arquitectura 2D y persistencia SQL.

## Notas de verificación

El entorno donde se preparó esta transformación no dispone del SDK de .NET ni de Windows Forms, por lo que no fue posible ejecutar una compilación real aquí. El proyecto queda configurado para Windows/.NET 10; antes de entregar o publicar una compilación, abre la solución en Visual Studio, restaura NuGet y ejecuta **Compilar solución**. Revisa cualquier advertencia de compatibilidad que dependa de la versión instalada del SDK.
