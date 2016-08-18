namespace FuckingCapchaDestructor
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using Tesseract;

    public class ImageToText
    {
        private readonly Bitmap Bitmap;

        private static string AssemblyDirectory { get; set; }

        static ImageToText()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);

            AssemblyDirectory = Path.GetDirectoryName(path);
        }

        public ImageToText(Bitmap bitmap) { this.Bitmap = bitmap; }

        public string Resolve()
        {
            var path = Path.Combine(AssemblyDirectory, "tessdata").Replace(@"\\", @"\");

            using (var engine = new TesseractEngine(path, "consulte", EngineMode.Default))
            {
                engine.SetVariable(
                    "tessedit_char_whitelist",
                    "abcdefghijklmnopqrstuvwyxzABCDEFGHIJKLMNOPQRSTUVWYXZ1234567890");

                //engine.SetVariable(
                //    "font",
                //    "Courier New");

                engine.SetVariable(
                    "fonts_dir",
                    @"C:\Windows\Fonts");

                using (var pix = PixConverter.ToPix(Bitmap))
                using (var page = engine.Process(pix, PageSegMode.SingleBlock))
                {
                    return page.GetText();
                }
            }
        }
    }
}