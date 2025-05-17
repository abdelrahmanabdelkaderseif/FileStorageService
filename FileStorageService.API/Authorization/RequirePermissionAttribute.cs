using Microsoft.AspNetCore.Authorization;

namespace FileStorageService.API.Authorization
{
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permission)
            : base(policy: permission)
        {
        }
    }
} 