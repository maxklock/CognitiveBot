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
                if (activity.Text == "age")
                {
                    var data = await activity.GetStateClient().BotState.GetConversationDataAsync(activity.ChannelId, activity.Conversation.Id);
                    var image = data.GetProperty<string>("image");
                    try
                    {
                        var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true, FaceAttributeType.Age);
                        switch (result.Length)
                        {
                            case 0:
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I can't recognize a face. Try a new image."));
                                break;
                            case 1:
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think you're {result[0].FaceAttributes.Age} years old."));
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
                    catch (Exception ex)
                    {
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"[{ex.GetType().Name}] {ex.Message}"));
                    }
                }
                if (activity.Text == "gender")
                {
                    var data = await activity.GetStateClient().BotState.GetConversationDataAsync(activity.ChannelId, activity.Conversation.Id);
                    var image = data.GetProperty<string>("image");
                    try
                    {
                        var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true, FaceAttributeType.Gender);
                        switch (result.Length)
                        {
                            case 0:
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I can't recognize a face. Try a new image."));
                                break;
                            case 1:
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think you're a {result[0].FaceAttributes.Gender}."));
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
                    catch (Exception ex)
                    {
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"[{ex.GetType().Name}] {ex.Message}"));
                    }
                }
                if (activity.Text == "count")
                {
                    var data = await activity.GetStateClient().BotState.GetConversationDataAsync(activity.ChannelId, activity.Conversation.Id);
                    var image = data.GetProperty<string>("image");
                    try
                    {
                        var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true);
                        switch (result.Length)
                        {
                            case 0:
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think there are no faces."));
                                break;
                            case 1:
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think there is one face."));
                                break;
                            default:
                                connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"I think there are {result.Length} faces."));
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"[{ex.GetType().Name}] {ex.Message}"));
                    }
                }
                if (activity.Text == "content")
                {

                    var data = await activity.GetStateClient().BotState.GetConversationDataAsync(activity.ChannelId, activity.Conversation.Id);
                    var image = data.GetProperty<string>("image");
                    try
                    {
                        var result = await CognitiveServiceHelper.AnalyzeImageAsync(image, VisualFeature.Tags, VisualFeature.Description);
                        var builder = new StringBuilder();
                        builder.AppendLine("I found the following content:  ");
                        builder.Append(result.Description.Captions.OrderByDescending(c => c.Confidence).FirstOrDefault()?.Text ?? "No description");
                        builder.AppendLine("  ");
                        builder.AppendLine(result.Tags.Aggregate("", (res, t) => res + t.Name + " (" + t.Confidence.ToString("0.00") + ")  \n"));
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply(builder.ToString()));
                    }
                    catch (Exception ex)
                    {
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"[{ex.GetType().Name}]: {ex.Message}"));
                    }
                }
                if (activity.Text == "text")
                {
                    var data = await activity.GetStateClient().BotState.GetConversationDataAsync(activity.ChannelId, activity.Conversation.Id);
                    var image = data.GetProperty<string>("image");
                    try
                    {
                        var result = await CognitiveServiceHelper.RecognizeImageTextAsync(image);
                        var builder = new StringBuilder();
                        builder.AppendLine("I think the text on the image is:  ");
                        foreach (var region in result.Regions)
                        {
                            builder.AppendLine(region.Lines.Aggregate(string.Empty, (res, line) => res + line.Words.Aggregate(string.Empty, (sentance, word) => sentance + " " + word.Text).Substring(1) + "  \n") + "  ");
                        }
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply(builder.ToString()));
                    }
                    catch (Exception ex)
                    {
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply($"[{ex.GetType().Name}]: {ex.Message}"));
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}