using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EstateEase.Data;
using EstateEase.Models.Entities;
using Microsoft.AspNetCore.Identity;
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
        Task CreateUserPropertyAssociation(string userId, string propertyId, string relationshipType);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public PaymentService(
            IConfiguration configuration,
            HttpClient httpClient,
            ApplicationDbContext context,
            ILogger<PaymentService> logger,
            UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            _userManager = userManager;

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
                        
                        // Create a UserProperty record for the renter
                        await CreateUserPropertyAssociation(transaction.UserId, property.Id, "Renter");
                    }
                    else if (transaction.TransactionType == "Purchase")
                    {
                        property.Status = "Sold";
                        
                        // Create a UserProperty record for the owner
                        await CreateUserPropertyAssociation(transaction.UserId, property.Id, "Owner");
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

        public async Task CreateUserPropertyAssociation(string userId, string propertyId, string relationshipType)
        {
            _logger.LogInformation($"Creating user property association: User={userId}, Property={propertyId}, Type={relationshipType}");
            
            try
            {
                // Input validation
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Cannot create user property association: User ID is null or empty");
                    return;
                }
                
                if (string.IsNullOrEmpty(propertyId))
                {
                    _logger.LogError("Cannot create user property association: Property ID is null or empty");
                    return;
                }
                
                // Debug information
                _logger.LogInformation($"Creating association for User ID: '{userId}', Property ID: {propertyId}, Type: {relationshipType}");
                
                // Check if property exists
                var property = await _context.Properties.FindAsync(propertyId);
                if (property == null)
                {
                    _logger.LogError($"Cannot create user property association: Property ID {propertyId} not found in database");
                    return;
                }
                
                // Check if user exists
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError($"Cannot create user property association: User ID '{userId}' not found in database");
                    return;
                }
                
                // Check if association already exists
                var existingAssociation = await _context.UserProperties
                    .FirstOrDefaultAsync(up => up.UserId.ToLower() == userId.ToLower() && up.PropertyId == propertyId);
                
                _logger.LogInformation($"Checking for existing association with UserID: {userId.ToLower()}, PropertyID: {propertyId}");
                    
                if (existingAssociation == null)
                {
                    // Find the most recent transaction for this property and user
                    // to get rental duration information
                    var transaction = await _context.Transactions
                        .Where(t => t.PropertyId == propertyId && 
                               (t.UserId.ToLower() == userId.ToLower() || t.UserId == userId) && 
                               t.Status == "Completed")
                        .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt)
                        .FirstOrDefaultAsync();
                        
                    // Calculate ExpiryDate based on transaction data
                    DateTime acquisitionDate = DateTime.Now;
                    DateTime? expiryDate = null;
                    
                    if (transaction != null && relationshipType == "Renter")
                    {
                        // First try StartDate and EndDate
                        if (transaction.StartDate.HasValue && transaction.EndDate.HasValue)
                        {
                            acquisitionDate = transaction.StartDate.Value;
                            expiryDate = transaction.EndDate.Value;
                            _logger.LogInformation($"Setting expiry date from transaction dates: {expiryDate}");
                        }
                        else
                        {
                            // Try to determine duration from transaction amount and property price
                            if (property.Price > 0 && transaction.Amount > 0)
                            {
                                int estimatedMonths = (int)Math.Ceiling(transaction.Amount / property.Price);
                                expiryDate = acquisitionDate.AddMonths(estimatedMonths);
                                _logger.LogInformation($"Calculated expiry date from transaction amount: {expiryDate} (estimated {estimatedMonths} months)");
                            }
                            else
                            {
                                // Default to 12 months rental
                                expiryDate = acquisitionDate.AddMonths(12);
                                _logger.LogInformation($"Using default 12-month expiry date: {expiryDate}");
                            }
                        }
                    }
                    
                    // Create new association
                    var userProperty = new UserProperty
                    {
                        UserId = userId,
                        PropertyId = propertyId,
                        OwnershipType = relationshipType == "Owner" ? "Bought" : "Rented",
                        RelationshipType = relationshipType,
                        AcquisitionDate = acquisitionDate,
                        ExpiryDate = relationshipType == "Renter" ? expiryDate : null,  // Only set expiry for rentals
                        CreatedAt = DateTime.Now
                    };
                    
                    _logger.LogInformation($"Creating new UserProperty association with type: {userProperty.RelationshipType}, expiry: {userProperty.ExpiryDate}");
                    
                    await _context.UserProperties.AddAsync(userProperty);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Successfully created user property association with ID: {userProperty.Id}");
                }
                else
                {
                    _logger.LogInformation($"Association already exists with ID: {existingAssociation.Id}");
                    
                    // Update existing association if needed
                    bool needsUpdate = false;
                    
                    // If it's a purchase/sale, always update to Owner
                    if ((relationshipType == "Sale" || relationshipType == "Purchase") 
                        && existingAssociation.RelationshipType != "Owner")
                    {
                        existingAssociation.RelationshipType = "Owner";
                        existingAssociation.OwnershipType = "Bought";
                        existingAssociation.ExpiryDate = null; // Remove expiry date for purchased properties
                        needsUpdate = true;
                    }
                    
                    if (needsUpdate)
                    {
                        existingAssociation.UpdatedAt = DateTime.Now;
                        _context.UserProperties.Update(existingAssociation);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated existing association");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user property association: {ex.Message}");
                throw; // Re-throw to propagate the error
            }
        }

        public async Task<List<Transaction>> GetUserTransactions(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting transactions for user: {userId}");
                
                // Explicitly query without projection first to check total count
                var allTransactionsCount = await _context.Transactions
                    .CountAsync(t => EF.Functions.Like(t.UserId, userId));
                
                _logger.LogInformation($"Found {allTransactionsCount} total transactions for user ID: {userId}");
                
                // Use explicit projection to avoid issues with missing RentalDuration column
                var transactions = await _context.Transactions
                    .Include(t => t.Property)
                    .Where(t => t.UserId.ToLower() == userId.ToLower() || t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new Transaction
                    {
                        Id = t.Id,
                        UserId = t.UserId,
                        PropertyId = t.PropertyId,
                        Property = t.Property,
                        Amount = t.Amount,
                        TransactionType = t.TransactionType,
                        Status = t.Status,
                        PaymentMethod = t.PaymentMethod,
                        PaymentProvider = t.PaymentProvider,
                        PaymentId = t.PaymentId,
                        CheckoutSessionId = t.CheckoutSessionId,
                        ReferenceNumber = t.ReferenceNumber,
                        Notes = t.Notes,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        CompletedAt = t.CompletedAt,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        IsReservation = t.IsReservation,
                        ReservationAmount = t.ReservationAmount
                        // RentalDuration is intentionally omitted to avoid errors if column doesn't exist
                    })
                    .ToListAsync();
                
                _logger.LogInformation($"Retrieved {transactions.Count} transactions after projection for user: {userId}");
                
                // Calculate the rental duration for each transaction from related data
                foreach (var transaction in transactions)
                {
                    try
                    {
                        // First try: Try to calculate from UserProperties (most accurate)
                        var userProperty = await _context.UserProperties
                            .FirstOrDefaultAsync(up => 
                                (up.UserId.ToLower() == userId.ToLower() || up.UserId == userId) && 
                                up.PropertyId == transaction.PropertyId);
                        
                        if (userProperty != null && userProperty.AcquisitionDate != default && userProperty.ExpiryDate.HasValue)
                        {
                            // Calculate months between acquisition and expiry
                            int months = ((userProperty.ExpiryDate.Value.Year - userProperty.AcquisitionDate.Year) * 12) + 
                                         (userProperty.ExpiryDate.Value.Month - userProperty.AcquisitionDate.Month);
                            transaction.RentalDuration = Math.Max(1, months); // Ensure at least 1 month
                            _logger.LogInformation($"Transaction {transaction.Id}: Set rental duration to {transaction.RentalDuration} months based on UserProperty");
                            continue;
                        }
                        
                        // Second try: Try to calculate from transaction dates
                        if (transaction.StartDate.HasValue && transaction.EndDate.HasValue)
                        {
                            int months = ((transaction.EndDate.Value.Year - transaction.StartDate.Value.Year) * 12) + 
                                         (transaction.EndDate.Value.Month - transaction.StartDate.Value.Month);
                            transaction.RentalDuration = Math.Max(1, months); // Ensure at least 1 month
                            _logger.LogInformation($"Transaction {transaction.Id}: Set rental duration to {transaction.RentalDuration} months based on transaction dates");
                            continue;
                        }
                        
                        // Last resort: Use default value of 12 months
                        transaction.RentalDuration = 12;
                        _logger.LogInformation($"Transaction {transaction.Id}: Set default rental duration to 12 months");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error calculating rental duration for transaction {transaction.Id}");
                        transaction.RentalDuration = 12; // Fallback to default
                    }
                }
                
                return transactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching user transactions: {ex.Message}");
                // Return empty list instead of throwing, to prevent cascading errors
                return new List<Transaction>();
            }
        }
    }
} 