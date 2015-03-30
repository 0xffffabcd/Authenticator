using System;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using Authenticator.Data.Models;

namespace Authenticator.Utils
{
	internal static class TOTP
	{
		/// <summary>
		///     Function that implements the Time-Based One-Time Password (TOTP) algorithm
		///     Explanation: http://garbagecollected.org/2014/09/14/how-google-authenticator-works/
		///     RFC: https://tools.ietf.org/rfc/rfc6238.txt
		/// </summary>
		/// <param name="secret">Shared secret</param>
		/// <param name="algorithm">HMAC algorithm</param>
		/// <param name="digits">Determines how long of a one-time passcode to display to the user. The default is 6</param>
		/// <param name="period">Defines a period that a TOTP code will be valid for, in seconds. The default value is 30</param>
		/// <returns></returns>
		public static uint TimeBasedOneTimePassword(string secret, Algorithm algorithm = Algorithm.SHA1, int digits = 6,
			int period = 30)
		{
			if (string.IsNullOrWhiteSpace(secret))
			{
				throw new ArgumentException("secret cannot be null or empty", "secret");
			}

			if ((digits != 6) && (digits != 8))
			{
				throw new ArgumentOutOfRangeException("digits", digits, "Digits can only be 6 or 8");
			}

			long unixTime = UnixTime()/period;

			byte[] secretBytes = Base32Encoding.ToBytes(secret.ToUpper());

			HMAC hmac;

			switch (algorithm)
			{
				case Algorithm.SHA1:
					hmac = new HMACSHA1(secretBytes);
					break;
				case Algorithm.SHA256:
					hmac = new HMACSHA256(secretBytes);
					break;
				case Algorithm.SHA512:
					hmac = new HMACSHA512(secretBytes);
					break;
				case Algorithm.MD5:
					hmac = new HMACMD5(secretBytes);
					break;
				default:
					hmac = new HMACSHA1(secretBytes);
					break;
			}

			byte[] result =
				hmac.ComputeHash(BitConverter.IsLittleEndian
					? BitConverter.GetBytes(unixTime).Reverse().ToArray()
					: BitConverter.GetBytes(unixTime).ToArray());

			// get a number between [0-F]
			int offset = result.Last() & 0x0F;

			// Generate a 4-byte array starting from the above offset
			byte[] fourBytes = new byte[4];
			Array.Copy(result, offset, fourBytes, 0, 4);

			fourBytes[0] = (byte) (fourBytes[0] & 0x7F);
			uint largeInteger =
				BitConverter.ToUInt32(BitConverter.IsLittleEndian ? fourBytes.Reverse().ToArray() : fourBytes.ToArray(), 0);

			uint smallInteger = largeInteger%((uint) Math.Pow(10, digits));
			return smallInteger;
		}

		/// <summary>
		///     Returns an int representing the current unix time.
		/// </summary>
		/// <returns>current unix time</returns>
		public static int UnixTime()
		{
			return (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		}

		/// <summary>
		///     Parse the key uri and returns an account object
		/// </summary>
		/// <param name="url">key uri</param>
		/// <returns>Account object</returns>
		public static Account ParseUrl(string url)
		{
			var keyUri = new Uri(url);
			if (keyUri.Host != "totp")
			{
				return null;
			}
			var parameters = HttpUtility.ParseQueryString(keyUri.Query);

			Algorithm hmacA = Algorithm.SHA1;
			Enum.TryParse(parameters.AllKeys.Contains("algorithm") ? parameters["algorithm"].ToUpper() : "SHA1", out hmacA);

			int digits = 6;
			if (parameters.AllKeys.Contains("digits"))
			{
				int.TryParse(parameters["digits"], out digits);
			}

			int period = 30;
			if (parameters.AllKeys.Contains("period"))
			{
				int.TryParse(parameters["period"], out period);
			}

			var result = new Account
			{
				HMACAlgorithm = hmacA,
				SharedSecret = parameters.AllKeys.Contains("secret") ? parameters["secret"] : string.Empty,
				Issuer = parameters.AllKeys.Contains("issuer") ? parameters["issuer"] : string.Empty,
				Email = keyUri.AbsolutePath.Substring(1, keyUri.AbsolutePath.Length - 1),
				OriginalUri = url,
				Digits = digits,
				Period = period
			};
			return result;
		}
	}
}