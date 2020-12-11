using System;
using System.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;

namespace ElsaDashboard.WebAssembly.Extensions
{
    public static class GrpcServicesServiceCollectionExtensions
    {
        public static IServiceCollection AddGrpcClient<T>(this IServiceCollection services) where T : class => services.AddScoped(CreateGrpcClient<T>);
        private static T CreateGrpcClient<T>(IServiceProvider sp) where T : class => CreateGrpcChannel(sp).CreateGrpcService<T>();

        private static GrpcChannel CreateGrpcChannel(IServiceProvider sp)
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            var backendUrl = navigationManager.BaseUri;
            var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
            //var tokenManager = sp.GetRequiredService<ITokenManager>();

            var credentials = CallCredentials.FromInterceptor(
                async (context, metadata) =>
                {
                    //var accessToken = await tokenManager.GetAccessTokenAsync();
                    var accessToken = "";

                    if (!string.IsNullOrEmpty(accessToken))
                        metadata.Add("Authorization", $"Bearer {accessToken}");
                });

            var channel = GrpcChannel.ForAddress(
                backendUrl,
                new GrpcChannelOptions
                {
                    HttpClient = httpClient,
                    Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
                });

            return channel;
        }
    }
}