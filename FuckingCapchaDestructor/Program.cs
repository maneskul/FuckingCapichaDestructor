namespace FuckingCapchaDestructor
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;

    public class Program
    {
        const string inputDir = @"C:\Users\Desenvolvedor\Downloads\Captchas";
        const string outputDir = @"C:\Users\Desenvolvedor\Downloads\Captchas_Output";
        const string inputDirSearchPattern = "*.jpg";

        static Program()
        {
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        }

        public static void Main(string[] args)
        {
            Directory
                .GetFiles(inputDir, inputDirSearchPattern)
                .AsParallel()
                .ForAll(path => Console.Write($"{path} >> {RemoveNoises(path)}"));

            Console.WriteLine("Finished");
        }

        private static string RemoveNoises(string filePath)
        {
            using (var bitmap = new Bitmap(filePath))
            using (var captchaWithoutNoises = new NoiseRemover(bitmap).RemoveNoises())
            {
                var path = Path.Combine(outputDir, Path.GetFileName(filePath));

                captchaWithoutNoises.Save(path, ImageFormat.Jpeg);

                return new ImageToText(captchaWithoutNoises).Resolve();
            }
        }
    }
}