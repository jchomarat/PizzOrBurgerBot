using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace PizzOrBurger
{
     public class PizzOrBurgerBot : IBot
    {
        private static IConfiguration Configuration;

        public PizzOrBurgerBot(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Take the input from the user and create the appropriate response.
                await ProcessInputAsync(turnContext, cancellationToken);
            }
        }

        private static async Task ProcessInputAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;

            if (activity.Attachments != null && activity.Attachments.Count() == 1)
            {
                // Only one attachement allowed
                await HandleIncomingAttachmentAsync(turnContext, cancellationToken);
            }
            else
            {
                // Not an attachment, display the howto
                await DisplayOptions(turnContext, cancellationToken);
            }
        }

        private static async Task DisplayOptions(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply();
            reply.Text = "Upload a picture, and I'll tell you whether it is a pizza or a burger (yay I rock)";
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private static async Task HandleIncomingAttachmentAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            var reply = activity.CreateReply();

            // Determine where the file is hosted.
            var file = activity.Attachments.FirstOrDefault();
            var remoteFileUrl = file.ContentUrl;

            // Get the pict and predict what it is
            var predictions = "";
            try
            {
                byte[] content;
                using (var webClient = new WebClient())
                {
                    content = webClient.DownloadData(remoteFileUrl);
                }

                // Check if it is a pizza or a burger
                var url = Configuration.GetSection("PredictionsURL").Value;

                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Post,
                        Content = new ByteArrayContent(content)
                    };

                    request.Headers.Add("Prediction-Key", Configuration.GetSection("PredictionsKey").Value);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                    var response = await client.SendAsync(request);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    dynamic obj = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    var percentageRaw = obj.predictions[0].probability.Value.ToString();
                    var percentage = Math.Round(decimal.Parse(percentageRaw), 2) * 100;

                    predictions = $"Well, I am sure that this photo is at {percentage}% a {obj.predictions[0].tagName}";
                }
            }
            catch (Exception)
            {
                predictions = "Oops, couldn't do it";
            }

            // Set reply
            reply.Text = predictions;
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
