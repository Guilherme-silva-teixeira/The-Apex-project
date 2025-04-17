using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Threading.Tasks;
using System.Windows;
using LangChain.Providers;
using LangChain.Providers.Google;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.TeamFoundation.Common.Logging; // Adicionado para versão 1.69.0

namespace AIAssistedBrowser
{
    public partial class MainWindow : Window
    {
        private IWebDriver _driver;
        private readonly GoogleProvider _provider;
        private readonly IChatModel _aiModel;

        public MainWindow()
        {
            InitializeComponent();
            
            // Inicializa o driver do Selenium
            var options = new ChromeOptions();
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            
            _driver = new ChromeDriver(options);
            
            // Configuração específica para Google.Apis.Auth 1.69.0
            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = "seu-client-id", // Pode ser deixado vazio se usar apenas API key
                    ClientSecret = "seu-client-secret" // Opcional para Gemini
                }
            };

            // Configura o provedor Google
            _provider = new GoogleProvider(
                apiKey: "sua-chave-api-google-aqui",
                authFlowInitializer: initializer, // Específico para 1.69.0
                logger: new DebugLogger());
            
            _aiModel = _provider.CreateChatModel("gemini-pro");
        }

        private async void btnNavigate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _driver.Navigate().GoToUrl(txtUrl.Text);
                webBrowser.Navigate(_driver.Url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navigating: {ex.Message}");
            }
        }

        private async void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCommand();
        }

        private async void txtCommand_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                await ExecuteCommand();
            }
        }

        private async Task ExecuteCommand()
        {
            var command = txtCommand.Text;
            if (string.IsNullOrWhiteSpace(command))
                return;

            try
            {
                // Envia informações da página atual para o Gemini
                var pageInfo = $@"
URL atual: {_driver.Url}
Título da página: {_driver.Title}
Elementos visíveis: {GetVisibleElementsInfo(_driver)}";

                // Prompt estruturado para o Gemini
                var prompt = $@"Você é um assistente especializado em automação web com Selenium. 
Baseado no comando do usuário e nas informações da página, gere instruções precisas.

COMANDO DO USUÁRIO: '{command}'

INFORMAÇÕES DA PÁGINA:
{pageInfo}

INSTRUÇÕES:
1. Analise o comando e determine a melhor ação (click, type, navigate, etc.)
2. Forneça detalhes específicos incluindo:
   - Seletor CSS do elemento alvo
   - Texto para digitação (se aplicável)
   - URL para navegação (se aplicável)
3. Seja preciso e conciso na resposta.

FORMATO DA RESPOSTA (JSON):
{{
  ""action"": ""tipo-da-acao"",
  ""selector"": ""seletor-css"",
  ""text"": ""texto-opcional"",
  ""url"": ""url-opcional"",
  ""reason"": ""explicacao-curta""
}}";

                // Obtém resposta do Gemini
                var response = await _aiModel.GenerateAsync(prompt);

                // Processa a resposta (implementação simplificada)
                ProcessAIResponse(response);

                // Atualiza o WebBrowser WPF
                webBrowser.Navigate(_driver.Url);

                // Mostra feedback
                txtCommand.Text = "";
                MessageBox.Show($"Comando executado: {response}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing command: {ex.Message}");
            }
        }

        private void ProcessAIResponse(string response)
        {
            // Implementação básica - em produção, use um parser JSON robusto
            if (response.Contains("\"action\": \"click\""))
            {
                var selectorStart = response.IndexOf("\"selector\": \"") + 12;
                var selectorEnd = response.IndexOf("\"", selectorStart);
                var selector = response.Substring(selectorStart, selectorEnd - selectorStart);

                var element = _driver.FindElement(By.CssSelector(selector));
                element.Click();
            }
            else if (response.Contains("\"action\": \"type\""))
            {
                var selectorStart = response.IndexOf("\"selector\": \"") + 12;
                var selectorEnd = response.IndexOf("\"", selectorStart);
                var selector = response.Substring(selectorStart, selectorEnd - selectorStart);

                var textStart = response.IndexOf("\"text\": \"") + 8;
                var textEnd = response.IndexOf("\"", textStart);
                var text = response.Substring(textStart, textEnd - textStart);

                var element = _driver.FindElement(By.CssSelector(selector));
                element.SendKeys(text);
            }
            else if (response.Contains("\"action\": \"navigate\""))
            {
                var urlStart = response.IndexOf("\"url\": \"") + 8;
                var urlEnd = response.IndexOf("\"", urlStart);
                var url = response.Substring(urlStart, urlEnd - urlStart);

                _driver.Navigate().GoToUrl(url);
            }
        }

        private string GetVisibleElementsInfo(IWebDriver driver)
        {
            // Implementação simplificada para obter elementos visíveis
            try
            {
                var elements = driver.FindElements(By.CssSelector("input, button, a"));
                var info = "";
                foreach (var element in elements)
                {
                    if (element.Displayed)
                    {
                        var tag = element.TagName;
                        var text = element.Text.Length > 20 ? element.Text.Substring(0, 20) + "..." : element.Text;
                        var id = element.GetAttribute("id");
                        var name = element.GetAttribute("name");

                        info += $"\n- {tag}: ";
                        if (!string.IsNullOrEmpty(id)) info += $"#{id} ";
                        if (!string.IsNullOrEmpty(name)) info += $"[name={name}] ";
                        if (!string.IsNullOrEmpty(text)) info += $"text='{text}'";
                    }
                }
                return info;
            }
            catch
            {
                return "Não foi possível obter elementos";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}
