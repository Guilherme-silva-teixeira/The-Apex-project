using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Linq;

namespace WpfApp3
{
    public partial class MainWindow : Window
    {
        private readonly GeminiChatbot _gemini = new GeminiChatbot();
        private IWebDriver _driver;
        private string _currentPageInfo;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBrowser();
        }

        private void InitializeBrowser()
        {
            try
            {
                var options = new ChromeOptions();
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--window-size=1920,1080");

                _driver = new ChromeDriver(options);
                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                AppendChatHistory("Navegador inicializado com sucesso");
                NavigateToUrl("https://www.google.com");
            }
            catch (Exception ex)
            {
                AppendChatHistory($"Erro ao iniciar navegador: {ex.Message}");
                MessageBox.Show(ex.Message, "Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToUrl(string url)
        {
            try
            {
                _driver.Navigate().GoToUrl(url);
                wbBrowser.Navigate(new Uri(url));
                txtUrl.Text = url;
                UpdatePageInfo();
                AppendChatHistory($"Navegado para: {url}");
            }
            catch (Exception ex)
            {
                AppendChatHistory($"Erro de navegação: {ex.Message}");
            }
        }

        private async void btnNavigate_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl(txtUrl.Text);
        }

        private void txtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrl(txtUrl.Text);
            }
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            await ProcessUserCommand();
        }

