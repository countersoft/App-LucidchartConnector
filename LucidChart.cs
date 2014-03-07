using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Commons.Entity;
using System.Web.Routing;
using System.Web;
using System.Collections.Specialized;
using Countersoft.Gemini.Infrastructure;
using Countersoft.Foundation.Commons.Extensions;
using System.Web.Mvc;
using LucidChart;
using System.Web.UI;
using System.Net;
using System.IO;
using Countersoft.Gemini.Infrastructure.Apps;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini;

namespace LucidChart
{
    public class LucidChartData
    {
        public string DocumentId { get; set; }
        public string DocumentName { get; set; }
        public byte[] ThumnailImage { get; set; }
        public byte[] Image { get; set; }
    }

    public class LucidChartUser
    {
        public string Token { get; set; }
        public string Secret { get; set; }
    }

    public class LucidChartConfig
    {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
    }

    [AppType(AppTypeEnum.Widget),
    AppGuid("17F18EC0-99A9-4851-8587-85CE6DF9C995"),
    AppControlGuid("28FE1D91-7730-4067-8A10-F452FCB9D090"),
    AppAuthor("Countersoft"),
    AppKey("Lucidchart"),
    AppName("LucidChart"),
    AppDescription("LucidChart"),
    AppRequiresConfigScreen(true)]
    [ValidateInput(false)]
    [OutputCache(Duration = 0, NoStore = false, Location = OutputCacheLocation.None)]
    public class LucidChartController : BaseAppController
    {
        private bool NoSettings()
        {
            HttpSessionManager.Set("Apps", SessionKey.ConfigureTab);
            
            HttpSessionManager.Set(Constants.AppId, SessionKey.ConfigureSubTab);
            
            return LucidChartsConsumer.consumerKey.IsEmpty() || LucidChartsConsumer.consumerSecret.IsEmpty();
        }

        [AppUrl(@"configure")]
        public ActionResult Configure(string consumerKey, string consumerSecret)
        {
            var data = GeminiContext.GlobalConfigurationWidgetStore.Get<LucidChartConfig>(Constants.AppId);
            
            if (data == null)
            {
                data = new GlobalConfigurationWidgetData<LucidChartConfig>();
                data.AppId = Constants.AppId;
            }
            
            data.Value = new LucidChartConfig() { ConsumerKey = consumerKey, ConsumerSecret = consumerSecret };
            
            GeminiContext.GlobalConfigurationWidgetStore.Save<LucidChartConfig>(Constants.AppId, data.Value);

            LucidChartsConsumer.consumerKey = consumerKey;
            
            LucidChartsConsumer.consumerSecret = consumerSecret;
            
            LucidChartsConsumer.token = new InMemoryTokenManager(consumerKey, consumerSecret);

            return JsonSuccess();
        }

        [AppUrl(@"verify")]
        public ActionResult Verify(string oauth_token, string oauth_verifier)
        {
            LucidChartUser user = new LucidChartsConsumer(UserContext.Url, null).Verify(oauth_token, oauth_verifier);

            if (user != null)
            {
                GeminiContext.UserWidgetStore.Save(CurrentUser.Entity.Id, Constants.AppId, Constants.ControlId, user);
            }

            string url = Request["callback"];
            
            if (url.IsEmpty()) return Redirect("~/");

            return Redirect(string.Concat(UserContext.Url, url));
        }

        [AppUrl(@"authenticate/{consumerkey}/{consumersecret}")]
        public ActionResult Authenticate(string consumerKey, string consumerSecret, string callback)
        {
            new LucidChartsConsumer(UserContext.Url, null).Authenticate(consumerKey, consumerSecret, callback);
            
            return new EmptyResult();
        }

        [AppUrl(@"newdocument/{projectcode}/{projectid}/{issueid}")]
        public ActionResult NewDocument(string projectCode, int projectId, int issueId, string documentId)
        {
            if (NoSettings())
            {
                return Redirect("~/configure");
            }

            UserWidgetData<LucidChartUser> userData = GeminiContext.UserWidgetStore.Get<LucidChartUser>(CurrentUser.Entity.Id, Constants.AppId, Constants.ControlId);
            
            if (userData == null || userData.IsNew)
            {
                return Authenticate(string.Empty, string.Empty, string.Format("apps/lucidchart/newdocument/{0}/{1}/{2}", projectCode, projectId, issueId));
            }

            HttpWebRequest request = new LucidChartsConsumer(UserContext.Url, userData.Value).CreateDocument(projectCode, projectId, issueId);

            string queryString = request.Headers["Authorization"].Replace(",", "&").Replace("\"", "").Replace("OAuth ", "");
            /*WebResponse response = request.GetResponse();
            string s = new StreamReader(response.GetResponseStream()).ReadToEnd();*/
            return Redirect(string.Concat(request.RequestUri.ToString(), "&", queryString));
        }

