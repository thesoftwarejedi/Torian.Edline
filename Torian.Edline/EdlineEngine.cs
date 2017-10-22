using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Torian.Edline
{
    public class EdlineEngine
    {
        private static readonly Uri _s_baseAddress = new Uri("https://www.edline.net");

        public static async Task<LookupGradesResponse> LookupGradesAsync(LookupGradesRequest req)
        {
            var cookieContainer = new CookieContainer();
            HtmlDocument doc = new HtmlDocument();
            var ret = new LookupGradesResponse();

            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler) { BaseAddress = _s_baseAddress })
            {
                //let cookies set and get csrf token
                var resp = await client.GetAsync(_s_baseAddress + "/Index.page");
                doc.Load(await resp.Content.ReadAsStreamAsync());
                var csrf = doc.DocumentNode.SelectSingleNode("//input[@name='csrfToken']").GetAttributeValue("value", string.Empty);

                //post a login
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("screenName", req.Username),
                    new KeyValuePair<string, string>("kclq", req.Password),
                    new KeyValuePair<string, string>("csrfToken", csrf),
                    new KeyValuePair<string, string>("TCNK", "authenticationEntryComponent"),
                    new KeyValuePair<string, string>("submitEvent", "1"),
                    new KeyValuePair<string, string>("guestLoginEvent", ""),
                    new KeyValuePair<string, string>("enterClicked", "true"),
                    new KeyValuePair<string, string>("bscf", ""),
                    new KeyValuePair<string, string>("bscv", ""),
                    new KeyValuePair<string, string>("targetEntid", ""),
                    new KeyValuePair<string, string>("ajaxSupported", "yes"),
                });
                await client.PostAsync(_s_baseAddress + "/post/Index.page", content);

                //get the student names
                content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("csrfToken", csrf),
                    new KeyValuePair<string, string>("invokeEvent", "viewUserDocList"),
                    new KeyValuePair<string, string>("eventParms", "TCNK=headerComponent"),
                });
                resp = await client.PostAsync(_s_baseAddress + "/GroupHome.page", content);
                doc.Load(await resp.Content.ReadAsStreamAsync());
                var students = doc.GetElementbyId("vusrList")
                    .SelectNodes("option")
                    .Skip(1)
                    .Select(a => Tuple.Create(
                        HtmlEntity.DeEntitize(a.NextSibling.InnerText).Trim().Split(' ').FirstOrDefault(), //first name
                        a.Attributes[0].Value)); //id

                //pick out the student by name or first letter of name
                var student = students.FirstOrDefault(a => a.Item1 == req.StudentName);
                if (student == null)
                    student = students.FirstOrDefault(a => a.Item1[0] == req.StudentName[0]);
                if (student == null)
                    throw new StudentNotFoundException() { StudentName = req.StudentName };

                //get the student doc list
                resp = await client.GetAsync(_s_baseAddress + $"/UserDocList.page?vusr={student.Item2}");
                doc.Load(await resp.Content.ReadAsStreamAsync());
                var classRows = doc.DocumentNode
                    .SelectNodes("//table[3]//tr")
                    .Skip(1);
                var classes = CreateClasses(classRows).ToLookup(a => a.Item1, a => a.Item2); //force enumerate

                ret.Grades = CreateGrades(client, classes.Select(a => Tuple.Create(a.Key, a.First())));
            }
            return ret;
        }

        private static IEnumerable<Tuple<string, string>> CreateClasses(IEnumerable<HtmlNode> classRows)
        {
            foreach (var row in classRows)
            {
                var a = row.SelectNodes("td/a");
                var id = a[0].Attributes[0].Value.Split('\'')[1]; //id
                var name = HtmlEntity.DeEntitize(a[1].InnerText).Trim(); //name
                yield return Tuple.Create(name, id);
            }
        }

        private static IEnumerable<ClassGrade> CreateGrades(HttpClient client, IEnumerable<Tuple<string, string>> classes)
        {
            var t = new List<Task<ClassGrade>>();
            foreach (var c in classes)
                t.Add(GetGrade(client, c));
            Task.WaitAll(t.ToArray());
            return t.Select(a => a.Result);
        }

        private static async Task<ClassGrade> GetGrade(HttpClient client, Tuple<string, string> c)
        {
            var doc = new HtmlDocument();
            doc.Load(await client.GetStreamAsync(_s_baseAddress + $"/DocViewBody.page?currentDocEntid={c.Item2}&returnPage=/UserDocList.page"));
            var comment = doc.DocumentNode.SelectSingleNode("//comment()[contains(., 'curavggrd')]").InnerHtml;
            /*
             * <!--<customdata>
                <curavggrd><![CDATA[A]]></curavggrd>
                <curavgpct><![CDATA[ 95.8%]]></curavgpct>
                <ytdavggrd><![CDATA[A]]></ytdavggrd>
                <ytdavgpct><![CDATA[  4.0%]]></ytdavgpct>
                </customdata>-->
                */
            comment = comment.Split(new string[] { "<![CDATA[" }, StringSplitOptions.RemoveEmptyEntries)[2];
            comment = comment.Split(']').First().Trim();
            return new ClassGrade() { ClassName = c.Item1, Grade = comment };
        }
    }
}
