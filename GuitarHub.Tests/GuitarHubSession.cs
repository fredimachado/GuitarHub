using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using System;
using System.IO;

namespace GuitarHub.Tests
{
    public class GuitarHubSession
    {
        protected const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
        private static string GuitarHubAppId = @"..\..\..\GuitarHub\bin\Debug\GuitarHub.exe";

        protected static WindowsDriver<WindowsElement> session;
        protected static WindowsElement okButton;

        public static void Setup(TestContext context)
        {
            // Launch a new instance of GuitarHub application
            if (session == null)
            {
                if (!File.Exists(GuitarHubAppId))
                {
                    GuitarHubAppId = Environment.GetEnvironmentVariable("AppFilePath");
                }

                Assert.IsTrue(File.Exists(GuitarHubAppId), $"File doesn't exist: {GuitarHubAppId}");

                GuitarHubAppId = Path.GetFullPath(GuitarHubAppId);

                // Create a new session to launch GuitarHub application
                DesiredCapabilities appCapabilities = new DesiredCapabilities();
                appCapabilities.SetCapability("app", GuitarHubAppId);
                session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), appCapabilities);
                Assert.IsNotNull(session);
                Assert.IsNotNull(session.SessionId);

                // Verify the app title
                Assert.IsTrue(session.Title.StartsWith("Guitar Hub"));

                // Set implicit timeout to 1.5 seconds to make element search to retry every 500 ms for at most three times
                session.Manage()
                       .Timeouts()
                       .ImplicitWait = TimeSpan.FromSeconds(1.5);

                // Keep track of the edit box to be used throughout the session
                okButton = session.FindElementByName("OK");
                Assert.IsNotNull(okButton);
            }
        }

        public static void TearDown()
        {
            // Close the application and delete the session
            if (session != null)
            {
                session.Close();
                session.Quit();
                session = null;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {

        }
    }
}
