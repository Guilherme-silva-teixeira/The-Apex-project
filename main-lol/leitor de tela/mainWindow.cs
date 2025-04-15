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
using Microsoft.Web.WebView2;
using moreTestes;
using Microsoft.Web.WebView2.Core;
using System.Speech.Synthesis;
using System.Diagnostics;
using moreTestes;

namespace moreTestes;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>

public partial class MainWindow : Window
{

    private readonly A11yService _a11yService;
    private readonly SpeechService _speechService;

    public MainWindow()
    {
        InitializeComponent();
        _a11yService = new A11yService();
        _speechService = new SpeechService();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
      try
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Navigate("https://google.com");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao inicializar WebView: {ex.Message}");
        }
    }

    private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (webView?.CoreWebView2 == null)
            {
                MessageBox.Show("WebView não está inicializado. Aguarde...");
                return;
            }

            string currentUrl = webView.CoreWebView2.Source.ToString();
            var a11yResult = await _a11yService.AnalyzeAccessibility(currentUrl);
            _speechService.Speak($"Encontrados {a11yResult.Errors.Count} erros de acessibilidade.");
            
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro na análise: {ex.Message}");
            _speechService.Speak("Ocorreu um erro durante a análise.");
        }
    }
}
