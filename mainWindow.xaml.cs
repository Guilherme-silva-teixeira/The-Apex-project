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
public class GlobalKeyboardHook
{
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    private delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private KeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;

    public event EventHandler<KeyPressedEventArgs> KeyPressed;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
        _hookID = SetHook(_proc);
    }

    private IntPtr SetHook(KeyboardProc proc)
    {
        using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Key key = KeyInterop.KeyFromVirtualKey(vkCode);

            KeyPressed?.Invoke(this, new KeyPressedEventArgs(key));
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookID);
    }
}

//negocio do getter e setter - getter and setter stuff
public class KeyPressedEventArgs : EventArgs
{
    public Key Key { get; }

    public KeyPressedEventArgs(Key key)
    {
        Key = key;
    }
}

//classe da janela principal - the main window class
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private double _posL;
    private double _posT;
    private double _buttonL;

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

    private void search()
    {
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

    private async void AdressView_TextChanged(object sender, TextChangedEventArgs e)
    {
        var hook0 = new GlobalKeyboardHook();
        //ver depois se dá para adicionar o evento das teclas e fazer com que os leitores de tela avisem

        if(AdressView.Text == "")
        {
            httpBlock.Text = "https://";
        }
        else
        {
            hook0.KeyPressed += (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    search();
                }
            };
            httpBlock.Text = "";
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
