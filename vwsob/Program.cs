using System;



class Program
{
    static void Main(string[] args)
    {

        // Crea una instancia del cliente de autenticación
        var authClient = new AuthClient();

        try
        {
            string accessToken = authClient.GetAccessToken().GetAwaiter().GetResult();
            Console.WriteLine(accessToken);

            // Continúa con las operaciones adicionales utilizando el access token obtenido
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Console.ReadKey();
        }
    }
}
