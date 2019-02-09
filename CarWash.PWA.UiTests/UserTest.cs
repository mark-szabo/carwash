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
    public class UserTest
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

        [Priority(0)]
        [TestMethod]
        public void RunTest()
        {
            #region Login
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
            #endregion

            #region Make a new reservation
            TestContext.WriteLine("Clicking Reserve button.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='My reservations'])[1]/following::span[3]")).Click();

            TestContext.WriteLine("Selecting services.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Basic'])[1]/following::span[1]")).Click();
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='exterior'])[1]/following::span[2]")).Click();

            TestContext.WriteLine("Clicking Next button.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Back'])[1]/following::button[1]")).Click();

            TestContext.WriteLine("Selecting date.");
            driver.FindElement(By.XPath("//*[@id='root']/div/main/div/div[3]/div/div/div/div/div[1]/div/div/div/div/div[2]/div/ul[3]/li[1]")).Click();

            TestContext.WriteLine("Selecting slot.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Choose time'])[1]/following::span[12]")).Click();

            TestContext.WriteLine("Typing vehicle plate number.");
            driver.FindElement(By.Id("vehiclePlateNumber")).Click();
            driver.FindElement(By.Id("vehiclePlateNumber")).Clear();
            driver.FindElement(By.Id("vehiclePlateNumber")).SendKeys("TEST01");

            TestContext.WriteLine("Typing comment.");
            driver.FindElement(By.Id("comment")).Click();
            driver.FindElement(By.Id("comment")).Clear();
            driver.FindElement(By.Id("comment")).SendKeys("test");

            TestContext.WriteLine("Clicking Reserve button.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Back'])[1]/following::span[2]")).Click();

            TestContext.WriteLine("Waiting to save reservation.");
            for (int retry = 0; ; retry++)
            {
                if (retry >= 5) Assert.Fail("timeout");
                if (IsElementPresent(By.XPath("//*[@id='root']/div/main/div/div[1]/div/div/div[1]"))) break;
                TestContext.WriteLine("Still waiting...");
                Thread.Sleep(500);
            }

            Assert.AreEqual("Scheduled", driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Privacy & cookies policy'])[1]/following::span[2]")).Text);

            Assert.AreEqual("TEST01", driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Vehicle plate number'])[1]/following::p[1]")).Text);

            Assert.AreEqual("test", driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Vehicle plate number'])[1]/following::p[2]")).Text);

            Assert.AreEqual("exterior", driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Selected services'])[1]/following::span[1]")).Text);

            Assert.AreEqual("interior", driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='exterior'])[1]/following::span[1]")).Text);
            #endregion

            #region Cancel reservation
            TestContext.WriteLine("Clicking Cancel button.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Edit'])[1]/following::span[2]")).Click();

            TestContext.WriteLine("Confirming cancellation.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)=concat('Don', \"'\", 't cancel')])[1]/following::span[2]")).Click();

            TestContext.WriteLine("Waiting for reservation cancellation.");
            for (int retry = 0; ; retry++)
            {
                if (retry >= 5) Assert.Fail("timeout");
                if (IsElementPresent(By.XPath("//*[@id='root']/div/main/div/h6"))) break;
                TestContext.WriteLine("Still waiting...");
                Thread.Sleep(500);
            }

            Assert.AreEqual("Your reservations will show up here...", driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Privacy & cookies policy'])[1]/following::h6[1]")).Text);
            #endregion

            #region Logout
            TestContext.WriteLine("Clicking Sign out button.");
            driver.FindElement(By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Contact support'])[1]/following::span[2]")).Click();

            TestContext.WriteLine("Clicking options menu button next to the email address.");
            driver.FindElement(By.XPath("//*[@id='tilesHolder']/div[1]/div/div/div/div[3]/div")).Click();

            TestContext.WriteLine("Clicking Forget.");
            driver.FindElement(By.Id("forgetLink")).Click();
            #endregion
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
