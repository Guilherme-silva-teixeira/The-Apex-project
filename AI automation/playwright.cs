using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfApp3
{
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
            try
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    Channel = "chrome"
                });
                _page = await _browser.NewPageAsync();

                MessageBox.Show("Playwright inicializado com sucesso!");
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
            browserFrame.Content = image;
        }

        private async void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pageInfo = await GetPageInfo();
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
{await GetPageInfo()}

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
]";

                var aiResponse = await GeminiChatbot.GenerateTextAsync(prompt);
                await ExecuteAIAction(aiResponse);
                await UpdateBrowserPreview();

                MessageBox.Show("Comandos executados com sucesso!");
            }
            catch (Exception ex)
            {

                try
                {
                    var pageInfo = await GetPageInfo();
                    var userCommand = txtCommand.Text;


                    var prompt = $@"
                        Tente executar o comando novamente
                        Comando complexo: {userCommand}
                        Contexto atual:
                        {pageInfo}";


                    var aiResponse = await GeminiChatbot.GenerateTextAsync(prompt);
                    await ExecuteAIAction(aiResponse);
                    await UpdateBrowserPreview();
                }
                catch (Exception innerEx)
                {
                    //MessageBox.Show($"Erro ao processar resposta de erro: {innerEx.Message}");
                    MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }

        private async Task<string> GetPageInfo()
        {
            try
            {
                var url = _page.Url;
                var title = await _page.TitleAsync();
                var builder = new StringBuilder();

                builder.AppendLine($"URL atual: {url}");
                builder.AppendLine($"Título da página: {title}");
                builder.AppendLine("\n## Estrutura da Página ##");

                // 1. Captura de elementos HTML estruturais (como anteriormente)
                await AppendHtmlElementsInfo(builder);

                // 2. Captura de informações CSS
                //await AppendCssInfo(builder);

                // 3. Captura de informações JavaScript
                await AppendJavaScriptInfo(builder);

                return builder.ToString();
            }
            catch (Exception ex)
            {
                return $"Erro ao obter informações da página: {ex.Message}";
            }
        }

        private async Task AppendCssInfo(StringBuilder builder)
        {
            builder.AppendLine("\n## Estilos CSS ##");

            // 1. Classes CSS mais utilizadas
            var allElements = await _page.QuerySelectorAllAsync("*");
            var classFrequency = new Dictionary<string, int>();

            foreach (var element in allElements.Take(500)) // Limitar para performance
            {
                var classes = await element.GetAttributeAsync("class");
                if (!string.IsNullOrEmpty(classes))
                {
                    foreach (var cls in classes.Split(' '))
                    {
                        if (classFrequency.ContainsKey(cls))
                            classFrequency[cls]++;
                        else
                            classFrequency[cls] = 1;
                    }
                }
            }

            if (classFrequency.Count > 0)
            {
                builder.AppendLine("\n### Classes CSS mais comuns ###");
                foreach (var kvp in classFrequency.OrderByDescending(x => x.Value).Take(10))
                {
                    builder.AppendLine($"- .{kvp.Key}: {kvp.Value} elementos");
                }
            }

            // 2. Estilos inline
            var styledElements = await _page.QuerySelectorAllAsync("[style]");
            if (styledElements.Count > 0)
            {
                builder.AppendLine($"\n### Elementos com estilos inline ({styledElements.Count}) ###");
                foreach (var element in styledElements.Take(5))
                {
                    var tag = await element.GetAttributeAsync("tagName");
                    var id = await element.GetAttributeAsync("id");
                    var style = await element.GetAttributeAsync("style");

                    builder.AppendLine($"- {tag}{(string.IsNullOrEmpty(id) ? "" : $"#{id}")}");
                    builder.AppendLine($"  {style}");
                }
            }
        }

        private async Task<string> AppendHtmlElementsInfo(StringBuilder builder)
        {
            try
            {
                var url = _page.Url;
                var title = await _page.TitleAsync();

                new StringBuilder().AppendLine($"URL atual: {url}");
                new StringBuilder().AppendLine($"Título da página: {title}");
                new StringBuilder().AppendLine("\n## Estrutura da Página ##");

                // Captura elementos h1-h6
                for (int i = 1; i <= 6; i++)
                {
                    var headings = await _page.QuerySelectorAllAsync($"h{i}");
                    if (headings.Count > 0)
                    {
                        builder.AppendLine($"\n### Cabeçalhos H{i} ({headings.Count}) ###");
                        foreach (var heading in headings)
                        {
                            var text = await heading.InnerTextAsync();
                            var id = await heading.GetAttributeAsync("id");
                            builder.AppendLine($"- {(string.IsNullOrEmpty(id) ? "" : $"#{id} ")}{text?.Trim()}");
                        }
                    }
                }

                // Captura botões
                var buttons = await _page.QuerySelectorAllAsync("button, input[type='button'], input[type='submit']");
                if (buttons.Count > 0)
                {
                    new StringBuilder().AppendLine($"\n### Botões ({buttons.Count}) ###");
                    foreach (var button in buttons)
                    {
                        var text = await button.InnerTextAsync();
                        var id = await button.GetAttributeAsync("id");
                        var name = await button.GetAttributeAsync("name");
                        var type = await button.GetAttributeAsync("type");
                        var value = await button.GetAttributeAsync("value");

                        new StringBuilder().AppendLine($"- {(string.IsNullOrEmpty(id) ? "" : $"#{id} ")}" +
                                          $"{(string.IsNullOrEmpty(name) ? "" : $"[name={name}] ")}" +
                                          $"{(string.IsNullOrEmpty(type) ? "" : $"(type={type}) ")}" +
                                          $"{(string.IsNullOrEmpty(value) ? "" : $"[value={value}] ")}" +
                                          $"{text?.Trim()}");
                    }
                }

                // Captura inputs (exceto botões já capturados)
                var inputs = await _page.QuerySelectorAllAsync("input:not([type='button']):not([type='submit'])");
                if (inputs.Count > 0)
                {
                    new StringBuilder().AppendLine($"\n### Campos de Input ({inputs.Count}) ###");
                    foreach (var input in inputs)
                    {
                        var type = await input.GetAttributeAsync("type");
                        var id = await input.GetAttributeAsync("id");
                        var name = await input.GetAttributeAsync("name");
                        var placeholder = await input.GetAttributeAsync("placeholder");
                        var value = await input.GetAttributeAsync("value");

                        new StringBuilder().AppendLine($"- {(string.IsNullOrEmpty(id) ? "" : $"#{id} ")}" +
                                          $"{(string.IsNullOrEmpty(name) ? "" : $"[name={name}] ")}" +
                                          $"{(string.IsNullOrEmpty(type) ? "" : $"(type={type}) ")}" +
                                          $"{(string.IsNullOrEmpty(placeholder) ? "" : $"[placeholder={placeholder}] ")}" +
                                          $"{(string.IsNullOrEmpty(value) ? "" : $"[value={value}] ")}");
                    }
                }

                // Captura divs com conteúdo relevante
                var divs = await _page.QuerySelectorAllAsync("div");
                if (divs.Count > 0)
                {
                    new StringBuilder().AppendLine($"\n### Divs com conteúdo ({divs.Count}) ###");
                    foreach (var div in divs.Take(20)) // Limita para não ficar muito grande
                    {
                        var text = await div.InnerTextAsync();
                        var id = await div.GetAttributeAsync("id");
                        var @class = await div.GetAttributeAsync("class");

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            new StringBuilder().AppendLine($"- {(string.IsNullOrEmpty(id) ? "" : $"#{id} ")}" +
                                              $"{(string.IsNullOrEmpty(@class) ? "" : $"[class={@class}] ")}" +
                                              $"{text?.Trim().Substring(0, Math.Min(100, text.Length))}" +
                                              $"{(text.Length > 100 ? "..." : "")}");
                        }
                    }
                }

                // Captura parágrafos
                var paragraphs = await _page.QuerySelectorAllAsync("p");
                if (paragraphs.Count > 0)
                {
                    new StringBuilder().AppendLine($"\n### Parágrafos ({paragraphs.Count}) ###");
                    foreach (var p in paragraphs.Take(10)) // Limita para não ficar muito grande
                    {
                        var text = await p.InnerTextAsync();
                        var id = await p.GetAttributeAsync("id");

                        new StringBuilder().AppendLine($"- {(string.IsNullOrEmpty(id) ? "" : $"#{id} ")}" +
                                          $"{text?.Trim().Substring(0, Math.Min(150, text.Length))}" +
                                          $"{(text.Length > 150 ? "..." : "")}");
                    }
                }

                return new StringBuilder().ToString();
            }
            catch (Exception ex)
            {
                return $"Erro ao obter informações da página: {ex.Message}";
            }
        }

        private async Task AppendJavaScriptInfo(StringBuilder builder)
        {
            builder.AppendLine("\n## Comportamento JavaScript ##");

            // 1. Event listeners importantes
            var interactiveElements = await _page.QuerySelectorAllAsync(
                "[onclick], [onchange], [onmouseover], [onkeydown], [onload]");

            if (interactiveElements.Count > 0)
            {
                builder.AppendLine($"\n### Elementos com eventos JavaScript ({interactiveElements.Count}) ###");
                foreach (var element in interactiveElements.Take(10))
                {
                    var tag = await element.GetAttributeAsync("tagName");
                    var id = await element.GetAttributeAsync("id");

                    builder.Append($"- {tag}{(string.IsNullOrEmpty(id) ? "" : $"#{id}")}");

                    var events = new List<string>();
                    if (await element.GetAttributeAsync("onclick") != null) events.Add("onclick");
                    if (await element.GetAttributeAsync("onchange") != null) events.Add("onchange");
                    if (await element.GetAttributeAsync("onmouseover") != null) events.Add("onmouseover");
                    if (await element.GetAttributeAsync("onkeydown") != null) events.Add("onkeydown");
                    if (await element.GetAttributeAsync("onload") != null) events.Add("onload");

                    builder.AppendLine($" ({string.Join(", ", events)})");
                }
            }

            // 2. Detecção de frameworks JS
            var scripts = await _page.QuerySelectorAllAsync("script");
            var frameworks = new HashSet<string>();

            foreach (var script in scripts)
            {
                var src = await script.GetAttributeAsync("src");
                if (!string.IsNullOrEmpty(src))
                {
                    if (src.Contains("jquery")) frameworks.Add("jQuery");
                    if (src.Contains("react")) frameworks.Add("React");
                    if (src.Contains("vue")) frameworks.Add("Vue");
                    if (src.Contains("angular")) frameworks.Add("Angular");
                }
            }

            if (frameworks.Count > 0)
            {
                builder.AppendLine("\n### Frameworks JavaScript detectados ###");
                foreach (var framework in frameworks)
                {
                    builder.AppendLine($"- {framework}");
                }
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

                using var jsonDoc = JsonDocument.Parse(aiResponse);
                var root = jsonDoc.RootElement;

                // Verifica se é um array ou um objeto único
                if (root.ValueKind == JsonValueKind.Array)
                {
                    // Processa cada ação sequencialmente
                    foreach (var actionElement in root.EnumerateArray())
                    {
                        await ExecuteSingleAction(actionElement);
                    }
                }
                else
                {
                    // Processa ação única (backward compatibility)
                    await ExecuteSingleAction(root);
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao executar ação: {ex.Message}\nResposta original: {aiResponse}");
            }
        }

        private async Task ExecuteSingleAction(JsonElement actionElement)
        {
            var action = actionElement.GetProperty("action").GetString();
            var selector = actionElement.GetProperty("selector").GetString();
            var options = new PageWaitForSelectorOptions { Timeout = 40000 };

            try
            {
                switch (action?.ToLower())
                {
                    case "click":
                        // Espera o elemento estar visível e habilitado
                        await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = 40000
                        });

                        await _page.ClickAsync(selector, new PageClickOptions
                        {
                            Timeout = 40000,
                            Force = false
                        });
                        await Task.Delay(1000); // Pausa após clique
                        break;

                    case "type":
                        var text = actionElement.GetProperty("text").GetString();
                        await _page.FillAsync(selector, text);
                        await Task.Delay(500); // Pausa após digitação
                        break;

                    case "navigate":
                        var url = actionElement.GetProperty("url").GetString();
                        await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                        await Task.Delay(2000); // Pausa após navegação
                        break;

                    case "wait":
                        var milliseconds = actionElement.TryGetProperty("time", out var timeProp)
                            ? timeProp.GetInt32()
                            : 2000;
                        await Task.Delay(milliseconds);
                        break;

                    default:
                        throw new Exception($"Ação não suportada: {action}");
                }
            }
            catch (Exception ex)
            {
                // Captura screenshot para diagnóstico
                var screenshot = await _page.ScreenshotAsync();
                throw new Exception($"Falha na ação: {action} | Seletor: {selector} | Erro: {ex.Message}");
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_browser != null) await _browser.CloseAsync();
            if (_playwright != null) _playwright.Dispose();
        }
    }

    public class GeminiChatbot
    {
        private const string ApiKey = "exemplo";
        private const string ModelName = "gemini-2.0-flash";
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string ApiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={ApiKey}";

        public static async Task<string> GenerateTextAsync(string prompt)
        {
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
