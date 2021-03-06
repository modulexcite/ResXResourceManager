namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    using tomenglertde.ResXManager.Translators.BingServiceReference;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    [Export(typeof(ITranslator))]
    public class BingTranslator : TranslatorBase
    {
        private static readonly Uri _uri = new Uri("https://datamarket.azure.com/dataset/bing/microsofttranslator");

        public BingTranslator()
            : base("Bing", "Bing", _uri, GetCredentials())
        {
        }

        public override void Translate(Session session)
        {
            try
            {
                var clientId = ClientId;
                var clientSecret = ClientSecret;

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    session.AddMessage("Bing Translator requires client id and secret.");
                    return;
                }

                var token = AdmAuthentication.GetAuthToken(WebProxy, clientId, clientSecret);

                var binding = new BasicHttpBinding();
                var endpointAddress = new EndpointAddress("http://api.microsofttranslator.com/V2/soap.svc");

                using (var client = new LanguageServiceClient(binding, endpointAddress))
                {
                    using (new OperationContextScope(client.InnerChannel))
                    {
                        var httpRequestProperty = new HttpRequestMessageProperty();
                        httpRequestProperty.Headers.Add("Authorization", token);
                        var operationContext = OperationContext.Current;
                        Contract.Assume(operationContext != null); // because we are inside OperationContextScope
                        operationContext.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                        foreach (var languageGroup in session.Items.GroupBy(item => item.TargetCulture))
                        {
                            Contract.Assume(languageGroup != null);

                            var cultureKey = languageGroup.Key;
                            Contract.Assume(cultureKey != null);

                            var targetLanguage = cultureKey.Culture ?? session.NeutralResourcesLanguage;

                            using (var itemsEnumerator = languageGroup.GetEnumerator())
                            {
                                while (true)
                                {
                                    var sourceItems = itemsEnumerator.Take(10);
                                    var sourceStrings = sourceItems.Select(item => item.Source).ToArray();

                                    if (!sourceStrings.Any())
                                        break;

                                    var response = client.GetTranslationsArray("", sourceStrings, session.SourceLanguage.IetfLanguageTag, targetLanguage.IetfLanguageTag, 5,
                                        new TranslateOptions()
                                        {
                                            ContentType = "text/plain",
                                            IncludeMultipleMTAlternatives = true
                                        });

                                    session.Dispatcher.BeginInvoke(() => ReturnResults(sourceItems, response));

                                    if (session.IsCanceled)
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                session.AddMessage("Bing translator reported a problem: " + ex);
            }
        }

        [DataMember]
        [ContractVerification(false)]
        public string ClientSecret
        {
            get
            {
                return SaveCredentials ? Credentials[1].Value : null;
            }
            set
            {
                Credentials[1].Value = value;
            }
        }

        [DataMember]
        [ContractVerification(false)]
        public string ClientId
        {
            get
            {
                return SaveCredentials ? Credentials[0].Value : null;
            }
            set
            {
                Credentials[0].Value = value;
            }
        }

        private void ReturnResults(IEnumerable<ITranslationItem> items, IEnumerable<GetTranslationsResponse> responses)
        {
            Contract.Requires(items != null);
            Contract.Requires(responses != null);

            foreach (var tuple in Enumerate.AsTuples(items, responses))
            {
                Contract.Assume(tuple != null);

                var response = tuple.Item2;
                Contract.Assume(response != null);

                var translationItem = tuple.Item1;
                Contract.Assume(translationItem != null);

                var translations = response.Translations;
                Contract.Assume(translations != null);

                foreach (var match in translations)
                {
                    Contract.Assume(match != null);
                    translationItem.Results.Add(new TranslationMatch(this, match.TranslatedText, match.Rating / 5.0));
                }
            }
        }

        private static IList<ICredentialItem> GetCredentials()
        {
            Contract.Ensures(Contract.Result<IList<ICredentialItem>>() != null);

            return new ICredentialItem[]
            {
                new CredentialItem("ClientId", "Client ID"),
                new CredentialItem("ClientSecret", "Client Secret")
            };
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
        }
    }
}