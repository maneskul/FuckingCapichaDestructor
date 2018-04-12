namespace FuckingCapchaDestructor
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using OpenCvSharp;
    using OpenCvSharp.Extensions;

    public class Program
    {
        public static string _currentFile { get; set; }
        
        const string inputDir = @"C:\Users\Desenvolvedor\Downloads\Captchas";
        const string outputDir = @"C:\Users\Desenvolvedor\Downloads\Captchas_Output";
        const string inputDirSearchPattern = "00VQ3.jpg";

        static Program()
        {
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        }

        public static void Main(string[] args)
        {
            //var files = Directory.GetFiles(@"c:\Temp\Captchas\Bests\", "*.jpg");
            var files = new[] { @"c:\Temp\Captchas\236GF.jpg" };
            foreach (var file in files)
            {
                _currentFile = file;
                var fileName = Path.GetFileName(_currentFile);

                try
                {
                    Resolve(_currentFile);
                    Console.WriteLine($"=> Arquivo {fileName} - OK");

                }
                catch
                {
                    Console.WriteLine($"=> Arquivo {fileName} - ERRO");
                }
            }
        }

        private static string RemoveNoises(string filePath)
        {
            using (var bitmap = new Bitmap(filePath))
            {
                using (var captchaWithoutNoises = new NoiseRemover(bitmap).RemoveNoises())
                {
                    bitmap.Dispose();

                    var filename = string.Concat(Path.GetFileNameWithoutExtension(filePath), "_result", Path.GetExtension(filePath));
                    var path = Path.Combine(Path.GetDirectoryName(filePath), filename);
                    captchaWithoutNoises.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);

                    var positions = GetEachLetter(captchaWithoutNoises);
                    var expected = Path.GetFileNameWithoutExtension(_currentFile);
                    SaveLetters(positions, expected, captchaWithoutNoises);

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
                decimal proportion = (decimal)rect.Width / (decimal)rect.Height;
                if (proportion > 1.25m)
                {
                    var halfWidth = rect.Width / 2;

                    letterRegions.Add(new LetterLocation(rect.X, rect.Y, halfWidth, rect.Height));
                    letterRegions.Add(new LetterLocation(rect.X + halfWidth, rect.Y, halfWidth, rect.Height));
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
                var img = mat[pos.Y - 2, pos.Y + pos.H + 2, pos.X - 2, pos.X + pos.W + 2];
                var resize = img.Resize(new OpenCvSharp.Size(20, 20));
                var bitmap = resize.ToBitmap();

                // salvamos a letra numa pasta especifica com as letras
                var directory = Path.Combine(Path.GetDirectoryName(_currentFile), "Data", letter.ToString());
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                var count = Directory.GetFiles(directory).Count() + 1;
                var filename = $"{letter}_{count:000000}.jpg";
                var path = Path.Combine(directory, filename);
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        private static Mat ResizeToFit(Mat mat, int width, int height)
        {
            
            return mat;
        }

        private static Bitmap GetLetter(Bitmap image, LetterLocation pos)
        {
            var mat = BitmapConverter.ToMat(image);

            return mat[pos.Y - 2, pos.Y + pos.H + 2, pos.X - 2, pos.X + pos.W + 2].ToBitmap();
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