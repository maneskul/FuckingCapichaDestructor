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
                return new ImageToText(captchaWithoutNoises).Resolve();
        }
    }
}