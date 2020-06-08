using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace HTTP_core
{
    public class HttpCreator
    {
        private readonly string SaveFolder;
        private readonly HttpClient Client;
        private int DeepLevel;
        private DomainRestrictionEnum DomainRestriction;
        private List<string> DownloadResourceRestriction;
        private bool VerboseMode;
        private Uri Url;
        private readonly Uri BaseUrl;

        public event EventHandler<string> Progress;

        #region Set Up
        public HttpCreator(string saveFolder, string baseUrl)
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = true
            };

            this.SaveFolder = saveFolder;
            this.Client = new HttpClient(handler);
            Init();
            this.DeepLevel = 0;
            this.DomainRestriction = DomainRestrictionEnum.OnlyCurrentDomain;
            this.DownloadResourceRestriction = new List<string> { "*" };
            this.VerboseMode = false;
            this.BaseUrl = new Uri(baseUrl);
        }

        public HttpCreator SetDeepLevel(int level)
        {
            this.DeepLevel = level;
            return this;
        }

        public HttpCreator SetDomainRestriction(DomainRestrictionEnum domainRestricition)
        {
            this.DomainRestriction = domainRestricition;
            return this;
        }

        public HttpCreator SetDownloadResourceRestriction(List<string> downloadResourceRestriction)
        {
            this.DownloadResourceRestriction = downloadResourceRestriction;
            return this;
        }

        public HttpCreator SetVerboseMode(bool verboseMode)
        {
            this.VerboseMode = verboseMode;
            return this;
        }

        private void Init()
        {
            if (Directory.Exists(SaveFolder))
            {
                Directory.Delete(SaveFolder, true);
            }

            Directory.CreateDirectory(SaveFolder);
        }
        #endregion

        protected void OnProgress(string message)
        {
            var tmp = Progress;
            if (tmp != null && this.VerboseMode != false)
                Progress(this, message);
        }

        private string CreateFilePath(string url)
        {
            this.Url = CreateUrl(url);

            var fileName = $"{Url.AbsolutePath.Replace("/", "_")}.html";
            return Path.Combine(SaveFolder, fileName);
        }

        private Uri CreateUrl(string url)
        {
            try
            {
                return new Uri(url);
            }
            catch (UriFormatException)
            {
                return new Uri(BaseUrl.AbsoluteUri + url);
            }
        }

        public void AnalysContent(string url, int level = 0)
        {
            OnProgress($"Handle page: {url}");
            var filePath = CreateFilePath(url);

            string html = GetPage(Url);
            File.WriteAllText(filePath, html);

            HtmlDocument doc = new HtmlDocument();
            doc.Load(filePath);

            DownloadResources(doc);

            NodeComparer comparer = new NodeComparer();
            HtmlNodeCollection htmlNodes = doc.DocumentNode.SelectNodes(GetHrefSelectors());

            if (htmlNodes == null)
            {
                return;
            }

            var nodes = htmlNodes.Distinct(comparer);

            foreach (var node in htmlNodes)
            {
                if (node.Attributes["href"].Value == BaseUrl.ToString())
                {
                    continue;
                }

                Uri hrefUri = CreateUrl(node.Attributes["href"].Value);
                var urlDeep = hrefUri.PathAndQuery.Trim('/').Split('/').Count();

                if (urlDeep > this.DeepLevel)
                {
                    continue;
                }

                var newFilePath = CreateFilePath(node.Attributes["href"].Value);

                if (File.Exists(newFilePath))
                {
                    continue;
                }

                AnalysContent(node.Attributes["href"].Value, ++level);
            }
        }

        private void DownloadResources(HtmlDocument doc)
        {
            foreach (var format in DownloadResourceRestriction)
            {
                var pngNodes = doc.DocumentNode.SelectNodes($".//img[contains(@src,'{format}')]/@src");

                if (pngNodes == null)
                {
                    continue;
                }

                foreach (var node in pngNodes)
                {
                    OnProgress($"       Download resource: {node.Attributes["src"].Value}");
                    var url = CreateUrl(node.Attributes["src"].Value);
                    var res = GetResourse(url);

                    if (res != null)
                    {
                        Image img = Image.FromStream(res);
                        var savePath = CreateImagePath(node.Attributes["src"].Value, format);
                        img.Save(savePath);
                    }
                }
            }
            
        }

        private string CreateImagePath(string url, string format)
        {
            var fileName = $"{CreateUrl(url).AbsolutePath.Replace("/", "_") + format}";
            return Path.Combine(SaveFolder, fileName);
        }

        private Stream GetResourse(Uri address)
        {
            try
            {
                var resource = Client.GetStreamAsync(address).Result;

                return resource;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (AggregateException)
            {
                return null;
            }
        }

        private string GetPage(Uri address)
        {
            try
            {
                string page = Client.GetStringAsync(address).Result;

                return page;
            }
            catch (AggregateException)
            {

                return "";
            }
            catch (ArgumentException)
            {
                return "System.ArgumentException: 'Only 'http' and 'https' schemes are allowed. (Parameter 'requestUri')";
            }

        }

        private string GetHrefSelectors()
        {
            string result = "";

            switch (DomainRestriction)
            {
                case DomainRestrictionEnum.NoHigherWay:
                    string urlNoHigherWay = this.BaseUrl.Authority + this.BaseUrl.PathAndQuery;
                    result = $".//a[contains(@href,'{urlNoHigherWay}')]/@href";
                    break;
                case DomainRestrictionEnum.OnlyCurrentDomain:
                    string urlOnlyCurr = this.BaseUrl.Host;
                    result = $".//a[contains(@href,'{urlOnlyCurr}')]/@href";
                    break;
                case DomainRestrictionEnum.WithoutRestrition:
                    result = ".//a[contains(@href,'http')]/@href";
                    break;
                default:
                    throw new Exception("Unknow error!");
            }

            return result;
        }


    }
}
