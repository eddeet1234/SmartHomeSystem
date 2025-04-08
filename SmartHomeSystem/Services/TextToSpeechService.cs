using Google.Cloud.TextToSpeech.V1;

namespace SmartHomeSystem.Services
{
    public class TextToSpeechService
    {

        private TextToSpeechClient _client;

        public TextToSpeechService()
        {
            // Load the credentials from the service account key JSON file
            var builder = new TextToSpeechClientBuilder
            {
                CredentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "creds", "smart-home-455211-91e5887782a7.json")
            };

            _client = builder.Build();
        }

        public async Task<string> SynthesizeSpeechAsync(string text, string filename = "alarm.mp3")
        {
            var response1 = await _client.ListVoicesAsync(new ListVoicesRequest { LanguageCode = "en-US" });

            foreach (var voice1 in response1.Voices)
            {
                Console.WriteLine($"Name: {voice1.Name}, Gender: {voice1.SsmlGender}, Natural Sample Rate: {voice1.NaturalSampleRateHertz}");
            }
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

            var outputPath = Path.Combine("wwwroot", "audio", filename);

            // Ensure the audio folder exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            await File.WriteAllBytesAsync(outputPath, response.AudioContent.ToByteArray());

            return $"/audio/{filename}"; // returns relative URL for playback
        }
    }
}
