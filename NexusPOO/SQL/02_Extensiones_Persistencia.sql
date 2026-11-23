USE Nexus;
GO



IF OBJECT_ID('dbo.ConfiguracionJugador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfiguracionJugador (
        id_configuracion INT IDENTITY(1,1) PRIMARY KEY,
        id_perfil INT NOT NULL UNIQUE,
        dificultad NVARCHAR(20) NOT NULL DEFAULT 'Normal',
        sonido_activo BIT NOT NULL DEFAULT 1,
        pantalla_completa BIT NOT NULL DEFAULT 1,
        fecha_actualizacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_Configuracion_Perfil FOREIGN KEY (id_perfil)
            REFERENCES dbo.PerfilJugador(id_perfil) ON DELETE CASCADE,
        CONSTRAINT CK_Configuracion_Dificultad CHECK (dificultad IN ('Normal','Difícil'))
    );
END;
GO

IF OBJECT_ID('dbo.EstadisticaPartida', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EstadisticaPartida (
        id_estadistica INT IDENTITY(1,1) PRIMARY KEY,
        id_partida INT NOT NULL UNIQUE,
        tiempo_segundos INT NOT NULL DEFAULT 0,
        movimientos INT NOT NULL DEFAULT 0,
        ataques INT NOT NULL DEFAULT 0,
        entidades_derrotadas INT NOT NULL DEFAULT 0,
        xp_total INT NOT NULL DEFAULT 0,
        nivel_alcanzado INT NOT NULL DEFAULT 1,
        fecha_actualizacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_Estadistica_Partida FOREIGN KEY (id_partida)
            REFERENCES dbo.Partida(id_partida) ON DELETE CASCADE,
        CONSTRAINT CK_Estadistica_Valores CHECK (tiempo_segundos >= 0 AND movimientos >= 0 AND ataques >= 0
            AND entidades_derrotadas >= 0 AND xp_total >= 0 AND nivel_alcanzado >= 1)
    );
END;
GO

IF COL_LENGTH('dbo.ProgresoZona','mapa_estado') IS NULL
    ALTER TABLE dbo.ProgresoZona ADD mapa_estado NVARCHAR(MAX) NULL;
GO

IF OBJECT_ID('dbo.EnemigoPartida', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EnemigoPartida (
        id_enemigo_partida INT IDENTITY(1,1) PRIMARY KEY,
        id_partida INT NOT NULL,
        nombre NVARCHAR(100) NOT NULL,
        tipo VARCHAR(10) NOT NULL,
        energia_actual DECIMAL(6,2) NOT NULL,
        energia_maxima DECIMAL(6,2) NOT NULL,
        ataque INT NOT NULL,
        habilidad VARCHAR(30) NOT NULL DEFAULT 'NINGUNA',
        es_jefe BIT NOT NULL DEFAULT 0,
        defensa_activa BIT NOT NULL DEFAULT 0,
        punto_debil BIT NOT NULL DEFAULT 0,
        x INT NOT NULL,
        y INT NOT NULL,
        CONSTRAINT FK_EnemigoPartida_Partida FOREIGN KEY (id_partida)
            REFERENCES dbo.Partida(id_partida) ON DELETE CASCADE,
        CONSTRAINT CK_EnemigoPartida_Tipo CHECK (tipo IN ('ENEMIGO','ROBOT')),
        CONSTRAINT CK_EnemigoPartida_Energia CHECK (energia_actual >= 0 AND energia_maxima > 0 AND energia_actual <= energia_maxima),
        CONSTRAINT CK_EnemigoPartida_Posicion CHECK (x BETWEEN 0 AND 29 AND y BETWEEN 0 AND 19)
    );
END;
GO

IF OBJECT_ID('dbo.Logro', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Logro (
        id_logro INT IDENTITY(1,1) PRIMARY KEY,
        codigo VARCHAR(50) NOT NULL UNIQUE,
        titulo NVARCHAR(100) NOT NULL,
        descripcion NVARCHAR(250) NOT NULL,
        xp_recompensa INT NOT NULL DEFAULT 0,
        CONSTRAINT CK_Logro_XP CHECK (xp_recompensa >= 0)
    );
END;
GO

IF OBJECT_ID('dbo.LogroJugador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LogroJugador (
        id_logro_jugador INT IDENTITY(1,1) PRIMARY KEY,
        id_perfil INT NOT NULL,
        id_logro INT NOT NULL,
        fecha_obtencion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_LogroJugador_Perfil FOREIGN KEY (id_perfil)
            REFERENCES dbo.PerfilJugador(id_perfil) ON DELETE CASCADE,
        CONSTRAINT FK_LogroJugador_Logro FOREIGN KEY (id_logro)
            REFERENCES dbo.Logro(id_logro),
        CONSTRAINT UQ_LogroJugador UNIQUE (id_perfil, id_logro)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Logro WHERE codigo='ANALISTA_ANOMALIAS')
    INSERT INTO dbo.Logro(codigo,titulo,descripcion,xp_recompensa)
    VALUES ('ANALISTA_ANOMALIAS','Analista de anomalías','Analiza la primera anomalía de una realidad desconocida.',10);
IF NOT EXISTS (SELECT 1 FROM dbo.Logro WHERE codigo='PRIMER_CONTACTO')
    INSERT INTO dbo.Logro(codigo,titulo,descripcion,xp_recompensa)
    VALUES ('PRIMER_CONTACTO','Primer contacto','Derrota a tu primera entidad hostil.',10);
IF NOT EXISTS (SELECT 1 FROM dbo.Logro WHERE codigo='NUCLEO_RESONANCIA')
    INSERT INTO dbo.Logro(codigo,titulo,descripcion,xp_recompensa)
    VALUES ('NUCLEO_RESONANCIA','Resonancia recuperada','Recupera el núcleo energético de la misión.',25);
IF NOT EXISTS (SELECT 1 FROM dbo.Logro WHERE codigo='ANOMALIA_PERDIDA')
    INSERT INTO dbo.Logro(codigo,titulo,descripcion,xp_recompensa)
    VALUES ('ANOMALIA_PERDIDA','La anomalía perdida','Completa la misión principal y regresa a extracción.',100);
GO

PRINT 'Extensiones de persistencia NEXUS instaladas correctamente.';
GO
