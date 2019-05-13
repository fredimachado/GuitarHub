using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace GuitarHub.Tests
{
    [TestClass]
    public class BasicUITests : GuitarHubSession
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            Setup(context);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TearDown();
        }

        [TestMethod]
        public void Test25Frets()
        {
            okButton.Click();
            Thread.Sleep(200);

            var fretNumbers = session.FindElementsByXPath("//Text[starts-with(@AutomationId, 'fret')]");

            // 25 because the open string note is considered a fret
            Assert.AreEqual(25, fretNumbers.Count);
        }
    }
}
