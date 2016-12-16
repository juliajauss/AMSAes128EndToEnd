using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ADFSmockup_IdentityServer
{
    public class Startup
    {
        // AddIdentityServer registers the IdentityServer services in DI. It also registers an in-memory store for runtime state. 
        // The AddTemporarySigningCredential extension creates temporary key material for signing tokens on every start. Again 
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentityServer()
                .AddTemporarySigningCredential()
                .AddInMemoryScopes(Config.GetScopes())
                .AddInMemoryClients(Config.GetClients());
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            app.UseDeveloperExceptionPage();

            app.UseIdentityServer();
        }
    }
}
