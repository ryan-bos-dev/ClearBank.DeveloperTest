using ClearBank.DeveloperTest.Abstractions;
using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClearBank.DeveloperTest.Tests
{
    public class DependencyInjectionTests
    {
        [Theory]
        [InlineData("Backup", typeof(BackupAccountDataStore))]
        [InlineData("backup", typeof(BackupAccountDataStore))]
        [InlineData("Primary", typeof(AccountDataStore))]
        [InlineData("primary", typeof(AccountDataStore))]
        public void AddServices_Registers_Backup_WhenConfigured(string dataStoreTypeConfig, Type type)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["DataStoreType"] = dataStoreTypeConfig })
                .Build();

            var services = new ServiceCollection();
            services.AddServices(config);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var store = scope.ServiceProvider.GetRequiredService<IAccountDataStore>();
            Assert.IsType(type, store);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ABC")]
        public void AddServices_InvalidOrMissing_Throws(string? dataStoreTypeConfig)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["DataStoreType"] = dataStoreTypeConfig })
                .Build();

            var services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() => services.AddServices(config));
        }
    }
}
