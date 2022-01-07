using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Emby.MeiamSub.Thunder.Model;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Template
{
    /// <summary>
    /// 射手字幕组件.
    /// </summary>
    public class ThunderProvider : ISubtitleProvider, IHasOrder
    {
        #region 变量声明

        /// <summary>
        /// ASS.
        /// </summary>
        public const string ASS = "ass";

        /// <summary>
        /// SSA.
        /// </summary>
        public const string SSA = "ssa";

        /// <summary>
        /// SRT.
        /// </summary>
        public const string SRT = "srt";

        private readonly ILogger<ThunderProvider> _logger;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Gets Order.
        /// </summary>
        public int Order => 0;

        /// <summary>
        /// Gets Name.
        /// </summary>
        public string Name => "MeiamSub.Thunder";

        /// <summary>
        /// Gets 支持电影、剧集.
        /// </summary>
        public IEnumerable<VideoContentType> SupportedMediaTypes => new List<VideoContentType>() { VideoContentType.Movie, VideoContentType.Episode };
        #endregion

        #region 构造函数
        /// <summary>
        /// ThunderProvider.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ThunderProvider}"/> interface.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for creating Http Clients.</param>
        public ThunderProvider(ILogger<ThunderProvider> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient(Name);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _logger.LogDebug("MeiamSub.Thunder init");
        }
        #endregion

        #region 查询字幕

        /// <summary>
        /// 查询请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("MeiamSub.Thunder  Search | Request -> {Request}", JsonSerializer.Serialize(request));

            var subtitles = await SearchSubtitlesAsync(request);

            return subtitles;
        }

        /// <summary>
        /// 查询字幕
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitlesAsync(SubtitleSearchRequest request)
        {
            if (request.Language != "chi" && request.Language != "eng")
            {
                return Array.Empty<RemoteSubtitleInfo>();
            }

            var cid = GetCidByFile(request.MediaPath);

            using var httprequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://sub.xmp.sandai.net:8000/subxl/{cid}.json"),
                Headers =
                {
                    UserAgent = { new ProductInfoHeaderValue(new ProductHeaderValue("Emby.MeiamSub.Thunder")) },
                    Accept = { new MediaTypeWithQualityHeaderValue("*/*") }
                }
            };

            _logger.LogDebug("MeiamSub.Thunder  Search | Request -> {Request}", JsonSerializer.Serialize(httprequest));

            var response = await _httpClient.SendAsync(httprequest).ConfigureAwait(false);

            _logger.LogDebug("MeiamSub.Thunder  Search | Response -> {Response}", JsonSerializer.Serialize(response));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var subtitleResponse = JsonSerializer.Deserialize<SubtitleResponseRoot>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

                if (subtitleResponse != null)
                {
                    _logger.LogDebug("MeiamSub.Thunder Search | Response -> {Response}", JsonSerializer.Serialize(subtitleResponse));

                    var subtitles = subtitleResponse.sublist.Where(m => !string.IsNullOrEmpty(m.sname));

                    if (subtitles.Count() > 0)
                    {
                        _logger.LogDebug("MeiamSub.Thunder Search | Summary -> Get  {Count}  Subtitles", subtitles.Count());

                        return subtitles.Select(m => new RemoteSubtitleInfo()
                        {
                            Id = Base64Encode(JsonSerializer.Serialize(new DownloadSubInfo
                            {
                                Url = m.surl,
                                Format = ExtractFormat(m.sname),
                                Language = request.Language,
                                TwoLetterISOLanguageName = request.TwoLetterISOLanguageName
                            })),
                            Name = $"[MEIAMSUB] { Path.GetFileName(request.MediaPath) } | {request.TwoLetterISOLanguageName} | 迅雷",
                            Author = "Meiam ",
                            CommunityRating = Convert.ToSingle(m.rate, CultureInfo.InvariantCulture),
                            ProviderName = "MeiamSub.Thunder",
                            Format = ExtractFormat(m.sname),
                            Comment = $"Format : { ExtractFormat(m.sname)}  -  Rate : { m.rate }"
                        }).OrderByDescending(m => m.CommunityRating);
                    }
                }
            } else {
                _logger.LogDebug("MeiamSub.Thunder Search | Response -> StatusCode={StatusCode} {Content}", response.StatusCode, await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }

            _logger.LogDebug("MeiamSub.Thunder Search | Summary -> Get  0  Subtitles");

            return Array.Empty<RemoteSubtitleInfo>();
        }
        #endregion

        #region 下载字幕
        /// <summary>
        /// 下载请求.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                _logger.LogDebug("MeiamSub.Thunder  DownloadSub | Request -> {Id}", id);
            }, cancellationToken).ConfigureAwait(false);

            return await DownloadSubAsync(id).ConfigureAwait(false);
        }

        /// <summary>
        /// 下载字幕
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private async Task<SubtitleResponse> DownloadSubAsync(string info)
        {
            var downloadSub = JsonSerializer.Deserialize<DownloadSubInfo>(Base64Decode(info));

            if (downloadSub != null)
            {
                _logger.LogDebug("MeiamSub.Thunder  DownloadSub | Url -> {Url}  |  Format -> {Format} |  Language -> {Language}", downloadSub.Url, downloadSub.Format, downloadSub.Language);

                using var httprequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(downloadSub.Url),
                    Headers =
                    {
                        UserAgent = { new ProductInfoHeaderValue(new ProductHeaderValue("Emby.MeiamSub.Thunder")) },
                        Accept = { new MediaTypeWithQualityHeaderValue("*/*") }
                    }
                };

                var response = await _httpClient.SendAsync(httprequest).ConfigureAwait(false);

                _logger.LogDebug("MeiamSub.Thunder  DownloadSub | Response -> {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.OK)
                {

                    return new SubtitleResponse()
                    {
                        Language = downloadSub.Language,
                        IsForced = false,
                        Format = downloadSub.Format,
                        Stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                    };
                }
            }

            return new SubtitleResponse();
        }
        #endregion

        #region 内部方法

        /// <summary>
        /// Base64 加密
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Base64 解密
        /// </summary>
        /// <param name="base64EncodedData"></param>
        /// <returns></returns>
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// 提取格式化字幕类型
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected string ExtractFormat(string text)
        {

            string result = null;

            if (text != null)
            {
                text = text.ToLower();
                if (text.Contains(ASS)) result = ASS;
                else if (text.Contains(SSA)) result = SSA;
                else if (text.Contains(SRT)) result = SRT;
                else result = null;
            }
            return result;
        }

        /// <summary>
        /// 获取文件 CID (迅雷)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string GetCidByFile(string filePath)
        {
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var reader = new BinaryReader(stream);
            var fileSize = new FileInfo(filePath).Length;
            var SHA1 = new SHA1CryptoServiceProvider();
            var buffer = new byte[0xf000];
            if (fileSize < 0xf000)
            {
                reader.Read(buffer, 0, (int)fileSize);
                buffer = SHA1.ComputeHash(buffer, 0, (int)fileSize);
            }
            else
            {
                reader.Read(buffer, 0, 0x5000);
                stream.Seek(fileSize / 3, SeekOrigin.Begin);
                reader.Read(buffer, 0x5000, 0x5000);
                stream.Seek(fileSize - 0x5000, SeekOrigin.Begin);
                reader.Read(buffer, 0xa000, 0x5000);

                buffer = SHA1.ComputeHash(buffer, 0, 0xf000);
            }
            var result = "";
            foreach (var i in buffer)
            {
                result += string.Format(CultureInfo.InvariantCulture, "{0:X2}", i);
            }
            return result;
        }
        #endregion
    }
}
