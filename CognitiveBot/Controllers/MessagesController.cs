using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace CognitiveBot
{
    using System.Configuration;
    using System.Text;
    using System.Threading;

    using Microsoft.Bing.Speech;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Microsoft.Cognitive.LUIS;
    using Microsoft.ProjectOxford.Face;
    using Microsoft.ProjectOxford.Vision;

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));
            if (activity.Type == ActivityTypes.Message)
            {
                if (activity.Attachments != null && activity.Attachments.Count == 1 && activity.Attachments[0].ContentType.Contains("image"))
                {
                    var data = await activity.GetStateClient().BotState.GetConversationDataAsync(activity.ChannelId, activity.Conversation.Id);
                    data.SetProperty("image", activity.Attachments[0].ContentUrl);
                    await activity.GetStateClient().BotState.SetConversationDataAsync(activity.ChannelId, activity.Conversation.Id, data);
                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply("Now I will use this image."));
                }
                else
                {
                    var data = await activity.GetStateClient().BotState.GetConversationDataAsync(activity.ChannelId, activity.Conversation.Id);
                    var image = data.GetProperty<string>("image");
                    if (image == null)
                    {
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I can't work without an image. Please send me one."));
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    if (activity.Text == "emotion")
                    {
                        var result = await CognitiveServiceHelper.RecognizeEmotionsAsync(image);
                        switch (result.Length)
                        {
                            case 0:
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I can't recognize a face. Try a new image."));
                                break;
                            case 1:
                                var emotion1 = result[0];
                                var builder1 = new StringBuilder();
                                builder1.AppendLine($"I think you emotions are:  \n");
                                builder1.AppendLine($"{nameof(emotion1.Scores.Anger)} ({emotion1.Scores.Anger:0.00})  \n");
                                builder1.AppendLine($"{nameof(emotion1.Scores.Contempt)} ({emotion1.Scores.Contempt:0.00})  \n");
                                builder1.AppendLine($"{nameof(emotion1.Scores.Disgust)} ({emotion1.Scores.Disgust:0.00})  \n");
                                builder1.AppendLine($"{nameof(emotion1.Scores.Fear)} ({emotion1.Scores.Fear:0.00})  \n");
                                builder1.AppendLine($"{nameof(emotion1.Scores.Happiness)} ({emotion1.Scores.Happiness:0.00})  \n");
                                builder1.AppendLine($"{nameof(emotion1.Scores.Neutral)} ({emotion1.Scores.Neutral:0.00})  \n");
                                builder1.AppendLine($"{nameof(emotion1.Scores.Sadness)} ({emotion1.Scores.Sadness:0.00})  \n");
                                builder1.AppendLine($"{nameof(emotion1.Scores.Surprise)} ({emotion1.Scores.Surprise:0.00})  \n");
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply(builder1.ToString()));
                                break;
                            default:
                                var builder2 = new StringBuilder();
                                builder2.AppendLine("I think you have the following emotions (From left to right):  ");
                                var i = 0;
                                foreach (var emotion2 in result.OrderBy(f => f.FaceRectangle.Left))
                                {
                                    builder2.AppendLine($"`Face {++i}`");
                                    builder2.AppendLine($"{nameof(emotion2.Scores.Anger)} ({emotion2.Scores.Anger:0.00})  \n");
                                    builder2.AppendLine($"{nameof(emotion2.Scores.Contempt)} ({emotion2.Scores.Contempt:0.00})  \n");
                                    builder2.AppendLine($"{nameof(emotion2.Scores.Disgust)} ({emotion2.Scores.Disgust:0.00})  \n");
                                    builder2.AppendLine($"{nameof(emotion2.Scores.Fear)} ({emotion2.Scores.Fear:0.00})  \n");
                                    builder2.AppendLine($"{nameof(emotion2.Scores.Happiness)} ({emotion2.Scores.Happiness:0.00})  \n");
                                    builder2.AppendLine($"{nameof(emotion2.Scores.Neutral)} ({emotion2.Scores.Neutral:0.00})  \n");
                                    builder2.AppendLine($"{nameof(emotion2.Scores.Sadness)} ({emotion2.Scores.Sadness:0.00})  \n");
                                    builder2.AppendLine($"{nameof(emotion2.Scores.Surprise)} ({emotion2.Scores.Surprise:0.00})  \n");
                                }
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply(builder2.ToString()));
                                break;
                        }
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    var typing = activity.CreateReply(string.Empty);
                    typing.Type = ActivityTypes.Typing;
                    await connectorClient.Conversations.ReplyToActivityAsync(typing);

                    try
                    {
                        var client = new LuisClient(ConfigurationManager.AppSettings["LuisId"], ConfigurationManager.AppSettings["LuisKey"]);
                        var predict = await client.Predict(activity.Text);

                        if (predict.TopScoringIntent.Name == "Face" && predict.Entities["Attribute"][0].Value.ToLower() == "gender")
                        {
                            var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true, FaceAttributeType.Gender);
                            switch (result.Length)
                            {
                                case 0:
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I can't recognize a face. Try a new image."));
                                    break;
                                case 1:
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think the person is a {result[0].FaceAttributes.Gender}."));
                                    break;
                                default:
                                    var builder = new StringBuilder();
                                    builder.AppendLine("I think you have the following gender (From left to right):  ");
                                    foreach (var face in result.OrderBy(f => f.FaceRectangle.Left))
                                    {
                                        builder.AppendLine(face.FaceAttributes.Gender + "  ");
                                    }
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply(builder.ToString()));
                                    break;
                            }
                        }
                        else if (predict.TopScoringIntent.Name == "Face" && predict.Entities["Attribute"][0].Value.ToLower() == "age")
                        {
                            var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true, FaceAttributeType.Age);
                            switch (result.Length)
                            {
                                case 0:
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I can't recognize a face. Try a new image."));
                                    break;
                                case 1:
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think the person is {result[0].FaceAttributes.Age} years old."));
                                    break;
                                default:
                                    var builder = new StringBuilder();
                                    builder.AppendLine("I think you have the following ages (From left to right):  ");
                                    foreach (var face in result.OrderBy(f => f.FaceRectangle.Left))
                                    {
                                        builder.AppendLine(face.FaceAttributes.Age + "  ");
                                    }
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply(builder.ToString()));
                                    break;
                            }
                        }
                        else if (predict.TopScoringIntent.Name == "Face" && predict.Entities["Attribute"][0].Value.ToLower() == "how many")
                        {
                            var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true);
                            switch (result.Length)
                            {
                                case 0:
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think there are no persons."));
                                    break;
                                case 1:
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think there is one person."));
                                    break;
                                default:
                                    connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think there are {result.Length} persons."));
                                    break;
                            }
                        }
                        else if (predict.TopScoringIntent.Name == "Vision" && (predict.Entities["Type"][0].Value.ToLower() == "content" || predict.Entities["Type"][0].Value.ToLower() == "on"))
                        {
                            var result = await CognitiveServiceHelper.AnalyzeImageAsync(image, VisualFeature.Tags, VisualFeature.Description);
                            var builder = new StringBuilder();
                            builder.AppendLine("I found the following content:  ");
                            builder.Append(result.Description.Captions.OrderByDescending(c => c.Confidence).FirstOrDefault()?.Text ?? "No description");
                            builder.AppendLine("  ");
                            builder.AppendLine(result.Tags.Aggregate("", (res, t) => res + t.Name + " (" + t.Confidence.ToString("0.00") + ")  \n"));
                            connectorClient.Conversations.ReplyToActivity(activity.CreateReply(builder.ToString()));
                        }
                        else if (predict.TopScoringIntent.Name == "Vision" && predict.Entities["Type"][0].Value.ToLower() == "text")
                        {
                            var result = await CognitiveServiceHelper.RecognizeImageTextAsync(image);
                            var builder = new StringBuilder();
                            builder.AppendLine("I think the text on the image is:  ");
                            foreach (var region in result.Regions)
                            {
                                builder.AppendLine(
                                    region.Lines.Aggregate(string.Empty, (res, line) => res + line.Words.Aggregate(string.Empty, (sentance, word) => sentance + " " + word.Text).Substring(1) + "  \n")
                                    + "  ");
                            }
                            connectorClient.Conversations.ReplyToActivity(activity.CreateReply(builder.ToString()));
                        }
                        else
                        {
                            connectorClient.Conversations.ReplyToActivity(
                                activity.CreateReply("Sorry, but I didn't understand you. Try something like `What's the age of the person?` or `What's the text on the image?`"));
                        }
                    }
                    catch (Exception ex)
                    {
                        connectorClient.Conversations.ReplyToActivity(
                            activity.CreateReply("Sry, but I made a misstake and didn't understand you. Try something like `What's the age of the person?` or `What's the text on the image?`"));
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private static Activity HandleSystemMessage(IActivity message)
        {
            switch (message.Type)
            {
                case ActivityTypes.DeleteUserData:
                    // Implement user deletion here
                    // If we handle user deletion, return a real message
                    break;
                case ActivityTypes.ConversationUpdate:
                    // Handle conversation state changes, like members being added and removed
                    // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                    // Not available in all channels
                    break;
                case ActivityTypes.ContactRelationUpdate:
                    // Handle add/remove from contact lists
                    // Activity.From + Activity.Action represent what happened
                    break;
                case ActivityTypes.Typing:
                    // Handle knowing tha the user is typing
                    break;
                case ActivityTypes.Ping:
                    break;
            }

            return null;
        }
    }
}