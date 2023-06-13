using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Configuration;

public class AuthClient
{
    private readonly string _endpointUrl = ConfigurationManager.AppSettings["EndpointUrl"];
    private readonly string _clientId = ConfigurationManager.AppSettings["ClientId"];
    private readonly string _clientSecret = ConfigurationManager.AppSettings["ClientSecret"];

    public AuthClient()
    {
        
    }

    public async Task<string> GetAccessToken()
    {
        using (var httpClient = new HttpClient())
        {
            var requestContent = new StringContent(
                "grant_type=client_credentials" +
                "&client_id=" + Uri.EscapeDataString(_clientId) +
                "&client_secret=" + Uri.EscapeDataString(_clientSecret),
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            var response = await httpClient.PostAsync(_endpointUrl, requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
                return tokenResponse.access_token;
            }
            else
            {
                throw new Exception("Error en la solicitud de autenticación. Código de respuesta: " + response.StatusCode);
            }
        }
    }
}

// Clase para representar la estructura del token JSON
public class TokenResponse
{
    public string access_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }
    public string user_id { get; set; }
}
