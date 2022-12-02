using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickQuiz.Interfaces
{
	public interface IPasswordHasher
	{
		public string Hash(string password);
		public bool Check(string hash, string password);
	}
}
