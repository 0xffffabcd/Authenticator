using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Authenticator.Data;
using Authenticator.Data.Models;
using Authenticator.Utils;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ZXing;
using ZXing.Common;
using ZXing.Presentation;

namespace Authenticator
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly ObservableCollection<Account> _accounts;
        private readonly DispatcherTimer _dispatcherTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 1) };
        private readonly AuthenticatorContext _db = new AuthenticatorContext();

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
            _accounts = new ObservableCollection<Account>(_db.Accounts);
            AccountsListBox.ItemsSource = _accounts;
        }

        private async void AddAccountFromUrl(object sender, RoutedEventArgs e)
        {
            string uriString = await this.ShowInputAsync("Auth URI", "Please enter the Auth URI:", new MetroDialogSettings
            {
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
                            MessageDialogStyle.Affirmative, new MetroDialogSettings { AffirmativeButtonText = "OK" });
                    return;
                }

                var account = TOTP.ParseUrl(uriString);
                await SaveAccountToDb(account);
            }
            catch (Exception exception)
            {
                await
                    this.ShowMessageAsync("Error", $"Error adding that account: {exception.Message}",
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings { AffirmativeButtonText = "Cancel", AnimateHide = false, AnimateShow = false });
            }
        }

        private async Task SaveAccountToDb(Account account)
        {
            try
            {
                // Save the new account to the DB
                _db.Accounts.Add(account);
                await _db.SaveChangesAsync();

                // Add the account to the listbox
                _accounts.Add(account);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await
                       this.ShowMessageAsync("Error adding the account", "There was an error while trying to save the account to the database.",
                           MessageDialogStyle.Affirmative, new MetroDialogSettings { AffirmativeButtonText = "Abort" });
            }
        }

        private void CopyToClipboardListBoxClick(object sender, MouseButtonEventArgs e)
        {
            if (AccountsListBox.SelectedItem != null)
                Clipboard.SetText(((Account)AccountsListBox.SelectedItem).AuthCode);
        }

        private async void RemoveItem(object sender, RoutedEventArgs routedEventArgs)
        {
            var item = AccountsListBox.SelectedItem as Account;

            if (item == null) return;
            _accounts.Remove(item);

            try
            {
                var itemToDelete = _db.Accounts.FirstOrDefault(o => o.Id == item.Id);
                if (itemToDelete != null)
                {
                    _db.Accounts.Remove(itemToDelete);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await
                       this.ShowMessageAsync("Error deleting the account", "There was an error while trying to delete the account from the database.",
                           MessageDialogStyle.Affirmative, new MetroDialogSettings { AffirmativeButtonText = "Abort" });
            }
        }

        private async void AddAccountButton_OnClick(object sender, RoutedEventArgs e)
        {
            var uri =
                $"otpauth://totp/{LabelTextBox.Text}?secret={SharedSecretTextBox.Text}&issuer={IssuerTextBox.Text}&=algorithm={AlgorithmComboBox.Text}&digits={DigitsNumericUpDown.Value}&period={PeriodNumericUpDown.Value}";
            await SaveAccountToDb(TOTP.ParseUrl(uri));
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
            var currFlyout = Flyouts.Items[0] as Flyout;
            if (currFlyout != null) currFlyout.IsOpen = false;
        }

        private void QRCodeItem(object sender, RoutedEventArgs e)
        {
            var account = (Account) AccountsListBox.SelectedItem;
            if (account == null)
            {
                return;
            }
            var uri = $"otpauth://totp/{account.Email}?secret={account.SharedSecret}&issuer={account.Issuer}&=algorithm={account.HMACAlgorithm}&digits={account.Digits}&period={account.Period}";
           
            var writer = new BarcodeWriterGeometry
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 200,
                    Width = 200,
                    Margin = 0
                }
            };
            var image = writer.Write(uri);
            var window = new QRWindow(image, account.Email) {Owner = this};
            window.ShowDialog();
        }


        private void FlyoutClosingReset(object sender, RoutedEventArgs e)
        {
            IssuerTextBox.Text = string.Empty;
            SharedSecretTextBox.Text = string.Empty;
            LabelTextBox.Text = string.Empty;
            AlgorithmComboBox.SelectedIndex = 0;
            DigitsNumericUpDown.Value = 6;
            PeriodNumericUpDown.Value = 30;
        }
    }
}