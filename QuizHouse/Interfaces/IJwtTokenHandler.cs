using System.Security.Claims;
using System;

namespace QuizHouse.Interfaces
{
	public interface IJwtTokenHandler
	{
		public string GenerateToken(ClaimsIdentity claims, DateTime? expires);
		public bool ValidateToken(string token);
	}
}
