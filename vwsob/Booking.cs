using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.Odbc;
using System.Net.Http;
using Newtonsoft.Json;
using System.Globalization;
using Serilog;

namespace vwsob
{
    public class Booking
    {
        private readonly string _eventsSQLConnection = ConfigurationManager.AppSettings["EventosSQLServerConnectionString"];
        private string _token;
        private readonly ILogger _logger;

        public Booking(string token, ILogger logger)
        {
            _token = token;
            _logger = logger;
        }

        /// <summary>
        /// Transfiere los turnos actuales de VW a la agenda interna del concesionario.
        /// Este método realiza una serie de pasos:
        /// 1. Recupera la fecha y hora de la última ejecución a través de los Eventos Config.
        /// 2. Realiza una solicitud HTTP a la API de VW SOB para obtener los turnos.
        /// 3. Almacena los turnos en la agenda interna del concesionario.
        /// 4. Actualiza la fecha y hora de la última ejecución para futuras referencias.
        /// </summary>
        public async Task syncAgendaAsync()
        {
            Database eventsDB = new Database(_eventsSQLConnection);
            
            Log.Information("Abriendo conexión BBDD");
            using (OdbcConnection connection = eventsDB.OpenConnection())
            {
                Log.Information("BBDD Eventos, conectada satisfactoriamente");

                using (OdbcTransaction transaction = connection.BeginTransaction())
                {
                    OdbcCommand cmnd = new OdbcCommand("SELECT FecUltEjec, CodTaller FROM [Eventos].[dbo].[Eventos_Config] where CodEvento=10004", connection, transaction);
                    OdbcDataReader reader = cmnd.ExecuteReader();
                    cmnd.Dispose();
                    try
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException("Evento 10004 no encontrado");
                        }
                        
                        int fecUltEjecIndex = reader.GetOrdinal("FecUltEjec");
                        if (reader.IsDBNull(fecUltEjecIndex))
                        {
                            throw new InvalidOperationException("El campo FecUltEjec, de la tabla Eventos_Config está vacío");
                        }
                        int codTallerIndex = reader.GetOrdinal("CodTaller");
                        string strCodTaller = reader.IsDBNull(codTallerIndex) ? string.Empty : reader.GetString(codTallerIndex).Trim();

                        DateTime dteFecIni = reader.GetDateTime(fecUltEjecIndex);
                        DateTime lastExecution = DateTime.Now.AddMonths(-1);

                        OdbcCommand updateCommand = new OdbcCommand("UPDATE [Eventos].[dbo].[Eventos_Config] SET FecUltEjec = ? WHERE CodEvento = 10004", connection, transaction);
                        updateCommand.Parameters.Add(new OdbcParameter("@lastExecution", lastExecution));
                        updateCommand.ExecuteNonQuery();
                        
                        string url = ConfigurationManager.AppSettings["SobHostUrl"] +
                            "booking/byRegistrationDate/" +
                            ConfigurationManager.AppSettings["DealerNumber"] + "/" +
                            FormatDateTime(dteFecIni);
                        string json = await GetJsonFromEndpoint(url, _token);

                        List<RegistroAgendamiento.Registro> registros = JsonConvert.DeserializeObject<List<RegistroAgendamiento.Registro>>(json);
                        if (!registros.Any())
                        {
                            Log.Information("No hay nuevos turnos para agendar");
                        } else
                        {
                            foreach (var registro in registros)
                            {
                                ///<summary>
                                ///Guardando en la agenda interna del concesionario
                                ///</summary>

                                // Busca si el turno ya se descargó alguna vez (AgendaTurnosTerm)
                                string queryStringTerm = "SELECT Referencia FROM " + "Servicios" + ".dbo.AgendaTurnosTerm WHERE Id_TurnoTerm=" + registro.Agendamento.IdAgendamento;
                                OdbcCommand cmndTerm = new OdbcCommand(queryStringTerm, connection, transaction);
                                OdbcDataReader rsAgenda = cmndTerm.ExecuteReader();

                                if (rsAgenda.Read())
                                {
                                    Log.Information($"El turno con Id_TurnoTerm: {registro.Agendamento.IdAgendamento} ya existe en Autopack y no debe descargarse");
                                    rsAgenda.Close();
                                    continue;
                                }
                                else{
                                    rsAgenda.Close();
                                }
                                cmndTerm.Dispose();
                                long lngRef = 1;
                                string queryString = "SELECT ISNULL(MAX(Referencia), 0) + 1 as ref FROM " + "Servicios" + ".dbo.Agenda";
                                OdbcCommand cmndAgenda = new OdbcCommand(queryString, connection, transaction);
                                OdbcDataReader rsCount = cmndAgenda.ExecuteReader();
                                if (rsCount.Read())
                                {
                                    lngRef = rsCount.GetInt64(rsCount.GetOrdinal("ref"));
                                }

                                DateTime dateTimeFrom = ConvertStringToDateTime(registro.Agendamento.DataAgendamentoInicio, "dd/MM/yyyy HH:mm:ss");
                                DateTime dateTimeTo = ConvertStringToDateTime(registro.Agendamento.DataAgendamentoFim, "dd/MM/yyyy HH:mm:ss");
                                DateTime creationDate = ConvertStringToDateTime(registro.Agendamento.DtCriacao, "dd/MM/yyyy HH:mm:ss");
                              
                                string phone1 = "'',";
                                string phone2 = "'',";
                                for (int i = 0; i < Math.Min(registro.Telefones.Count, 2); i++)
                                {
                                    string phoneString = $"{registro.Telefones[i].DDD} - {registro.Telefones[i].Telefone} ({registro.Telefones[i].Tipo_Tel})";

                                    if (i == 0)
                                    {
                                        phone1 = $"'{phoneString}',";
                                    }
                                    else if (i == 1)
                                    {
                                        phone2 = $"'{phoneString}',";
                                    }
                                }
                                // Inserta registro de Agenda
                                queryString = "INSERT " + "Servicios" + ".dbo.Agenda " +
                                    "(Referencia, FechaProc,Recepcionista,NroVehiculo,KM,CodCli,Contacto,Cargo," +
                                    "FPago,FechaEnt,HoraEnt,FechaSal,HoraSal,MObra,Repuestos,Observaciones," +
                                    "Cliente,Modelo,Tel1,Tel2,EMail,Usuario,Fecha) " +
                                    "VALUES (" + lngRef.ToString() + "," +
                                    "'" + lastExecution.ToString("yyyy-MM-dd") + "'," +
                                    "'" + "SI" + "'," +
                                    "0,0,0,null,'C',''," +
                                    "'" + dateTimeFrom.ToString("yyyy-MM-dd 00:00:00.000") + "'," +
                                    "'" + dateTimeFrom.ToString("HH.mm") + "'," +
                                    "'" + dateTimeTo.ToString("yyyy-MM-dd 00:00:00.000") + "'," +
                                    "'" + dateTimeTo.ToString("HH.mm") + "'," +
                                    "0,0," +
                                    "'" + "strDetalle" + "'," +
                                    "'" + registro.Nome + "'," +
                                    "'" + registro.Agendamento.Veiculo.Modelo + "'," +
                                    phone1 +
                                    phone2 + 
                                    "'" + registro.Email + "'," +
                                    "'TurnosSOB - " + registro.Agendamento.UsuarioCriacao + "'," +
                                    "'" + creationDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "')";
                                cmnd = new OdbcCommand(queryString, connection, transaction);
                                cmnd.ExecuteNonQuery();
                                cmnd.Dispose();

                                // Busca el próximo nro. de item del día
                                queryString = "SELECT ISNULL(MAX(Item), 0) + 1 as item FROM " + "Servicios" + ".dbo.AgendaDias where Dia='" + dateTimeFrom.ToString("yyyy-MM-dd 00:00:00.000") + "'";
                                cmnd = new OdbcCommand(queryString, connection, transaction);
                                rsCount = cmnd.ExecuteReader();
                                long lngItem = 1;
                                if (rsCount.Read())
                                {
                                    lngItem = rsCount.GetInt64(rsCount.GetOrdinal("item"));
                                }
                                cmnd.Dispose();

                                // Inserta registro de AgendaDias
                                queryString = "INSERT " + "Servicios" + ".dbo.AgendaDias " +
                                        "(CodTaller, Dia, Item, Referencia) " +
                                        "VALUES ('" + strCodTaller + "'," +
                                        "'" + dateTimeFrom.ToString("yyyy-MM-dd 00:00:00.000") + "'," +
                                        lngItem.ToString() + "," +
                                        lngRef.ToString() + ")";
                                cmnd = new OdbcCommand(queryString, connection, transaction);
                                cmnd.ExecuteNonQuery();
                                cmnd.Dispose();
                                
                                string descripcionServicio = string.Empty;

                                if (registro != null && registro.Agendamento != null && registro.Agendamento.Servicios != null && registro.Agendamento.Servicios.Count > 0)
                                {
                                    descripcionServicio = registro.Agendamento.Servicios[0]?.Descripcion ?? string.Empty;
                                }
                                lngItem = 1;
                                    queryString = "INSERT " + "Servicios" + ".dbo.AgendaReclamos " +
                                        "(Referencia, Item, Reclamo) " +
                                        "VALUES (" + lngRef + "," +
                                        lngItem.ToString() + "," +
                                        "'" + descripcionServicio + "')";
                                    cmnd = new OdbcCommand(queryString, connection, transaction);
                                    cmnd.ExecuteNonQuery();
                                cmnd.Dispose();

                                //Inserta registro de AgendaTurnosTerm
                                queryString = "INSERT " + "Servicios" + ".dbo.AgendaTurnosTerm " +
                                        "(Referencia, Id_TurnoTerm, Origen) " +
                                        "VALUES (" + lngRef.ToString() + "," +
                                        registro.Agendamento.IdAgendamento + ", 'SOB')";
                                cmnd = new OdbcCommand(queryString, connection, transaction);
                                cmnd.ExecuteNonQuery();
                                cmnd.Dispose();
                            }
                        }

                        transaction.Commit();
                        reader.Close();
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex.Message);
                        Console.WriteLine(ex.Message);
                        reader.Close();
                        connection.Close();
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene los nuevos turnos de VW SOB
        /// </summary>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        static async Task<string> GetJsonFromEndpoint(string url, string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return json;
                }
                else
                {
                    Log.Error($"Error en la solicitud: {response.StatusCode}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Formatea una fecha y hora en el formato "ddMMyyyyHHmmss".
        /// </summary>
        /// <param name="fechaHora">La fecha y hora a formatear.</param>
        /// <returns>Una cadena con la fecha y hora formateadas.</returns>
        public static string FormatDateTime(DateTime fechaHora)
        {
            // Obtener los componentes de la fecha y hora
            int dia = fechaHora.Day;
            int mes = fechaHora.Month;
            int anio = fechaHora.Year;
            int hora = fechaHora.Hour;
            int minutos = fechaHora.Minute;
            int segundos = fechaHora.Second;

            // Formatear los componentes como cadenas con ceros a la izquierda si es necesario
            string diaStr = dia.ToString("00");
            string mesStr = mes.ToString("00");
            string anioStr = anio.ToString();
            string horaStr = hora.ToString("00");
            string minutosStr = minutos.ToString("00");
            string segundosStr = segundos.ToString("00");

            // Concatenar los componentes formateados en el orden deseado
            string resultado = $"{diaStr}{mesStr}{anioStr}{horaStr}{minutosStr}{segundosStr}";

            return resultado;
        }

        /// <summary>
        /// Formatea una fecha y hora en el formato "dd/MM/yyyy HH:mm:ss" y lo devuelve como datetime.
        /// </summary>
        /// <param name="dateTimeString">La fecha y hora a formatear.</param>
        /// <param name="format">formato de fecha recibido.</param>
        /// <returns>Una cadena con la fecha y hora formateadas.</returns>
        static DateTime ConvertStringToDateTime(string dateTimeString, string format)
        {
            DateTime fechaHora;

            if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaHora))
            {
                return fechaHora;
            }
            else
            {
                return DateTime.MinValue;
            }
        }

    }
}
