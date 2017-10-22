using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using System.Text;
using Alexa.NET;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace Torian.Edline.Skill.AWSLambda
{
    public class Function
    {

        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            var requestType = input.GetRequestType();
            if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;
                // check the name to determine what you should do
                if (intentRequest.Intent.Name.Equals("GetGradesIntent"))
                {
                    // get the slots
                    var student = intentRequest.Intent.Slots["StudentName"].Value;
                    var r = await EdlineEngine.LookupGradesAsync(new LookupGradesRequest() { Username = "locuester", Password = "", StudentName = student });
                    string gradeString = r.Grades.Aggregate(new StringBuilder(), (a, b) => a.Append($"{b.ClassName} : {b.Grade}. ")).ToString();
                    return ResponseBuilder.Tell($"{student}'s grades are: {gradeString}");
                }
            }
            return ResponseBuilder.Empty();
        }

    }
}
