namespace FuckingCapchaDestructor
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading;

    public class Program
    {
        public static void Main(string[] args)
        {
            foreach (var filePath in args)
                Console.WriteLine(File.Exists(filePath) ? $"{filePath} => {Resolve(filePath)}" : $"The file {filePath} does not exists");

            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        private static string Resolve(string filePath)
        {
            using (var bitmap = new Bitmap(filePath))
            using (var captchaWithoutNoises = new NoiseRemover(bitmap).RemoveNoises())
            {
                bitmap.Dispose();

                var filename = string.Concat(Path.GetFileNameWithoutExtension(filePath), "_result", Path.GetExtension(filePath));
                var path = Path.Combine(Path.GetDirectoryName(filePath), filename);
                captchaWithoutNoises.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);

                return new ImageToText(captchaWithoutNoises).Resolve();
            }
        }
    }
}