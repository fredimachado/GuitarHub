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

        [TestMethod]
        public void TestShowIntervals()
        {
            okButton.Click();
            Thread.Sleep(200);

            var note = session.FindElementByName("A");
            var showIntervals = session.FindElementByName("Show Interval Names");

            showIntervals.Click();
            Thread.Sleep(200);

            Assert.AreEqual("P1", note.Text);
        }
    }
}
