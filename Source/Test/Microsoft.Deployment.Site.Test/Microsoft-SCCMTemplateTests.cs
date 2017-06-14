using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Deployment.Site.Test.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace Microsoft.Deployment.Site.Web.Tests
{
    [TestClass]
    public class SCCMTemplateTests
    {
        private RemoteWebDriver driver;
        private string slot = "slot1";
        private string msiPath = @"C:\Program Files\Microsoft Templates\Microsoft-SCCMTemplate\Microsoft.Bpst.App.Msi.exe";

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                HelperMethods.CleanSubscription(
                    Credential.Instance.ServiceAccount.Username,
                    Credential.Instance.ServiceAccount.Password,
                    Credential.Instance.ServiceAccount.TenantId,
                    Credential.Instance.ServiceAccount.ClientId,
                    Credential.Instance.ServiceAccount.SubscriptionId);
            }
            catch
            {
                //if the clean subscription fails, then test probably failed before creation
            }

            try
            {
                HelperMethods.DeleteDatabase(Credential.Instance.Sql.Server,
                                            Credential.Instance.Sql.Username, Credential.Instance.Sql.Password,
                                            Credential.Instance.Sql.SCCMDatabase);
            }
            catch
            {
                //if the delete DB fails, then the test probably failed to create
            }
            try
            {
                HelperMethods.driver.Quit();
            }
            catch
            {
                //if we can't quit the driver, probably the process started and failed.  Cleanup so the next test doesn't get messed up
                var driver = Process.GetProcessesByName("chromedriver.exe");
                driver[0].Kill();
            }
        }

        [TestMethod]
        public void RunSCCMTests()
        {
            Credential.Load();
            DownloadAndInstallMSI();
            OpenWebBrowser();
            HelperMethods.driver = this.driver;
            HelperMethods.WaitForPage();
            try
            {
                HelperMethods.CreateDatabase(Credential.Instance.Sql.Server,
                                            Credential.Instance.Sql.Username, Credential.Instance.Sql.Password,
                                            Credential.Instance.Sql.SCCMDatabase);
            }
            catch
            {
                HelperMethods.DeleteDatabase(Credential.Instance.Sql.Server,
                                            Credential.Instance.Sql.Username, Credential.Instance.Sql.Password,
                                            Credential.Instance.Sql.SCCMDatabase);
                HelperMethods.CreateDatabase(Credential.Instance.Sql.Server,
                                            Credential.Instance.Sql.Username, Credential.Instance.Sql.Password,
                                            Credential.Instance.Sql.SCCMDatabase);
            }
            try
            {
                var background = driver.FindElementByCssSelector("div[class='st-email-background st-email-wrapper au-target']");
                background.Click();
            }
            catch { /* If not found means s3 is behind s1, expected behaviour*/}
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            Given_AlternativeWindowsCredentials_When_Validate_Then_Success();
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            Given_CorrectSqlCredentials_When_Validate_Then_Success();
            Thread.Sleep(new TimeSpan(0, 0, 5));
            HelperMethods.SelectSqlDatabase(Credential.Instance.SccmSql.Source);
            Thread.Sleep(new TimeSpan(0, 0, 5));
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            Thread.Sleep(new TimeSpan(0, 0, 45));
            Given_CorrectSqlCredentials_When_Validate_Then_Success();
            HelperMethods.SelectSqlDatabase(Credential.Instance.SccmSql.Target);
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            Thread.Sleep(new TimeSpan(0, 0, 45));
            HelperMethods.ClickButton("Validate");
            HelperMethods.WaitForPage();
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            Thread.Sleep(new TimeSpan(0, 0, 5));
            HelperMethods.ClickButton("Run");
            HelperMethods.CheckDeploymentStatus();

            int totalNumRows = HelperMethods.rowsInAllTables(Credential.Instance.Sql.Server,
                            Credential.Instance.Sql.Username, Credential.Instance.Sql.Password,
                            Credential.Instance.Sql.SCCMDatabase);
            if (totalNumRows <= 0)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void RunSCCMTestsWithAs()
        {
            Credential.Load();
            DownloadAndInstallMSI();
            OpenWebBrowser();
            HelperMethods.driver = this.driver;
            HelperMethods.WaitForPage();
            HelperMethods.CreateDatabase(Credential.Instance.Sql.Server,
                                            Credential.Instance.Sql.Username, Credential.Instance.Sql.Password,
                                            Credential.Instance.Sql.SCCMDatabase);
            try
            {
                var background = driver.FindElementByCssSelector("div[class='st-email-background st-email-wrapper au-target']");
                background.Click();
            }
            catch { /* If not found means s3 is behind s1, expected behaviour*/}
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            Given_AlternativeWindowsCredentials_When_Validate_Then_Success();
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            Given_CorrectSqlCredentials_When_Validate_Then_Success();
            HelperMethods.SelectSqlDatabase(Credential.Instance.SccmSql.Source);
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            SelectSqlAzure();
            HelperMethods.SelectSqlDatabase(Credential.Instance.Sql.SCCMDatabase);
            HelperMethods.ClickButton("Next");
            Thread.Sleep(new TimeSpan(0, 0, 5));
            HelperMethods.WaitForPage();
            MsiAsSelectionExperience();
            MsiAzureExperience();
            HelperMethods.ClickButton("Next");
            Thread.Sleep(new TimeSpan(0, 0, 5));
            HelperMethods.WaitForPage();
            MsiAsExperience("sccmas" + HelperMethods.resourceGroupName, Credential.Instance.ServiceAccount.Username, Credential.Instance.ServiceAccount.Password);
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            HelperMethods.ClickButton("Validate");
            HelperMethods.WaitForPage();
            HelperMethods.ClickButton("Next");
            HelperMethods.WaitForPage();
            HelperMethods.ClickButton("Run");
            HelperMethods.CheckDeploymentStatus();

            int totalNumRows = HelperMethods.rowsInAllTables(Credential.Instance.Sql.Server,
                            Credential.Instance.Sql.Username, Credential.Instance.Sql.Password,
                            Credential.Instance.Sql.SCCMDatabase);
            if (totalNumRows <= 0)
            {
                Assert.Fail();
            }
        }

        private void SelectSqlAzure()
        {
            Thread.Sleep(new TimeSpan(0, 0, 45));
            HelperMethods.WaitForPage();
            var sqlAzure = driver.FindElementsByCssSelector("input[class='au-target']").FirstOrDefault(e => e.GetAttribute("checked.bind") == "isAzureSql");

            while (sqlAzure == null)
            {
                sqlAzure = driver.FindElementsByCssSelector("input[class='au-target']").FirstOrDefault(e => e.GetAttribute("checked.bind") == "isAzureSql");
                Thread.Sleep(new TimeSpan(0, 0, 3));
            }


            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].click()", sqlAzure);

            var elements = driver.FindElementsByCssSelector("input[class='st-input au-target']");

            var serverBox = elements.First(e => e.GetAttribute("value.bind").Contains("sqlServer"));
            serverBox.SendKeys(Credential.Instance.Sql.Server.Split('.').First());

            var usernameBox = elements.First(e => e.GetAttribute("value.bind").Contains("username"));
            usernameBox.SendKeys(Credential.Instance.Sql.Username);

            var passwordBox = elements.First(e => e.GetAttribute("value.bind").Contains("password"));
            passwordBox.SendKeys(Credential.Instance.Sql.Password);

            HelperMethods.ClickButton("Validate");
        }

        public void MsiAzureExperience()
        {
            HelperMethods.AzurePage(
               Credential.Instance.ServiceAccount.Username,
               Credential.Instance.ServiceAccount.Password,
               Credential.Instance.ServiceAccount.SubscriptionName);
            var validated = driver.FindElementByClassName("st-validated");
            Assert.IsTrue(validated.Text == "Successfully validated");
        }

        public void MsiAsSelectionExperience()
        {
            Thread.Sleep(new TimeSpan(0, 0, 30));
            var button = driver.FindElementByCssSelector("select[class='btn btn-default dropdown-toggle st-input au-target']");
            
            while (button.Enabled != true)
            {
                Thread.Sleep(new TimeSpan(0, 0, 1));
                button = driver.FindElementByCssSelector("select[class='btn btn-default dropdown-toggle st-input au-target']");
            }

            button.SendKeys("Yes");

            HelperMethods.ClickButton("Next");
        }

        public void MsiAsExperience(string server, string username, string password)
        {
            Thread.Sleep(new TimeSpan(0, 0, 2));
            var newAas = driver.FindElementByCssSelector("select[class='btn btn-default dropdown-toggle st-input au-target']");

            while (newAas.Enabled != true)
            {
                Thread.Sleep(new TimeSpan(0, 0, 1));
                newAas = driver.FindElementByCssSelector("select[class='btn btn-default dropdown-toggle st-input au-target']");
            }

            newAas.SendKeys("New");

            var elements = driver.FindElementsByCssSelector("input[class='st-input au-target']");

            var serverBox = elements.FirstOrDefault(e => e.GetAttribute("value.bind").Contains("server"));
            var usernameBox = elements.FirstOrDefault(e => e.GetAttribute("value.bind").Contains("email"));
            var passwordBox = elements.FirstOrDefault(e => e.GetAttribute("value.bind").Contains("password"));

            while (usernameBox.Enabled != true && passwordBox.Enabled != true && passwordBox.Enabled != true)
            {
                Thread.Sleep(new TimeSpan(0, 0, 1));
            }

            passwordBox.SendKeys(password);
            usernameBox.Clear();
            usernameBox.SendKeys(username);
            serverBox.SendKeys(server);

            var aasSku = driver.FindElementByCssSelector("select[class='btn btn-default dropdown-toggle st-input au-target']");

            while (aasSku.Enabled != true)
            {
                Thread.Sleep(new TimeSpan(0, 0, 1));
                aasSku = driver.FindElementByCssSelector("select[class='btn btn-default dropdown-toggle st-input au-target']");
            }

            aasSku.SendKeys("Developer");

            HelperMethods.ClickButton("Validate");
        }

        public void SelectSqlDatabase(string databaseName)
        {
            var database = driver.FindElementsByCssSelector("select[class='btn btn-default dropdown-toggle st-input au-target']")
                            .FirstOrDefault(e => !e.Text.Contains("Windows"));

            while (database == null)
            {
                database = driver.FindElementsByCssSelector("select[class='btn btn-default dropdown-toggle st-input au-target']")
                            .FirstOrDefault(e => !e.Text.Contains("Windows"));
            }
            database.SendKeys(databaseName);
        }

        public void Given_CorrectSqlCredentials_When_Validate_Then_Success()
        {
            var button = driver.FindElementsByTagName("Button").FirstOrDefault(e => e.Text == "Validate");
            while (button == null || !button.Enabled)
            {
                button = driver.FindElementsByTagName("Button").FirstOrDefault(e => e.Text == "Validate");
                Thread.Sleep(1000);
            }

            var elements = driver.FindElementsByCssSelector("input[class='st-input au-target']");
            var serverBox = elements.FirstOrDefault(e => e.GetAttribute("value.bind").Contains("sqlServer"));
            while (serverBox == null)
            {
                serverBox = elements.FirstOrDefault(e => e.GetAttribute("value.bind").Contains("sqlServer"));
            }

            serverBox.Clear();
            serverBox.SendKeys(Credential.Instance.SccmSql.Server);

            HelperMethods.ClickButton("Validate");

            var validated = driver.FindElementByClassName("st-validated");
            Assert.IsTrue(validated.Text == "Successfully validated");
        }

        public void ValidateCurentUserCredentialsOnSourcePage()
        {
            var checkBox = driver.FindElementsByCssSelector("input[class='au-target']").First(e => e.GetAttribute("type") == "checkbox");
            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].click()", checkBox);

            var elements = driver.FindElementsByCssSelector("input[class='st-input au-target']");
            var passwordBox = elements.First(e => e.GetAttribute("value.bind").Contains("password"));
            passwordBox.SendKeys("");

            var validated = driver.FindElementByClassName("st-validated");
            Assert.IsTrue(validated.Text == "Successfully validated");
        }

        public void Given_AlternativeWindowsCredentials_When_Validate_Then_Success()
        {
            var elements = driver.FindElementsByCssSelector("input[class='st-input au-target']");

            var usernameBox = elements.First(e => e.GetAttribute("value.bind").Contains("username"));
            while (!usernameBox.Enabled)
            {
                Thread.Sleep(1000);
            }

            usernameBox.Clear();
            usernameBox.SendKeys($@"{Credential.Instance.ServiceAccount.Domain}\{Credential.Instance.ServiceAccount.Username.Split('@')[0]}");

            var passwordBox = elements.First(e => e.GetAttribute("value.bind").Contains("password"));
            passwordBox.SendKeys(Credential.Instance.ServiceAccount.Password);

            HelperMethods.ClickButton("Validate");

            var validated = driver.FindElementByClassName("st-validated");

            Assert.IsTrue(validated.Text == "Successfully validated");
        }

        public void OpenWebBrowser()
        {
            ChromeOptions options = new ChromeOptions();
            options.BinaryLocation = msiPath;
            options.AddArgument("?name=Microsoft-SCCMTemplate");
            options.AddUserProfilePreference("profile.password_manager_enabled", false);
            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
        }

        public void DownloadAndInstallMSI()
        {
            if (File.Exists("SCCM.exe"))
            {
                File.Delete("SCCM.exe");
            }

            var downloadUrl = $"https://bpstservice-{slot}.azurewebsites.net/bin//Apps/Microsoft/Released/Microsoft-SCCMTemplate/Microsoft-SCCMTemplate.exe";
            using (var client = new WebClient())
            {
                client.DownloadFile(downloadUrl, "SCCM.exe");
            }

            try
            {
                ProcessDownload("uninstall");
            }
            catch
            {
                // Program was not installed
            }

            ProcessDownload("install");
        }

        private void ProcessDownload(string type)
        {
            using (var p = new Process())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "SCCM.exe";
                startInfo.Arguments = $"/{type} /quiet /norestart";

                p.StartInfo = startInfo;

                if (p.Start())
                {
                    while (!p.HasExited)
                    {
                        Thread.Sleep(1000);
                    }
                }

                var installer = Process.GetProcessesByName("Microsoft.Bpst.App.Msi");
                installer[0].Kill();
            }
        }
    }
}