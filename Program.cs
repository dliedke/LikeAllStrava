using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace LikeAllStrava
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Seu login do strava, senha e nome completo do perfil
                string login = "dliedke@gmail.com";
                string password = "";
                string nomeCompleto = "Daniel Carvalho Liedke";

                // Fecha qualquer ChromeDriver aberto
                FechaChromeDrivers();

                // Inicializa o ChromeDriver com zero logs
                string currentRuntimeDirectory = AppContext.BaseDirectory;
                ChromeDriverService service = ChromeDriverService.CreateDefaultService(currentRuntimeDirectory);
                service.SuppressInitialDiagnosticInformation = true;  // Desabilita logs
                service.EnableVerboseLogging = false;                 // Desabilita logs
                service.EnableAppendLog = false;                      // Desabilita logs
                service.HideCommandPromptWindow = true;               // Esconde janela
                IWebDriver driver = new ChromeDriver(service);
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

                Console.WriteLine("Iniciando login no Strava...");

                // Abre o Strava (2x para garantir melhor carregamento da página)
                driver.Url = "https://www.strava.com/login";
                System.Threading.Thread.Sleep(2000);
                driver.Url = "https://www.strava.com/login";
                System.Threading.Thread.Sleep(2000);

                // Click no botão de aceitar os cookies
                var acceptCookiesButton = driver.FindElement(By.CssSelector(".btn-accept-cookie-banner"));
                acceptCookiesButton.Click();

                // Seta email para login 
                var emailText = driver.FindElement(By.Id("email"));
                emailText.SendKeys(login);

                // Seta senha para login 
                var passwordText = driver.FindElement(By.Id("password"));
                passwordText.SendKeys(password);

                // Aperta no botão de login
                var loginButton = driver.FindElement(By.Id("login-button"));
                loginButton.Click();

                // Espera um pouco pra logar e verifica se elemento carregou
                System.Threading.Thread.Sleep(2000);
                WebDriverExtensions.WaitExtension.WaitUntilElement(driver, By.CssSelector(".EntryHeader--entry-header--14ujs"), 15);
                System.Threading.Thread.Sleep(2000);
                Console.WriteLine("Completado login no Strava");

                int totalCartoesTreinamento = 0;

            retry:

                try
                {
                    // Encontra todos botoes de like ainda não clicados
                    var botoesLike = driver.FindElements(By.CssSelector("[data-testid='unfilled_kudos']"));
                    foreach (var botao in botoesLike)
                    {
                        try
                        {
                            // Busca o html do card do treino pra ver se não 
                            // é treino do próprio usuário
                            var element1 = GetParent(GetParent(GetParent(GetParent(GetParent(GetParent(botao))))));
                            var str = element1.GetAttribute("innerHTML");
                            if (!str.Contains(@$"data-testid=""owners-name"">{nomeCompleto}</a>"))
                            {
                                // Faz scroll até o botão de like
                                Console.Write("Encontrado treino pra dar like...");
                                ScrollToElement(js, botao);
                                System.Threading.Thread.Sleep(1000);

                                // Clica nos botões de like e espera 7s pra não ser 
                                // bloqueado pelo Strava
                                js.ExecuteScript("var evt = document.createEvent('MouseEvents');" + "evt.initMouseEvent('click',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);" + "arguments[0].dispatchEvent(evt);", botao);
                                Console.WriteLine("LIKED!");
                                System.Threading.Thread.Sleep(7000);
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // Faz scroll até o final da página para carregar mais conteúdo e aguarda
                Console.WriteLine("Fazendo scroll para carregar mais conteúdo...");
                ScrollToBottom(js);

                // Pega o total de cards com treinos
                var cards = driver.FindElements(By.CssSelector(".react-card-container"));
                int totalCartoesAgora = cards.Count;

                // Repete scroll até não ter mais novos treinos na página
                if (totalCartoesAgora != totalCartoesTreinamento)
                {
                    totalCartoesTreinamento = totalCartoesAgora;
                    goto retry;
                }

                Console.WriteLine("Terminado! Obrigado!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error na automação (talvez dados errados de login?): " + ex.ToString());
            }
            finally
            {
                // Fecha o ChromeDriver e sai
                FechaChromeDrivers();
            }
        }

        public static IWebElement GetParent(IWebElement e)
        {
            // Busca elemento pai
            return e.FindElement(By.XPath(".."));
        }

        private static void ScrollToElement(IJavaScriptExecutor js, IWebElement element)
        {
            try
            {
                // Faz scroll até o elemento
                if (element.Location.Y > 200)
                {
                    js.ExecuteScript($"window.scrollTo({0}, {element.Location.Y - 200 })");
                }
            }
            catch { }
        }

        private static void FechaChromeDrivers()
        {
            // Fecha todos processos do ChromeDriver
            System.Diagnostics.Process.Start("taskkill", "/F /IM chromedriver.exe /T");
        }

        private static void ScrollToBottom(IJavaScriptExecutor js)
        {
            // Faz scroll até o final da página e aguarda 5s carregar
            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            Thread.Sleep(5000);
        }
    }
}

