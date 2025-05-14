using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EstateEase.Services
{
    public class MigrationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MigrationHostedService> _logger;

        public MigrationHostedService(
            IServiceProvider serviceProvider,
            ILogger<MigrationHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    _logger.LogInformation("Running admin inquiry update migration");
                    var adminInquiryService = scope.ServiceProvider.GetRequiredService<AdminInquiryUpdateService>();
                    await adminInquiryService.UpdateAdminInquiries();
                    _logger.LogInformation("Admin inquiry update migration completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while running admin inquiry migration");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
} 