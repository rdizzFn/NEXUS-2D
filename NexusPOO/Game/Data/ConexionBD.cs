using System;
using Microsoft.Data.SqlClient;

namespace NexusPOO.Game.Data;

/// <summary>Único punto de configuración y apertura de conexiones a SQL Server.</summary>
public static class ConexionBD
{

    private static readonly string cadenaConexion = "Server=DESKTOP-V08UIA0;Database=Nexus;Integrated Security=True;TrustServerCertificate=True;";

    public static SqlConnection CrearConexion()
    {
        if (cadenaConexion.Contains("AQUI_COLOCARE_MI_CADENA_DE_CONEXION", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Configura la cadena de conexión en Game/Data/ConexionBD.cs antes de usar las partidas guardadas.");
        return new SqlConnection(cadenaConexion);
    }
}
