# EstateEase

A modern real estate platform with integrated payment processing via PayMongo.

## Development Setup

### API Keys and Secrets

This project uses ASP.NET Core User Secrets to securely store API keys and other sensitive information. To set up the required secrets for development, run the following commands:

```bash
# Initialize user secrets (only needed once)
dotnet user-secrets init --project EstateEase

# Add ReCaptcha keys
dotnet user-secrets set "ReCaptcha:SiteKey" "your-recaptcha-site-key" --project EstateEase
dotnet user-secrets set "ReCaptcha:SecretKey" "your-recaptcha-secret-key" --project EstateEase

# Add PayMongo keys
dotnet user-secrets set "PayMongo:SecretKey" "your-paymongo-secret-key" --project EstateEase
dotnet user-secrets set "PayMongo:PublicKey" "your-paymongo-public-key" --project EstateEase
dotnet user-secrets set "PayMongo:WebhookSecret" "your-paymongo-webhook-secret" --project EstateEase
```

### List All Secrets

To view all configured secrets:

```bash
dotnet user-secrets list --project EstateEase
```

### Deployment Considerations

For production deployment, these secrets should be configured as environment variables on your hosting platform, not through user secrets which are only for development.

## Features

- Property listings with detailed information
- User authentication and profile management
- Property search and filtering
- Integrated payment processing for property purchase and rental
- Transaction history and receipts
- Admin dashboard for property management

## Technologies

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Bootstrap 5
- PayMongo API Integration 