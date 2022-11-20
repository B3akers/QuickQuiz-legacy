using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace QuizHouse.Utility
{
	public static class Randomizer
	{
		[ThreadStatic]
		private static Random _local;

		private static Random GetInstance()
		{
			Random inst = _local;
			if (inst == null)
			{
				byte[] buffer = RandomNumberGenerator.GetBytes(4);
				_local = inst = new Random(
					BitConverter.ToInt32(buffer, 0));
			}

			return inst;
		}

		public static int Next()
		{
			return GetInstance().Next();
		}

		public static int Next(int maxValue)
		{
			return GetInstance().Next(maxValue);
		}

		public static string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz_-";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[Next(s.Length)]).ToArray());
		}

		public static string RandomPassword(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz(~!@#$%^&*_-+=`|(){}[]:;<>,.?/";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[Next(s.Length)]).ToArray());
		}

		public static string RandomReadableString(int length)
		{
			const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[Next(s.Length)]).ToArray());
		}
	}

}
