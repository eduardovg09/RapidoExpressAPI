using Microsoft.AspNetCore.Mvc;
// Importamos la librería para conectarnos a SQL Server
using Microsoft.Data.SqlClient;
// Importamos la librería para usar tipos de datos de SQL (como CommandType)
using System.Data;

namespace RapidoExpressAPI.Controllers
{
    // [Route("api")] define que todas las URLs de este controlador empiezan con /api
    // [ApiController] habilita características automáticas de validación y binding de ASP.NET Core
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        // Variable para guardar la configuración (donde está la cadena de conexión)
        private readonly IConfiguration _config;

        // Constructor: Recibe la configuración al iniciar el controlador
        public ApiController(IConfiguration config)
        {
            _config = config;
        }

        // =========================================
        // MÉTODOS AUXILIARES (HELPERS)
        // =========================================

        // Función para convertir los resultados de SQL (SqlDataReader) a una lista de Diccionarios.
        // Esto hace que sea fácil convertirlos a JSON después.
        private List<Dictionary<string, object>> LeerResultados(SqlDataReader reader)
        {
            var resultados = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                var fila = new Dictionary<string, object>();
                // Recorremos todas las columnas de la fila actual
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    fila[reader.GetName(i)] = reader.GetValue(i);
                }
                resultados.Add(fila);
            }
            return resultados;
        }

        // =========================================
        // ENDPOINTS (RUTAS DE LA API)
        // =========================================

        // 1. GET /api/estados
        // Obtiene la lista completa de estados para llenar el primer combo.
        [HttpGet("estados")]
        public async Task<IActionResult> GetEstados()
        {
            var resultados = new List<Dictionary<string, object>>();
            string query = "SELECT * FROM Estados ORDER BY nombre_estado";

            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        var reader = await cmd.ExecuteReaderAsync();
                        resultados = LeerResultados(reader);
                    }
                }
                return Ok(resultados); // Retorna HTTP 200 con los datos en JSON
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 2. GET /api/clientes
        // Obtiene la lista de todos los clientes registrados.
        [HttpGet("clientes")]
        public async Task<IActionResult> GetClientes()
        {
            var resultados = new List<Dictionary<string, object>>();
            string query = "SELECT * FROM Clientes ORDER BY nombre";

            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        var reader = await cmd.ExecuteReaderAsync();
                        resultados = LeerResultados(reader);
                    }
                }
                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 3. GET /api/ciudades/{id_estado}
        // Obtiene las ciudades que pertenecen a un estado específico (para el combo en cascada).
        [HttpGet("ciudades/{id_estado}")]
        public async Task<IActionResult> GetCiudades(int id_estado)
        {
            var resultados = new List<Dictionary<string, object>>();

            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    // Usamos el Procedimiento Almacenado creado en la BD
                    using (SqlCommand cmd = new SqlCommand("sp_ObtenerCiudadesPorEstado", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id_estado", id_estado);

                        var reader = await cmd.ExecuteReaderAsync();
                        resultados = LeerResultados(reader);
                    }
                }
                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 4. POST /api/envios
        // Registra un nuevo envío usando un Procedimiento Almacenado con validaciones.
        [HttpPost("envios")]
        public async Task<IActionResult> RegistrarEnvio([FromBody] EnvioRequest request)
        {
            string resultado = "";
            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_RegistrarEnvio", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetros de entrada (vienen del JSON del cuerpo de la petición)
                        cmd.Parameters.AddWithValue("@id_cliente", request.id_cliente);
                        cmd.Parameters.AddWithValue("@id_ciudad", request.id_ciudad);
                        cmd.Parameters.AddWithValue("@descripcion", request.descripcion);

                        // Parámetro de salida (para recibir el mensaje de éxito o error del SP)
                        SqlParameter outputParam = new SqlParameter
                        {
                            ParameterName = "@resultado",
                            SqlDbType = SqlDbType.VarChar,
                            Size = 100,
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputParam);

                        await cmd.ExecuteNonQueryAsync();

                        // Leemos lo que el SP nos devolvió en la variable @resultado
                        resultado = outputParam.Value.ToString();
                    }
                }

                if (resultado.Contains("correctamente"))
                {
                    return StatusCode(201, new { mensaje = resultado }); // HTTP 201 Created
                }
                else
                {
                    return BadRequest(new { mensaje = resultado }); // HTTP 400 Bad Request
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 5. POST /api/clientes
        // Registra un nuevo cliente.
        [HttpPost("clientes")]
        public async Task<IActionResult> RegistrarCliente([FromBody] ClienteRequest request)
        {
            string resultado = "";
            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_RegistrarCliente", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@nombre", request.nombre);
                        cmd.Parameters.AddWithValue("@correo", request.correo);

                        SqlParameter outputParam = new SqlParameter
                        {
                            ParameterName = "@resultado",
                            SqlDbType = SqlDbType.VarChar,
                            Size = 100,
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputParam);

                        await cmd.ExecuteNonQueryAsync();
                        resultado = outputParam.Value.ToString();
                    }
                }

                if (resultado.Contains("correctamente"))
                {
                    return StatusCode(201, new { mensaje = resultado });
                }
                else
                {
                    return BadRequest(new { mensaje = resultado });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 6. DELETE /api/clientes/{id_cliente}
        // Elimina un cliente (si no tiene envíos registrados).
        [HttpDelete("clientes/{id_cliente}")]
        public async Task<IActionResult> EliminarCliente(int id_cliente)
        {
            string resultado = "";
            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_EliminarCliente", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id_cliente", id_cliente);

                        SqlParameter outputParam = new SqlParameter
                        {
                            ParameterName = "@resultado",
                            SqlDbType = SqlDbType.VarChar,
                            Size = 100,
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputParam);

                        await cmd.ExecuteNonQueryAsync();
                        resultado = outputParam.Value.ToString();
                    }
                }

                if (resultado.Contains("correctamente"))
                {
                    return Ok(new { mensaje = resultado }); // HTTP 200 OK
                }
                else
                {
                    return BadRequest(new { mensaje = resultado });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 7. GET /api/envios/{id_envio}
        // Obtiene el detalle completo de UN solo envío para mostrar en el modal.
        [HttpGet("envios/{id_envio}")]
        public async Task<IActionResult> GetDetalleEnvio(int id_envio)
        {
            var resultados = new List<Dictionary<string, object>>();

            // Consulta con JOINS para traer los nombres de cliente, ciudad y estado
            string query = @"
                SELECT 
                    E.id_envio, E.descripcion, E.fecha_envio, E.estatus,
                    C.nombre AS nombre_cliente, C.correo,
                    Ci.nombre_ciudad AS ciudad_destino,
                    Est.nombre_estado AS estado_destino
                FROM Envios E
                JOIN Clientes C ON E.id_cliente = C.id_cliente
                JOIN Ciudades Ci ON E.id_ciudad_destino = Ci.id_ciudad
                JOIN Estados Est ON Ci.id_estado = Est.id_estado
                WHERE E.id_envio = @id_envio;
            ";

            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id_envio", id_envio);
                        var reader = await cmd.ExecuteReaderAsync();
                        resultados = LeerResultados(reader);
                    }
                }

                if (resultados.Count == 0)
                {
                    return NotFound(new { mensaje = "Envío no encontrado" }); // HTTP 404 Not Found
                }

                return Ok(resultados[0]); // Devolvemos solo el primer objeto, no la lista
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 8. GET /api/clientes/{id_cliente}/envios
        // Obtiene la lista de todos los envíos de un cliente específico.
        [HttpGet("clientes/{id_cliente}/envios")]
        public async Task<IActionResult> GetEnviosPorCliente(int id_cliente)
        {
            var resultados = new List<Dictionary<string, object>>();

            string query = @"
                SELECT 
                    E.id_envio, E.descripcion, E.fecha_envio, E.estatus,
                    Ci.nombre_ciudad AS ciudad_destino
                FROM Envios E
                JOIN Ciudades Ci ON E.id_ciudad_destino = Ci.id_ciudad
                WHERE E.id_cliente = @id_cliente
                ORDER BY E.fecha_envio DESC;
            ";

            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id_cliente", id_cliente);
                        var reader = await cmd.ExecuteReaderAsync();
                        resultados = LeerResultados(reader);
                    }
                }
                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 9. PUT /api/envios/{id_envio}/estatus
        // Actualiza el estatus de un envío existente.
        [HttpPut("envios/{id_envio}/estatus")]
        public async Task<IActionResult> ActualizarEstatus(int id_envio, [FromBody] EstatusRequest request)
        {
            string resultado = "";
            try
            {
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_ActualizarEstatusEnvio", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@id_envio", id_envio);
                        cmd.Parameters.AddWithValue("@nuevo_estatus", request.estatus);

                        SqlParameter outputParam = new SqlParameter
                        {
                            ParameterName = "@resultado",
                            SqlDbType = SqlDbType.VarChar,
                            Size = 100,
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputParam);

                        await cmd.ExecuteNonQueryAsync();
                        resultado = outputParam.Value.ToString();
                    }
                }

                if (resultado.Contains("correctamente"))
                {
                    return Ok(new { mensaje = resultado });
                }
                else
                {
                    return BadRequest(new { mensaje = resultado });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

    } // FIN DE LA CLASE ApiController

    // =========================================
    // CLASES MODELO (PARA RECIBIR DATOS JSON)
    // =========================================

    public class ClienteRequest
    {
        public string nombre { get; set; }
        public string correo { get; set; }
    }

    public class EnvioRequest
    {
        public int id_cliente { get; set; }
        public int id_ciudad { get; set; }
        public string descripcion { get; set; }
    }

    public class EstatusRequest
    {
        public string estatus { get; set; }
    }

} // FIN DEL NAMESPACE