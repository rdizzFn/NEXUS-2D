# SQL Server

La base de datos `Nexus` la crea manualmente el propietario del proyecto. No se incluye un MDF/BAK ni un script que cree o elimine la base de datos.

Orden de ejecución dentro de `Nexus`:

1. `01_Esquema_Base_Nexus.sql`: verifica/crea las 10 tablas base y registra los catálogos iniciales de misión, objetivos, objetos y sectores IRIS.
2. `02_Extensiones_Persistencia.sql`: agrega el mapa persistente y las tablas para enemigos vivos, estadísticas, configuraciones y logros.

La aplicación necesita ambos scripts para que todas las funciones del repositorio ADO.NET estén disponibles.
