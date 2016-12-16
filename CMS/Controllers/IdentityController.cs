namespace CMS.Controllers
{
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;

    [Route("identity")]
    [Authorize]
    public class IdentityController : Controller
    {
        /// <summary>
        /// This controller will be used to test the authorization requirement, as well as visualize the claims identity through the eyes of the API.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get()
        {
            return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
        }
    }
}
