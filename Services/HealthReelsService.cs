using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class HealthReelsService : IHealthReelsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private const string CacheKey = "HEALTH_REELS_YOUTUBE_CACHE";
        private const int CacheDurationMinutes = 60;

        // Negative alarming/clickbait keywords to filter out for a clean medical app experience
        private static readonly string[] NegativeKeywords = new string[]
        {
            "tử vong", "tai nạn", "bắt giữ", "bốc cháy", "án mạng", "tử hình", "bão", "chết", "phẫn nộ", "bỏ chạy", "đâm trực diện"
        };

        public HealthReelsService(HttpClient httpClient, IMemoryCache cache, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<HealthReelsResponseDto> GetHealthReelsAsync()
        {
            // 1. Check RAM cache first
            if (_cache.TryGetValue(CacheKey, out HealthReelsResponseDto cachedResponse))
            {
                return cachedResponse;
            }

            var apiKey = _configuration["YouTube:ApiKey"];
            var channelIds = _configuration.GetSection("YouTube:ChannelIds").Get<List<string>>() ?? new List<string>();

            var fallbackVideos = GetFallbackVideos();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var fallbackResponse = new HealthReelsResponseDto
                {
                    Videos = fallbackVideos,
                    IsFallback = true,
                    ErrorMessage = "YouTube API Key chưa được cấu hình. Vui lòng bổ hình API Key."
                };
                _cache.Set(CacheKey, fallbackResponse, TimeSpan.FromMinutes(CacheDurationMinutes));
                return fallbackResponse;
            }

            try
            {
                var allFetchedDtos = new List<YouTubeVideoDto>();
                var targetChannels = channelIds.Any() ? channelIds : new List<string> { "" };

                // 2. Fetch videos across ALL configured channels (e.g. Sức khỏe & Đời sống AND Lương y Nguyễn Công Đức)
                foreach (var chanId in targetChannels)
                {
                    string channelFilter = !string.IsNullOrEmpty(chanId) ? $"&channelId={chanId}" : "";
                    string queryParam = !string.IsNullOrEmpty(chanId) ? "sức+khỏe" : "sức+khỏe+đông+y+bài+thuốc";
                    string searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=8&order=date&q={queryParam}&type=video&key={apiKey}{channelFilter}";

                    var searchResponse = await _httpClient.GetAsync(searchUrl);
                    if (!searchResponse.IsSuccessStatusCode) continue;

                    var searchJson = await searchResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(searchJson);
                    if (!doc.RootElement.TryGetProperty("items", out var items)) continue;

                    var channelVideoIds = new List<string>();
                    var channelVideoMap = new Dictionary<string, YouTubeVideoDto>();

                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("id", out var idElem) && idElem.TryGetProperty("videoId", out var vIdProp))
                        {
                            string vId = vIdProp.GetString() ?? "";
                            if (string.IsNullOrEmpty(vId)) continue;

                            var snippet = item.GetProperty("snippet");
                            string rawTitle = snippet.GetProperty("title").GetString() ?? "";
                            string channelTitle = snippet.GetProperty("channelTitle").GetString() ?? "";
                            string pubAt = snippet.GetProperty("publishedAt").GetString() ?? "";
                            string rawDesc = snippet.GetProperty("description").GetString() ?? "";

                            // Decode HTML entities cleanly (&quot; -> ", &#39; -> ', &amp; -> &)
                            string title = WebUtility.HtmlDecode(rawTitle);
                            string desc = WebUtility.HtmlDecode(rawDesc);

                            // Filter out alarming/negative news headlines
                            bool isNegative = NegativeKeywords.Any(nk => title.IndexOf(nk, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (isNegative) continue;

                            string thumbUrl = "";
                            if (snippet.TryGetProperty("thumbnails", out var thumbs))
                            {
                                if (thumbs.TryGetProperty("high", out var highThumb))
                                    thumbUrl = highThumb.GetProperty("url").GetString() ?? "";
                                else if (thumbs.TryGetProperty("medium", out var medThumb))
                                    thumbUrl = medThumb.GetProperty("url").GetString() ?? "";
                            }

                            var dto = new YouTubeVideoDto
                            {
                                VideoId = vId,
                                Title = title,
                                ChannelName = "@" + channelTitle.Replace(" ", "").ToLower(),
                                ThumbnailUrl = thumbUrl,
                                PublishedAt = pubAt,
                                Description = desc,
                                EmbedUrl = $"https://www.youtube.com/embed/{vId}"
                            };

                            channelVideoIds.Add(vId);
                            channelVideoMap[vId] = dto;
                        }
                    }

                    // Fetch stats & duration (likes, views, duration)
                    if (channelVideoIds.Any())
                    {
                        string statsUrl = $"https://www.googleapis.com/youtube/v3/videos?part=statistics,contentDetails&id={string.Join(",", channelVideoIds)}&key={apiKey}";
                        var statsResponse = await _httpClient.GetAsync(statsUrl);

                        if (statsResponse.IsSuccessStatusCode)
                        {
                            var statsJson = await statsResponse.Content.ReadAsStringAsync();
                            using var statsDoc = JsonDocument.Parse(statsJson);
                            if (statsDoc.RootElement.TryGetProperty("items", out var statItems))
                            {
                                foreach (var sItem in statItems.EnumerateArray())
                                {
                                    string sId = sItem.GetProperty("id").GetString() ?? "";
                                    if (channelVideoMap.TryGetValue(sId, out var targetDto))
                                    {
                                        if (sItem.TryGetProperty("statistics", out var statsElem))
                                        {
                                            if (statsElem.TryGetProperty("likeCount", out var likeProp) && long.TryParse(likeProp.GetString(), out long likes))
                                                targetDto.LikeCount = likes;
                                            else
                                                targetDto.LikeCount = 120;

                                            if (statsElem.TryGetProperty("viewCount", out var viewProp) && long.TryParse(viewProp.GetString(), out long views))
                                                targetDto.ViewCount = views;
                                            else
                                                targetDto.ViewCount = 1500;
                                        }

                                        if (sItem.TryGetProperty("contentDetails", out var detailsElem) && detailsElem.TryGetProperty("duration", out var durProp))
                                        {
                                            string isoDur = durProp.GetString() ?? "";
                                            targetDto.DurationFormatted = FormatIsoDuration(isoDur);
                                        }

                                        targetDto.ViewCountFormatted = FormatViewCount(targetDto.ViewCount);
                                    }
                                }
                            }
                        }

                        allFetchedDtos.AddRange(channelVideoMap.Values);
                    }
                }

                // 3. Sort & Interleave: Prioritize Eastern Medicine / Remedies ("Đông y", "Lương y", "bài thuốc", "an thần", "bổ huyết") at the top!
                var prioritizedList = allFetchedDtos
                    .OrderByDescending(v => v.Title.Contains("Đông y", StringComparison.OrdinalIgnoreCase) ||
                                            v.Title.Contains("Lương y", StringComparison.OrdinalIgnoreCase) ||
                                            v.Title.Contains("bài thuốc", StringComparison.OrdinalIgnoreCase) ||
                                            v.Title.Contains("thuốc", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(v => v.PublishedAt)
                    .ToList();

                var response = new HealthReelsResponseDto
                {
                    Videos = prioritizedList.Any() ? prioritizedList : fallbackVideos,
                    IsFallback = !prioritizedList.Any()
                };

                _cache.Set(CacheKey, response, TimeSpan.FromMinutes(CacheDurationMinutes));
                return response;
            }
            catch (Exception ex)
            {
                var errResponse = new HealthReelsResponseDto
                {
                    Videos = fallbackVideos,
                    IsFallback = true,
                    ErrorMessage = $"Lỗi xử lý YouTube API: {ex.Message}"
                };
                _cache.Set(CacheKey, errResponse, TimeSpan.FromMinutes(5));
                return errResponse;
            }
        }

        private List<YouTubeVideoDto> GetFallbackVideos()
        {
            return new List<YouTubeVideoDto>();
        }

        private static string FormatIsoDuration(string isoDuration)
        {
            if (string.IsNullOrWhiteSpace(isoDuration)) return "02:35";
            try
            {
                TimeSpan ts = System.Xml.XmlConvert.ToTimeSpan(isoDuration);
                if (ts.Hours > 0)
                {
                    return ts.ToString(@"h\:mm\:ss");
                }
                return ts.ToString(@"m\:ss");
            }
            catch
            {
                return "02:35";
            }
        }

        private static string FormatViewCount(long viewCount)
        {
            if (viewCount >= 1_000_000)
            {
                double millions = viewCount / 1_000_000.0;
                return millions.ToString("0.#") + "M";
            }
            if (viewCount >= 1_000)
            {
                double thousands = viewCount / 1_000.0;
                return thousands.ToString("0.#") + "K";
            }
            return viewCount.ToString();
        }
    }
}
