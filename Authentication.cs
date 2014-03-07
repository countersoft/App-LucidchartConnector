using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using DotNetOpenAuth.OpenId.Extensions.OAuth;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.Messaging;
using System.Net;
using System.IO;
using System.Xml;
using Countersoft.Foundation.Commons.Extensions;
using System.Web;

namespace LucidChart
{
    /// <summary>
    /// A token manager that only retains tokens in memory. 
    /// Meant for SHORT TERM USE TOKENS ONLY.
    /// </summary>
    /// <remarks>
    /// A likely application of this class is for "Sign In With Twitter",
    /// where the user only signs in without providing any authorization to access
    /// Twitter APIs except to authenticate, since that access token is only useful once.
    /// </remarks>
    internal class InMemoryTokenManager : IConsumerTokenManager, IOpenIdOAuthTokenManager
    {
        private Dictionary<string, string> tokensAndSecrets = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryTokenManager"/> class.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="consumerSecret">The consumer secret.</param>
        public InMemoryTokenManager(string consumerKey, string consumerSecret)
        {
            /*if (String.IsNullOrEmpty(consumerKey))
            {
                throw new ArgumentNullException("consumerKey");
            }*/

            this.ConsumerKey = consumerKey;
            
            this.ConsumerSecret = consumerSecret;
        }

        /// <summary>
        /// Gets the consumer key.
        /// </summary>
        /// <value>The consumer key.</value>
        public string ConsumerKey { get; private set; }

        /// <summary>
        /// Gets the consumer secret.
        /// </summary>
        /// <value>The consumer secret.</value>
        public string ConsumerSecret { get; private set; }

        #region ITokenManager Members

        /// <summary>
        /// Gets the Token Secret given a request or access token.
        /// </summary>
        /// <param name="token">The request or access token.</param>
        /// <returns>
        /// The secret associated with the given token.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if the secret cannot be found for the given token.</exception>
        public string GetTokenSecret(string token)
        {
            return this.tokensAndSecrets[token];
        }

        public void SetToken(string access, string secret)
        {
            this.tokensAndSecrets.Add(access, secret);
        }

        /// <summary>
        /// Stores a newly generated unauthorized request token, secret, and optional
        /// application-specific parameters for later recall.
        /// </summary>
        /// <param name="request">The request message that resulted in the generation of a new unauthorized request token.</param>
        /// <param name="response">The response message that includes the unauthorized request token.</param>
        /// <exception cref="ArgumentException">Thrown if the consumer key is not registered, or a required parameter was not found in the parameters collection.</exception>
        /// <remarks>
        /// Request tokens stored by this method SHOULD NOT associate any user account with this token.
        /// It usually opens up security holes in your application to do so.  Instead, you associate a user
        /// account with access tokens (not request tokens) in the <see cref="ExpireRequestTokenAndStoreNewAccessToken"/>
        /// method.
        /// </remarks>
        public void StoreNewRequestToken(UnauthorizedTokenRequest request, ITokenSecretContainingMessage response)
        {
            this.tokensAndSecrets[response.Token] = response.TokenSecret;
        }

        /// <summary>
        /// Deletes a request token and its associated secret and stores a new access token and secret.
        /// </summary>
        /// <param name="consumerKey">The Consumer that is exchanging its request token for an access token.</param>
        /// <param name="requestToken">The Consumer's request token that should be deleted/expired.</param>
        /// <param name="accessToken">The new access token that is being issued to the Consumer.</param>
        /// <param name="accessTokenSecret">The secret associated with the newly issued access token.</param>
        /// <remarks>
        /// 	<para>
        /// Any scope of granted privileges associated with the request token from the
        /// original call to <see cref="StoreNewRequestToken"/> should be carried over
        /// to the new Access Token.
        /// </para>
        /// 	<para>
        /// To associate a user account with the new access token,
        /// <see cref="System.Web.HttpContext.User">HttpContext.Current.User</see> may be
        /// useful in an ASP.NET web application within the implementation of this method.
        /// Alternatively you may store the access token here without associating with a user account,
        /// and wait until <see cref="WebConsumer.ProcessUserAuthorization()"/> or
        /// <see cref="DesktopConsumer.ProcessUserAuthorization(string, string)"/> return the access
        /// token to associate the access token with a user account at that point.
        /// </para>
        /// </remarks>
        public void ExpireRequestTokenAndStoreNewAccessToken(string consumerKey, string requestToken, string accessToken, string accessTokenSecret)
        {
            this.tokensAndSecrets.Remove(requestToken);
            this.tokensAndSecrets[accessToken] = accessTokenSecret;
        }

