using System.Linq;
using System.Web.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace IdentityServerAPI.Controllers
{
    [Route("identity")]
    [Authorize]
    public class IdentityController : Controller
    {
        /// <summary>
        /// This controller will be used later to test the authorization requirement, as well as visualize the claims identity through the eyes of the API.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get()
        {
            return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
        }
    }
}
