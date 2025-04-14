using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;

namespace WpfWebView2Tabs
{
    //getter e setter
    public class BrowserTab
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public WebView2 WebView { get; set; }
    }

    public partial class MainWindow : Window
    {
        private string CurrentSearchEngine = "Google";
        public ObservableCollection<BrowserTab> Tabs { get; set; } = new ObservableCollection<BrowserTab>();
        private BrowserTab _currentTab;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            tabControl.ItemsSource = Tabs;

            // Comando para fechar abas
            CloseTabCommand = new RelayCommand(CloseTab);

            // Adiciona a primeira aba
            AddNewTab("Nova aba", "https://www.google.com");

            ComboSearchEngine.Items.Add("Google");
            ComboSearchEngine.Items.Add("DuckDuckgo");
            ComboSearchEngine.Items.Add("Yandex");
        }

        public RelayCommand CloseTabCommand { get; set; }

        private async void AddNewTab(string title, string url)
        {
            var webView = new WebView2();
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;

            var tab = new BrowserTab
            {
                Title = title,
                Url = url,
                WebView = webView
            };

            Tabs.Add(tab);
            tabControl.SelectedItem = tab;

            await webView.EnsureCoreWebView2Async();
            webView.Source = new Uri(url);
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                var webView = sender as WebView2;
                webView.CoreWebView2.DocumentTitleChanged += (s, args) =>
                {
                    var tab = Tabs.FirstOrDefault(t => t.WebView == webView);
                    if (tab != null)
                    {
                        tab.Title = webView.CoreWebView2.DocumentTitle;
                    }
                };

                webView.CoreWebView2.SourceChanged += (s, args) =>
                {
                    var tab = Tabs.FirstOrDefault(t => t.WebView == webView);
                    if (tab != null)
                    {
                        tab.Url = webView.Source.ToString();
                        if (tab == _currentTab)
                        {
                            addressBar.Text = tab.Url;
                        }
                    }
                };
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl.SelectedItem is BrowserTab selectedTab)
            {
                _currentTab = selectedTab;

                // Remove o WebView atual do container
                webViewContainer.Children.Clear();

                // Adiciona o WebView da aba selecionada
                webViewContainer.Children.Add(selectedTab.WebView);

                // Atualiza a barra de endereço
                addressBar.Text = selectedTab.Url;
            }
        }

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab("Nova aba", "https://www.google.com");
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTab != null && !string.IsNullOrWhiteSpace(addressBar.Text))
            {
                var url = addressBar.Text;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                _currentTab.WebView.Source = new Uri(url);
                _currentTab.Url = url;
            }
        }

        private void CloseTab(object parameter)
        {
            if (parameter is BrowserTab tabToClose && Tabs.Count > 1)
            {
                int index = Tabs.IndexOf(tabToClose);
                Tabs.Remove(tabToClose);

                // Se fechou a aba atual, seleciona a próxima ou anterior
                if (tabToClose == _currentTab)
                {
                    if (index >= Tabs.Count) index = Tabs.Count - 1;
                    if (index >= 0) tabControl.SelectedIndex = index;
                }
            }
        }

        private void Recharge_Click(object sender, RoutedEventArgs e)
        {
            _currentTab.WebView.Reload();
        }

        private void Foward_Click(object sender, RoutedEventArgs e)
        {
            if(_currentTab.WebView.CanGoForward)
            {
                _currentTab.WebView.GoForward();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if(_currentTab.WebView.CanGoBack)
            {
                _currentTab.WebView.GoBack();
            }
        }
    }


    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute) : this(execute, null) { 
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                System.Windows.Input.CommandManager.RequerySuggested += value;
            }
            remove
            {
                System.Windows.Input.CommandManager.RequerySuggested -= value;
            }
        }

        public void Execute(object parameter) => _execute(parameter);
    }
}