        private async void txtUserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                await ProcessUserCommand();
            }
        }

        private async Task ProcessUserCommand()
        {
            var userInput = txtUserInput.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            AppendChatHistory($"Você: {userInput}");
            txtUserInput.Clear();

            try
            {
                // Atualiza informações da página antes de enviar o comando
                await UpdatePageInfo();

                // Corrigindo o problema com a cadeia de caracteres bruta interpolada
                var prompt = """
    Comando: {userInput}
    Contexto: {pageContext}

    Regras:
    1. Responda APENAS com JSON válido
    2. NÃO use markdown (como ```json```)
    3. Formato exigido:
    {
        "action": "click|type|navigate|scroll|wait",
        "selector": "seletor-css",
        "text": "(opcional)",
        "url": "(opcional)",
        "description": "Explicação"
    }
    """.Replace("{userInput}", userInput)
       .Replace("{pageContext}", _currentPageInfo);

                // Obtém resposta do Gemini
                var jsonResponse = await GeminiChatbot.GenerateTextAsync(prompt);
                AppendChatHistory($"Gemini: {jsonResponse}");

                // Executa as ações
                ExecuteJsonActions(jsonResponse);
            }
            catch (Exception ex)
            {
                AppendChatHistory($"Erro: {ex.Message}");
            }
        }

      /*  private void ExecuteJsonActions(string jsonResponse)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var action = JsonSerializer.Deserialize<WebAction>(jsonResponse, options);
                
               
            }
            catch (JsonException ex)
            {
                AppendChatHistory($"Erro ao interpretar JSON: {ex.Message}");
                AppendChatHistory($"Resposta recebida: {jsonResponse}");
            }
        }*/

        private void ExecuteJsonActions(string jsonResponse)
        {
            try
            {
                // Etapa 1: Remover markdown e whitespace
                string cleanedJson = jsonResponse
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                // Etapa 2: Verificar se o JSON começa e termina corretamente
                if (!cleanedJson.StartsWith("{") || !cleanedJson.EndsWith("}"))
                {
                    AppendChatHistory("Resposta não é um JSON válido. Resposta bruta:\n" + jsonResponse);
                    return;
                }

                // Etapa 3: Desserializar
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var action = JsonSerializer.Deserialize<WebAction>(cleanedJson, options);

                switch (action.Action.ToLower())
                {
                    case "click":
                        FindElement(action.Selector, action.Timeout)?.Click();
                        break;

                    case "type":
                        var element = FindElement(action.Selector, action.Timeout);
                        element?.Clear();
                        element?.SendKeys(action.Text);
                        break;

                    case "navigate":
                        NavigateToUrl(action.Url);
                        break;

                    case "scroll":
                        ((IJavaScriptExecutor)_driver)
                            .ExecuteScript("window.scrollBy(0, arguments[0])", action.Text);
                        break;

                    case "wait":
                        System.Threading.Thread.Sleep((action.Timeout ?? 1) * 1000);
                        break;

                    default:
                        AppendChatHistory($"Ação não reconhecida: {action.Action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                AppendChatHistory($"✖ Erro crítico: {ex.Message}\nResposta original: {jsonResponse}");
            }
        }


        private IWebElement FindElement(string selector, int? timeoutSeconds = null)
        {
            try
            {
                var timeout = timeoutSeconds ?? 10;
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeout));
                return wait.Until(drv =>
                {
                    try
                    {
                        var element = drv.FindElement(By.CssSelector(selector));
                        return element.Displayed ? element : null;
                    }
                    catch
                    {
                        return null;
                    }
                });
            }
            catch
            {
                AppendChatHistory($"Elemento não encontrado: {selector}");
                return null;
            }
        }

        private async Task UpdatePageInfo()
        {
            try
            {
                _currentPageInfo = $"""
                    URL: {_driver.Url}
                    Título: {_driver.Title}
                    Elementos visíveis:
                    {GetVisibleElements()}
                    """;
            }
            catch (Exception ex)
            {
                AppendChatHistory($"Erro ao atualizar informações: {ex.Message}");
            }
        }

        private string GetVisibleElements()
        {
            try
            {
                var elements = _driver.FindElements(By.CssSelector("input, button, a, [role='button'], select, textarea"));
                var sb = new StringBuilder();

                foreach (var element in elements)
                {
                    if (element.Displayed)
                    {
                        sb.Append($"\n- {element.TagName}");
                        if (!string.IsNullOrEmpty(element.GetAttribute("id")))
                            sb.Append($" #{element.GetAttribute("id")}");
                        if (!string.IsNullOrEmpty(element.GetAttribute("class")))
                            sb.Append($" .{element.GetAttribute("class").Replace(" ", ".")}");
                        if (!string.IsNullOrEmpty(element.Text))
                            sb.Append($" (texto: {Truncate(element.Text, 30)})");
                    }
                }

                return sb.Length > 0 ? sb.ToString() : "Nenhum elemento visível encontrado";
            }
            catch
            {
                return "Erro ao obter elementos";
            }
        }

        private void btnGetElements_Click(object sender, RoutedEventArgs e)
        {
            AppendChatHistory("Elementos visíveis:\n" + GetVisibleElements());
        }

        private void btnTakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                screenshot.SaveAsFile(fileName);
                AppendChatHistory($"Captura de tela salva como: {Path.GetFullPath(fileName)}");
            }
            catch (Exception ex)
            {
                AppendChatHistory($"Erro ao capturar tela: {ex.Message}");
            }
        }

        private void AppendChatHistory(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtChatHistory.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                txtChatHistory.ScrollToEnd();
            });
        }

        private string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _driver?.Quit();
            _driver?.Dispose();
        }

        // Classe para desserializar o JSON
        public class WebAction
        {
            public string Action { get; set; }
            public string Selector { get; set; }
            public string Text { get; set; }
            public string Url { get; set; }
            public int? Timeout { get; set; }
            public string Description { get; set; }
        }
    }


    public class GeminiChatbot
    {
        // --- Substitua pela sua API Key ---
        private const string ApiKey = "EXEMPLO_AI_KEY";
        // --- Escolha o modelo ---
        private const string ModelName = "gemini-2.0-flash"; // Ou outro modelo compatível

        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string ApiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={ApiKey}";

        // --- Classes para representar a estrutura JSON da requisição ---
        private class GeminiRequest
        {
            [JsonPropertyName("contents")]
            public List<Content> Contents { get; set; } = new List<Content>();

            // Opcional: Adicionar configurações de segurança
            [JsonPropertyName("safetySettings")]
            public List<SafetySetting> SafetySettings { get; set; } = new List<SafetySetting> {
            new SafetySetting { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_NONE" },
            new SafetySetting { Category = "HARM_CATEGORY_HATE_SPEECH", Threshold = "BLOCK_NONE" },
            new SafetySetting { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_NONE" },
            new SafetySetting { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_NONE" }
        };

            // Opcional: Adicionar configurações de geração
            // [JsonPropertyName("generationConfig")]
            // public GenerationConfig GenerationConfig { get; set; } = new GenerationConfig();
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part> Parts { get; set; } = new List<Part>();
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = "";
        }

        // Opcional: Classe para configurações de segurança
        private class SafetySetting
        {
            [JsonPropertyName("category")]
            public string Category { get; set; } = "";
            [JsonPropertyName("threshold")]
            public string Threshold { get; set; } = ""; // BLOCK_NONE, BLOCK_ONLY_HIGH, BLOCK_MEDIUM_AND_ABOVE, BLOCK_LOW_AND_ABOVE
        }

        // Opcional: Classe para configurações de geração (temperatura, etc.)
        // private class GenerationConfig
        // {
        //     [JsonPropertyName("temperature")]
        //     public double Temperature { get; set; } = 0.9; // Exemplo
        //     [JsonPropertyName("topK")]
        //     public int TopK { get; set; } = 1; // Exemplo
        // }

        // --- Classes para representar a estrutura JSON da resposta ---
        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate>? Candidates { get; set; }

            // Incluir promptFeedback se precisar analisar por que algo foi bloqueado
            [JsonPropertyName("promptFeedback")]
            public PromptFeedback? PromptFeedback { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }

            [JsonPropertyName("finishReason")]
            public string? FinishReason { get; set; } // Ex: STOP, SAFETY, RECITATION, OTHER

            [JsonPropertyName("safetyRatings")]
            public List<SafetyRating>? SafetyRatings { get; set; }
        }

        private class SafetyRating
        {
            [JsonPropertyName("category")]
            public string? Category { get; set; }
            [JsonPropertyName("probability")]
            public string? Probability { get; set; } // NEGLIGIBLE, LOW, MEDIUM, HIGH
        }

        private class PromptFeedback
        {
            [JsonPropertyName("blockReason")]
            public string? BlockReason { get; set; } // SAFETY, OTHER

            [JsonPropertyName("safetyRatings")]
            public List<SafetyRating>? SafetyRatings { get; set; }
        }

        // --- Função principal assíncrona para chamar a API ---
        public static async Task<string> GenerateTextAsync(string prompt)
        {
            if (string.IsNullOrEmpty(ApiKey) || ApiKey == "SUA_API_KEY_AQUI")
            {
                return "ERRO: API Key não configurada. Por favor, edite o código e insira sua chave.";
            }

            try
            {
                // Montar o corpo da requisição JSON
                var requestData = new GeminiRequest
                {
                    Contents = new List<Content>
                {
                    new Content
                    {
                        Parts = new List<Part> { new Part { Text = prompt } }
                    }
                }
                    // Adicione aqui SafetySettings ou GenerationConfig se necessário
                };

                // Serializar o objeto C# para uma string JSON
                string jsonRequest = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                // Criar o conteúdo da requisição HTTP
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // Fazer a requisição POST
                HttpResponseMessage response = await httpClient.PostAsync(ApiEndpoint, content);

                // Ler a resposta como string
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Verificar se a requisição foi bem-sucedida
                if (response.IsSuccessStatusCode)
                {
                    // Desserializar a resposta JSON para um objeto C#
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

                    // Extrair o texto da primeira candidata (se existir)
                    if (geminiResponse?.Candidates?.Count > 0 &&
                        geminiResponse.Candidates[0].Content?.Parts?.Count > 0)
                    {
                        return geminiResponse.Candidates[0].Content.Parts[0].Text;
                    }
                    else if (geminiResponse?.PromptFeedback?.BlockReason != null)
                    {
                        // A resposta foi bloqueada por segurança
                        string blockReason = geminiResponse.PromptFeedback.BlockReason;
                        string safetyIssues = "";
                        if (geminiResponse.PromptFeedback.SafetyRatings != null)
                        {
                            safetyIssues = string.Join(", ", geminiResponse.PromptFeedback.SafetyRatings.Select(r => $"{r.Category}: {r.Probability}"));
                        }
                        return $"A resposta foi bloqueada. Motivo: {blockReason}. Detalhes: {safetyIssues}";
                    }
                    else
                    {
                        // Verificar se há texto na resposta mesmo sem candidatos (pode acontecer em erros)
                        if (geminiResponse?.Candidates != null && geminiResponse.Candidates.Count > 0 && geminiResponse.Candidates[0].FinishReason != "STOP")
                        {
                            return $"Geração finalizada por motivo inesperado: {geminiResponse.Candidates[0].FinishReason}. Resposta JSON: {jsonResponse}";
                        }
                        return "Não foi possível extrair o texto da resposta da API. Resposta JSON: " + jsonResponse;
                    }
                }
                else
                {
                    // Retornar a mensagem de erro da API
                    return $"Erro na API: {response.StatusCode} - {jsonResponse}";
                }
            }
            catch (HttpRequestException e)
            {
                return $"Erro na requisição HTTP: {e.Message}";
            }
            catch (JsonException e)
            {
                return $"Erro ao processar JSON: {e.Message}";
            }
            catch (Exception e)
            {
                return $"Erro inesperado: {e.Message}";
            }
        }

        
    }

}
