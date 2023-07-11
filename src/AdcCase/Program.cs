﻿using Microsoft.Extensions.Configuration;
using System;

namespace AdcCase
{
    public delegate void SaveImageHandler(string path, CancellationToken token);

    class Program
    {
        static async Task Main(string[] args)
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

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                ClearFiles(imageSetting.SavePath);
            };

            var eventPublisher = new SaveImagePublisher();
            eventPublisher.HandleChange += new SaveImageHandler(DownloadImage);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = imageSetting.Parallelism,
                CancellationToken = cts.Token,
            };

            var completedCount = 0;

            var tasks = new List<Task>();

            try
            {
                await Task.WhenAll(tasks.ToArray());

                Parallel.For(0, imageSetting.Count, parallelOptions, (index, token) =>
                {
                    completedCount++;

                    var task = Task.Run(() => eventPublisher.Download($"{imageSetting.SavePath}\\{index + 1}.jpg", cts.Token)).ContinueWith(x =>
                    {
                        SetMessage($"Downloading {imageSetting.Count} images ({imageSetting.Parallelism} parallel downloads at most)", $"Progress: {completedCount}/{imageSetting.Count}");
                        return Task.CompletedTask;
                    });

                    var taskResult = task.Result;
                });
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Operation cancelled, all images have been deleted.");
            }
            finally
            {
                cts.Dispose();
            }

            Console.ReadKey();
        }

        private static void DownloadImage(string path, CancellationToken token)
        {
            var client = new HttpClient();

            client.BaseAddress = new Uri("https://picsum.photos/100/200");
            var image = client.GetAsync("").Result;

            if (!Directory.Exists(Directory.GetDirectoryRoot(path)))
            {
                Directory.CreateDirectory(Directory.GetDirectoryRoot(path));
            }

            var fileArray = image.Content.ReadAsByteArrayAsync().Result;

            if (!token.IsCancellationRequested)
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(fileArray);
                }
            }
        }

        private static void SetMessage(string downloadMessage, string proccesMessage)
        {
            Console.Clear();
            Console.WriteLine(downloadMessage);
            Console.WriteLine(proccesMessage);
        }

        private static void ClearFiles(string path)
        {
            var files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }

    class SaveImagePublisher
    {
        public event SaveImageHandler HandleChange;

        public void Download(string path, CancellationToken token)
        {
            HandleChange(path, token);
        }
    }
}