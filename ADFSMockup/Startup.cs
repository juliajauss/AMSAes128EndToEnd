using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ADFSMockup.Startup))]
namespace ADFSMockup
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
