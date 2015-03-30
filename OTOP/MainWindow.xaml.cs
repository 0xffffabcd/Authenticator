using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Authenticator.Data;
using Authenticator.Data.Models;
using Authenticator.Utils;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Authenticator
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private ObservableCollection<Account> _accounts;
		private DispatcherTimer _dispatcherTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 0, 1)};

		public MainWindow()
		{
			InitializeComponent();

			_dispatcherTimer.Tick += (sender, args) =>
			{
				foreach (var account in _accounts)
				{
					account.UpdateCode();
				}
			};

			_dispatcherTimer.Start();

			using (var context = new AuthenticatorContext())
			{
				_accounts = new ObservableCollection<Account>(context.Accounts.ToList());
			}
			AccountsListBox.ItemsSource = _accounts;
		}

		private async void AddAccountFromUrl(object sender, RoutedEventArgs e)
		{
			string uriString = await this.ShowInputAsync("Auth URI", "Please enter the Auth URI:", new MetroDialogSettings
			{
				AnimateShow = true,
				AnimateHide = false,
				ColorScheme = MetroDialogColorScheme.Accented
			});

			if (string.IsNullOrWhiteSpace(uriString))
			{
				return;
			}
			try
			{
				var uri = new Uri(uriString);
				if (uri.Host != "totp")
				{
					await
						this.ShowMessageAsync("Error adding the account", "Only TOTP is supported for now.",
							MessageDialogStyle.Affirmative, new MetroDialogSettings {AffirmativeButtonText = "OK"});
					return;
				}

				var account = TOTP.ParseUrl(uriString);
				SaveAccountToDb(account);
			}
			catch (Exception exception)
			{
				await
					this.ShowMessageAsync("Error", string.Format("Error adding that account: {0}", exception.Message),
						MessageDialogStyle.Affirmative,
						new MetroDialogSettings {AffirmativeButtonText = "Cancel", AnimateHide = false, AnimateShow = false});
			}
		}

		private void SaveAccountToDb(Account account)
		{
			using (var context = new AuthenticatorContext())
			{
				// Save the new account to the DB
				context.Accounts.Add(account);
				context.SaveChanges();
				// Add the account to the listbox
				_accounts.Add(account);
			}
		}

		private void CopyToClipboardListBoxClick(object sender, MouseButtonEventArgs e)
		{
			if (AccountsListBox.SelectedItem != null)
				Clipboard.SetText(((Account) AccountsListBox.SelectedItem).AuthCode);
		}

		private void RemoveItem(object sender, RoutedEventArgs e)
		{
			var item = AccountsListBox.SelectedItem as Account;

			if (item == null) return;

			_accounts.Remove(item);
			using (var context = new AuthenticatorContext())
			{
				var toDeleteItem = context.Accounts.Single(o => o.Id == item.Id);
				context.Accounts.Remove(toDeleteItem);
				context.SaveChanges();
			}
		}

		private void AddAccountButton_OnClick(object sender, RoutedEventArgs e)
		{
			var uri = string.Format("otpauth://totp/{2}?secret={1}&issuer={0}&=algorithm={3}&digits={4}&period={5}",
				IssuerTextBox.Text,
				SharedSecretTextBox.Text, LabelTextBox.Text, AlgorithmComboBox.Text, DigitsNumericUpDown.Value,
				PeriodNumericUpDown.Value);

			SaveAccountToDb(TOTP.ParseUrl(uri));
			var currFlyout = Flyouts.Items[0] as Flyout;
			if (currFlyout != null) currFlyout.IsOpen = false;
		}

		private void OpenNewAccountFlyout(object sender, RoutedEventArgs e)
		{
			// Flayouts
			var currFlyout = Flyouts.Items[0] as Flyout;
			if (currFlyout != null) currFlyout.IsOpen = true;
		}

		private void CloseFlyer(object sender, RoutedEventArgs e)
		{
			IssuerTextBox.Text = string.Empty;
			SharedSecretTextBox.Text = string.Empty;
			LabelTextBox.Text = string.Empty;
			AlgorithmComboBox.SelectedIndex = 0;
			DigitsNumericUpDown.Value = 6;
			PeriodNumericUpDown.Value = 30;
			var currFlyout = Flyouts.Items[0] as Flyout;
			if (currFlyout != null) currFlyout.IsOpen = false;
		}
	}
}