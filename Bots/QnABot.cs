// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.AI.QnA;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Graph;
using File = System.IO.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using AdaptiveCards;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class WelcomeUserState
    {
        // Gets or sets whether the user has been welcomed in the conversation.
        public bool DidBotWelcomeUser { get; set; } = false;
    }

    public class QnABot<T> : ActivityHandler where T : Microsoft.Bot.Builder.Dialogs.Dialog
    {
        protected readonly BotState ConversationState;
        protected readonly Microsoft.Bot.Builder.Dialogs.Dialog Dialog;
        protected readonly BotState UserState;
        private const string WelcomeMessage = "Hey this is the cyclotron helpdesk bot.\nHow may I help you today?";
        private readonly IBotServices _services;
        //private readonly IConfiguration _config;


        static string[] scopes =
        {
            "User.Read", 
            "Mail.Send"  
        };

        public QnABot(ConversationState conversationState, UserState userState, T dialog, IBotServices services, IConfiguration configuration)
        {
            ConversationState = conversationState;
            UserState = userState;
            Dialog = dialog;
            _services = services;
            //_config = configuration;

            
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeUserStateAccessor = UserState.CreateProperty<WelcomeUserState>(nameof(WelcomeUserState));
            var didBotWelcomeUser = await welcomeUserStateAccessor.GetAsync(turnContext, () => new WelcomeUserState());
            var options = new QnAMakerOptions { Top = 1 };

            var text = "";
            if (turnContext.Activity.Text == null)
            {
                text = "null";
            }
            else {
                text = turnContext.Activity.Text.ToLowerInvariant();
            }          
            switch (text)
            {
                case "intro":
                case "help":
                    await SendIntroCardAsync(turnContext, cancellationToken);
                    break;
                case "file an incident":                   
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(CreateAdaptiveCardAttachment()));                   
                    break;
                case "null":
                    if (turnContext.Activity.Value != null)
                    {
                        var ticket = JsonConvert.SerializeObject(turnContext.Activity.Value);
                        var ticket_num = 123456789;
                        
                        JObject jObject = JObject.Parse(ticket);

                        string t_date = (string)jObject.SelectToken("dateinput");
                        string t_name = (string)jObject.SelectToken("nameinput");
                        string t_issue = (string)jObject.SelectToken("issueinput");
                        string t_urgency = "";
                        if ((int)jObject.SelectToken("urgencyinput") == 1)
                        {
                            t_urgency = "Normal";
                        }
                        else {
                            t_urgency = "Urgent";
                        }

                        try {                                                    
                            string resourceId = "https://graph.microsoft.com/";
                            string authString = "https://login.microsoftonline.com/" + "8ad25bda-a157-4eda-a940-b1069931e221";
                            //secret - BO.7~0daznFl84x-3B0_79X9OMMpwH.UYM
                            //tenant id- 8ad25bda-a157-4eda-a940-b1069931e221 
                            //app id - e6679829-27ac-4d4e-ae24-f752ab77755a 

                            var authenticationContext = new AuthenticationContext(authString, false);
                            IdentityModel.Clients.ActiveDirectory.ClientCredential clientCred = new IdentityModel.Clients.ActiveDirectory.ClientCredential("e6679829-27ac-4d4e-ae24-f752ab77755a", "BO.7~0daznFl84x-3B0_79X9OMMpwH.UYM");
                            IdentityModel.Clients.ActiveDirectory.AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync(resourceId, clientCred);

                            var acc = "eyJ0eXAiOiJKV1QiLCJub25jZSI6Ijg1SEkzRE5Ob3ZERDFRb2dhcDdQaEczTk1qTVJhbUlXZEoxWTNsWjltZEEiLCJhbGciOiJSUzI1NiIsIng1dCI6IlNzWnNCTmhaY0YzUTlTNHRycFFCVEJ5TlJSSSIsImtpZCI6IlNzWnNCTmhaY0YzUTlTNHRycFFCVEJ5TlJSSSJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC82NTgzNjM2Yi1kMTU2LTQ5MmUtODZjZS1iOGZjY2I3OTBkZjEvIiwiaWF0IjoxNTkxOTc4NzAxLCJuYmYiOjE1OTE5Nzg3MDEsImV4cCI6MTU5MTk4MjYwMSwiYWNjdCI6MCwiYWNyIjoiMSIsImFpbyI6IjQyZGdZTkR5NkM2YVdkWDMrOGJFNjJ0U0h3bTJSYmIyMWt6VDJ0UjU4ck1NWXlxVFpBSUEiLCJhbXIiOlsicHdkIl0sImFwcF9kaXNwbGF5bmFtZSI6IkdyYXBoIGV4cGxvcmVyIiwiYXBwaWQiOiJkZThiYzhiNS1kOWY5LTQ4YjEtYThhZC1iNzQ4ZGE3MjUwNjQiLCJhcHBpZGFjciI6IjAiLCJmYW1pbHlfbmFtZSI6IkthcmRhIiwiZ2l2ZW5fbmFtZSI6IlVkZGVzaCIsImlwYWRkciI6IjY4LjE4MC44Ni4yMyIsIm5hbWUiOiJVZGRlc2ggS2FyZGEiLCJvaWQiOiI2NWI5NmZkNS1hNTcxLTQ3MTktODlkMC0wMmQ1ZDA3M2Y5MTIiLCJwbGF0ZiI6IjMiLCJwdWlkIjoiMTAwMzIwMDBCRTYyM0FCMSIsInNjcCI6IkFjY2Vzc1Jldmlldy5SZWFkLkFsbCBBY2Nlc3NSZXZpZXcuUmVhZFdyaXRlLkFsbCBDYWxlbmRhcnMuUmVhZFdyaXRlIENvbnRhY3RzLlJlYWRXcml0ZSBEZXZpY2VNYW5hZ2VtZW50QXBwcy5SZWFkLkFsbCBEZXZpY2VNYW5hZ2VtZW50QXBwcy5SZWFkV3JpdGUuQWxsIERldmljZU1hbmFnZW1lbnRDb25maWd1cmF0aW9uLlJlYWQuQWxsIERldmljZU1hbmFnZW1lbnRDb25maWd1cmF0aW9uLlJlYWRXcml0ZS5BbGwgRGV2aWNlTWFuYWdlbWVudE1hbmFnZWREZXZpY2VzLlByaXZpbGVnZWRPcGVyYXRpb25zLkFsbCBEZXZpY2VNYW5hZ2VtZW50TWFuYWdlZERldmljZXMuUmVhZC5BbGwgRGV2aWNlTWFuYWdlbWVudE1hbmFnZWREZXZpY2VzLlJlYWRXcml0ZS5BbGwgRGV2aWNlTWFuYWdlbWVudFJCQUMuUmVhZC5BbGwgRGV2aWNlTWFuYWdlbWVudFJCQUMuUmVhZFdyaXRlLkFsbCBEZXZpY2VNYW5hZ2VtZW50U2VydmljZUNvbmZpZy5SZWFkLkFsbCBEZXZpY2VNYW5hZ2VtZW50U2VydmljZUNvbmZpZy5SZWFkV3JpdGUuQWxsIERpcmVjdG9yeS5BY2Nlc3NBc1VzZXIuQWxsIERpcmVjdG9yeS5SZWFkLkFsbCBEaXJlY3RvcnkuUmVhZFdyaXRlLkFsbCBGaWxlcy5SZWFkV3JpdGUuQWxsIEdyb3VwLlJlYWRXcml0ZS5BbGwgSWRlbnRpdHlSaXNrRXZlbnQuUmVhZC5BbGwgTWFpbC5SZWFkV3JpdGUgTWFpbC5TZW5kIE1haWxib3hTZXR0aW5ncy5SZWFkV3JpdGUgTm90ZXMuUmVhZFdyaXRlLkFsbCBvcGVuaWQgUGVvcGxlLlJlYWQgcHJvZmlsZSBSZXBvcnRzLlJlYWQuQWxsIFNpdGVzLlJlYWRXcml0ZS5BbGwgVGFza3MuUmVhZFdyaXRlIFVzZXIuUmVhZCBVc2VyLlJlYWRCYXNpYy5BbGwgVXNlci5SZWFkV3JpdGUgVXNlci5SZWFkV3JpdGUuQWxsIGVtYWlsIiwic2lnbmluX3N0YXRlIjpbImttc2kiXSwic3ViIjoiZUFpZUNLaF9TNEhUcXMwUDVWa2FXVURKRE0wY05DSVJWV3QtRVM1dVFZayIsInRlbmFudF9yZWdpb25fc2NvcGUiOiJOQSIsInRpZCI6IjY1ODM2MzZiLWQxNTYtNDkyZS04NmNlLWI4ZmNjYjc5MGRmMSIsInVuaXF1ZV9uYW1lIjoidWRkZXNoQGN5Y2xvdHJvbmdyb3VwLmNvbSIsInVwbiI6InVkZGVzaEBjeWNsb3Ryb25ncm91cC5jb20iLCJ1dGkiOiIwU3IwYTNjc3pVV2Nockg2V0VjN0FBIiwidmVyIjoiMS4wIiwieG1zX3N0Ijp7InN1YiI6IkN4UWtzeV9DMXVVaUtlRGNkMmVzT0pIeHh2b20xMU5UektrOXI4SzJlWlUifSwieG1zX3RjZHQiOjE0MTgwNjc3Njl9.FPDziisnCX9KHEpLTslWYFN6bLYI6GyUjMecZL9ZKdT-mcCPMFsVcsFcl2qQnXcnKxLeKl4LZexLFD-wlk5a7rJCNhrp92k3guxrLmFXF0z398ksu_QcP1lc0na___WQOjOB4wmB1ak-1-6nAvedjSQfSV6RwCGFRCpkYLQIR6BsjN3agE9h88ik9covtAi-QYQNgooqaxF4OmjJWGv-6QvijhtrLMZXsoU2ev3giQ0bwsHmqN4gp3Amc1S7gFoTgCR4qIjncOUxZ_KBdQA22M09U3t-SMAiYKAvHV_EMxA8yPtVKTh5bVd_tf-obuad4hTgRmS2clmWYom1TjIocA";
                            GraphServiceClient graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
                                (requestMessage) => {
                                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acc);
                                    return Task.FromResult(0);
                                }));

                            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                            {
                                Body = new List<AdaptiveElement>()
                                {   
                                    new AdaptiveContainer
                                    {
                                        Items = new List<AdaptiveElement>(){
                                            new AdaptiveColumnSet(){ 
                                                Columns = new List<AdaptiveColumn>(){ 
                                                    new AdaptiveColumn{ 
                                                        Width = AdaptiveColumnWidth.Auto,
                                                        Items = new List<AdaptiveElement>(){
                                                            new AdaptiveTextBlock{
                                                                Text = "Ticket Date " + t_date,
                                                                Size = AdaptiveTextSize.Small,
                                                            },
                                                            new AdaptiveTextBlock{
                                                                Text = "Ticket By " + t_name,
                                                                Size = AdaptiveTextSize.Small
                                                            },
                                                            new AdaptiveTextBlock{
                                                                Text = "Issue : " + t_issue,
                                                                Size = AdaptiveTextSize.Small
                                                            },
                                                            new AdaptiveTextBlock{
                                                                Text = "Assistance urgency : " + t_urgency,
                                                                Size = AdaptiveTextSize.Small
                                                            },
                                                            new AdaptiveTextBlock{
                                                                Text = "Ticket number : " + ticket_num,
                                                                Size = AdaptiveTextSize.Small
                                                            }
                                                        }
                                                    }
                                                }
                                            }                                         
                                        }
                                    }
                                },
                                Actions = new List<AdaptiveAction>() 
                                {
                                    new AdaptiveSubmitAction
                                    {
                                        Title = "Resolved",
                                        Id = "resolvedticket",
                                        Data = new AdaptiveShowCardAction{ 
                                            
                                        }
                                    }
                                }
                            };
                            
                            string cardstr = card.ToJson();
                            JObject cardobj = JObject.Parse(cardstr);
                            var cardType = "application/adaptivecard+json";
                                //cardobj.SelectToken("type");
                            var content = string.Format(File.ReadAllText(@".\Message.html"), cardType, cardobj.ToString());

                            var message = new Message
                            {
                                Subject = "Ticket request",
                                Body = new ItemBody
                                {
                                    ContentType = BodyType.Html,
                                    Content = content
                                },

                                Attachments = new MessageAttachmentsCollectionPage(),

                                ToRecipients = new List<Recipient>()
                                {
                                    new Recipient
                                    {
                                        EmailAddress = new EmailAddress
                                        {
                                            Address = "uddesh@cyclotrongroup.com"
                                        }
                                    }
                                }
                            };
                            await graphClient.Users["uddesh@cyclotrongroup.com"]
                                .SendMail(message, null)
                                .Request()
                                .PostAsync();

                        }
                        catch (MsalException)
                        {
                            await turnContext.SendActivityAsync($"broken");
                        }
                        
                    }
                    else {
                        await turnContext.SendActivityAsync($"No Input");
                    }
                    break;
                case "thank you":
                case "bye":
                    await turnContext.SendActivityAsync($"Bye");
                    break;
                default:
                    var qnaMaker = _services.QnAMakerService;
                    var response = await qnaMaker.GetAnswersAsync(turnContext, options);
                    string[] split_response = (response[0].Answer).Split(';');
                    if (split_response.Length > 1) {
        
                        var card = new HeroCard();
                        card.Title = split_response[0];
                        card.Text = split_response[1];
                        if (split_response.Length == 3) {
                            card.Buttons = new List<CardAction>()
                            {   
                                new CardAction(ActionTypes.OpenUrl, split_response[0], null, split_response[0], split_response[0], split_response[2]),
                            };
                        }
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()));
                    }
                    else {
                        await turnContext.SendActivityAsync(split_response[0]);
                    }
                                  
                    break;
            }
            await UserState.SaveChangesAsync(turnContext);
        }
       
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await SendIntroCardAsync(turnContext, cancellationToken);
                }
            }
        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard();
            card.Title = "HelpDesk Bot";
            card.Text = @"Welcome to the helpdesk bot, how can I help you today?";
            //card.Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") };
            card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.OpenUrl, "Get an overview", null, "Get an overview", "Get an overview", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                new CardAction(ActionTypes.OpenUrl, "Ask a question", null, "Ask a question", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
                new CardAction(ActionTypes.OpenUrl, "Learn how to file an incident", null, "Learn how to file an incident", "Learn how to file an incident", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        public static Bot.Schema.Attachment CreateAdaptiveCardAttachment()
        {
            // combine path for cross platform support
            var paths = new[] { ".", "adaptiveCard.json" };
            var adaptiveCardJson = File.ReadAllText(Path.Combine(paths));

            var adaptiveCardAttachment = new Bot.Schema.Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };

            return adaptiveCardAttachment;
        }       
    }
}