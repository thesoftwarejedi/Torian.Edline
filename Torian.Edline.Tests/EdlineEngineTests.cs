using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Torian.Edline.Tests
{
    [TestClass]
    public class EdlineEngineTests
    {
        [TestMethod]
        public async Task TestLookupGrades()
        {
            var req = new LookupGradesRequest() { Username = "locuester", Password = "", StudentName = "Victoria" };
            var a = await EdlineEngine.LookupGradesAsync(req);
            Assert.IsTrue(a.Grades.Any());
        }
    }
}
