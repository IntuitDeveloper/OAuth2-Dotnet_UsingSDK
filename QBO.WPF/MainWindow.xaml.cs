using Intuit.Ipp.OAuth2PlatformClient;
using QBO.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QBO.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the WebView2 control
            // and begin the authorization
            // process inside the WebView2 control.
            this.RunAuthorization();
        }

        public async void RunAuthorization()
        {
            // Use the shared helper library (QBO.Shared)
            // to load the token json data (Local.Tokens)
            // and initialize the OAuth2
            // client (Local.Client).
            QboLocal.Initialize();

            // Initialize the WebView2 control.
            await WebView.EnsureCoreWebView2Async(null);

            // Navigate the WebView2 control
            // a generated authorization URL.
            WebView.CoreWebView2.Navigate(QboHelper.GetAuthorizationURL(OidcScopes.Accounting));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // When the user closes the form we
            // assume that the operation has
            // completed with success or failure.

            // Get the current query parameters
            // from the current WebView source (page)
            string query = WebView.Source.Query;

            // Use the the shared helper library
            // to validate the query parameters
            // and write the output file.
            if (QboHelper.CheckQueryParamsAndSet(query) == true && QboLocal.Tokens != null) {
                QboHelper.WriteTokensAsJson(QboLocal.Tokens);
            }
            else {
                MessageBox.Show("Quickbooks Online failed to authenticate.");
            }
        }
    }
}
