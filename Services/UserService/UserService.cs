using System.Security.Claims;

namespace NetHRSystem.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public string GetUserName()
        {
            var result = string.Empty;
            if (this._httpContextAccessor != null) 
            {
                result = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
            }

            return result;
        }

        public string GetUser()
        {
            var result = string.Empty;
            if (this._httpContextAccessor != null)
            {
                result = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
            }

            return result;
        }
    }
}
