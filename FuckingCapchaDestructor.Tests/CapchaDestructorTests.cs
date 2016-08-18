namespace FuckingCapchaDestructor.Tests
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CapchaDestructorTests
    {
        private static string CaptchaDirecotry = Path.Combine(Environment.CurrentDirectory, "Captchas");
        private static string OutputDirectory = Path.Combine(Environment.CurrentDirectory, "CaptchasWithoutNoises");

        static CapchaDestructorTests()
        {
            if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);
        }

        public string[] GetCaptchas()
        {
            var filter = new string[] { /*"7E3qeu.png", "5JWcQP.png"*/ };

            var captchaFiles = Directory
                .GetFiles(CaptchaDirecotry)
                .Where(filePath => !filter.Any() || filter.Any(pattern => filePath.Contains(pattern)))
                .ToArray();

            return captchaFiles;
        }

        [TestMethod]
        public void Destroy()
        {
            Program.Main(this.GetCaptchas());
        }

        [TestMethod]
        public void RemoveNoises()
        {
            foreach (var captchaFile in this.GetCaptchas())
            {
                using (var captcha = new Bitmap(captchaFile))
                using (var captchaWitoutNoises = new NoiseRemover(captcha).RemoveNoises())
                using (var fileStream = File.Create(Path.Combine(OutputDirectory, Path.GetFileName(captchaFile))))
                    captchaWitoutNoises.Save(fileStream, captcha.RawFormat);
            }
        }

        [TestMethod]
        public void AssertPixelGroupingIsWorking()
        {
            var pixos = new PixoCollection(Resources.FourGroups);

            Func<Pixo, bool> noWhites = pixo => !pixo.Readed && pixo.Color.R != 255 && pixo.Color.G != 255 && pixo.Color.B != 255;

            var groups = pixos
                .Where(noWhites)
                .Select(d => d.GroupWhen(noWhites))
                .Where(d => d.Count() < 5);

            Assert.AreEqual(4, groups.Count());
        }
    }
}