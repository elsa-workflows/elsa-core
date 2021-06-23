using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;
using PhoneNumbers;

namespace Elsa.Activities.Telnyx.Scripting.JavaScript
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>, INotificationHandler<RenderingTypeScriptDefinitions>
    {
        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            
            engine.SetValue("formatE164PhoneNumber", (Func<string, string, string>) (FormatE164PhoneNumber));
            return Task.CompletedTask;
        }

        public Task Handle(RenderingTypeScriptDefinitions notification, CancellationToken cancellationToken)
        {
            notification.Output.AppendLine("declare function formatE164PhoneNumber(number: string, defaultRegion: string): string;");
            return Task.CompletedTask;
        }
        
        private string FormatE164PhoneNumber(string number, string defaultRegion)
        {
            var phoneNumberUtil = PhoneNumberUtil.GetInstance();
            var phoneNumber = phoneNumberUtil.Parse(number, defaultRegion);
            return phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.E164);
        }
    }
}