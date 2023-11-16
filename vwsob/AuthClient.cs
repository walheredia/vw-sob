using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Configuration;
using Serilog;

public class AuthClient
{
    private readonly ILogger _logger;
    private readonly string _endpointUrl = ConfigurationManager.AppSettings["LoginEndpointUrl"];
    private readonly string _clientId = ConfigurationManager.AppSettings["ClientId"];
    private readonly string _clientSecret = ConfigurationManager.AppSettings["ClientSecret"];

    public AuthClient(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<string> GetAccessToken()
    {
        using (var httpClient = new HttpClient())
        {
            _logger.Information("Autenticando contra SOB");
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
                var error = "Error en la solicitud de autenticación. Código de respuesta: " + response.StatusCode;
                _logger.Error(error);
                throw new Exception(error);
            }
        }
    }
}

public class TokenResponse
{
    public string access_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }
    public string user_id { get; set; }
}
