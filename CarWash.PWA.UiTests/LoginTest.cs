using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace CarWash.PWA.UiTests
{
    [TestClass]
    public class LoginTest
    {
        private static IWebDriver driver;
        private StringBuilder _verificationErrors;
        private IConfiguration _configuration;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void InitializeClass(TestContext testContext)
        {
            var options = new ChromeOptions();
            options.AddArguments("--incognito");
            driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            try
            {
                //driver.Quit();// quit does not close the window
                driver.Close();
                driver.Dispose();
            }
            catch (Exception)
            {
                // Ignore errors if unable to close the browser
            }
        }

        [TestInitialize]
        public void InitializeTest()
        {
            _verificationErrors = new StringBuilder();

            _configuration = GetConfiguration();
        }

        [TestCleanup]
        public void CleanupTest()
        {
            Assert.AreEqual("", _verificationErrors.ToString());
        }

        [TestMethod]
        public void Login()
        {
            TestContext.WriteLine("Navigating to CarWash app.");
            driver.Navigate().GoToUrl(_configuration["BaseUrl"] + "/");

            TestContext.WriteLine("Typing email address.");
            driver.FindElement(By.Id("i0116")).Clear();
            driver.FindElement(By.Id("i0116")).SendKeys(_configuration["TestEmail"]);

            TestContext.WriteLine("Pressing enter.");
            driver.FindElement(By.Id("i0116")).SendKeys(Keys.Enter);

            TestContext.WriteLine("Typing password.");
            driver.FindElement(By.Id("i0118")).Clear();
            driver.FindElement(By.Id("i0118")).SendKeys(_configuration["TestPassword"]);
            Thread.Sleep(500);

            TestContext.WriteLine("Pressing enter.");
            driver.FindElement(By.Id("i0118")).SendKeys(Keys.Enter);

            TestContext.WriteLine("Clicking Yes button.");
            driver.FindElement(By.Id("idSIButton9")).Click();

            Assert.AreEqual("My reservations", driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Reserve'])[1]/preceding::h6[1]")).Text);
        }

        [TestMethod]
        public void Logout()
        {
            TestContext.WriteLine("Navigating to CarWash app.");
            driver.Navigate().GoToUrl(_configuration["BaseUrl"] + "/");

            TestContext.WriteLine("Clicking Sign out button.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Contact support'])[1]/following::span[2]")).Click();

            TestContext.WriteLine("Clicking options menu button next to the email address.");
            driver.FindElement(By.XPath("//*[@id='tilesHolder']/div[1]/div/div/div/div[3]/div")).Click();

            TestContext.WriteLine("Clicking Forget.");
            driver.FindElement(By.Id("forgetLink")).Click();
        }

        private bool IsElementPresent(By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public static IConfiguration GetConfiguration() => new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile("appsettings.Development.json", true)
            .Build();
    }
}
