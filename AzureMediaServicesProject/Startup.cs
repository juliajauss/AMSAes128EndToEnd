using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AzureMediaServicesProject.Startup))]
namespace AzureMediaServicesProject
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
