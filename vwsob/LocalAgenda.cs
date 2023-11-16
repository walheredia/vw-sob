using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// 1. Recupera la ultima Agenda Local almacenada internamente (Referencia) a través de los Eventos Config.
        /// 2. Recupera todas las agendas locales que hay que volcar en VW SOB.
        /// 3. Almacena los turnos en VW SOB.
        /// 4. Actualiza la ultima Agenda procesada, para futuras referencias.
        /// </summary>

        public async Task syncAgendaLocalToSobAsync()
        {
            Database eventsDB = new Database(_eventsSQLConnection);

            Log.Information("Abriendo conexión BBDD");
            using (OdbcConnection connection = eventsDB.OpenConnection())
            {
                Log.Information("BBDD Eventos, conectada satisfactoriamente");
                using (OdbcTransaction transaction = connection.BeginTransaction())
                {
                    OdbcCommand cmnd = new OdbcCommand("SELECT NroRegistro1, FecUltEjec, CodTaller FROM [Eventos].[dbo].[Eventos_Config] where CodEvento=10019", connection, transaction);
                    OdbcDataReader reader = cmnd.ExecuteReader();
                    cmnd.Dispose();
                    try
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException("Evento 10019 no encontrado");
                        }

                        int ultRefProcIndex = reader.GetOrdinal("NroRegistro1");
                        if (reader.IsDBNull(ultRefProcIndex))
                        {
                            throw new InvalidOperationException("El campo NroRegistro1, de la tabla Eventos_Config está vacío");
                        }
                        Decimal ultRefProc = reader.GetDecimal(ultRefProcIndex);
                        Log.Information(ultRefProc.ToString());

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
    }
}
