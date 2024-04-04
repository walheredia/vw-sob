using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static vwsob.RegistroAgendamiento;
using Newtonsoft.Json;

namespace vwsob
{
    class LocalAgenda
    {
        private readonly string _eventsSQLConnection = ConfigurationManager.AppSettings["EventosSQLServerConnectionString"];
        private readonly ILogger _logger;

        public LocalAgenda(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Transfiere los turnos actuales de la agenda local a la de VW SOB
        /// Este método realiza una serie de pasos:
        /// 1. Recupera la ultima Agenda Local almacenada internamente (Referencia) a través de los Eventos Config. NroRegistro1 es la referencia de la agenda
        /// 2. Recupera todas las agendas locales que hay que volcar en VW SOB.
        /// 3. Almacena los turnos en VW SOB.
        /// 4. Actualiza la ultima Agenda procesada, para futuras referencias.
        /// </summary>

        public async Task syncAgendaLocalToSobAsync()
        {
            var authClient = new AuthClient(Log.Logger);
            Database eventsDB = new Database(_eventsSQLConnection);

            Log.Information("Abriendo conexión BBDD");
            using (OdbcConnection connection = eventsDB.OpenConnection())
            {
                Log.Information("BBDD Eventos, conectada satisfactoriamente");
                using (OdbcTransaction transaction = connection.BeginTransaction())
                {
                    OdbcCommand cmnd = new OdbcCommand("SELECT NroRegistro1, FecUltEjec, CodTaller FROM [Eventos].[dbo].[Eventos_Config] where CodEvento=10004", connection, transaction);
                    OdbcDataReader reader = cmnd.ExecuteReader();
                    try
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException("Evento 10004 no encontrado");
                        }

                        int ultRefProcIndex = reader.GetOrdinal("NroRegistro1");
                        if (reader.IsDBNull(ultRefProcIndex))
                        {
                            throw new InvalidOperationException("El campo NroRegistro1, de la tabla Eventos_Config está vacío");
                        }
                        Decimal ultRefProc = reader.GetDecimal(ultRefProcIndex);
                        reader.Close();
                        cmnd.Dispose();

                        //obtenemos las agendas locales 
                        OdbcCommand cmnd2 = new OdbcCommand("SELECT * FROM [Servicios].[dbo].[Agenda]" +
                            " WHERE Referencia > " + ultRefProc +
                            " AND NOT EXISTS (" +
                                "SELECT 1 FROM [Servicios].[dbo].[AgendaTurnosTerm]" +
                                    " WHERE AgendaTurnosTerm.Referencia = Agenda.Referencia" +
                                ")" +
                            " ORDER BY Referencia", connection, transaction);
                        OdbcDataReader reader2 = cmnd2.ExecuteReader();

                        //Obtengo token de SOB
                        string accessToken = authClient.GetAccessToken().GetAwaiter().GetResult();
                        //Fin Obtengo token de SOB

                        while (reader2.Read())
                        {
                            int referenciaIndex = reader2.GetOrdinal("Referencia");
                            int referencia = reader2.IsDBNull(referenciaIndex) ? 0 : reader2.GetInt32(referenciaIndex);
                            Console.WriteLine(referencia);

                            int fechaProcIndex = reader2.GetOrdinal("FechaProc");
                            DateTime fechaProc = !reader2.IsDBNull(fechaProcIndex) ? reader2.GetDateTime(fechaProcIndex) : DateTime.Now;


                            string recepcionista = reader2.GetString(reader2.GetOrdinal("Recepcionista"));

                            int nroVehiculoIndex = reader2.GetOrdinal("NroVehiculo");
                            int nroVehiculo = reader2.GetInt32(nroVehiculoIndex);

                            string modelo = !reader2.IsDBNull(reader2.GetOrdinal("Modelo")) ? reader2.GetString(reader2.GetOrdinal("Modelo")) : null;

                            int kmIndex = reader2.GetOrdinal("Km");
                            int km = reader2.GetInt32(kmIndex);

                            short combustible = reader2.GetInt16(reader2.GetOrdinal("Combustible"));

                            int codCliIndex = reader2.GetOrdinal("CodCli");
                            int codCli = reader2.GetInt32(codCliIndex);

                            string cliente = !reader2.IsDBNull(reader2.GetOrdinal("Cliente")) ? reader2.GetString(reader2.GetOrdinal("Cliente")) : null;

                            string tel1 = !reader2.IsDBNull(reader2.GetOrdinal("Tel1")) ? reader2.GetString(reader2.GetOrdinal("Tel1")) : null;

                            string tel2 = !reader2.IsDBNull(reader2.GetOrdinal("Tel2")) ? reader2.GetString(reader2.GetOrdinal("Tel2")) : null;

                            string email = !reader2.IsDBNull(reader2.GetOrdinal("Email")) ? reader2.GetString(reader2.GetOrdinal("Email")) : null;

                            string contacto = !reader2.IsDBNull(reader2.GetOrdinal("Contacto")) ? reader2.GetString(reader2.GetOrdinal("Contacto")) : null;

                            string cargo = reader2.GetString(reader2.GetOrdinal("Cargo"));

                            string fPago = !reader2.IsDBNull(reader2.GetOrdinal("FPago")) ? reader2.GetString(reader2.GetOrdinal("FPago")) : null;

                            bool remisTaxi = reader2.GetBoolean(reader2.GetOrdinal("RemisTaxi"));

                            bool recepDinamica = reader2.GetBoolean(reader2.GetOrdinal("RecepDinamica"));

                            bool cedulaVerde = reader2.GetBoolean(reader2.GetOrdinal("CedulaVerde"));

                            bool manualServ = reader2.GetBoolean(reader2.GetOrdinal("ManualServ"));

                            DateTime fechaEnt = reader2.GetDateTime(reader2.GetOrdinal("FechaEnt"));

                            decimal horaEnt = reader2.GetDecimal(reader2.GetOrdinal("HoraEnt"));

                            DateTime fechaSal = reader2.GetDateTime(reader2.GetOrdinal("FechaSal"));

                            decimal horaSal = reader2.GetDecimal(reader2.GetOrdinal("HoraSal"));

                            decimal horas = reader2.GetDecimal(reader2.GetOrdinal("Horas"));

                            bool impGlobalPresu = reader2.GetBoolean(reader2.GetOrdinal("ImpGlobalPresu"));

                            decimal mObra = reader2.GetDecimal(reader2.GetOrdinal("MObra"));

                            decimal repuestos = reader2.GetDecimal(reader2.GetOrdinal("Repuestos"));

                            int codCampProm = reader2.GetInt32(reader2.GetOrdinal("CodCampProm"));

                            string observaciones = !reader2.IsDBNull(reader2.GetOrdinal("Observaciones")) ? reader2.GetString(reader2.GetOrdinal("Observaciones")) : null;

                            bool reconfTurno = reader2.GetBoolean(reader2.GetOrdinal("ReconfTurno"));

                            bool confRepuestos = reader2.GetBoolean(reader2.GetOrdinal("ConfRepuestos"));

                            int? nroCompIntPr = reader2.IsDBNull(reader2.GetOrdinal("NroCompIntPr")) ? (int?)null : reader2.GetInt32(reader2.GetOrdinal("NroCompIntPr"));

                            bool clienteEspera = reader2.GetBoolean(reader2.GetOrdinal("ClienteEspera"));

                            short reparRepetida = reader2.GetInt16(reader2.GetOrdinal("ReparRepetida"));

                            bool campaña = reader2.GetBoolean(reader2.GetOrdinal("Campaña"));

                            bool aCampo = reader2.GetBoolean(reader2.GetOrdinal("ACampo"));

                            bool revision = reader2.GetBoolean(reader2.GetOrdinal("Revision"));

                            int? ultNroCompIntOR = reader2.IsDBNull(reader2.GetOrdinal("UltNroCompIntOR")) ? (int?)null : reader2.GetInt32(reader2.GetOrdinal("UltNroCompIntOR"));

                            string comentariosPreOr = !reader2.IsDBNull(reader2.GetOrdinal("ComentariosPreOr")) ? reader2.GetString(reader2.GetOrdinal("ComentariosPreOr")) : null;

                            string codTipCompPreOr = !reader2.IsDBNull(reader2.GetOrdinal("CodTipCompPreOr")) ? reader2.GetString(reader2.GetOrdinal("CodTipCompPreOr")) : null;

                            bool avisoEvento = reader2.GetBoolean(reader2.GetOrdinal("AvisoEvento"));

                            short? envioSMS = reader2.IsDBNull(reader2.GetOrdinal("EnvioSMS")) ? (short?)null : reader2.GetInt16(reader2.GetOrdinal("EnvioSMS"));

                            bool peritajeFirmado = reader2.GetBoolean(reader2.GetOrdinal("PeritajeFirmado"));

                            bool peritajeEnviado = reader2.GetBoolean(reader2.GetOrdinal("PeritajeEnviado"));

                            DateTime? fechaRecep = reader2.IsDBNull(reader2.GetOrdinal("FechaRecep")) ? (DateTime?)null : reader2.GetDateTime(reader2.GetOrdinal("FechaRecep"));

                            DateTime? fechaEnvioTerminal = reader2.IsDBNull(reader2.GetOrdinal("FechaEnvioTerminal")) ? (DateTime?)null : reader2.GetDateTime(reader2.GetOrdinal("FechaEnvioTerminal"));

                            bool demorado = reader2.GetBoolean(reader2.GetOrdinal("Demorado"));

                            string usuario = !reader2.IsDBNull(reader2.GetOrdinal("Usuario")) ? reader2.GetString(reader2.GetOrdinal("Usuario")) : null;

                            string usuarioM = !reader2.IsDBNull(reader2.GetOrdinal("UsuarioM")) ? reader2.GetString(reader2.GetOrdinal("UsuarioM")) : null;

                            DateTime? fecha = reader2.IsDBNull(reader2.GetOrdinal("Fecha")) ? (DateTime?)null : reader2.GetDateTime(reader2.GetOrdinal("Fecha"));

                            DateTime? fechaM = reader2.IsDBNull(reader2.GetOrdinal("FechaM")) ? (DateTime?)null : reader2.GetDateTime(reader2.GetOrdinal("FechaM"));

                            string movCodTaller = !reader2.IsDBNull(reader2.GetOrdinal("MovCodTaller")) ? reader2.GetString(reader2.GetOrdinal("MovCodTaller")) : null;

                            DateTime? movDia = reader2.IsDBNull(reader2.GetOrdinal("MovDia")) ? (DateTime?)null : reader2.GetDateTime(reader2.GetOrdinal("MovDia"));

                            decimal? movHora = reader2.IsDBNull(reader2.GetOrdinal("MovHora")) ? (decimal?)null : reader2.GetDecimal(reader2.GetOrdinal("MovHora"));

                            string movUsuario = !reader2.IsDBNull(reader2.GetOrdinal("MovUsuario")) ? reader2.GetString(reader2.GetOrdinal("MovUsuario")) : null;

                            DateTime? movFecha = reader2.IsDBNull(reader2.GetOrdinal("MovFecha")) ? (DateTime?)null : reader2.GetDateTime(reader2.GetOrdinal("MovFecha"));

                            // Construir el JSON utilizando los datos obtenidos

                            int agendamientoId = 0;
                            //Registramos turno en SOB
                            string url = ConfigurationManager.AppSettings["SobHostUrl"] + "booking";

                            
                            // Extrae la hora y los minutos
                            string[] horaMinutos = horaEnt.ToString().Split(',');
                            string horaFormatted = horaMinutos[0].Length == 1 ? "0" + horaMinutos[0] : horaMinutos[0];
                            string minutosFormatted = horaMinutos[1].Length == 1 ? "0" + horaMinutos[1] : horaMinutos[1];
                            string horaEntFormatted = horaFormatted + ":" + minutosFormatted + ":00";
                            string fechaEntFormatted = fechaEnt.ToString("dd/MM/yyyy") + " " + horaEntFormatted;

                            string[] horaMinutosSal = horaSal.ToString().Split(',');
                            string horaSalFormatted = horaMinutosSal[0].Length == 1 ? "0" + horaMinutosSal[0] : horaMinutosSal[0];
                            string minutosSalFormatted = horaMinutosSal[1].Length == 1 ? "0" + horaMinutosSal[1] : horaMinutosSal[1];
                            string horaFormattedSal = horaSalFormatted + ":" + minutosSalFormatted + ":00";
                            string fechaSalFormatted = fechaSal.ToString("dd/MM/yyyy") + " " + horaFormattedSal;

                            string jsonBody = @"
                            {
                                ""Agendamento"": {
                                    ""CpfConsultor"": """ + recepcionista.Trim() + @""",
                                    ""NomeConsultor"": """ + recepcionista.Trim() + @""",
                                    ""DataAgendamentoInicio"": """ + fechaEntFormatted + @""",
                                    ""DataAgendamentoFim"": """ + fechaSalFormatted + @""",
                                    ""DtAlteracao"": """ + fechaProc.ToString("dd/MM/yyyy HH:mm:ss") + @""",
                                    ""DtCriacao"": """ + fechaProc.ToString("dd/MM/yyyy HH:mm:ss") + @""",
                                    ""IdAgendamento"": """ + agendamientoId + @""",
                                    ""IdStatus"": 0,
                                    ""MinutosExecucao"": 0,
                                    ""ReparoRepetitivo"": ""Reparo Repetitivo"",
                                    ""Servicos"": [
                                        {
                                            ""TipoServico"": 1,
                                            ""Descricao"": ""Servicio de Mantenimiento"",
                                            ""Observacao"": ""1° Servicio""
                                        }
                                    ],
                                    ""TipoAgendamento"": 0,
                                    ""UsuarioAlteracao"": ""EMosto"",
                                    ""UsuarioCriacao"": ""EMosto"",
                                    ""Veiculo"": {
                                        ""AnoModelo"": ""2020"",
                                        ""Chassi"": """",
                                        ""CodigoDoMotor"": ""CodigoMotor"",
                                        ""CorExterna"": ""CorExterna"",
                                        ""CorInterna"": ""CorInterna"",
                                        ""DataDaVenda"": ""DataDaVenda"",
                                        ""DataEmplacamento"": ""DataEmplacamento"",
                                        ""DescricaoModelo"": ""DescricaoModelo"",
                                        ""Marca"": ""Marca"",
                                        ""Modelo"": ""Modelo Ejemplo"",
                                        ""NrMotor"": ""NrMotor"",
                                        ""Placa"": ""AE407VW"",
                                        ""TipoDeTransmissao"": ""Manual""
                                    }
                                },
                                ""AutorizaDados"": ""AutorizaDados"",
                                ""CpfCnpj"": ""34874283"",
                                ""DN"": ""53056"",
                                ""Email"": ""emosto@informixsys.com.ar"",
                                ""Nome"": ""Emiliano Mosto"",
                                ""Origem_Id"": 1,
                                ""Preferencia_Contato"": 0,
                                ""Sub_Origem_Id"": 16,
                                ""Telefones"": [
                                    {
                                        ""DDD"": ""0336"",
                                        ""Telefone"": ""154018012"",
                                        ""Tipo_Tel"": 1
                                    },
                                    {
                                        ""DDD"": ""0336"",
                                        ""Telefone"": ""154211254"",
                                        ""Tipo_Tel"": 2
                                    }
                                ]
                            }";
                            Log.Error(jsonBody);
                            // Configurar el cliente HttpClient
                            using (HttpClient client = new HttpClient())
                            {
                                // Configurar el encabezado de autorización
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                                // Configurar el contenido de la solicitud como JSON
                                StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                                // Realizar la solicitud POST
                                HttpResponseMessage response = await client.PostAsync(url, content);

                                if (response.IsSuccessStatusCode)
                                {
                                    // Leer la respuesta como JSON
                                    string responseBody = await response.Content.ReadAsStringAsync();

                                    // Deserializar la respuesta JSON en un objeto de la clase ResponseModel
                                    ResponseModel responseModel = JsonConvert.DeserializeObject<ResponseModel>(responseBody);

                                    // Imprimir la información de la respuesta
                                    Console.WriteLine("AgendamentoId: " + responseModel.AgendamentoId);
                                    Console.WriteLine("Message: " + responseModel.Message);
                                    Console.WriteLine("Status: " + responseModel.Status);
                                    Console.WriteLine("StatusMessage: " + responseModel.StatusMessage);
                                    agendamientoId = responseModel.AgendamentoId;

                                    //Inserta registro de AgendaTurnosTerm
                                    using (OdbcConnection connection2 = eventsDB.OpenConnection())
                                    {
                                        string queryString = "INSERT " + "Servicios" + ".dbo.AgendaTurnosTerm " +
                                                "(Referencia, Id_TurnoTerm, Origen) " +
                                        "VALUES (" + referencia.ToString() + "," +
                                                agendamientoId + ", 'AUT')";
                                        OdbcCommand cmnd3 = new OdbcCommand(queryString, connection2);
                                        cmnd3.ExecuteNonQuery();
                                        cmnd3.Dispose();
                                    }
                                }
                                else
                                {
                                    // Imprimir el código de estado si la solicitud falló
                                    Console.WriteLine($"Error: {response.StatusCode}");
                                }
                            }
                            //Fin registro en SOB

                            
                        }

                        Log.Information(ultRefProc.ToString());
                        reader2.Close();
                        cmnd2.Dispose();
                        transaction.Commit();
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Log.Error(ex.Message);
                        Console.WriteLine(ex.Message);
                        reader.Close();
                        connection.Close();
                       
                    } finally
                    {
                        connection.Close();
                    }
                }
            }
        }
    }
}

public class ResponseModel
{
    public int AgendamentoId { get; set; }
    public string Message { get; set; }
    public int Status { get; set; }
    public string StatusMessage { get; set; }
}