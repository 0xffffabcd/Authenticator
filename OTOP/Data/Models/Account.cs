using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Authenticator.Properties;
using Authenticator.Utils;

namespace Authenticator.Data.Models
{
	public class Account : INotifyPropertyChanged
	{
		private int _digits;
		private string _email;
		private Algorithm _hmacAlgorithm;
		private int _id;
		private string _issuer;
		private string _originalUri;
		private int _period;
		private string _sharedSecret;

		public Account()
		{
			Digits = 6;
			Period = 30;
			HMACAlgorithm = Algorithm.SHA1;
		}

		[Key]
		public int Id
		{
			get { return _id; }
			set
			{
				_id = value;
				OnPropertyChanged();
			}
		}

		public string Email
		{
			get { return _email; }
			set
			{
				if (value == _email) return;
				_email = value;
				OnPropertyChanged();
			}
		}

		public string Issuer
		{
			get { return _issuer; }
			set
			{
				if (value == _issuer) return;
				_issuer = value;
				OnPropertyChanged();
			}
		}

		public string OriginalUri
		{
			get { return _originalUri; }
			set
			{
				if (value == _originalUri) return;
				_originalUri = value;
				OnPropertyChanged();
			}
		}

		[Required]
		public string SharedSecret
		{
			get { return _sharedSecret; }
			set
			{
				if (value == _sharedSecret) return;
				_sharedSecret = value;
				OnPropertyChanged();
				OnPropertyChanged("AuthCode");
			}
		}

		public int Period
		{
			get { return _period; }
			set
			{
				if (value == _period) return;
				_period = value;
				OnPropertyChanged();
			}
		}

		public int Digits
		{
			get { return _digits; }
			set
			{
				if (value == _digits) return;
				_digits = value;
				OnPropertyChanged();
			}
		}

		public Algorithm HMACAlgorithm
		{
			get { return _hmacAlgorithm; }
			set
			{
				if (value == _hmacAlgorithm) return;
				_hmacAlgorithm = value;
				OnPropertyChanged();
			}
		}

		[NotMapped]
		public string AuthCode => TOTP.TimeBasedOneTimePassword(SharedSecret).ToString(Digits == 6 ? "D6" : "D8");

		[NotMapped]
		public int TimeLeft => Period - (TOTP.UnixTime()%Period);

		public event PropertyChangedEventHandler PropertyChanged;

		public void UpdateCode()
		{
			OnPropertyChanged("AuthCode");
			OnPropertyChanged("TimeLeft");
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}