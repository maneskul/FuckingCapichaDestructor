namespace FuckingCapchaDestructor
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using OpenCvSharp;
    using OpenCvSharp.Extensions;

    public class Program
    {
        const string inputDir = @"c:\Temp\Captchas\";
        const string outputDir = @"c:\Temp\Captchas\Output";
        const string inputDirSearchPattern = "*.jpg";

        static Program()
        {
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        }

        public static void Main(string[] args)
        {
            var files = Directory.GetFiles(inputDir, inputDirSearchPattern)
                .OrderBy(d => Guid.NewGuid());

            var ok = 0;
            var error = 0;

            Parallel.ForEach(files, file =>
            {
                var fileName = Path.GetFileName(file);
                try
                {
                    RemoveNoises(file);
                    Console.WriteLine($"=> Arquivo {fileName} - OK {++ok}");
                }
                catch
                {
                    Console.WriteLine($"=> Arquivo {fileName} - ERRO {++error}");
                }
            });
        }

        private static string RemoveNoises(string filePath)
        {
            using (var bitmap = new Bitmap(filePath))
            {
                using (var captchaWithoutNoises = new NoiseRemover(bitmap).RemoveNoises())
                {
                    bitmap.Dispose();

                    var path = Path.Combine(outputDir, Path.GetFileName(filePath));
                    captchaWithoutNoises.Save(path, ImageFormat.Jpeg);

                    var positions = GetEachLetter(captchaWithoutNoises);
                    var expected = Path.GetFileNameWithoutExtension(filePath);
                    SaveLetters(filePath, positions, expected, captchaWithoutNoises);

                    return new ImageToText(captchaWithoutNoises).Resolve();
                }
            }
        }

        private static List<LetterLocation> GetEachLetter(Bitmap captchaWithoutNoises)
        {
            var original = BitmapConverter.ToMat(captchaWithoutNoises);
            var mat = original
                .CvtColor(ColorConversionCodes.BGR2GRAY)
                .Threshold(0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            var countours = mat.FindContoursAsArray(RetrievalModes.External, ContourApproximationModes.ApproxTC89KCOS);
            var letterRegions = new List<LetterLocation>();

            foreach (var contour in countours)
            {
                var rect = Cv2.BoundingRect(contour);

                // se a altura for menor que 18 ou se a largura for menor que 10, provavelmente se trata de um falso positivo
                if (rect.Height < 18 || rect.Width < 10)
                    continue;

                if (countours.Count() != 5)
                {
                    decimal proportion = (decimal)rect.Width / (decimal)rect.Height;
                    if (proportion > 2m)
                    {
                        var halfWidth = rect.Width / 2;
                        letterRegions.Add(new LetterLocation(rect.X, rect.Y, halfWidth, rect.Height));
                        letterRegions.Add(new LetterLocation(rect.X + halfWidth, rect.Y, halfWidth, rect.Height));
                    }
                }
                else
                    letterRegions.Add(new LetterLocation(rect.X, rect.Y, rect.Width, rect.Height));
            }

            // se não tiver identificado 5 regiões, deu ruim
            if (letterRegions.Count != 5)
                throw new Exception("Não foi possível localizar as 5 letras do captcha. Tente outra imagem!");

            // organiza pelo X para garantir que estamos lendo da esquerda pra direita
            return letterRegions.OrderBy(d => d.X).ToList();
        }

        private static void SaveLetters(
            string filePath,
            List<LetterLocation> positions,
            string expected,
            Bitmap original)
        {
            var posExpected = positions.Zip(expected, (pos, letter) => new { Pos = pos, Letter = letter });
            var mat = BitmapConverter.ToMat(original);

            foreach (var posAndLetter in posExpected)
            {
                var pos = posAndLetter.Pos;
                var letter = posAndLetter.Letter;

                // extraimos a letra do arquivo original adicionando uma margem de 2 px
                var img = mat[
                    Math.Max(0, pos.Y - 2),
                    Math.Min(original.Height, pos.Y + pos.H + 2),
                    Math.Max(pos.X - 2, 0),
                    Math.Min(original.Width, pos.X + pos.W + 2)];

                var resize = img.Resize(new OpenCvSharp.Size(25, 25));
                var bitmap = resize.ToBitmap();

                // salvamos a letra numa pasta especifica com as letras
                var directory = Path.Combine(Path.GetDirectoryName(filePath), "Data", letter.ToString());
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                var count = Directory.GetFiles(directory).Count() + 1;
                var filename = $"{letter}_{count:000000}_{expected}.jpg";
                var path = Path.Combine(directory, filename);
                bitmap.Save(path, ImageFormat.Jpeg);
            }
        }

        private class LetterLocation
        {
            public LetterLocation(int x, int y, int w, int h)
            {
                this.X = x; this.Y = y; this.W = w; this.H = h;
            }

            public int X { get; set; }
            public int Y { get; set; }
            public int W { get; set; }
            public int H { get; set; }
        }
    }
}