        /// <summary>
        /// Classifies a token as a request token or an access token.
        /// </summary>
        /// <param name="token">The token to classify.</param>
        /// <returns>Request or Access token, or invalid if the token is not recognized.</returns>
        public TokenType GetTokenType(string token)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOpenIdOAuthTokenManager Members

        /// <summary>
        /// Stores a new request token obtained over an OpenID request.
        /// </summary>
        /// <param name="consumerKey">The consumer key.</param>
        /// <param name="authorization">The authorization message carrying the request token and authorized access scope.</param>
        /// <remarks>
        /// 	<para>The token secret is the empty string.</para>
        /// 	<para>Tokens stored by this method should be short-lived to mitigate
        /// possible security threats.  Their lifetime should be sufficient for the
        /// relying party to receive the positive authentication assertion and immediately
        /// send a follow-up request for the access token.</para>
        /// </remarks>
        public void StoreOpenIdAuthorizedRequestToken(string consumerKey, AuthorizationApprovedResponse authorization)
        {
            this.tokensAndSecrets[authorization.RequestToken] = String.Empty;
        }

        #endregion
    }

    public class LucidChartsConsumer
    {
        private static string lucidBaseChartUrl = "https://www.lucidchart.com/";
        
        private string _geminiUrl;
        
        private LucidChartUser _user;

        public LucidChartsConsumer(string geminiUrl, LucidChartUser user)
        {
            _geminiUrl = geminiUrl;
           
            _user = user;
        }

