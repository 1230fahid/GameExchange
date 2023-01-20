using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace GameExchange.Utility
{
	public class EmailSender : IEmailSender
	{
		public Task SendEmailAsync(string email, string subject, string htmlMessage) //confused behind logic.
		{
			//return Task.CompletedTask; //how we do a fake implementation
			var emailToSend = new MimeMessage();
			emailToSend.From.Add(MailboxAddress.Parse("1230fahid@gmail.com"));
			emailToSend.To.Add(MailboxAddress.Parse(email));
			emailToSend.Subject = subject;
			emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html){ Text = htmlMessage};
			
			//send email
			using(var emailClient = new SmtpClient()) //don't use System.Net.Mail
            {				
				emailClient.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
				emailClient.Authenticate("1230fahid@gmail.com", "jyur oljy xkwl jzmr");
				emailClient.Send(emailToSend);
				emailClient.Disconnect(true);
			}

			return Task.CompletedTask;
			
		}
	}
}
