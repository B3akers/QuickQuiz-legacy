using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
	public class JwtTokensService
	{
		private readonly IConfiguration _configuration;
		public JwtTokensService(IConfiguration configuration)
		{
			_configuration = configuration;
		}
		public string GenerateToken(ClaimsIdentity claims, DateTime? expires)
		{
			var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]));
			var tokenHandler = new JwtSecurityTokenHandler();
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = claims,
				Expires = expires,
				Issuer = _configuration["Jwt:Issuer"],
				Audience = _configuration["Jwt:Audience"],
				SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		public bool ValidateToken(string token)
		{
			var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]));
			var tokenHandler = new JwtSecurityTokenHandler();
			try
			{
				tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidIssuer = _configuration["Jwt:Issuer"],
					ValidAudience = _configuration["Jwt:Audience"],
					IssuerSigningKey = mySecurityKey
				}, out SecurityToken validatedToken);
			}
			catch
			{
				return false;
			}
			return true;
		}
	}
}
