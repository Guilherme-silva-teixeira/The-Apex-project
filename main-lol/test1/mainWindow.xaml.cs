using Consul.Filtering;
using Microsoft.Playwright;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Net;
using OpenQA.Selenium.Chromium;
using System.Security.Policy;

/*======================================== PLANOS FUTUROS =========================================*/
/* 1. Salvar dados no JSON (do prompt e resultados de AI)                                          */
/* 2. Ver se tem uma maneira de automatizar ainda mais (capturar elementos de uma parte da página  */
/* 3. Ver se é possivel tirar o CAPTCHA                                                            */
/* 4. Implementar no navegador (isso é prototipo de como vai funcionar a parte inicial do projeto) */
/* 5. fazer com que o código funcione (É CLARO)          
 * 
 
 
 
 using (WebClient client = new WebClient())
                {
                    string html = await client.DownloadStringTaskAsync(txtUrl.Text);
                    pageData.Text += "\nHTML da página: \n\n" + html;
                }


 
 
 
 */
/*=================================================================================================*/

namespace WpfApp3
{

    public class PageInfo
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public List<HeadingInfo> Headings { get; set; } = new();
        public List<ButtonInfo> Buttons { get; set; } = new();
        public List<InputInfo> Inputs { get; set; } = new();
        public List<DivInfo> Divs { get; set; } = new();
        public List<ParagraphInfo> Paragraphs { get; set; } = new();
        public CssInfo Css { get; set; } = new();
        public JavaScriptInfo JavaScript { get; set; } = new();
    }

    public class HeadingInfo
    {
        public string Level { get; set; }  // "h1" a "h6"
        public string Text { get; set; }
        public string Id { get; set; }
    }

    public class ButtonInfo
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class InputInfo
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Placeholder { get; set; }
        public string Value { get; set; }
    }

    public class DivInfo
    {
        public string Id { get; set; }
        public string Class { get; set; }
        public string TextPreview { get; set; }
    }

    public class ParagraphInfo
    {
        public string Id { get; set; }
        public string TextPreview { get; set; }
    }

    public class CssInfo
    {
        public Dictionary<string, int> CommonClasses { get; set; } = new();
        public List<InlineStyleInfo> InlineStyles { get; set; } = new();
    }

    public class InlineStyleInfo
    {
        public string Tag { get; set; }
        public string Id { get; set; }
        public string Styles { get; set; }
    }

    public class JavaScriptInfo
    {
        public List<JsEventInfo> EventListeners { get; set; } = new();
        public List<string> DetectedFrameworks { get; set; } = new();
    }

    public class JsEventInfo
    {
        public string Tag { get; set; }
        public string Id { get; set; }
        public List<string> Events { get; set; } = new();
    }

    public class WebActionStep
    {
        public string Action { get; set; } // "navigate", "type", "click", "wait", "notify"
        public string Target { get; set; } // URL ou seletor CSS
        public string Value { get; set; } // texto para digitar ou mensagem
        public int DelayAfter { get; set; } // tempo de espera em ms
    }

    public class AutomationPlan
    {
        public List<WebActionStep> Steps { get; set; } = new();
        public string OriginalCommand { get; set; }
    }

    public partial class MainWindow : Window
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;
        private readonly GeminiChatbot _geminiChatbot = new GeminiChatbot();

        public MainWindow()
        {
            InitializeComponent();
            InitializePlaywright();
        }

        private async void InitializePlaywright()
        {
            DateTime dateTime = DateTime.Now;
            try
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    Channel = "chrome"
                });
                _page = await _browser.NewPageAsync();

                pageData.Text += TimeZoneInfo.Local.ToString();
                pageData.Text += "\n" + dateTime + " Playwright iniciado com sucesso";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar Playwright: {ex.Message}");
            }
        }

        private async void btnNavigate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _page.GotoAsync(txtUrl.Text);
                await UpdateBrowserPreview();
                pageData.Text += "\n Navegação iniciada";
                
                // pegar o html ta página total
                //string actualPageHTMLElementsInfo = await AppendHtmlElementsInfo(new StringBuilder());
                //pageData.Text += GetPageInfo().ToString() + "\n";
                //pageData.Text += actualPageHTMLElementsInfo + "\n";
                /*using (WebClient client = new WebClient())
                {
                    string html = await client.DownloadStringTaskAsync(txtUrl.Text);
                    pageData.Text += "\nHTML da página: " + html;
                }*/
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro de navegação: {ex.Message}");
            }
        }

        private async Task UpdateBrowserPreview()
        {
            // Captura de tela da página atual
            var screenshotBytes = await _page.ScreenshotAsync();

            // Cria uma imagem bitmap a partir dos bytes
            using var stream = new MemoryStream(screenshotBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            // Exibe no frame
            var image = new System.Windows.Controls.Image { Source = bitmap };
            //browserFrame.Content = image;
        }

        private async void executeAI()
        {
            try
            {
                using (WebClient client = new WebClient())
                { 
                    string currentUrl = _page.Url;

                    string html = await client.DownloadStringTaskAsync(currentUrl);
                    pageData.Text += "\nHTML da página: \n\n" + html;



                    var userCommand = txtCommand.Text;

                    var prompt = $@"Você é um assistente de automação web especializado no Google. 
Siga estas regras rigorosamente:

1. Use apenas seletores CSS válidos
2. Para links do Google Images, use sempre: 'a[href*='bm=isch']'
3. Inclua ações 'wait' entre passos críticos
4. Priorize seletores por ID ou name
5. Para pesquisa no Google, use:
   - Campo de busca: 'textarea[name='q']'
   - Botão de pesquisa: 'input[name='btnK']'

Comando: {userCommand}

Estrutura atual da página:
 {html}

Retorne um ARRAY JSON com este formato exato:
[
    {{
        ""action"": ""click|type|navigate|wait"",
        ""selector"": ""seletor CSS válido"",
        ""text"": ""(apenas para type)"",
        ""url"": ""(apenas para navigate)"",
        ""time"": ""(apenas para wait)"",
        ""reason"": ""explicação breve""
    }}
]
                ";


                    /*
                    Console.WriteLine($"Tentando localizar: {Selector}");
                    var elements = await _page.QuerySelectorAllAsync(selector);
                    Console.WriteLine($"Elementos encontrados: {elements.Count}");
    */

                    var aiResponse = await GeminiChatbot.GenerateTextAsync(prompt);
                    await ExecuteAIAction(aiResponse);
                    await UpdateBrowserPreview();
                    string secHTML = await client.DownloadStringTaskAsync(currentUrl);

                    var secondPrompt = $@"Muito bem! você conseguiu executar o primeiro comando!!
                    aqui está o segundo passo:
                    1. Analise o JSON anterior
                    2. Execute o comando
                    
                    JSON anterior: {aiResponse}
                    HTML da página: {secHTML}
                    Comando do usuário: {userCommand}
";

                    MessageBox.Show("Comandos executados com sucesso!");
                }
            }
            catch (Exception ex)
            {
                pageData.AppendText($"\nErro: {ex.Message}");
                try
                {
                    using (WebClient cli = new WebClient())
                    {
                        var userCommand = txtCommand.Text;
                        string html = await cli.DownloadStringTaskAsync(_page.Url);

                        var prompt = $@"
                        Tente executar o comando novamente
                        Comando complexo: {userCommand}
                        HTML da página:
                        {html}";

                        var aiResponse = await GeminiChatbot.GenerateTextAsync(prompt);
                        await ExecuteAIAction(aiResponse);
                        await UpdateBrowserPreview();

                    }
                }
                catch (Exception innerEx)
                {
                    pageData.AppendText($"\nErro ao tentar executar novamente: {innerEx.Message}");
                    pageData.Foreground = new SolidColorBrush(Colors.Red);
                    MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            executeAI();
        }

        public async Task<object> EvaluateJavaScriptInSection(string selector, string script)
        {
            try
            {
                return await _page.EvaluateAsync<object>($@"
                    (() => {{
                        const element = document.querySelector('{selector}');
                        {script}
                    }})()
                ");
            }
            catch (Exception ex)
            {
                return $"Erro ao executar script: {ex.Message}";
            }
        }

        
        private async void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_browser != null) await _browser.CloseAsync();
                if (_playwright != null) _playwright.Dispose();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao fechar o navegador: {ex.Message}");
            }
        }
        private async Task ExecuteAIAction(string aiResponse)
        {
            try
            {
                // Verifica se é uma mensagem de erro
                if (aiResponse.StartsWith("ERRO:") || aiResponse.StartsWith("Resposta inválida"))
                {
                    throw new Exception(aiResponse);
                }

                // Limpa a resposta para garantir que é um JSON válido
                aiResponse = aiResponse.Replace("```json", "").Replace("```", "").Trim();
                
                using var jsonDoc = JsonDocument.Parse(aiResponse);
                var root = jsonDoc.RootElement;

                // Verifica se é um array ou um objeto único
                if (root.ValueKind == JsonValueKind.Array)
                {
                    // Processa cada ação sequencialmente
                    foreach (var actionElement in root.EnumerateArray())
                    {
                        try
                        {
                            await ExecuteSingleAction(actionElement);
                        }
                        catch (Exception ex)
                        {
                            // Captura screenshot para diagnóstico
                            var screenshot = await _page.ScreenshotAsync();
                            var actionType = actionElement.TryGetProperty("action", out var a) ? a.GetString() : "desconhecida";
                            var selector = actionElement.TryGetProperty("selector", out var s) ? s.GetString() : "não especificado";
                            throw new Exception($"Falha na ação '{actionType}' com seletor '{selector}': {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Processa ação única (backward compatibility)
                    await ExecuteSingleAction(root);
                }
            }
            catch (JsonException jsonEx)
            {
                throw new Exception($"Erro de formato JSON: {jsonEx.Message}\nResposta original: {aiResponse}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao executar ação: {ex.Message}\nResposta original: {aiResponse}");
            }
        }

        private async Task ExecuteSingleAction(JsonElement actionElement)
        {
            // Verifica se a propriedade action existe
            if (!actionElement.TryGetProperty("action", out var actionProp))
                throw new Exception("Ação não especificada na resposta");

            var action = actionProp.GetString();
            if (string.IsNullOrEmpty(action))
                throw new Exception("Valor da ação é nulo ou vazio");

            // As demais propriedades são verificadas conforme necessidade da ação
            switch (action.ToLower())
            {
                case "click":
                    // Verifica se a propriedade selector existe para ações que precisam
                    if (!actionElement.TryGetProperty("selector", out var selectorProp))
                        throw new Exception("Propriedade 'selector' é necessária para ação 'click'");

                    var clickSelector = selectorProp.GetString();
                    if (string.IsNullOrEmpty(clickSelector))
                        throw new Exception("Valor do seletor é nulo ou vazio para ação 'click'");

                    // Espera o elemento estar visível e habilitado
                    await _page.WaitForSelectorAsync(clickSelector, new PageWaitForSelectorOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 400000
                    });

                    await _page.ClickAsync(clickSelector, new PageClickOptions
                    {
                        Timeout = 400000,
                        Force = false
                    });
                    await Task.Delay(1000); // Pausa após clique
                    break;

                case "type":
                    // Verifica propriedade selector
                    if (!actionElement.TryGetProperty("selector", out var typeSelectorProp))
                        throw new Exception("Propriedade 'selector' é necessária para ação 'type'");

                    var typeSelector = typeSelectorProp.GetString();
                    if (string.IsNullOrEmpty(typeSelector))
                        throw new Exception("Valor do seletor é nulo ou vazio para ação 'type'");

                    // Verifica propriedade text
                    if (!actionElement.TryGetProperty("text", out var textProp))
                        throw new Exception("Propriedade 'text' é necessária para ação 'type'");

                    var text = textProp.GetString() ?? "";

                    await _page.FillAsync(typeSelector, text);
                    await Task.Delay(500); // Pausa após digitação
                    break;

                case "navigate":
                    // Verifica propriedade url
                    if (!actionElement.TryGetProperty("url", out var urlProp))
                        throw new Exception("Propriedade 'url' é necessária para ação 'navigate'");

                    var url = urlProp.GetString();
                    if (string.IsNullOrEmpty(url))
                        throw new Exception("Valor da URL é nulo ou vazio para ação 'navigate'");

                    await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                    await Task.Delay(2000); // Pausa após navegação
                    break;

                case "wait":
                    // Para wait, time é opcional com valor padrão
                    int milliseconds = 2000; // Valor padrão
                    
                    if (actionElement.TryGetProperty("time", out var timeProp))
                    {
                        // Tenta interpretar o valor como número ou string
                        if (timeProp.ValueKind == JsonValueKind.Number)
                        {
                            milliseconds = timeProp.GetInt32();
                        }
                        else if (timeProp.ValueKind == JsonValueKind.String)
                        {
                            // Tenta converter string para double (pode ser "0.5" segundos) e depois para milissegundos
                            if (double.TryParse(timeProp.GetString(), out var seconds))
                                milliseconds = (int)(seconds * 1000);
                        }
                    }
                    await Task.Delay(milliseconds);
                    break;

                default:
                    throw new Exception($"Ação não suportada: {action}");
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_browser != null) await _browser.CloseAsync();
            if (_playwright != null) _playwright.Dispose();
        }

        private void txtUrl_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }

    public class GeminiChatbot
    {
        // Use sua própria chave da API - esta é apenas um placeholder
        private const string ApiKey = ""; 
        private const string ModelName = "gemini-2.0-flash";
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string ApiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={ApiKey}";

        // O método precisa ser estático para compatibilidade com chamadas existentes
        public static async Task<string> GenerateTextAsync(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                return "Erro: Prompt não pode ser vazio";

            if (string.IsNullOrEmpty(ApiKey) || ApiKey == "SUA_API_KEY_AQUI")
                return "Erro: API Key não configurada. Adicione sua chave no código.";

            try
            {
                var requestData = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };

                var jsonRequest = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(ApiEndpoint, content);

                // Verifica se a resposta é JSON válido antes de processar
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"ERRO: {response.StatusCode} - {responseContent}";
                }

                // Verifica se é um JSON válido
                if (!IsValidJson(responseContent))
                {
                    return $"Resposta inválida da API: {responseContent}";
                }

                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                // Verifica se a estrutura do JSON é a esperada
                if (!root.TryGetProperty("candidates", out var candidates) ||
                    candidates.GetArrayLength() == 0 ||
                    !candidates[0].TryGetProperty("content", out var contentElement) ||
                    !contentElement.TryGetProperty("parts", out var parts) ||
                    parts.GetArrayLength() == 0)
                {
                    return $"Estrutura de resposta inesperada: {responseContent}";
                }

                return parts[0].GetProperty("text").GetString() ?? "Resposta vazia";
            }
            catch (Exception ex)
            {
                return $"Erro ao chamar Gemini API: {ex.Message}";
            }
        }

        private static bool IsValidJson(string strInput)
        {
            try
            {
                JsonDocument.Parse(strInput);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
