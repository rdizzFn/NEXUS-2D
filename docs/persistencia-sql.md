# Persistencia SQL Server

La aplicación no crea la base de datos. El usuario debe crear `Nexus` y ejecutar los scripts de `NexusPOO/SQL`.

## Scripts

1. `01_Esquema_Base_Nexus.sql`: verifica/crea las diez tablas base y los catálogos de la misión, objetivos, objetos y sectores IRIS.
2. `02_Extensiones_Persistencia.sql`: añade la matriz de mapa serializada y las tablas de enemigos activos, estadísticas, configuración y logros.

## Guardado

`PartidaRepository.Guardar` ejecuta una transacción que actualiza la partida, cadete, inventario, objetivos, mapas, enemigos vivos, estadísticas y logros. Si alguna operación falla, se revierte la transacción y se muestra el error al jugador.

## Recuperación

`PartidaRepository.Cargar` reconstruye el cadete y el objeto `Nexus`, restaura inventario, progreso de objetivos, mapa, posición, enemigos y estadísticas. Las opciones de audio/dificultad/pantalla se recuperan desde `ConfiguracionJugador`.

## Configuración de conexión

La única cadena de conexión está en `Game/Data/ConexionBD.cs`. No se guardan credenciales en otros archivos ni se simula el éxito de operaciones SQL.
