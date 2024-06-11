// using System.Reflection;
// using Elsa.Framework.Tenants;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.DependencyInjection.Extensions;
//
// namespace Elsa.Framework.Shells.Services;
//
// public class TenantShellFactory(
//     IApplicationServicesAccessor applicationServicesAccessor,
//     IShellFeatureTypesProvider shellFeatureTypesProvider,
//     IConfiguration applicationConfiguration,
//     IServiceProvider serviceProvider) : ITenantShellFactory
// {
//     public Shell CreateShell(Tenant tenant)
//     {
//         var tenantServiceCollection = CreateTenantServiceCollection(tenant);
//         tenantServiceCollection.AddSingleton(tenant);
//         AddFeatures(tenant, tenantServiceCollection);
//         var tenantServiceProvider = tenantServiceCollection.BuildServiceProvider();
//         return new Shell(tenantServiceCollection, tenantServiceProvider);
//     }
//
//     private IServiceCollection CreateTenantServiceCollection(Tenant tenant)
//     {
//         var applicationServices = applicationServicesAccessor.ApplicationServices;
//         var tenantServiceCollection = new ServiceCollection();
//         tenantServiceCollection.AddSingleton(tenant);
//
//         foreach (var serviceDescriptor in applicationServices)
//         {
//             if (serviceDescriptor.Lifetime == ServiceLifetime.Singleton)
//             {
//                 tenantServiceCollection.AddSingleton(serviceDescriptor.ServiceType, _ => serviceProvider.GetService(serviceDescriptor.ServiceType)!);
//             }
//
//             tenantServiceCollection.Add(serviceDescriptor);
//         }
//
//         return tenantServiceCollection;
//     }
//
//     private void AddFeatures(Tenant tenant, IServiceCollection serviceCollection)
//     {
//         var enabledFeatureNames = tenant.EnabledFeatures;
//         var shellFeatureTypes = shellFeatureTypesProvider.GetFeatureTypes();
//         var enabledFeatureTypes = shellFeatureTypes.Where(x => enabledFeatureNames.Contains(GetFeatureName(x))).ToList();
//         var configurationBuilder = new ConfigurationBuilder().AddConfiguration(applicationConfiguration);
//
//         if (tenant.Configuration != null)
//         {
//             // var configurationJson = System.Text.Json.JsonSerializer.Serialize(tenant.Configuration);
//             // configurationBuilder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(configurationJson)));
//         }
//
//         var configuration = configurationBuilder.Build();
//
//         foreach (var featureType in enabledFeatureTypes)
//         {
//             var feature = (IShellFeature)ActivatorUtilities.CreateInstance(serviceProvider, featureType, configuration);
//             feature.ConfigureServices(serviceCollection);
//         }
//     }
//
//     private string GetFeatureName(Type featureType)
//     {
//         var featureAttribute = featureType.GetCustomAttribute<FeatureAttribute>();
//         return featureAttribute?.Name ?? featureType.Name.Replace("ShellFeature", string.Empty);
//     }
// }