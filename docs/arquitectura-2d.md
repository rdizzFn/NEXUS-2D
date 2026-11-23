# Arquitectura de NEXUS 2D

## Flujo de inicio

`Program.Main` inicializa Windows Forms y abre `NexusGameForm`. El formulario contiene el menú, HUD, pausa y `GameCanvas`. `GameCanvas` renderiza el mapa y las entidades; el temporizador del formulario actualiza el mundo mientras la partida está activa.

## Separación de responsabilidades

- `Program.cs`: contiene las clases originales del simulador y el punto de entrada gráfico.
- `Game/Engine/GameWorld.cs`: controla movimiento, terreno, colisiones, IA sencilla, combate, interacciones y progreso de misión.
- `Game/UI/NexusGameForm.cs`: menús, controles, HUD, pantalla de pausa, estadísticas y renderizado 2D.
- `Game/UI/GraphicsExtensions.cs`: utilidades de dibujo.
- `Game/Data/ConexionBD.cs`: creación centralizada de conexiones SQL Server.
- `Game/Data/PartidaRepository.cs`: operaciones ADO.NET parametrizadas y transaccionales.
- `Game/Models/PartidaGuardada.cs`: datos recuperados para continuar una partida.

## Mecánicas reutilizadas

El juego gráfico utiliza las clases originales `Cadete`, `Personaje`, `Enemigo`, `Robot`, `Objeto`, `Inventario`, `Mision`, `Objetivo`, `Nexus` y `Zona`. El combate vuelve a utilizar los cálculos de daño, escudo, cargas de módulo, punto débil, drenaje, interferencia y pulso del jefe que ya existían en el proyecto de consola.

## Renderizado

La vista es top-down. La cámara sigue al cadete y dibuja una ventana del mapa activo. El personaje y las entidades se representan con sprites vectoriales dibujados en GDI+; los enemigos se desplazan hacia el jugador y los ataques generan partículas. Las imágenes y sonidos originales del proyecto se mantienen como recursos.