        [AppUrl(@"viewdocument/{projectcode}/{projectid}/{issueid}/{documentid}")]
        public ActionResult ViewDocument(string projectCode, int projectId, int issueId, string documentId)
        {
            if (NoSettings())
            {
                return Redirect("~/configure");
            }

            UserWidgetData<LucidChartUser> userData = GeminiContext.UserWidgetStore.Get<LucidChartUser>(CurrentUser.Entity.Id, Constants.AppId, Constants.ControlId);
            
            if (userData == null || userData.IsNew)
            {
                return Authenticate(string.Empty, string.Empty, string.Format("apps/lucidchart/viewdocument/{0}/{1}/{2}/{3}", projectCode, projectId, issueId, documentId));
            }

            HttpWebRequest request = new LucidChartsConsumer(UserContext.Url, userData.Value).EditDocument(documentId, projectCode, projectId, issueId);

            string queryString = request.Headers["Authorization"].Replace(",", "&").Replace("\"", "").Replace("OAuth ", "");

            return Redirect(string.Concat(request.RequestUri.ToString(), "&", queryString));
        }

        [AppUrl("deletedocument")]
        public ActionResult DeleteDocument(string projectCode, int projectId, int issueId, string documentId)
        {
            if (NoSettings())
            {
                return Redirect("~/configure");
            }

            IssueWidgetData<List<LucidChartData>> data = GeminiContext.IssueWidgetStore.Get<List<LucidChartData>>(issueId, Constants.AppId, Constants.ControlId);

            if (data.Value.RemoveAll(c => string.Compare(c.DocumentId, documentId, true) == 0) > 0)
            {
                GeminiContext.IssueWidgetStore.Save<List<LucidChartData>>(issueId, Constants.AppId, Constants.ControlId, data.Value);
            }

            return JsonSuccess();
        }

        [AppUrl(@"editdocument/{projectcode}/{projectid}/{issueid}")]
        public ActionResult SaveDocument(string projectCode, int projectId, int issueId, string documentId)
        {
            if (NoSettings())
            {
                return Redirect("~/configure");
            }

            UserWidgetData<LucidChartUser> userData = GeminiContext.UserWidgetStore.Get<LucidChartUser>(CurrentUser.Entity.Id, Constants.AppId, Constants.ControlId);
            
            if (userData == null || userData.IsNew)
            {
                return Authenticate(string.Empty, string.Empty, string.Format("apps/lucidchart/editdocument/{0}/{1}/{2}", projectCode, projectId, issueId));
            }

            string name = new LucidChartsConsumer(UserContext.Url, userData.Value).GetDocumentDescription(documentId).Name;
            
            byte[] img = new LucidChartsConsumer(UserContext.Url, userData.Value).GetDocumentImage(documentId, 0, 500, false);
            
            byte[] thumb = new LucidChartsConsumer(UserContext.Url, userData.Value).GetDocumentImage(documentId, 0, 128, true);

            IssueWidgetData<List<LucidChartData>> data = GeminiContext.IssueWidgetStore.Get<List<LucidChartData>>(issueId, Constants.AppId, Constants.ControlId);
            
            if (data == null || data.IsNew)
            {
                data = new IssueWidgetData<List<LucidChartData>>();
                
                data.IssueId = issueId;
                
                data.AppId = Constants.AppId;
                
                data.ControlId = Constants.ControlId;
                
                data.Value = new List<LucidChartData>();
            }

            LucidChartData doc = data.Value.Find(d => string.Compare(d.DocumentId, documentId, true) == 0);
            
            if (doc == null)
            {
                data.Value.Add(new LucidChartData() { DocumentId = documentId, Image = img, ThumnailImage = thumb, DocumentName = name });
            }
            else
            {
                doc.DocumentName = name;
                
                doc.Image = img;
                
                doc.ThumnailImage = thumb;
            }

            GeminiContext.IssueWidgetStore.Save<List<LucidChartData>>(data.IssueId, data.AppId, data.ControlId, data.Value);

            string redirectURL = Countersoft.Gemini.Infrastructure.Helpers.NavigationHelper.GetIssuePageUrl(projectId, projectCode, issueId, UserContext.Card.Id, UserContext.Url);
            
            return Redirect(redirectURL);
        }

