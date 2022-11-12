using Microsoft.AspNetCore.Http;
using QuizHouse.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Interfaces
{
	public interface IUserAuthentication
	{
		public Task<AccountDTO> GetAuthenticatedUser(HttpContext context);
		public Task AuthorizeForUser(HttpContext context, string accountId, bool permanent);
		public bool CheckCredentials(AccountDTO account, string password);
		public Task LogoutUser(HttpContext context);
	}
}
