using ClearBank.DeveloperTest.Abstractions;
using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Services;
using ClearBank.DeveloperTest.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ClearBank.DeveloperTest.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {
            var rawDataStoreType = config["DataStoreType"];

            if (string.IsNullOrWhiteSpace(rawDataStoreType) || 
                !Enum.TryParse<DataStoreType>(rawDataStoreType, ignoreCase: true, out var dataStoreType))
            {
                throw new InvalidOperationException("Configuration key 'DataStoreType' is required.");
            }

            if (dataStoreType == DataStoreType.Backup)
            {
                services.AddScoped<IAccountDataStore, BackupAccountDataStore>();
            }
            else
            {
                services.AddScoped<IAccountDataStore, AccountDataStore>();
            }

            services.AddScoped<IPaymentService, PaymentService>();

            return services;
        }
    }
}
