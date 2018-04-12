namespace FuckingCapchaDestructor
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public class NoiseRemover
    {
        private readonly Bitmap Bitmap;

        private PixoCollection Pixos { get; set; }

        private Bitmap OutputBitmap { get; set; }

        public NoiseRemover(Bitmap bitmap)
        {
            this.Bitmap = bitmap;
            this.OutputBitmap = new Bitmap(this.Bitmap);
            this.Pixos = new PixoCollection(this.Bitmap);
        }

        public Bitmap RemoveNoises()
        {
            while (
                //this.RemoveNonBlacks() |
                this.KeepClosetsTo(Color.Black, 33) |
                this.PaintClosetsTo(Color.Black, Color.Black, 333) |
                this.RemoveAloneGroups() |
                this.RemoveWeakLines() |
                this.RemoveWeakPixosInDiagonal() | 
                //this.RemoveTwoHorizontalSequentialPixelsAlone() |
                this.RemoveTwoVerticalSequentialPixelsAlone() |
                this.RemoveAloneLines()) ;

            return this.OutputBitmap;
        }

        private void Paint(Pixo pixo, Color color)
        {
            pixo.Color = color;
            this.OutputBitmap.SetPixel(pixo.X, pixo.Y, color);
        }

        private void BlackAndWhite(Bitmap image)
        {
            // for every pixel in the rectangle region
            for (Int32 xx = 0; xx < image.Width; xx++)
            {
                for (Int32 yy = 0; yy < image.Height; yy++)
                {
                    // average the red, green and blue of the pixel to get a gray value
                    Color pixel = image.GetPixel(xx, yy);
                    Int32 avg = (pixel.R + pixel.G + pixel.B) / 3;

                    image.SetPixel(xx, yy, Color.FromArgb(0, avg, avg, avg));
                }
            }
        }

        private bool RemoveNonBlacks()
        {
            var nonBlacks = this.Pixos
                .Where(this.NotWhite)
                .Where(pixo => pixo.Color.R != 0 && pixo.Color.G != 0 && pixo.Color.B != 0);

            var anyChange = false;

            foreach (var noBlack in nonBlacks)
            {
                anyChange = true;
                noBlack.Color = Color.White;
                this.OutputBitmap.SetPixel(noBlack.X, noBlack.Y, Color.White);
            }

            return anyChange;
        }

        private bool KeepClosetsTo(Color color, int threshold)
        {
            var nonClosest = this.Pixos
                .Where(NotWhite)
                .Where(pixo => !IsCloseTo(color, pixo.Color, threshold));

            var anyChange = false;

            foreach (var noBlack in nonClosest)
            {
                anyChange = true;
                noBlack.Color = Color.White;
                this.OutputBitmap.SetPixel(noBlack.X, noBlack.Y, Color.White);
            }

            return anyChange;
        }

        private bool PaintClosetsTo(Color example, Color targetColor, int threshold)
        {
            var closest = this.Pixos
                .Where(NotWhite)
                .Where(pixo => pixo.Color != Color.Black)
                .Where(pixo => IsCloseTo(example, pixo.Color, 33));

            var anyChange = false;

            foreach (var pixo in closest)
            {
                anyChange = true;
                pixo.Color = targetColor;
                this.OutputBitmap.SetPixel(pixo.X, pixo.Y, targetColor);
            }

            return anyChange;
        }

        private bool RemoveAloneGroups()
        {
            var anyChange = false;

            var groupsToRemove = this.Pixos
                .Where(pixo => !pixo.Readed && this.NotWhite(pixo))
                .Select(d => d.GroupWhen(pixo => !pixo.Readed && this.NotWhite(pixo)).ToArray())
                .Where(group => group.Count() >= 1 && group.Count() < 100)
                .ToArray();

            foreach (var group in groupsToRemove)
            {
                anyChange = true;

                foreach (var pixo in group)
                {
                    pixo.Color = Color.White;
                    this.OutputBitmap.SetPixel(pixo.X, pixo.Y, Color.White);
                }
            }

            foreach (var pixo in this.Pixos) pixo.MarkAsUnread();

            return anyChange;
        }

        private bool RemoveAloneLines()
        {
            var anyChange = false;

            var items = this.Pixos
                .Where(d => d.Color.ToArgb() == Color.Black.ToArgb() && d.GetSiblings(Directions.Top | Directions.Bottom).All(e => e.Color.ToArgb() == Color.White.ToArgb()))
                .GroupBy(
                    d => d.Y,
                    (y, pixos) =>
                    {
                        var lineIndex = 0;
                        var lastX = pixos.First();
                        return pixos.GroupBy(pixo =>
                        {
                            lineIndex = pixo.X - lastX.X > 1 ? lineIndex + 1 : lineIndex;
                            lastX = pixo;
                            return lineIndex;
                        }, (key, sequentialPixos) => new { Y = y, LineIndex = key, Pixos = sequentialPixos });
                    })
                .SelectMany(groups => groups)
                .Where(d => d.Pixos.Count() > 3);


            foreach (var item in items)
            {
                anyChange = true;
                foreach (var pixo in item.Pixos)
                    Paint(pixo, Color.White);
            }

            return anyChange;
        }

        private bool RemoveWeakLines()
        {
            var weakLines = this.Pixos
                .Where(NotWhite)
                .Select(pixo => new { Pixo = pixo, Siblings = pixo.GetSiblings(this.NotWhite).ToArray() })
                .Where(pixo => pixo.Siblings.Count() == 1);

            var anyChange = false;

            foreach (var weakLine in weakLines)
            {
                anyChange = true;

                var pixo = weakLine.Pixo;
                var pixoB = weakLine.Siblings[0];

                pixo.Color = Color.White;
                this.OutputBitmap.SetPixel(pixo.X, pixo.Y, Color.White);

                pixoB.Color = Color.White;
                this.OutputBitmap.SetPixel(pixoB.X, pixoB.Y, Color.White);
            }

            return anyChange;
        }

        private bool RemoveWeakPixosInDiagonal()
        {
            var weakLines =
                    from pixo in this.Pixos.Where(d => this.NotWhite(d))
                    from diagonal in pixo.GetSiblings(d => this.NotWhite(d) && pixo.IsDiagonal(d))
                    where pixo.GetPixosToCompleteRectangle(diagonal).All(this.IsWhite)
                    select new { PixoA = pixo, PixoB = diagonal };

            var anyChange = false;

            foreach (var weakLine in weakLines)
            {
                anyChange = true;

                weakLine.PixoA.Color = Color.White;
                this.OutputBitmap.SetPixel(weakLine.PixoA.X, weakLine.PixoA.Y, Color.White);

                weakLine.PixoB.Color = Color.White;
                this.OutputBitmap.SetPixel(weakLine.PixoB.X, weakLine.PixoB.Y, Color.White);
            }

            return anyChange;
        }

        private bool RemoveTwoHorizontalSequentialPixelsAlone()
        {
            var anyChange = false;

            var items = this.Pixos
                .Where(d => !this.IsWhite(d))
                .Select(d => new { Pixo = d, Siblings = d.GetSiblings() })
                .Where(d => d.Siblings.Where(sibling => sibling.X == d.Pixo.X).All(this.IsWhite))
                .Select(d => new
                {
                    Pixo = d.Pixo,
                    NextPixo = d.Siblings.FirstOrDefault(sibling => this.NotWhite(sibling) && d.Pixo.X + 1 == sibling.X && d.Pixo.Y == sibling.Y),
                })
                .Where(d => d.NextPixo != null)
                .Where(d => d.NextPixo.GetSiblings().Where(sibling => sibling.X == d.NextPixo.X).All(this.IsWhite));

            foreach (var item in items)
            {
                anyChange = true;

                item.Pixo.Color = Color.White;
                this.OutputBitmap.SetPixel(item.Pixo.X, item.Pixo.Y, Color.White);

                item.NextPixo.Color = Color.White;
                this.OutputBitmap.SetPixel(item.NextPixo.X, item.NextPixo.Y, Color.White);
            }

            return anyChange;
        }

        private bool RemoveTwoVerticalSequentialPixelsAlone()
        {
            var anyChange = false;

            var items = this.Pixos
                .Where(d => !this.IsWhite(d))
                .Select(d => new { Pixo = d, Siblings = d.GetSiblings() })
                .Where(d => d.Siblings.Where(sibling => sibling.Y == d.Pixo.Y).All(this.IsWhite))
                .Select(d => new
                {
                    Pixo = d.Pixo,
                    NextPixo = d.Siblings.FirstOrDefault(sibling => this.NotWhite(sibling) && d.Pixo.Y + 1 == sibling.Y && d.Pixo.X == sibling.X),
                })
                .Where(d => d.NextPixo != null)
                .Where(d => d.NextPixo.GetSiblings().Where(sibling => sibling.Y == d.NextPixo.Y).All(this.IsWhite));

            foreach (var item in items)
            {
                anyChange = true;

                item.Pixo.Color = Color.White;
                this.OutputBitmap.SetPixel(item.Pixo.X, item.Pixo.Y, Color.White);

                item.NextPixo.Color = Color.White;
                this.OutputBitmap.SetPixel(item.NextPixo.X, item.NextPixo.Y, Color.White);
            }

            return anyChange;
        }

        private bool NotWhite(Pixo pixo) => !IsWhite(pixo);

        private bool IsWhite(Pixo pixo) => pixo.Color.R == 255 && pixo.Color.G == 255 && pixo.Color.B == 255;

        private bool IsCloseTo(Color example, Color color, int threshold)
        {
            int r = example.R - color.R,
                g = example.G - color.G,
                b = example.B - color.B;

            return (r * r + g * g + b * b) <= threshold * threshold;
        }
    }

    public class PixoCollection : List<Pixo>
    {
        public PixoCollection(Bitmap bitmap)
        {
            this.Pixos = new Dictionary<string, Pixo>();

            var pixos =
                from x in Enumerable.Range(0, bitmap.Width)
                from y in Enumerable.Range(0, bitmap.Height)
                select new Pixo(this, bitmap, x, y);

            foreach (var pixo in pixos) this.Add(this[pixo] = pixo);
        }

        public Pixo this[int x, int y]
        {
            get { return this.Pixos[$"X:{x}Y:{y}"]; }
        }

        public Pixo this[Pixo pixo]
        {
            set { this.Pixos[$"X:{pixo.X}Y:{pixo.Y}"] = pixo; }
        }

        private IDictionary<string, Pixo> Pixos { get; set; }
    }

    public class Pixo
    {
        public Pixo(PixoCollection pixos, Bitmap bitmap, int x, int y)
        {
            this.Pixos = pixos;
            this.Bitmap = bitmap;
            this.Color = bitmap.GetPixel(x, y);
            this.X = x;
            this.Y = y;
        }

        public PixoCollection Pixos { get; private set; }
        public Bitmap Bitmap { get; private set; }
        public Color Color { get; set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public bool Readed { get; private set; }

        public IEnumerable<Pixo> GroupWhen(Func<Pixo, bool> filter)
        {
            var stack = new Stack<Pixo>(new[] { this });

            while (stack.Count > 0)
            {
                var item = stack.Pop();

                if (filter(item))
                {
                    foreach (var sibling in item.GetSiblings())
                        stack.Push(sibling);

                    yield return item;
                }

                item.Read();
            }
        }

        public IEnumerable<Pixo> GetSiblings()
        {
            return this.GetSiblings(Directions.All);
        }

        public IEnumerable<Pixo> GetSiblings(Directions directions)
        {
            return this.GetSiblingsCoordinates(directions).Select(d => this.Pixos[d.X, d.Y]);
        }

        public IEnumerable<Pixo> GetSiblings(Func<Pixo, bool> filter)
        {
            return this.GetSiblingsCoordinates(Directions.All).Select(d => this.Pixos[d.X, d.Y]).Where(filter);
        }

        public bool IsDiagonal(Pixo pixo)
        {
            return this.X != pixo.X && this.Y != pixo.Y;
        }

        public IEnumerable<Pixo> GetPixosToCompleteRectangle(Pixo diagonalPixo)
        {
            if (!this.IsDiagonal(diagonalPixo)) throw new InvalidOperationException("Only diagonal pixos bitch");

            var coordinates = new
            {
                FromX = Math.Min(this.X, diagonalPixo.X),
                ToX = Math.Max(this.X, diagonalPixo.X),
                FromY = Math.Min(this.Y, diagonalPixo.Y),
                ToY = Math.Max(this.Y, diagonalPixo.Y),
            };

            return this.Pixos
                .Where(pixo =>
                    pixo.X >= coordinates.FromX &&
                    pixo.X <= coordinates.ToX &&
                    pixo.Y >= coordinates.FromY &&
                    pixo.Y <= coordinates.ToY)
                .Where(pixo =>
                    pixo != this &&
                    pixo != diagonalPixo);
        }

        private IEnumerable<Point> GetSiblingsCoordinates(Directions sides)
        {
            var canGrowX = this.X + 1 < this.Bitmap.Width;
            var canGrowY = this.Y + 1 < this.Bitmap.Height;

            if (this.Y > 0)
            {
                var y = this.Y - 1;

                if (sides.HasFlag(Directions.TopLeft) && this.X > 0) yield return new Point(this.X - 1, y);
                if (sides.HasFlag(Directions.Top)) yield return new Point(this.X, y);
                if (sides.HasFlag(Directions.TopRight) && canGrowX) yield return new Point(this.X + 1, y);
            }

            if (sides.HasFlag(Directions.Left) && this.X > 0) yield return new Point(this.X - 1, this.Y);
            if (sides.HasFlag(Directions.Right) && canGrowX) yield return new Point(this.X + 1, this.Y);

            if (canGrowY)
            {
                var y = this.Y + 1;

                if (sides.HasFlag(Directions.BottomLeft) && this.X > 0) yield return new Point(this.X - 1, y);
                if (sides.HasFlag(Directions.Bottom)) yield return new Point(this.X, y);
                if (sides.HasFlag(Directions.BottomRight) && canGrowX) yield return new Point(this.X + 1, y);
            }
        }

        public Pixo Read()
        {
            this.Readed = true;
            return this;
        }

        public Pixo MarkAsUnread()
        {
            this.Readed = false;
            return this;
        }
    }

    public enum Directions
    {
        Top = 2 << 0,
        Right = 2 << 1,
        Bottom = 2 << 2,
        Left = 2 << 3,
        TopRight = 2 << 4,
        BottomRight = 2 << 5,
        BottomLeft = 2 << 6,
        TopLeft = 2 << 7,
        All = Directions.TopLeft | Directions.Top | Directions.TopRight |
              Directions.BottomLeft | Directions.BottomRight | Directions.Bottom |
              Directions.Right | Directions.Left
    }
}