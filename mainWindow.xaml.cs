using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Wpf;

namespace Apex;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
///

//esta classe captura eventos do teclado - this class get the keyboard events

//classe da janela principal - the main window class
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private double _posL;
    private double _posT;
    private double _buttonL;
    private string searchTool;

    public double PosL
    {
        get => _posL;
        set
        {
            _posL = value;
            OnPropertyChanged(nameof(PosL));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindow()
    {
        PosL = -1459;
        _buttonL = 10;
        InitializeComponent();
        InitializeAsync();
        searchOptions.Items.Add("Google");
        searchOptions.Items.Add("DuckDuckGo");
        searchOptions.Items.Add("Yandex");
        searchOptions.SelectedItem = "Google";
    }

    async void InitializeAsync()
    {
        var options = new CoreWebView2EnvironmentOptions("--remote-debugging-port=9222");

        var env = await CoreWebView2Environment.CreateAsync(null, null, options);
        await WebView.EnsureCoreWebView2Async(env);
        await WebView.EnsureCoreWebView2Async(null);

        WebView.CoreWebView2.Navigate("https://example.com");

        if(WebView.CanGoForward||WebView.CanGoBack)
        {
            if (WebView != null)
            {
                string urlViaSource = WebView.Source?.ToString() ?? "N/A";
                //string urlViaJS = await WebView.ExecuteScriptAsync("window.location.href;");
                //urlViaJS = urlViaJS.Trim('"');
                httpBlock.Text = urlViaSource;
            }
        }
    }

    private void AddNewPage(object sender, RoutedEventArgs e)
    {
        Rectangle Page = new Rectangle
        {
            Height = 35,
            Width = 197,
            Fill = new SolidColorBrush(Colors.White),
            Margin = new Thickness(PosL,0,0,0),//left,top,right,bottom
            RadiusX = 7.49,
            RadiusY = 7.49
        };

        Pages.Children.Add(Page);
        PosL += 413;
        _buttonL += 207;    

        AddButton.Margin = new Thickness(_buttonL, 0, 0, 0);
        AddButtonBackgroundPanel.Margin = new Thickness(_buttonL, 0, 0, 0);
    }

    private void GoSearch(object sender, RoutedEventArgs e)
    {
        search();
    }

    private async void search()
    {
        if (searchOptions.SelectedItem == "Google")
        {
            MessageBox.Show("Google selecionado");
            try
            {
                WebView.Source = new Uri(AdressView.Text);
            }
            catch (UriFormatException)
            {
                try
                {
                    WebView.Source = new Uri("https://www.google.com/search/" + AdressView.Text);
                }
                catch (UriFormatException)
                {

                }
            }
        }
        else if (searchOptions.SelectedItem == "DuckDuckGo")
        {
            MessageBox.Show("DuckDuckGo selecionado");
        }
    }

    private async void AdressView_TextChanged(object sender, TextChangedEventArgs e)
    {
        //ver depois se dá para adicionar o evento das teclas e fazer com que os leitores de tela avisem

        if(AdressView.Text == "")
        {
            httpBlock.Text = "https://";
        }
        else
        {
            httpBlock.Text = "";
        }
    }

    private void AdressView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter)
        {
            search();
        }
        else if (e.Key == Key.Tab)
        {
            AdressView.Text = "https://";
        }
    }

    private void AdressView_MouseEnter(object sender, MouseEventArgs e)
    {
        AdressView.Cursor = Cursors.Hand;
        AdressBackPanel.Cursor = Cursors.Hand;
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if(WebView.CanGoForward)
        {
            WebView.GoForward();
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if(WebView.CanGoBack)
        {
            WebView.GoBack();
        }
    }

    private void Recharge_Click(object sender, RoutedEventArgs e)
    {
        WebView.Reload();
    }
}
