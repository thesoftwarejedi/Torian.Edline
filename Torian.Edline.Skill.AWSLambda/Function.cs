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
            var intent = input.Request as IntentRequest;
            if (input.GetRequestType() == typeof(LaunchRequest))
                return ResponseBuilder.Tell("You can ask me for a particular student grades");
            else if (intent.DialogState != DialogState.Completed)
                return ResponseBuilder.DialogDelegate();
            else if (intent.Intent.ConfirmationStatus == ConfirmationStatus.Denied)
                return ResponseBuilder.Empty();
            else if (intent.Intent.Name.Equals("GetGradesIntent"))
            {
                try
                {
                    // get the slots
                    var student = intent.Intent.Slots["StudentName"].Value;
                    var r = await EdlineEngine.LookupGradesAsync(new LookupGradesRequest() { Username = "locuester", Password = "", StudentName = student });
                    string gradeString = r.Grades.Aggregate(new StringBuilder(), (a, b) => a.Append($"{b.ClassName} : {b.Grade}.\r\n")).ToString();
                    return ResponseBuilder.TellWithCard($"{r.StudentName}'s grades are: {gradeString}", $"{r.StudentName}'s grades", gradeString);
                }
                catch (Exception ex)
                {
                    return ResponseBuilder.Tell($"I had a problem with your request: {ex.Message}");
                }
            }
            else
                return ResponseBuilder.Empty();
        }

    }
}
