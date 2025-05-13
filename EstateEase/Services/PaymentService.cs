using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EstateEase.Data;
using EstateEase.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EstateEase.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSession(Transaction transaction, string successUrl, string cancelUrl);
        Task<Transaction> GetTransactionByCheckoutId(string checkoutId);
        Task<Transaction> UpdateTransactionStatus(string transactionId, string status);
        Task<bool> ProcessWebhookEvent(string payload, string signature);
        Task<List<Transaction>> GetUserTransactions(string userId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IConfiguration configuration,
            HttpClient httpClient,
            ApplicationDbContext context,
            ILogger<PaymentService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _context = context;
            _logger = logger;

            // Configure HttpClient for PayMongo
            string secretKey = _configuration["PayMongo:SecretKey"];
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{secretKey}:"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> CreateCheckoutSession(Transaction transaction, string successUrl, string cancelUrl)
        {
            try
            {
                // Log for debugging
                _logger.LogInformation($"Creating checkout session for transaction {transaction.Id} with amount {transaction.Amount}");
                _logger.LogInformation($"Success URL: {successUrl}, Cancel URL: {cancelUrl}");

                // Simplified request format for PayMongo based on their documentation
                var payload = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            line_items = new[]
                            {
                                new
                                {
                                    name = $"Payment for {transaction.Property.Title}",
                                    amount = (int)(transaction.Amount * 100), // Convert to centavos
                                    currency = "PHP",
                                    quantity = 1
                                }
                            },
                            payment_method_types = new[] { "card" }, // Simplify to just card for now
                            success_url = successUrl,
                            cancel_url = cancelUrl
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                _logger.LogInformation($"Request payload: {jsonPayload}");
                
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // Re-create the authorization header to ensure it's fresh
                string secretKey = _configuration["PayMongo:SecretKey"];
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{secretKey}:"));
                
                // Remove existing Authorization header if present
                if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                }
                
                // Add fresh Authorization header
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
                
                // Log the headers for debugging
                _logger.LogInformation($"Authorization header exists: {_httpClient.DefaultRequestHeaders.Contains("Authorization")}");
                
                var response = await _httpClient.PostAsync("https://api.paymongo.com/v1/checkout_sessions", content);

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Response status: {response.StatusCode}");
                _logger.LogInformation($"Response body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var checkoutSessionId = jsonResponse.GetProperty("data").GetProperty("id").GetString();
                    var checkoutUrl = jsonResponse.GetProperty("data").GetProperty("attributes").GetProperty("checkout_url").GetString();

                    // Update transaction with checkout session id
                    transaction.CheckoutSessionId = checkoutSessionId;
                    transaction.Status = "Pending";
                    await _context.SaveChangesAsync();

                    return checkoutUrl;
                }
                else
                {
                    _logger.LogError($"PayMongo checkout creation failed: {responseBody}");
                    throw new Exception($"Failed to create checkout session: {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating PayMongo checkout session: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                
                throw;
            }
        }

        public async Task<Transaction> GetTransactionByCheckoutId(string checkoutId)
        {
            return await _context.Transactions
                .Include(t => t.Property)
                .FirstOrDefaultAsync(t => t.CheckoutSessionId == checkoutId);
        }

        public async Task<Transaction> UpdateTransactionStatus(string transactionId, string status)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
            {
                throw new Exception($"Transaction not found: {transactionId}");
            }

            transaction.Status = status;
            transaction.UpdatedAt = DateTime.Now;

            if (status == "Completed")
            {
                transaction.CompletedAt = DateTime.Now;
                
                // Update property status based on transaction type
                var property = await _context.Properties.FindAsync(transaction.PropertyId);
                if (property != null)
                {
                    if (transaction.TransactionType == "Rent")
                    {
                        property.Status = "Rented";
                    }
                    else if (transaction.TransactionType == "Purchase")
                    {
                        property.Status = "Sold";
                    }
                    
                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> ProcessWebhookEvent(string payload, string signature)
        {
            try
            {
                // Verify webhook signature (simplified for now)
                // In production, implement proper signature verification with your webhook secret
                
                var jsonData = JsonSerializer.Deserialize<JsonElement>(payload);
                var eventType = jsonData.GetProperty("type").GetString();
                var data = jsonData.GetProperty("data");
                
                if (eventType == "checkout_session.payment_succeeded")
                {
                    var checkoutSessionId = data.GetProperty("id").GetString();
                    var transaction = await GetTransactionByCheckoutId(checkoutSessionId);
                    
                    if (transaction != null)
                    {
                        // Update payment details from the webhook
                        var paymentId = data.GetProperty("attributes").GetProperty("payment_id").GetString();
                        transaction.PaymentId = paymentId;
                        transaction.Status = "Completed";
                        transaction.CompletedAt = DateTime.Now;
                        
                        // Update property status
                        var property = await _context.Properties.FindAsync(transaction.PropertyId);
                        if (property != null)
                        {
                            if (transaction.TransactionType == "Rent")
                            {
                                property.Status = "Rented";
                                
                                // Create a UserProperty record for the renter
                                await CreateUserPropertyAssociation(transaction.UserId, property.Id, "Renter");
                            }
                            else if (transaction.TransactionType == "Purchase")
                            {
                                property.Status = "Sold";
                                
                                // Create a UserProperty record for the owner
                                await CreateUserPropertyAssociation(transaction.UserId, property.Id, "Owner");
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook event");
                return false;
            }
        }

        private async Task CreateUserPropertyAssociation(string userId, string propertyId, string relationshipType)
        {
            // Check if association already exists
            var existingAssociation = await _context.UserProperties
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PropertyId == propertyId);
                
            if (existingAssociation == null)
            {
                var userProperty = new UserProperty
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    PropertyId = propertyId,
                    RelationshipType = relationshipType,
                    CreatedAt = DateTime.Now
                };
                
                _context.UserProperties.Add(userProperty);
                await _context.SaveChangesAsync();
            }
            else if (existingAssociation.RelationshipType != relationshipType)
            {
                // Update relationship type if it's different
                existingAssociation.RelationshipType = relationshipType;
                existingAssociation.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Transaction>> GetUserTransactions(string userId)
        {
            return await _context.Transactions
                .Include(t => t.Property)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
} 