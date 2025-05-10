using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace EstateEase.Services
{
    public interface IReCaptchaService
    {
        Task<ReCaptchaResponse> VerifyAsync(string recaptchaResponse, string remoteIp = null);
    }

    public class ReCaptchaService : IReCaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReCaptchaService> _logger;

        public ReCaptchaService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ReCaptchaService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ReCaptchaResponse> VerifyAsync(string recaptchaResponse, string remoteIp = null)
        {
            try
            {
                if (string.IsNullOrEmpty(recaptchaResponse))
                {
                    _logger.LogWarning("reCAPTCHA response is empty");
                    return new ReCaptchaResponse { Success = false, ErrorCodes = new[] { "missing-input-response" } };
                }

                var secretKey = _configuration["ReCaptcha:SecretKey"];
                var url = $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaResponse}";
                
                if (!string.IsNullOrEmpty(remoteIp))
                {
                    url += $"&remoteip={remoteIp}";
                }

                var response = await _httpClient.PostAsync(url, null);
                var responseString = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"reCAPTCHA response: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"reCAPTCHA verification failed with status code: {response.StatusCode}");
                    return new ReCaptchaResponse 
                    { 
                        Success = false, 
                        ErrorCodes = new[] { $"http-error-{response.StatusCode}" } 
                    };
                }

                var recaptchaResult = JsonSerializer.Deserialize<ReCaptchaResponse>(responseString);
                return recaptchaResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying reCAPTCHA");
                return new ReCaptchaResponse 
                { 
                    Success = false, 
                    ErrorCodes = new[] { "internal-error" } 
                };
            }
        }
    }

    public class ReCaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public DateTime? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }
    }
} 