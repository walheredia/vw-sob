using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Serilog;


class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Inicio de ejecución");
        var loggerConfig = new vwsob.LoggerConfig();
        Log.Logger = loggerConfig.CreateLogger();

        var authClient = new AuthClient(Log.Logger);
        string dealerNumber = ConfigurationManager.AppSettings["DealerNumber"];
        Log.Information("Dealer Number: " + dealerNumber);

        try
        {
            Log.Information("Inicio ejecución");
            Log.Information("Interpretando argumentos");

            vwsob.LocalAgenda localAgenda = new vwsob.LocalAgenda(Log.Logger);
            Task.Run(async () =>
            {
                await localAgenda.syncAgendaLocalToSobAsync();
            }).GetAwaiter().GetResult();
            return;


            var arguments = ParseArguments(args);
            if (arguments.ContainsKey("sync-to-sob"))
            {
                Log.Information("Inicio sincronización (sync-to-sob)");
                Log.Information("Fin sincronización (sync-to-sob)");
            }

            if (arguments.ContainsKey("sync-from-sob")) 
            {
                Log.Information("Inicio sincronización (sync-from-sob)");
                string accessToken = authClient.GetAccessToken().GetAwaiter().GetResult();
                Log.Information("Token SOB: " + accessToken);
                vwsob.Booking agenda = new vwsob.Booking(accessToken, Log.Logger);
                Log.Information("Sincronizando agenda interna desde SOB");
                Task.Run(async () =>
                {
                    await agenda.syncAgendaAsync();
                }).GetAwaiter().GetResult();
                Log.Information("Fin sincronización (sync-from-sob)");
            }
            

        }   
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Console.WriteLine("Fin de ejecución");
            Log.Information("Fin de ejecución");
            Log.CloseAndFlush();
            Console.ReadKey();
        }
    }

    //Este método permite interpretar los argumentos recibidos, si los hay, bajo este formato --argumento=valor
    static Dictionary<string, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.StartsWith("--"))
            {
                string key = arg.Substring(2);
                string value = "true"; // Valor predeterminado si no se proporciona un valor explícito

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    // Si el siguiente argumento no es una opción, lo consideramos como el valor
                    value = args[i + 1];
                    i++; // Avanzamos al siguiente argumento
                }

                arguments[key] = value;
            }
        }

        return arguments;
    }
}