        public static readonly ServiceProviderDescription ServiceDescription = new ServiceProviderDescription
        {
            RequestTokenEndpoint = new MessageReceivingEndpoint(lucidBaseChartUrl + "oauth/requestToken", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            
            UserAuthorizationEndpoint = new MessageReceivingEndpoint(lucidBaseChartUrl + "oauth/authorize", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            
            AccessTokenEndpoint = new MessageReceivingEndpoint(lucidBaseChartUrl + "oauth/accessToken", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
            
            TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() },
            
            ProtocolVersion = ProtocolVersion.V10a
        };

        public LucidChartUser Verify(string oauth_token, string oauth_verifier)
        {
            ServiceProviderDescription desc = new ServiceProviderDescription
            {
                RequestTokenEndpoint = new MessageReceivingEndpoint(lucidBaseChartUrl + "oauth/requestToken", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                UserAuthorizationEndpoint = new MessageReceivingEndpoint(lucidBaseChartUrl + "oauth/authorize", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                AccessTokenEndpoint = new MessageReceivingEndpoint(lucidBaseChartUrl + "oauth/accessToken?oauth_verifier=" + Uri.EscapeDataString(oauth_verifier), HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest),
                TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() },
                ProtocolVersion = ProtocolVersion.V10a
            };
            
            var lucid = new WebConsumer(desc, token);
            
            var accessTokenResponse = lucid.ProcessUserAuthorization();
            
            if (accessTokenResponse != null)
            {
                string accessToken = accessTokenResponse.AccessToken;
                string secret = lucid.TokenManager.GetTokenSecret(accessToken);

                return new LucidChartUser() { Token = accessToken, Secret = secret };
                /*HttpWebRequest req = lucid.PrepareAuthorizedRequest(new MessageReceivingEndpoint(lucidBaseChartUrl + "documents/describe/49db-4974-4f156c2b-a802-796b0a48117a", HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest), accessToken);
                req.ContentType = "application/xml";
                return new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();*/
            }

            return null;
        }

        internal static string consumerKey = string.Empty;//"21b16149b7378428d3a96ef5c4777714";
        
        internal static string consumerSecret = string.Empty;//  "121883a7827ae3844ca7ffb43e79e428";
        // Gemini
        //static InMemoryTokenManager token = new InMemoryTokenManager("890e6c36a8f93100532ddd262d172726", "75e16f21d1bbdd6f978ea03426d3df70");
        internal static InMemoryTokenManager token = new InMemoryTokenManager(consumerKey, consumerSecret);
        
        internal static InMemoryTokenManager GetTokenManager(LucidChartUser user)
        {
            /*InMemoryTokenManager t = new InMemoryTokenManager("2f5f3b5ff6f0cf6ed2e36541d9fa6939", "0c709b00d02237168e5bd840a817f1b7");
            t.SetToken("533ccd4b7bb82abd7cbfc6909892604d58e37f2d", "eb803f21bc177b3e40c02294faeeebd5a0e68d5b");*/

            //InMemoryTokenManager t = new InMemoryTokenManager("890e6c36a8f93100532ddd262d172726", "75e16f21d1bbdd6f978ea03426d3df70");
            InMemoryTokenManager t = new InMemoryTokenManager(consumerKey, consumerSecret);
            t.SetToken(user.Token, user.Secret);
            
            return t;
        }

        public void Authenticate(string key, string secret, string callback)
        {            
            var lucid = new WebConsumer(ServiceDescription, token);
            
            lucid.Channel.Send(lucid.PrepareRequestUserAuthorization(new Uri(string.Format("{0}apps/lucidchart/verify?callback={1}", _geminiUrl, callback)), null, null));
        }

        public HttpWebRequest CreateDocument(string projectCode, int projectId, int issueId)
        {
            InMemoryTokenManager t = GetTokenManager(_user);
            
            var lucid = new WebConsumer(ServiceDescription, t);
            
            return lucid.PrepareAuthorizedRequest(new MessageReceivingEndpoint(string.Format(lucidBaseChartUrl + "api/newDoc?callback={3}apps/lucidchart/editdocument/{0}/{1}/{2}", projectCode, projectId, issueId, _geminiUrl), HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest), _user.Token);
        }

        public HttpWebRequest EditDocument(string documentId, string projectCode, int projectId, int issueId)
        {
            InMemoryTokenManager t = GetTokenManager(_user);
            
            var lucid = new WebConsumer(ServiceDescription, t);
            
            return lucid.PrepareAuthorizedRequest(new MessageReceivingEndpoint(string.Format(lucidBaseChartUrl + "documents/edit/{0}?callback={4}apps/lucidchart/editdocument/{1}/{2}/{3}", documentId, projectCode, projectId, issueId, _geminiUrl), HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest), _user.Token);
        }

        public LucidDocument GetDocumentDescription(string documentId)
        {
            InMemoryTokenManager t = GetTokenManager(_user);
            
            var lucid = new WebConsumer(ServiceDescription, t);
            
            string url = lucidBaseChartUrl + "documents/describe/" + documentId;
            
            HttpWebRequest req = lucid.PrepareAuthorizedRequest(new MessageReceivingEndpoint(url, HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest), _user.Token);
            
            req.ContentType = "application/xml";
            
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            
            string xml = new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            
            XmlDocument doc = new XmlDocument();
            
            doc.LoadXml(xml);

            LucidDocument document = new LucidDocument();
            
            XmlElement elm = doc.DocumentElement;
            
            foreach (XmlElement n in elm.ChildNodes[0])
            {
                switch (n.Name.ToLower())
                {
                    case "documentid":
                        document.Id = n.InnerText;
                        break;
                    case "title":
                        document.Name = n.InnerText;
                        break;
                    case "editurl":
                        document.EditUrl = n.InnerText;
                        break;
                    case "viewurl":
                        document.ViewUrl = n.InnerText;
                        break;
                    case "version":
                        document.Version = n.InnerText;
                        break;
                    case "pagecount":
                        document.PageCount = n.InnerText.ToInt();
                        break;
                }
            }

            return document;
        }

        public class LucidDocument
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string EditUrl { get; set; }
            public string ViewUrl { get; set; }
            public string Version { get; set; }
            public int PageCount { get; set; }
        }
                
        public byte[] GetDocumentImage(string documentId, int page, int width, bool square)
        {
            InMemoryTokenManager t = GetTokenManager(_user);
           
            var lucid = new WebConsumer(ServiceDescription, t);

            HttpWebRequest req = lucid.PrepareAuthorizedRequest(new MessageReceivingEndpoint(string.Format("{4}documents/image/{0}/{1}/{2}/{3}", documentId, page, width, square ? 1 : 0, lucidBaseChartUrl), HttpDeliveryMethods.GetRequest | HttpDeliveryMethods.AuthorizationHeaderRequest), _user.Token);
            
            byte[] buffer = new byte[1024 * 32];
            
            using (Stream s = req.GetResponse().GetResponseStream())
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }
        }
    }
}
