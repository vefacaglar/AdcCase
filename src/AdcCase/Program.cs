using Microsoft.Extensions.Configuration;

namespace AdcCase
{
    public delegate void SaveImageHandler(string path);

    class Program
    {

        event SaveImageHandler DownloadImageEvent;

        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile($"input.json", optional: false, reloadOnChange: true);

            var config = configuration.Build();
            var settings = config.GetSection("imagesSetting").GetChildren();
            var imageSetting = new ImageSetting
            {
                Count = int.Parse(settings.First(x => x.Key == "count").Value),
                Parallelism = int.Parse(settings.First(x => x.Key == "parallelism").Value),
                SavePath = settings.First(x => x.Key == "savePath").Value
            };

            var tasks = new List<Task>();

            var eventPublisher = new SaveImageEventPublisher();
            eventPublisher.HandleChange += new SaveImageHandler(DownloadImage);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = imageSetting.Parallelism,
            };

            var completedCount = 0;

            Parallel.For(0, imageSetting.Count, parallelOptions, index =>
            {
                completedCount++;
                Console.Clear();
                Console.WriteLine($"Downloading {imageSetting.Count} images ({imageSetting.Parallelism} parallel downloads at most)");
                Console.WriteLine($"Progress: {completedCount}/{imageSetting.Count}");
                eventPublisher.Download($"{imageSetting.SavePath}\\{index + 1}.jpg");
            });
        }

        private static void DownloadImage(string path)
        {
            var client = new HttpClient();

            client.BaseAddress = new Uri("https://picsum.photos/100/200");
            var image = client.GetAsync("").Result;

            if (!Directory.Exists(Directory.GetDirectoryRoot(path)))
            {
                Directory.CreateDirectory(Directory.GetDirectoryRoot(path));
            }

            var fileArray = image.Content.ReadAsByteArrayAsync().Result;

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                fs.Write(fileArray);
            }
        }
    }

    class SaveImageEventPublisher
    {
        public event SaveImageHandler HandleChange;

        public void Download(string path)
        {
            HandleChange(path);
        }
    }
}