        [AppUrl(@"thumbnail/{appid}/{controlid}/{issueid}/{documentid}")]
        public ActionResult Thumbnail(string appid, string controlid, int issueid, string documentid)
        {
            IssueWidgetData<List<LucidChartData>> data = GeminiContext.IssueWidgetStore.Get<List<LucidChartData>>(issueid, appid, controlid);

            LucidChartData image = data.Value.Find(d => string.Compare(d.DocumentId, documentid, true) == 0);

            return File(image == null ? new byte[0] : image.ThumnailImage, "image/png");
        }

        [AppUrl(@"image/{appid}/{controlid}/{issueid}/{documentid}")]
        public ActionResult Image(string appid, string controlid, int issueid, string documentid)
        {
            IssueWidgetData<List<LucidChartData>> data = GeminiContext.IssueWidgetStore.Get<List<LucidChartData>>(issueid, appid, controlid);

            LucidChartData image = data.Value.Find(d => string.Compare(d.DocumentId, documentid, true) == 0);

            return File(image == null ? new byte[0] : image.Image, "image/png");
        }

        public override WidgetResult Caption(IssueDto item)
        {
            WidgetResult result = new WidgetResult();
            
            result.Success = true;
            
            result.Markup.Html = "Lucidchart";
            
            if (item != null) result.Buttons.Add(new AppButton("Add", "add", AppButtonStyle.Default, string.Format("window.location='{0}apps/lucidchart/newdocument/{1}/{2}/{3}'", UserContext.Url, item.Project.Code, item.Project.Id, item.Id)));

            return result;
        }

        public override WidgetResult Show(IssueDto item)
        {
            WidgetResult result = new WidgetResult();
            
            bool hasData = false;

            IssueWidgetData<List<LucidChartData>> data = GeminiContext.IssueWidgetStore.Get<List<LucidChartData>>(item.Id, Constants.AppId, Constants.ControlId);
            
            if (data != null && data.Value.Count > 0)
                hasData = true;

            LucidChartModel model = new LucidChartModel { IssueId = item.Id.ToString(), AppId = Constants.AppId, ControlId = Constants.ControlId, ProjectId = item.Project.Id.ToString(), ProjectCode = item.Project.Code, HasData = hasData, LucidChartData = data, IsGeminiLicenseFree = GeminiApp.GeminiLicense.IsFree, IsGeminiTrial = GeminiApp.LicenseSummary.IsGeminiTrial(), Url = UserContext.Url.Substring(0,UserContext.Url.Length-1)};

            result.Success = true;
            
            result.Markup = new WidgetMarkup("views\\LucidChart.cshtml", model);

            return result;
        }

        public override void AppInitialize(WidgetArguments args)
        {
            var config = args.GeminiContext.GlobalConfigurationWidgetStore.Get<LucidChartConfig>(Constants.AppId);
            
            if (config != null)
            {
                LucidChartsConsumer.consumerKey = config.Value.ConsumerKey;
            
                LucidChartsConsumer.consumerSecret = config.Value.ConsumerSecret;
                
                LucidChartsConsumer.token = new InMemoryTokenManager(LucidChartsConsumer.consumerKey, LucidChartsConsumer.consumerSecret);
            }
        }

        public override WidgetResult Configuration()
        {
            GlobalConfigurationWidgetData<LucidChartConfig> config = GeminiContext.GlobalConfigurationWidgetStore.Get<LucidChartConfig>(Constants.AppId);
            
            if (config == null)
            {
                config = new GlobalConfigurationWidgetData<LucidChartConfig>();
            
                config.Value = new LucidChartConfig();
                
                config.Value.ConsumerKey = string.Empty;
                
                config.Value.ConsumerSecret = string.Empty;
            }

            return new WidgetResult()
            {
                Markup = new WidgetMarkup("views/Settings.cshtml", config.Value)
            };
        }
    } 
}
