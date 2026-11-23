USE Nexus;
GO
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.PerfilJugador','U') IS NULL
CREATE TABLE dbo.PerfilJugador(
 id_perfil INT IDENTITY(1,1) PRIMARY KEY,
 nombre NVARCHAR(100) NOT NULL,
 fecha_registro DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

IF OBJECT_ID('dbo.Mision','U') IS NULL
CREATE TABLE dbo.Mision(
 id_mision INT IDENTITY(1,1) PRIMARY KEY,
 titulo NVARCHAR(150) NOT NULL,
 descripcion NVARCHAR(500) NOT NULL,
 recompensa_xp INT NOT NULL,
 CONSTRAINT CK_Mision_Recompensa CHECK(recompensa_xp>=0)
);
GO

IF OBJECT_ID('dbo.ObjetivoMision','U') IS NULL
CREATE TABLE dbo.ObjetivoMision(
 id_objetivo INT IDENTITY(1,1) PRIMARY KEY,
 id_mision INT NOT NULL,
 descripcion NVARCHAR(250) NOT NULL,
 clave VARCHAR(50) NOT NULL,
 CONSTRAINT FK_Objetivo_Mision FOREIGN KEY(id_mision) REFERENCES dbo.Mision(id_mision),
 CONSTRAINT UQ_Objetivo_Clave UNIQUE(id_mision,clave)
);
GO

IF OBJECT_ID('dbo.Partida','U') IS NULL
CREATE TABLE dbo.Partida(
 id_partida INT IDENTITY(1,1) PRIMARY KEY,
 id_perfil INT NOT NULL,
 id_mision INT NOT NULL,
 nivel INT NOT NULL DEFAULT 1,
 anomalias INT NOT NULL DEFAULT 0,
 entidades_derrotadas INT NOT NULL DEFAULT 0,
 resonancia_activada BIT NOT NULL DEFAULT 0,
 recompensa_entregada BIT NOT NULL DEFAULT 0,
 fecha_creacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
 fecha_guardado DATETIME2 NULL,
 estado VARCHAR(20) NOT NULL DEFAULT 'EN_CURSO',
 CONSTRAINT FK_Partida_Perfil FOREIGN KEY(id_perfil) REFERENCES dbo.PerfilJugador(id_perfil),
 CONSTRAINT FK_Partida_Mision FOREIGN KEY(id_mision) REFERENCES dbo.Mision(id_mision),
 CONSTRAINT CK_Partida_Nivel CHECK(nivel>=1),
 CONSTRAINT CK_Partida_Anomalias CHECK(anomalias>=0),
 CONSTRAINT CK_Partida_Entidades CHECK(entidades_derrotadas>=0),
 CONSTRAINT CK_Partida_Estado CHECK(estado IN('EN_CURSO','COMPLETADA','GAME_OVER','PAUSADA'))
);
GO

IF OBJECT_ID('dbo.EstadoCadete','U') IS NULL
CREATE TABLE dbo.EstadoCadete(
 id_estado INT IDENTITY(1,1) PRIMARY KEY,
 id_partida INT NOT NULL UNIQUE,
 nombre NVARCHAR(100) NOT NULL,
 edad INT NOT NULL,
 realidad NVARCHAR(100) NOT NULL,
 energia DECIMAL(6,2) NOT NULL,
 energia_maxima DECIMAL(6,2) NOT NULL DEFAULT 100,
 estabilidad DECIMAL(6,2) NOT NULL,
 xp INT NOT NULL DEFAULT 0,
 escudo DECIMAL(6,2) NOT NULL DEFAULT 0,
 cargas_modulo INT NOT NULL DEFAULT 0,
 CONSTRAINT FK_EstadoCadete_Partida FOREIGN KEY(id_partida) REFERENCES dbo.Partida(id_partida) ON DELETE CASCADE,
 CONSTRAINT CK_Cadete_Edad CHECK(edad>0),
 CONSTRAINT CK_Cadete_Energia CHECK(energia>=0 AND energia<=energia_maxima),
 CONSTRAINT CK_Cadete_Estabilidad CHECK(estabilidad BETWEEN 0 AND 100),
 CONSTRAINT CK_Cadete_XP CHECK(xp>=0),
 CONSTRAINT CK_Cadete_Escudo CHECK(escudo>=0),
 CONSTRAINT CK_Cadete_Cargas CHECK(cargas_modulo>=0)
);
GO

IF OBJECT_ID('dbo.Zona','U') IS NULL
CREATE TABLE dbo.Zona(
 id_zona INT IDENTITY(1,1) PRIMARY KEY,
 nombre NVARCHAR(30) NOT NULL UNIQUE,
 titulo NVARCHAR(150) NOT NULL,
 cartografia INT NOT NULL,
 estado NVARCHAR(30) NOT NULL,
 lecturas INT NOT NULL,
 senales INT NOT NULL,
 prob_evento INT NOT NULL,
 filas INT NOT NULL DEFAULT 20,
 columnas INT NOT NULL DEFAULT 30,
 CONSTRAINT CK_Zona_Cartografia CHECK(cartografia BETWEEN 0 AND 100),
 CONSTRAINT CK_Zona_Probabilidad CHECK(prob_evento BETWEEN 0 AND 100),
 CONSTRAINT CK_Zona_Dimensiones CHECK(filas>0 AND columnas>0)
);
GO

IF OBJECT_ID('dbo.ProgresoZona','U') IS NULL
CREATE TABLE dbo.ProgresoZona(
 id_progreso_zona INT IDENTITY(1,1) PRIMARY KEY,
 id_partida INT NOT NULL,
 id_zona INT NOT NULL,
 fila_actual INT NOT NULL DEFAULT 0,
 columna_actual INT NOT NULL DEFAULT 0,
 cartografia_actual INT NOT NULL DEFAULT 0,
 lecturas_realizadas INT NOT NULL DEFAULT 0,
 senales_encontradas INT NOT NULL DEFAULT 0,
 estado_actual NVARCHAR(30) NOT NULL,
 descubierta BIT NOT NULL DEFAULT 0,
 CONSTRAINT FK_ProgresoZona_Partida FOREIGN KEY(id_partida) REFERENCES dbo.Partida(id_partida) ON DELETE CASCADE,
 CONSTRAINT FK_ProgresoZona_Zona FOREIGN KEY(id_zona) REFERENCES dbo.Zona(id_zona),
 CONSTRAINT UQ_ProgresoZona UNIQUE(id_partida,id_zona),
 CONSTRAINT CK_ProgresoZona_Coordenadas CHECK(fila_actual BETWEEN 0 AND 19 AND columna_actual BETWEEN 0 AND 29)
);
GO

IF OBJECT_ID('dbo.Objeto','U') IS NULL
CREATE TABLE dbo.Objeto(
 id_objeto INT IDENTITY(1,1) PRIMARY KEY,
 nombre NVARCHAR(100) NOT NULL UNIQUE,
 tipo VARCHAR(30) NOT NULL,
 icono NVARCHAR(10) NULL,
 descripcion NVARCHAR(250) NOT NULL,
 CONSTRAINT CK_Objeto_Tipo CHECK(tipo IN('CelulaEnergia','Escudo','ModuloAtaque','Analizador','NucleoResonancia','ModuloDesconocido'))
);
GO

IF OBJECT_ID('dbo.InventarioPartida','U') IS NULL
CREATE TABLE dbo.InventarioPartida(
 id_inventario INT IDENTITY(1,1) PRIMARY KEY,
 id_partida INT NOT NULL,
 id_objeto INT NOT NULL,
 numero_espacio INT NOT NULL,
 CONSTRAINT FK_Inventario_Partida FOREIGN KEY(id_partida) REFERENCES dbo.Partida(id_partida) ON DELETE CASCADE,
 CONSTRAINT FK_Inventario_Objeto FOREIGN KEY(id_objeto) REFERENCES dbo.Objeto(id_objeto),
 CONSTRAINT UQ_Inventario_Espacio UNIQUE(id_partida,numero_espacio),
 CONSTRAINT CK_Inventario_Espacio CHECK(numero_espacio BETWEEN 1 AND 8)
);
GO

IF OBJECT_ID('dbo.ProgresoObjetivo','U') IS NULL
CREATE TABLE dbo.ProgresoObjetivo(
 id_progreso INT IDENTITY(1,1) PRIMARY KEY,
 id_partida INT NOT NULL,
 id_objetivo INT NOT NULL,
 completado BIT NOT NULL DEFAULT 0,
 CONSTRAINT FK_ProgresoObjetivo_Partida FOREIGN KEY(id_partida) REFERENCES dbo.Partida(id_partida) ON DELETE CASCADE,
 CONSTRAINT FK_ProgresoObjetivo_Objetivo FOREIGN KEY(id_objetivo) REFERENCES dbo.ObjetivoMision(id_objetivo),
 CONSTRAINT UQ_ProgresoObjetivo UNIQUE(id_partida,id_objetivo)
);
GO

/* Catálogos originales del videojuego. Se insertan solo si no existen. */
IF NOT EXISTS(SELECT 1 FROM dbo.Mision WHERE titulo=N'LA ANOMALÍA PERDIDA')
 INSERT INTO dbo.Mision(titulo,descripcion,recompensa_xp)
 VALUES(N'LA ANOMALÍA PERDIDA',N'Investiga la anomalía detectada en una realidad desconocida y regresa con vida.',100);
GO

DECLARE @mision INT=(SELECT TOP 1 id_mision FROM dbo.Mision WHERE titulo=N'LA ANOMALÍA PERDIDA' ORDER BY id_mision);
IF NOT EXISTS(SELECT 1 FROM dbo.ObjetivoMision WHERE id_mision=@mision AND clave='anomalia')
 INSERT INTO dbo.ObjetivoMision(id_mision,descripcion,clave) VALUES(@mision,N'Llegar a la zona de anomalía (⚠)','anomalia');
IF NOT EXISTS(SELECT 1 FROM dbo.ObjetivoMision WHERE id_mision=@mision AND clave='nucleo')
 INSERT INTO dbo.ObjetivoMision(id_mision,descripcion,clave) VALUES(@mision,N'Encontrar el núcleo energético (◆)','nucleo');
IF NOT EXISTS(SELECT 1 FROM dbo.ObjetivoMision WHERE id_mision=@mision AND clave='entidad')
 INSERT INTO dbo.ObjetivoMision(id_mision,descripcion,clave) VALUES(@mision,N'Sobrevivir al encuentro con la entidad','entidad');
IF NOT EXISTS(SELECT 1 FROM dbo.ObjetivoMision WHERE id_mision=@mision AND clave='extraccion')
 INSERT INTO dbo.ObjetivoMision(id_mision,descripcion,clave) VALUES(@mision,N'Regresar al punto de extracción (⌂)','extraccion');
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Objeto WHERE tipo='CelulaEnergia') INSERT INTO dbo.Objeto(nombre,tipo,icono,descripcion) VALUES(N'CÉLULA DE ENERGÍA','CelulaEnergia',N'🔋',N'Restaura 25 de energía');
IF NOT EXISTS(SELECT 1 FROM dbo.Objeto WHERE tipo='Escudo') INSERT INTO dbo.Objeto(nombre,tipo,icono,descripcion) VALUES(N'ESCUDO','Escudo',N'🛡️',N'Absorbe 25 de daño');
IF NOT EXISTS(SELECT 1 FROM dbo.Objeto WHERE tipo='ModuloAtaque') INSERT INTO dbo.Objeto(nombre,tipo,icono,descripcion) VALUES(N'MÓDULO DE ATAQUE','ModuloAtaque',N'🔫',N'+10 daño x3 ataques');
IF NOT EXISTS(SELECT 1 FROM dbo.Objeto WHERE tipo='Analizador') INSERT INTO dbo.Objeto(nombre,tipo,icono,descripcion) VALUES(N'ANALIZADOR','Analizador',N'🔬',N'Expone el punto débil');
IF NOT EXISTS(SELECT 1 FROM dbo.Objeto WHERE tipo='NucleoResonancia') INSERT INTO dbo.Objeto(nombre,tipo,icono,descripcion) VALUES(N'NÚCLEO DE RESONANCIA','NucleoResonancia',N'◆',N'Objetivo de misión');
IF NOT EXISTS(SELECT 1 FROM dbo.Objeto WHERE tipo='ModuloDesconocido') INSERT INTO dbo.Objeto(nombre,tipo,icono,descripcion) VALUES(N'MÓDULO DESCONOCIDO','ModuloDesconocido',N'❖',N'+30 energía +15 estab.');
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Zona WHERE nombre='IRIS-01') INSERT INTO dbo.Zona(nombre,titulo,cartografia,estado,lecturas,senales,prob_evento) VALUES('IRIS-01',N'⛰ SECTOR MONTAÑOSO ⛰',78,'ESTABLE',1,3,18);
IF NOT EXISTS(SELECT 1 FROM dbo.Zona WHERE nombre='IRIS-02') INSERT INTO dbo.Zona(nombre,titulo,cartografia,estado,lecturas,senales,prob_evento) VALUES('IRIS-02',N'≈ SECTOR ACUÁTICO ≈',65,'INESTABLE',2,4,25);
IF NOT EXISTS(SELECT 1 FROM dbo.Zona WHERE nombre='IRIS-03') INSERT INTO dbo.Zona(nombre,titulo,cartografia,estado,lecturas,senales,prob_evento) VALUES('IRIS-03',N'☢ SECTOR ANÓMALO ☢',71,'CRÍTICO',5,7,32);
GO

PRINT 'Esquema base y catálogos NEXUS verificados.';
GO
