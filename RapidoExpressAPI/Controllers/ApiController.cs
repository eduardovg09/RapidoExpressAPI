using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // <--- El nuevo driver
using System.Data; // <--- Para los Stored Procedures

namespace RapidoExpressAPI.Controllers
{
    // Esto le dice a C# que esta clase maneja todas las rutas que empiezan con "api"
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IConfiguration _config;

        // "Inyectamos" la configuración (appsettings.json)
        public ApiController(IConfiguration config)
        {
            _config = config;
        }

        // Helper para leer los resultados de una consulta
        private List<Dictionary<string, object>> LeerResultados(SqlDataReader reader)
        {
            var resultados = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                var fila = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    fila[reader.GetName(i)] = reader.GetValue(i);
                }
                resultados.Add(fila);
            }
            return resultados;
        }


        // 1. GET /api/estados
        [HttpGet("estados")]
        public async Task<IActionResult> GetEstados()
        {
            var resultados = new List<Dictionary<string, object>>();
            string query = "SELECT * FROM Estados ORDER BY nombre_estado";

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

        // 2. GET /api/clientes
        [HttpGet("clientes")]
        public async Task<IActionResult> GetClientes()
        {
            var resultados = new List<Dictionary<string, object>>();
            string query = "SELECT * FROM Clientes ORDER BY nombre";

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

        // 3. GET /api/ciudades/:id_estado
        [HttpGet("ciudades/{id_estado}")]
        public async Task<IActionResult> GetCiudades(int id_estado)
        {
            var resultados = new List<Dictionary<string, object>>();

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                // Usamos el Stored Procedure
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

        // 4. POST /api/envios
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

                        // Parámetros de ENTRADA
                        cmd.Parameters.AddWithValue("@id_cliente", request.id_cliente);
                        cmd.Parameters.AddWithValue("@id_ciudad", request.id_ciudad);
                        cmd.Parameters.AddWithValue("@descripcion", request.descripcion);

                        // Parámetro de SALIDA
                        SqlParameter outputParam = new SqlParameter
                        {
                            ParameterName = "@resultado",
                            SqlDbType = SqlDbType.VarChar,
                            Size = 100,
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(outputParam);

                        // Ejecutamos el SP
                        await cmd.ExecuteNonQueryAsync();

                        // Leemos el valor del parámetro de salida
                        resultado = outputParam.Value.ToString();
                    }
                }

                
                if (resultado.Contains("correctamente"))
                {
                    // 201 = Creado
                    return StatusCode(201, new { mensaje = resultado });
                }
                else
                {
                    // 400 = Bad Request (Cliente no encontrado, etc.)
                    return BadRequest(new { mensaje = resultado });
                }
            }
            catch (SqlException ex)
            {
                // Captura errores de SQL (RAISERROR)
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // 5. POST /api/clientes
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
                    // Error de validación (correo duplicado, etc.)
                    return BadRequest(new { mensaje = resultado });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 6. DELETE /api/clientes/5
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
                    return Ok(new { mensaje = resultado }); // 200 OK
                }
                else
                {
                    // Error de validación (cliente no existe, tiene envíos, etc.)
                    return BadRequest(new { mensaje = resultado });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno: {ex.Message}" });
            }
        }

        // 7. GET /api/envios/1
        [HttpGet("envios/{id_envio}")]
        public async Task<IActionResult> GetDetalleEnvio(int id_envio)
        {
            var resultados = new List<Dictionary<string, object>>();

            // Consulta SQL que une todas las tablas para un detalle completo
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

            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id_envio", id_envio);
                    var reader = await cmd.ExecuteReaderAsync();
                    resultados = LeerResultados(reader); // Usamos tu función helper que ya existe
                }
            }

            if (resultados.Count == 0)
            {
                return NotFound(new { mensaje = "Envío no encontrado" });
            }

            return Ok(resultados[0]); // Devuelve solo el primer (y único) resultado
        }

        // 8. GET /api/clientes/1/envios
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

            return Ok(resultados); // Devuelve la lista (puede estar vacía)
        }
    }

    

   
    // Clase para registrar un nuevo cliente
    public class ClienteRequest
    {
        public string nombre { get; set; }
        public string correo { get; set; }
    }

    // --- Clase Auxiliar ---
    // C# necesita una clase para entender el JSON que le llega en el POST
    public class EnvioRequest
    {
        public int id_cliente { get; set; }
        public int id_ciudad { get; set; }
        public string descripcion { get; set; }
    }
}