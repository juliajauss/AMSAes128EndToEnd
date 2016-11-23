using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IdentityServerAPI.Models
{
     public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

    }
}
