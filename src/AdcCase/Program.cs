using Microsoft.Extensions.Configuration;

namespace AdcCase
{
    public delegate void SaveImageHandler(string path);

    class Program
    {

        event SaveImageHandler DownloadImageEvent;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

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
            eventPublisher.HandleChange += new SaveImageHandler(EventPublisher_OnChanged);

            for (int i = 0; i < imageSetting.Count; i++)
            {
                eventPublisher.Handle("100/200");
            }

        }

        private static void EventPublisher_OnChanged(string path)
        {
            Console.Clear();
            var client = new HttpClient();

            client.BaseAddress = new Uri("https://picsum.photos");
            var image = client.GetAsync(path).Result;
        }
    }

    class SaveImageEventPublisher
    {
        private string _path;

        public event SaveImageHandler HandleChange;

        public void Handle(string path)
        {
            HandleChange(path);
        }
    }
}