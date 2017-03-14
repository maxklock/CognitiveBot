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
                        connectorClient.Conversations.ReplyToActivity(activity.CreateReply(MessageCreator.GetEmotionsText(result)));
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
                            var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true, FaceAttributeType.Age, FaceAttributeType.Gender);
                            await connectorClient.Conversations.ReplyToActivityAsync(activity.CreateReply(MessageCreator.GetFacesText(result)));
                        }
                        else if (predict.TopScoringIntent.Name == "Face" && predict.Entities["Attribute"][0].Value.ToLower() == "age")
                        {
                            var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true, FaceAttributeType.Age, FaceAttributeType.Gender);
                            await connectorClient.Conversations.ReplyToActivityAsync(activity.CreateReply(MessageCreator.GetFacesText(result)));
                        }
                        else if (predict.TopScoringIntent.Name == "Face" && predict.Entities["Attribute"][0].Value.ToLower() == "how many")
                        {
                            var result = await CognitiveServiceHelper.DetectFacesAsync(image, true, true);
                            await connectorClient.Conversations.ReplyToActivityAsync(activity.CreateReply(MessageCreator.GetFacesCountText(result)));
                        }
                        else if (predict.TopScoringIntent.Name == "Vision" && (predict.Entities["Type"][0].Value.ToLower() == "content" || predict.Entities["Type"][0].Value.ToLower() == "on"))
                        {
                            var result = await CognitiveServiceHelper.AnalyzeImageAsync(image);
                            await connectorClient.Conversations.ReplyToActivityAsync(activity.CreateReply(MessageCreator.GetImageDescription(result)));
                        }
                        else if (predict.TopScoringIntent.Name == "Vision" && predict.Entities["Type"][0].Value.ToLower() == "text")
                        {
                            var result = await CognitiveServiceHelper.RecognizeImageTextAsync(image);
                            await connectorClient.Conversations.ReplyToActivityAsync(activity.CreateReply(MessageCreator.GetImageText(result)));
                        }
                        else
                        {
                            await connectorClient.Conversations.ReplyToActivityAsync(
                                activity.CreateReply("Sorry, but I didn't understand you. Try something like `What's the age of the person?` or `What's the text on the image?`"));
                        }
                    }
                    catch (Exception)
                    {
                        await connectorClient.Conversations.ReplyToActivityAsync(
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