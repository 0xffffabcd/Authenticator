using System.Windows.Media;

namespace Authenticator
{
    /// <summary>
    /// Interaction logic for QRWindow.xaml
    /// </summary>
    public partial class QRWindow
    {
        public QRWindow(Geometry geometry, string accountName)
        {
            InitializeComponent();
            this.qrCodePath.Data = geometry;
            this.accountNameTB.Text = accountName;
        }
        
    }
}
