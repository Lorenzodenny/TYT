using Microsoft.AspNetCore.Identity;

namespace TYT.Models
{
    public class TYTUser : IdentityUser
    {
        public string? Nome { get; set; }
        public string? Cognome { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
