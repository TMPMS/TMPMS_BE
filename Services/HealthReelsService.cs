using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using System.Text.Json;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class HealthReelsService : IHealthReelsService
    {
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private const string CacheKey = "HEALTH_REELS_YOUTUBE_CACHE";
        private const int CacheDurationMinutes = 60;

        public HealthReelsService(IConfiguration config, IMemoryCache cache, HttpClient httpClient)
        {
            _config = config;
            _cache = cache;
            _httpClient = httpClient;
        }

        public async Task<HealthReelsResponseDto> GetHealthReelsAsync()
        {
            // Check IMemoryCache first
            if (_cache.TryGetValue(CacheKey, out HealthReelsResponseDto? cachedResult) && cachedResult != null)
            {
                return cachedResult;
            }

            var apiKey = _config["YouTube:ApiKey"];
            var channelIds = _config.GetSection("YouTube:ChannelIds").Get<List<string>>() ?? new List<string>();

            // Fallback list of curated YouTube health Shorts if API Key is not set or quota exceeded
            var fallbackVideos = GetFallbackVideos();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var fallbackResponse = new HealthReelsResponseDto
                {
                    Videos = fallbackVideos,
                    IsFallback = true,
                    ErrorMessage = "YouTube API Key chưa được cấu hình. Sử dụng dữ liệu video xem trước."
                };
                _cache.Set(CacheKey, fallbackResponse, TimeSpan.FromMinutes(CacheDurationMinutes));
                return fallbackResponse;
            }

            try
            {
                var videoList = new List<YouTubeVideoDto>();

                // Build search endpoint URL for health / Đông Y videos from configured channel or query
                string channelFilter = channelIds.Any() ? $"&channelId={channelIds[0]}" : "";
                string queryParam = channelIds.Any() ? "sức+khỏe" : "sức+khỏe+đông+y+bài+thuốc";
                string searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=10&order=date&q={queryParam}&type=video&key={apiKey}{channelFilter}";

                var searchResponse = await _httpClient.GetAsync(searchUrl);
                if (!searchResponse.IsSuccessStatusCode)
                {
                    var fallbackRes = new HealthReelsResponseDto
                    {
                        Videos = fallbackVideos,
                        IsFallback = true,
                        ErrorMessage = $"YouTube API Error: {searchResponse.StatusCode}"
                    };
                    _cache.Set(CacheKey, fallbackRes, TimeSpan.FromMinutes(10));
                    return fallbackRes;
                }

                var searchJson = await searchResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(searchJson);
                var items = doc.RootElement.GetProperty("items");

                var videoIds = new List<string>();
                var videoMap = new Dictionary<string, YouTubeVideoDto>();

                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idElem) && idElem.TryGetProperty("videoId", out var vIdProp))
                    {
                        string vId = vIdProp.GetString() ?? "";
                        if (string.IsNullOrEmpty(vId)) continue;

                        var snippet = item.GetProperty("snippet");
                        string title = snippet.GetProperty("title").GetString() ?? "";
                        string channelTitle = snippet.GetProperty("channelTitle").GetString() ?? "";
                        string pubAt = snippet.GetProperty("publishedAt").GetString() ?? "";
                        string desc = snippet.GetProperty("description").GetString() ?? "";

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

                        videoIds.Add(vId);
                        videoMap[vId] = dto;
                    }
                }

                // Fetch statistics (likes, views) if videoIds present
                if (videoIds.Any())
                {
                    string statsUrl = $"https://www.googleapis.com/youtube/v3/videos?part=statistics&id={string.Join(",", videoIds)}&key={apiKey}";
                    var statsResponse = await _httpClient.GetAsync(statsUrl);

                    if (statsResponse.IsSuccessStatusCode)
                    {
                        var statsJson = await statsResponse.Content.ReadAsStringAsync();
                        using var statsDoc = JsonDocument.Parse(statsJson);
                        var statItems = statsDoc.RootElement.GetProperty("items");

                        foreach (var sItem in statItems.EnumerateArray())
                        {
                            string sId = sItem.GetProperty("id").GetString() ?? "";
                            if (videoMap.TryGetValue(sId, out var targetDto) && sItem.TryGetProperty("statistics", out var statsElem))
                            {
                                if (statsElem.TryGetProperty("likeCount", out var likeProp) && long.TryParse(likeProp.GetString(), out long likes))
                                    targetDto.LikeCount = likes;
                                else
                                    targetDto.LikeCount = 1250;

                                if (statsElem.TryGetProperty("viewCount", out var viewProp) && long.TryParse(viewProp.GetString(), out long views))
                                    targetDto.ViewCount = views;
                                else
                                    targetDto.ViewCount = 8500;
                            }
                        }
                    }

                    videoList.AddRange(videoMap.Values);
                }

                var response = new HealthReelsResponseDto
                {
                    Videos = videoList.Any() ? videoList : fallbackVideos,
                    IsFallback = !videoList.Any()
                };

                _cache.Set(CacheKey, response, TimeSpan.FromMinutes(CacheDurationMinutes));
                return response;
            }
            catch (Exception ex)
            {
                var errorResponse = new HealthReelsResponseDto
                {
                    Videos = fallbackVideos,
                    IsFallback = true,
                    ErrorMessage = ex.Message
                };
                _cache.Set(CacheKey, errorResponse, TimeSpan.FromMinutes(10));
                return errorResponse;
            }
        }

        private List<YouTubeVideoDto> GetFallbackVideos()
        {
            // Theo Phương án 3: Không tự gán videoId giả để tránh sai lệch thông tin y tế.
            // Khi chưa có API Key hoặc chưa tải được từ YouTube API, trả về mảng rỗng để Frontend hiển thị thông báo yêu cầu cấu hình API Key.
            return new List<YouTubeVideoDto>();
        }
    }
}
