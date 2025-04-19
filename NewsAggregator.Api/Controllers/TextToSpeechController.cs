using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NewsAggregator.Domain.TextToSpeech.Services;
using NewsAggregator.Domain.TextToSpeech.ValueObjects;
using NewsAggregator.Infrastructure.TextToSpeech.Services;

namespace NewsAggregator.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextToSpeechController : ControllerBase
    {
        private readonly ITextToSpeechFactory _ttsFactory;
        private readonly ILogger<TextToSpeechController> _logger;

        public TextToSpeechController(
            ITextToSpeechFactory ttsFactory,
            ILogger<TextToSpeechController> logger)
        {
            _ttsFactory = ttsFactory;
            _logger = logger;
        }

        [HttpPost("convert")]
        public async Task<IActionResult> ConvertTextToSpeech([FromBody] TextToSpeechRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Text))
                {
                    return BadRequest("Text cannot be empty");
                }

                var voiceConfig = VoiceConfiguration.Create(
                    request.VoiceId,
                    request.LanguageCode,
                    request.Gender,
                    request.Provider,
                    request.SpeakingRate,
                    request.Pitch);

                var provider = _ttsFactory.GetProviderForVoiceConfig(voiceConfig);
                var (filePath, durationMs) = await provider.ConvertToSpeechAsync(request.Text, voiceConfig);

                // Return the audio file
                var fileName = Path.GetFileName(filePath);
                var audioBinary = await System.IO.File.ReadAllBytesAsync(filePath);

                return File(audioBinary, "audio/mpeg", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in text-to-speech conversion");
                return StatusCode(500, $"Error processing request: {ex.Message}");
            }
        }

        [HttpGet("providers")]
        public IActionResult GetAvailableProviders()
        {
            return Ok(new string[] { "AWS", "Azure", "Google" });
        }

        public class TextToSpeechRequest
        {
            public string Text { get; set; }
            public string VoiceId { get; set; }
            public string LanguageCode { get; set; } = "en-US";
            public string Gender { get; set; } = "Neutral";
            public string Provider { get; set; } = "AWS";
            public float? SpeakingRate { get; set; }
            public float? Pitch { get; set; }
        }
    }
} 