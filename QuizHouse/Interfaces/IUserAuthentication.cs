using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Interfaces
{
    public interface IUserAuthentication
    {
        public Task<string> GetAuthenticatedUserId(HttpContext context);
    }
}
