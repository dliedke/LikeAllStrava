using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace LikeAllStrava
{
    class Program
    {
        static void Main(string[] args)
        {
            // Fecha qualquer ChromeDriver aberto
            FechaChromeDrivers();

            // Inicializa o ChromeDriver com zero logs
            ChromeDriverService service = ChromeDriverService.CreateDefaultService(@"C:\Users\Daniel\Source\Repos\LikeAllStrava\bin\Debug\net6.0");
            service.SuppressInitialDiagnosticInformation = true;  // Disable logs
            service.EnableVerboseLogging = false;                 // Disable logs
            service.EnableAppendLog = false;                      // Disable logs
            service.HideCommandPromptWindow = true;               // Hide any ChromeDriver window
            IWebDriver driver = new ChromeDriver(service);
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            Console.WriteLine("Iniciando login no Strava...");

            // Abre o Strava (2x para garantir melhor página)
            driver.Url = "https://www.strava.com/login";
            System.Threading.Thread.Sleep(2000);
            driver.Url = "https://www.strava.com/login";
            System.Threading.Thread.Sleep(2000);

            // Click no botão de aceitar os cookies
            var acceptCookiesButton = driver.FindElement(By.CssSelector(".btn-accept-cookie-banner"));
            acceptCookiesButton.Click();

            // Seta email para login 
            var emailText = driver.FindElement(By.Id("email"));
            emailText.SendKeys("dliedke@gmail.com");

            // Seta senha para login 
            var passwordText = driver.FindElement(By.Id("password"));
            passwordText.SendKeys("s3k5c2DA!");

            // Aperta no botão de login
            var loginButton = driver.FindElement(By.Id("login-button"));
            loginButton.Click();

            // Espera um pouco pra logar
            System.Threading.Thread.Sleep(2000);
            WebDriverExtensions.WaitExtension.WaitUntilElement(driver, By.CssSelector("div.athlete-name"), 30);
            System.Threading.Thread.Sleep(2000);
            Console.WriteLine("Completado login no Strava");

            int totalCards = 0;

        retry:

            try
            {
                // Encontra todos botoes de like
                var botoesLike = driver.FindElements(By.CssSelector("[data-testid='unfilled_kudos']"));
                foreach (var botao in botoesLike)
                {
                    try
                    {
                        var element1 = GetParent(GetParent(GetParent(GetParent(GetParent(GetParent(botao))))));
                        var str = element1.GetAttribute("innerHTML");
                        if (!str.Contains(@"data-testid=""owners-name"">Daniel Carvalho Liedke</a>"))
                        {
                            // Clica nos botões de like e espera 5s
                            Console.Write("Encontrado treino pra dar like...");
                            ScrollToElement(js, botao);
                            System.Threading.Thread.Sleep(1000);
                            js.ExecuteScript("var evt = document.createEvent('MouseEvents');" + "evt.initMouseEvent('click',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);" + "arguments[0].dispatchEvent(evt);", botao);
                            Console.WriteLine("LIKED!");
                            System.Threading.Thread.Sleep(7000);
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Faz scroll até o final da página para carregar mais e aguarda
            Console.WriteLine("Fazendo scroll para carregar mais conteúdo...");
            ScrollToBottomOnce(driver);

            var cards = driver.FindElements(By.CssSelector(".react-card-container"));
            int totalCardsNow = cards.Count;

            // Repete scroll até não ter mais novos conteúdos
            if (totalCardsNow != totalCards)
            {
                totalCards = totalCardsNow;
                goto retry;
            }

            // Fecha o ChromeDriver e sai
            Console.WriteLine("Terminado!!");
            FechaChromeDrivers();
        }

        public static IWebElement GetParent(IWebElement e)
        {
            return e.FindElement(By.XPath(".."));
        }

        private static void ScrollToElement(IJavaScriptExecutor js, IWebElement element)
        {
            try
            {
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

        private static void ScrollToBottomOnce(IWebDriver driver)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            Thread.Sleep(5000);
        }
    }
}

