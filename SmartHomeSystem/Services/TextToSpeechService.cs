using System.Security.Cryptography;
using System.Text;
using Google.Cloud.TextToSpeech.V1;

namespace SmartHomeSystem.Services
{
    public class TextToSpeechService
    {

        private TextToSpeechClient _client;
        private readonly string _audioDirectory;
        public TextToSpeechService()
        {
            // Load the credentials from the service account key JSON file
            var builder = new TextToSpeechClientBuilder
            {
                CredentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "creds", "smart-home-455211-91e5887782a7.json")
            };

            _client = builder.Build();

            _audioDirectory = Path.Combine("wwwroot", "audio");

            if (!Directory.Exists(_audioDirectory))
                Directory.CreateDirectory(_audioDirectory);
        }

        public async Task<string> SynthesizeSpeechAsync(string text)
        {
            //function to list the voices
            //var response1 = await _client.ListVoicesAsync(new ListVoicesRequest { LanguageCode = "en-US" });

            //foreach (var voice1 in response1.Voices)
            //{
            //    Console.WriteLine($"Name: {voice1.Name}, Gender: {voice1.SsmlGender}, Natural Sample Rate: {voice1.NaturalSampleRateHertz}");
            //}

            string filename = GetFileNameFromText(text);
            string outputPath = Path.Combine(_audioDirectory, filename);

            if (!File.Exists(outputPath))
            {
                var input = new SynthesisInput { Text = text };

                var voice = new VoiceSelectionParams
                {
                    //Name = "en-US-Chirp3-HD-Erinome",
                    Name = "en-US-Chirp3-HD-Sulafat",
                    LanguageCode = "en-US"
                };

                var audioConfig = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Mp3
                };

                var response = await _client.SynthesizeSpeechAsync(input, voice, audioConfig);

              
                await File.WriteAllBytesAsync(outputPath, response.AudioContent.ToByteArray());
            } 

            return $"/audio/{filename}"; // returns relative URL for playback
        }

        //function to get unique file name from the text
        private static string GetFileNameFromText(string text)
        {
            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            string hash = Convert.ToHexString(hashBytes);
            return $"{hash}.mp3";
        }
    }
}
