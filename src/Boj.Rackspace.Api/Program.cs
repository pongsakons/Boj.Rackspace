using Boj.Rackspace.Infrastructure.Services;
using Boj.Rackspace.Infrastructure.Options;
using Boj.RackSpace.Application.Interfaces.Authentication;
using Boj.Rackspace.Infrastructure.Authentication;
using Boj.RackSpace.Application.Interfaces;

namespace Boj.Rackspace.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

            // Configure RackSpace token provider options from appsettings
            builder.Services.Configure<RackSpaceTokenOptions>(
                builder.Configuration.GetSection(RackSpaceTokenOptions.SectionName));

            // Add memory cache for token caching
            builder.Services.AddMemoryCache();

            // Register RackSpace token provider with HttpClientFactory
            builder.Services.AddHttpClient<IRackSpaceTokenProvider, RackSpaceTokenProvider>();

            // Register auth token handler for injecting tokens into requests
            builder.Services.AddTransient<AuthTokenHandler>();

            // Register RackSpace clients
            builder.Services.AddHttpClient<IRackSpaceObjectClient, RackSpaceObjectClient>()
                .AddHttpMessageHandler<AuthTokenHandler>();

            builder.Services.AddHttpClient<IRackSpaceContainerClient, RackSpaceContainerClient>()
                .AddHttpMessageHandler<AuthTokenHandler>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.EnvironmentName == "dev"
                || app.Environment.EnvironmentName == "acc")
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
