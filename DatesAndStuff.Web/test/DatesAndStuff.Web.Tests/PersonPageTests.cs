using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }
    [TestCase(-5, 4750)]
    [TestCase(5, 5250)]
    [TestCase(10, 5500)]
    [TestCase(20, 6000)]
    public void Person_SalaryIncrease_ShouldIncrease(double percentage, double expectedSalary)
    {
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var inputBy = By.XPath("//*[@data-test='SalaryIncreasePercentageInput']");
        var buttonBy = By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']");
        var salaryBy = By.XPath("//*[@data-test='DisplayedSalary']");

        wait.Until(d =>
        {
            try
            {
                var input = d.FindElement(inputBy);
                input.Clear();
                input.SendKeys(percentage.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        wait.Until(d =>
        {
            try
            {
                d.FindElement(buttonBy).Click();
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        wait.Until(d =>
        {
            try
            {
                var text = d.FindElement(salaryBy).Text;
                return !string.IsNullOrWhiteSpace(text);
            }
            catch
            {
                return false;
            }
        });

        var salaryText = wait.Until(d => d.FindElement(salaryBy).Text);

        var salaryAfterSubmission = double.Parse(
            salaryText,
            System.Globalization.CultureInfo.InvariantCulture);

        salaryAfterSubmission.Should().BeApproximately(expectedSalary, 0.001);
    }

    [Test]
    public void Person_SalaryIncrease_ShouldShowErrorMessages_ForNegativePercentage()
    {
        // Arrange
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        var input = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));
        input.Clear();
        input.SendKeys("-15");

        // Act
        var submitButton = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']")));
        submitButton.Click();

        // Assert
        var validationMessage = wait.Until(ExpectedConditions.ElementExists(By.ClassName("validation-message")));
        validationMessage.Text.Should().NotBeNullOrWhiteSpace("Validation message should be displayed for invalid input.");
    }
    [Test]
    public void BlazeDemo_MexicoCityToDublin_ShouldHaveAtLeastThreeFlights()
    {
        driver.Navigate().GoToUrl("https://blazedemo.com");

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        var fromDropdown = wait.Until(
            ExpectedConditions.ElementIsVisible(By.Name("fromPort")));

        fromDropdown.FindElement(By.XPath(".//option[text()='Mexico City']")).Click();

        var toDropdown = wait.Until(
            ExpectedConditions.ElementIsVisible(By.Name("toPort")));

        toDropdown.FindElement(By.XPath(".//option[text()='Dublin']")).Click();

        wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("input[type='submit']"))).Click();

        wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(
            By.CssSelector("table.table tbody tr")));

        var flights = driver.FindElements(
            By.CssSelector("table.table tbody tr"));

        flights.Count.Should().BeGreaterThanOrEqualTo(3);
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

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}