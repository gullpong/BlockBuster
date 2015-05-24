using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;


namespace BlockBuster.Animation
{
    using Core;

    public abstract class Object : IObjectAnimation
    {
        protected Canvas canvas;
        protected Rect bound;
        protected int zIndex;
        protected double speed;

        public virtual bool IsBusy()
        {
            return false;
        }
        public abstract void Link(Core.Object model);
        public abstract void Unlink();
        public abstract void Animate();

        public Object(Canvas canvas, Rect bound, int zIndex, double speed)
        {
            this.canvas = canvas;
            this.bound = bound;
            this.zIndex = zIndex;
            this.speed = speed;
        }

        protected double SlideMove(double curPos, double tarPos)
        {
            var maxMomentum = Math.Max(this.canvas.Width, this.canvas.Height) * 0.10;
            var momentum = Math.Min(maxMomentum,
                                    Math.Exp(4.0 * Math.Abs(curPos - tarPos) / maxMomentum));
            var nextPos = curPos;
            if (curPos < tarPos)
            {
                nextPos = Math.Min(curPos + (momentum * this.speed), tarPos);
            }
            else if (curPos > tarPos)
            {
                nextPos = Math.Max(curPos - (momentum * this.speed), tarPos);
            }
            return nextPos;
        }
    }

    class ScoreSticker : Object
    {
        private double fontSize;
        private Size blockSize;

        private Core.ScoreSticker model;
        private TextBlock label;
        private double x;
        private double y;
        private int phase;
        private double progress;
        private int tick;

        public ScoreSticker(Canvas canvas, Rect bound, int zIndex, double speed,
                            double fontSize, Size blockSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
            this.blockSize = blockSize;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.ScoreSticker)model;

            this.label = new TextBlock();
            this.label.FontFamily = new FontFamily("Bauhaus 93");
            this.label.FontSize = this.fontSize * (1.0 + 0.2 * (this.model.Count - 3));
            this.label.Text = "0";
            Canvas.SetZIndex(this.label, this.zIndex);
            Canvas.SetTop(this.label, -this.fontSize * 2);
            this.canvas.Children.Add(this.label);

            this.x = this.model.Col * this.blockSize.Width + this.blockSize.Width / 2.0;
            this.y = this.model.Row * this.blockSize.Height + this.blockSize.Height / 2.0;
            this.phase = 0;
            this.progress = 0.0;
            this.tick = 0;
        }

        public override void Unlink()
        {
 	        this.canvas.Children.Remove(this.label);
        }

        public override bool IsBusy()
        {
            return this.phase < 2;
        }

        public override void Animate()
        {
            switch (this.phase)
            {
                case 0:
                    this.progress += 0.05 * this.speed;
                    if (this.progress > 1.0)
                        this.progress = 1.0;
                    this.label.Text = String.Format("{0:N0}", (int)(this.model.Score * this.progress));
                    if (this.progress >= 1.0)
                    {
                        this.phase = 1;
                        this.progress = 0.0;
                    }
                    break;

                case 1:
                    this.progress += 0.05 * this.speed;
                    if (this.progress >= 1.0)
                    {
                        this.phase = 2;
                        this.progress = 0.0;
                    }
                    break;

                case 2:
                    return;
            }

            Canvas.SetLeft(this.label, this.x - this.label.ActualWidth / 2.0 + this.bound.X);
            Canvas.SetTop(this.label, this.y - this.label.ActualHeight / 2.0 + this.bound.Y);

            Color[] col = { Colors.White, Colors.Gray, Colors.Black };
            this.label.Foreground = new SolidColorBrush(col[this.tick % 3]);
            this.tick++;
        }
    }

    class ShadowedTextBlock
    {
        private TextBlock content;
        private TextBlock shadow;

        public ShadowedTextBlock()
        {
            this.content = new TextBlock();
            this.shadow = new TextBlock();
        }

        public double ActualWidth 
        { 
            get { return this.content.ActualWidth; } 
        }
        public double ActualHeight 
        { 
            get { return this.content.ActualHeight; } 
        }
        public FontFamily FontFamily
        {
            get { return this.content.FontFamily; }
            set
            {
                this.content.FontFamily = value;
                this.shadow.FontFamily = value;
            }
        }
        public double FontSize
        {
            get { return this.content.FontSize; }
            set
            {
                this.content.FontSize = value;
                this.shadow.FontSize = value;
            }
        }
        public string Text
        {
            get { return this.content.Text; }
            set
            {
                this.content.Text = value;
                this.shadow.Text = value;
            }
        }
        public Brush Foreground
        {
            get { return this.content.Foreground; }
            set
            {
                this.content.Foreground = value;
                var col = Colors.Black;
                col.A = 196;
                this.shadow.Foreground = new SolidColorBrush(col);
            }
        }
        public int ZIndex
        {
            get { return Canvas.GetZIndex(this.content); }
            set
            {
                Canvas.SetZIndex(this.content, value);
                Canvas.SetZIndex(this.shadow, value);
            }
        }
        public double X
        {
            get { return Canvas.GetLeft(this.content); }
            set 
            {
                Canvas.SetLeft(this.content, value);
                Canvas.SetLeft(this.shadow, value + this.content.FontSize * 0.2);
            }
        }
        public double Y
        {
            get { return Canvas.GetTop(this.content); }
            set
            {
                Canvas.SetTop(this.content, value);
                Canvas.SetTop(this.shadow, value + this.content.FontSize * 0.2);
            }
        }
        public void Register(Canvas canvas)
        {
            canvas.Children.Add(this.shadow);
            canvas.Children.Add(this.content);
        }
        public void Unregister(Canvas canvas)
        {
            canvas.Children.Remove(this.content);
            canvas.Children.Remove(this.shadow);
        }
    }

    class ComboSticker : Object
    {
        private double fontSize;

        private Core.ComboSticker model;
        private ShadowedTextBlock comboLabel;
        private ShadowedTextBlock bonusLabel;
        private int phase;
        private double progress;

        public ComboSticker(Canvas canvas, Rect bound, int zIndex, double speed,
                            double fontSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.ComboSticker)model;

            this.comboLabel = new ShadowedTextBlock();
            this.comboLabel.FontFamily = new FontFamily("Bauhaus 93");
            this.comboLabel.FontSize = 1.0;
            this.comboLabel.Foreground = new SolidColorBrush(Colors.Red);
            if (this.model.Count == 1)
                this.comboLabel.Text = String.Format("Combo!!");
            else
                this.comboLabel.Text = String.Format("{0} Combos!!", this.model.Count);
            this.comboLabel.X = 0.0;
            this.comboLabel.Y = -10.0;
            this.comboLabel.ZIndex = this.zIndex;

            this.bonusLabel = new ShadowedTextBlock();
            this.bonusLabel.FontFamily = new FontFamily("Bauhaus 93");
            this.bonusLabel.FontSize = 1.0;
            this.bonusLabel.Foreground = new SolidColorBrush(Colors.Red);
            this.bonusLabel.Text = String.Format("Bonus +{0}%", this.model.Bonus * 100);
            this.bonusLabel.X = 0.0;
            this.bonusLabel.Y = -10.0;
            this.bonusLabel.ZIndex = this.zIndex;

            this.comboLabel.Register(this.canvas);
            this.bonusLabel.Register(this.canvas);

            this.phase = 0;
            this.progress = 0.0;
        }

        public override void Unlink()
        {
            this.comboLabel.Unregister(this.canvas);
            this.bonusLabel.Unregister(this.canvas);
        }

        public override bool IsBusy()
        {
            return this.phase < 4;
        }

        public override void Animate()
        {
            switch (this.phase)
            {
                case 0:
                    this.progress += 0.1 * this.speed;
                    if (this.progress >= 0.8)
                    {
                        this.progress = 0.8;
                        this.phase = 1;
                    }
                    this.comboLabel.FontSize = this.progress * this.fontSize;
                    this.bonusLabel.FontSize = this.progress * this.fontSize * 0.8;
                    break;

                case 1:
                    this.progress -= 0.1 * this.speed;
                    if (this.progress <= 0.5)
                    {
                        this.progress = 0.5;
                        this.phase = 2;
                    }
                    this.comboLabel.FontSize = this.progress * this.fontSize;
                    this.bonusLabel.FontSize = this.progress * this.fontSize * 0.8;
                    break;

                case 2:
                    this.progress += 0.1 * this.speed;
                    if (this.progress >= 1.0)
                    {
                        this.progress = 1.0;
                        this.phase = 3;
                    }
                    this.comboLabel.FontSize = this.progress * this.fontSize;
                    this.bonusLabel.FontSize = this.progress * this.fontSize * 0.8;
                    break;

                case 3:
                    this.progress -= 0.05 * this.speed;
                    if (this.progress <= 0.0)
                    {
                        this.progress = 0;
                        this.phase = 4;
                    }
                    break;

                case 4:
                    return;
            }

            double y = this.bound.Height / 2.0;
            this.comboLabel.X = (this.bound.Width - this.comboLabel.ActualWidth) / 2.0 + this.bound.X;
            this.comboLabel.Y = y - this.comboLabel.FontSize + this.bound.Y;
            this.bonusLabel.X = (this.bound.Width - this.bonusLabel.ActualWidth) / 2.0 + this.bound.X;
            this.bonusLabel.Y = y + this.bound.Y;
        }
    }    

    class Combo : Object
    {
        private double fontSize;
        private double tabWidth;

        private Core.Combo model;
        private TextBlock label;
        private TextBlock count;
        private TextBlock bonus;
        private Rectangle bar;
        private double guage;
        private int tick;

        public Combo(Canvas canvas, Rect bound, int zIndex, double speed,
                     double fontSize, double tabWidth)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
            this.tabWidth = tabWidth;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Combo)model;

            this.label = new TextBlock();
            this.label.Text = "COMBO";
            this.label.FontFamily = new FontFamily("Bauhaus 93");
            this.label.FontSize = this.fontSize;
            Canvas.SetLeft(this.label, this.bound.X);
            // Canvas.SetTop(this.label, this.bound.Y + (this.bound.Height - this.fontSize) / 2.0);
            Canvas.SetTop(this.label, this.bound.Y);
            Canvas.SetZIndex(this.label, this.zIndex);

            this.count = new TextBlock();
            this.count.Text = "";
            this.count.FontFamily = new FontFamily("Bauhaus 93");
            this.count.FontSize = this.fontSize * 0.8;
            Canvas.SetLeft(this.count, this.bound.X);
            Canvas.SetTop(this.count, this.bound.Bottom - this.fontSize);
            Canvas.SetZIndex(this.count, this.zIndex);

            this.bar = new Rectangle();
            this.bar.RadiusX = this.fontSize * 0.1;
            this.bar.RadiusY = this.fontSize * 0.1;
            this.bar.Width = 0;
            this.bar.Height = this.fontSize;
            Canvas.SetLeft(this.bar, this.bound.X + this.tabWidth);
            Canvas.SetTop(this.bar, this.bound.Y + (this.bound.Height - this.fontSize) / 2.0);
            Canvas.SetZIndex(this.bar, this.zIndex);

            this.bonus = new TextBlock();
            this.bonus.Text = "";
            this.bonus.FontFamily = new FontFamily("Bauhaus 93");
            this.bonus.FontSize = this.fontSize * 0.8;
            Canvas.SetLeft(this.bonus, this.bound.X + this.tabWidth);
            Canvas.SetTop(this.bonus, this.bound.Y + (this.bound.Height - this.fontSize) / 2.0);
            Canvas.SetZIndex(this.bonus, this.zIndex);

            this.canvas.Children.Add(this.label);
            this.canvas.Children.Add(this.count);
            this.canvas.Children.Add(this.bar);
            this.canvas.Children.Add(this.bonus);
        }

        public override void Unlink()
        {
            this.canvas.Children.Remove(this.label);
            this.canvas.Children.Remove(this.count);
            this.canvas.Children.Remove(this.bar);
            this.canvas.Children.Remove(this.bonus);
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
            if (this.model.Guage > 0.0)
            {
                this.label.Foreground = new SolidColorBrush(Colors.Ivory);

                this.guage = this.SlideMove(this.guage, this.model.Guage);

                this.bar.Fill = new SolidColorBrush(Colors.Red);
                this.bar.Width = this.guage * (this.bound.Width - this.tabWidth);
            }
            else
            {
                this.guage = 0.0;
                //this.label.Foreground = new SolidColorBrush(Colors.SlateGray);
                this.label.Foreground = null;

                this.bar.Fill = null;
            }

            //if (this.model.Count > 0)
            if (this.model.Guage > 0.0)
            {
                Canvas.SetTop(this.label, this.bound.Y);

                if (this.model.Count > 0)
                    this.count.Text = String.Format("{0}", this.model.Count);
                else
                    this.count.Text = String.Format("start");
                this.count.Foreground = new SolidColorBrush(Colors.Ivory);
                Canvas.SetLeft(this.count, this.bound.X + (this.tabWidth - this.count.ActualWidth) / 2.0);
            } else
            {
                Canvas.SetTop(this.label, this.bound.Y + (this.bound.Height - this.fontSize) / 2.0);

                this.count.Foreground = null;
            }

            if (!this.model.Elapse &&
                this.model.Bonus > 0.0)
            {
                this.bonus.Text = String.Format("Bonus +{0}%", this.model.Bonus * 100.0);
                Color[] col = { Colors.Black, Colors.Red, Colors.Pink };
                this.bonus.Foreground = new SolidColorBrush(col[this.tick % 3]);
                this.tick++;
                //this.bonus.FontSize = this.fontSize * 0.8 * Process.RandGen.NextDouble();
                Canvas.SetLeft(this.bonus, this.bound.X + this.tabWidth + (this.bound.Width - this.tabWidth - this.bonus.ActualWidth) / 2.0);
            }
            else
            {
                this.bonus.Foreground = null;
            }
        }
    }

    class Next : Object
    {
        private double fontSize;

        private Core.Next model;
        private TextBlock label;

        public Next(Canvas canvas, Rect bound, int zIndex, double speed,
                     double fontSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Next)model;

            this.label = new TextBlock();
            this.label.Text = "NEXT";
            this.label.FontFamily = new FontFamily("Bauhaus 93");
            this.label.FontSize = this.fontSize;
            this.label.Foreground = new SolidColorBrush(Colors.Ivory);
            Canvas.SetLeft(this.label, this.bound.X);
            Canvas.SetTop(this.label, this.bound.Y + (this.bound.Height - this.fontSize) / 2.0);
            Canvas.SetZIndex(this.label, this.zIndex);

            this.canvas.Children.Add(this.label);
        }

        public override void Unlink()
        {
            this.canvas.Children.Remove(this.label);
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
        }
    }

    class Record : Object
    {
        private double fontSize;
        private double scoreBase;

        private Core.Record model;
        private TextBlock scoreLabel;
        private TextBlock bustLabel;
        private TextBlock moveLabel;
        private int score;
        private int bustCount;

        public Record(Canvas canvas, Rect bound, int zIndex, double speed,
                     double fontSize, int scoreBase)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
            this.scoreBase = scoreBase;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Record)model;

            this.scoreLabel = new TextBlock();
            this.scoreLabel.FontFamily = new FontFamily("Bauhaus 93");
            this.scoreLabel.FontSize = this.fontSize;
            this.scoreLabel.Foreground = new SolidColorBrush(Colors.Ivory);
            Canvas.SetTop(this.scoreLabel, -this.fontSize * 2);
            Canvas.SetZIndex(this.scoreLabel, this.zIndex);

            this.bustLabel = new TextBlock();
            this.bustLabel.FontFamily = new FontFamily("Bauhaus 93");
            this.bustLabel.FontSize = this.fontSize * 0.7;
            this.bustLabel.Foreground = new SolidColorBrush(Colors.Ivory);
            Canvas.SetTop(this.bustLabel, -this.fontSize * 2);
            Canvas.SetZIndex(this.bustLabel, this.zIndex);

            this.moveLabel = new TextBlock();
            this.moveLabel.FontFamily = new FontFamily("OCR A Std");
            this.moveLabel.FontSize = this.fontSize * 0.7;
            this.moveLabel.Foreground = new SolidColorBrush(Colors.Ivory);
            Canvas.SetTop(this.moveLabel, -this.fontSize * 2);
            Canvas.SetZIndex(this.moveLabel, this.zIndex);

            this.canvas.Children.Add(this.scoreLabel);
            this.canvas.Children.Add(this.bustLabel);
            this.canvas.Children.Add(this.moveLabel);

            this.score = 0;
            this.bustCount = 0;
        }

        public override void Unlink()
        {
            this.canvas.Children.Remove(this.scoreLabel);
            this.canvas.Children.Remove(this.bustLabel);
            this.canvas.Children.Remove(this.moveLabel);
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
            if (this.score < this.model.Score)
            {
                this.score += (int)((double)this.scoreBase + (double)this.scoreBase * (Process.RandGen.NextDouble() - 0.5));
                if (this.score > this.model.Score)
                    this.score = this.model.Score;
            }
            this.scoreLabel.Text = String.Format("{0:N0}", this.score);
            Canvas.SetLeft(this.scoreLabel, (this.bound.Width - this.scoreLabel.ActualWidth) / 2.0 + this.bound.X);
            Canvas.SetTop(this.scoreLabel, (this.bound.Height - this.scoreLabel.ActualHeight) / 2.0 + this.bound.Y);

            if (this.bustCount < this.model.BustCount)
                this.bustCount++;
            this.bustLabel.Text = String.Format("{0}", this.bustCount);
            Canvas.SetLeft(this.bustLabel, (this.fontSize * 6.0 - this.bustLabel.ActualWidth) / 2.0 + this.bound.X);
            Canvas.SetTop(this.bustLabel, (this.bound.Height - this.bustLabel.ActualHeight) / 2.0 + this.bound.Y);

            this.moveLabel.Text = String.Format("{0}", this.model.MoveCount);
            Canvas.SetLeft(this.moveLabel, this.bound.Right - (this.fontSize * 6.0 - this.moveLabel.ActualWidth) / 2.0);
            Canvas.SetTop(this.moveLabel, (this.bound.Height - this.moveLabel.ActualHeight) / 2.0 + this.bound.Y);
        }
    }

    class Focus : Object
    {
        private Size blockSize;

        private Core.Focus model;
        private Rectangle rim;
        private int phase;
        private double progress;

        public Focus(Canvas canvas, Rect bound, int zIndex, double speed,
                     Size blockSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.blockSize = blockSize;
        }

        public override void Link(Core.Object model)
        {
 	        this.model = (Core.Focus)model;

            this.rim = new Rectangle();
            this.rim.StrokeThickness = Math.Min(this.blockSize.Width, this.blockSize.Height) * 0.1;
            this.rim.RadiusX = this.blockSize.Width * 0.2;
            this.rim.RadiusY = this.blockSize.Height * 0.2;
            this.rim.Width = this.model.ColSpan * this.blockSize.Width;
            this.rim.Height = this.model.RowSpan * this.blockSize.Height;
            Canvas.SetLeft(this.rim, this.model.Col * this.blockSize.Width + this.bound.X);
            Canvas.SetTop(this.rim, this.model.Row * this.blockSize.Height + this.bound.Y);
            Canvas.SetZIndex(this.rim, this.zIndex);

            this.canvas.Children.Add(this.rim);

            this.phase = 0;
            this.progress = 0.0;
        }

        public override void Unlink()
        {
            this.canvas.Children.Remove(this.rim);
        }

        public override bool IsBusy()
        { 
            return false;
        }

        public override void Animate()
        {
            switch (this.phase)
            {
                case 0:
                    this.progress += 0.02 * this.speed;
                    if (this.progress >= 1.0)
                    {
                        this.progress = 1.0;
                        this.phase = 1;
                    }
                    break;

                case 1:
                    this.progress -= 0.02 * this.speed;
                    if (this.progress <= 0.0)
                    {
                        this.progress = 0.0;
                        this.phase = 0;
                    }
                    break;
            }

            Color col = new Color();
            col.A = 200;
            col.R = (Byte)(255.0 * this.progress);
            col.G = (Byte)(255.0 * this.progress);
            col.B = (Byte)(255.0 * this.progress);
            this.rim.Stroke = new SolidColorBrush(col);
        }
    }

    public class IceBlock
    {
        private Core.Block.Types type;
        private Rect bound;
        private double speed;
        private double shineDelay;

        private double x;
        private double y;
        private double width;
        private double height;
        private int dur;

        private Rectangle frame;
        private TextBlock mark;
        private Color fillColor;
        private Color strokeColor;

        private DateTime shineTime;
        private double shineProgress;

        public IceBlock(Core.Block.Types type, Rect bound, double speed, double shineDelay,
                        Size size, int color, double fontSize, int zIndex)
        {
            this.type = type;
            this.bound = bound;
            this.speed = speed;
            this.shineDelay = shineDelay;

            if (this.type == Core.Block.Types.Normal)
            {
                Color[,] TYPE_COLORS = {
                                           { Colors.LightBlue, Colors.DarkBlue },
                                           { Colors.LightPink, Colors.DarkRed },
                                           { Colors.LightGoldenrodYellow, Colors.DarkOrange },
                                           { Colors.LightCyan, Colors.DarkCyan },
                                           { Colors.Plum, Colors.DarkMagenta },
                                           { Colors.PaleGreen, Colors.DarkGreen }
                                       };
                var colIndex = color % (TYPE_COLORS.GetLength(0));
                this.fillColor = TYPE_COLORS[colIndex, 0];
                this.strokeColor = TYPE_COLORS[colIndex, 1];
            }
            else
            {
                this.fillColor = Colors.DarkGray;
                this.strokeColor = Colors.Black;
            }

            this.frame = new Rectangle();
            this.frame.StrokeThickness = Math.Min(size.Width, size.Height) * 0.02;
            this.frame.Width = size.Width;
            this.frame.Height = size.Height;
            this.frame.RadiusX = size.Width * 0.2;
            this.frame.RadiusY = size.Height * 0.2;
            Canvas.SetZIndex(this.frame, zIndex);

            this.mark = new TextBlock();
            this.mark.FontFamily = new FontFamily("Bauhaus 93");
            this.mark.FontSize = fontSize;
            Canvas.SetTop(this.mark, -fontSize * 2.0);
            Canvas.SetZIndex(this.mark, zIndex);

            this.shineTime = DateTime.Now.AddSeconds(Process.RandGen.NextDouble() * this.shineDelay);
            this.shineProgress = -1.0;
        }

        public double X
        {
            get { return this.x; }
            set 
            {
                this.x = value;
                Canvas.SetLeft(this.frame, this.x - this.frame.Width / 2.0 + this.bound.X);
                if (this.mark.ActualWidth != double.NaN)
                    Canvas.SetLeft(this.mark, this.x - this.mark.ActualWidth / 2.0 + this.bound.X);
                else
                    Canvas.SetLeft(this.mark, 0.0);
            }
        }
        public double Y
        {
            get { return this.y; }
            set 
            {
                this.y = value;
                Canvas.SetTop(this.frame, this.y - this.frame.Height / 2.0 + this.bound.Y);
                if (this.mark.ActualHeight != double.NaN)
                    Canvas.SetTop(this.mark, this.y - this.mark.ActualHeight / 2.0 + this.bound.Y);
                else
                    Canvas.SetTop(this.mark, -this.height * 2.0);
            }
        }
        public double Width
        {
            get { return this.width; }
            set
            {
                this.width = value;
                this.frame.Width = this.width;
            }
        }
        public double Height
        {
            get { return this.height; }
            set
            {
                this.height = value;
                this.frame.Height = this.height;
            }
        }
        public int Dur
        {
            get { return this.dur; }
            set 
            {
                this.dur = value;
                this.mark.Text = String.Format("{0}", this.dur);
                if (this.dur > 1)
                {
                    var col = this.strokeColor;
                    col.A = 68;
                    this.mark.Foreground = new SolidColorBrush(col);
                }
                else
                    this.mark.Foreground = null;
            }
        }

        public void Register(Canvas canvas)
        {
            canvas.Children.Add(this.frame);
            canvas.Children.Add(this.mark);
        }
        public void Unregister(Canvas canvas)
        {
            canvas.Children.Remove(this.frame);
            canvas.Children.Remove(this.mark);
        }

        public void Animate()
        {
            if (this.shineProgress >= 0.0)
            {
                var gradStopCol = new GradientStopCollection();
                GradientStop gradStop;
                gradStop = new GradientStop();
                gradStop.Color = this.fillColor;
                gradStop.Offset = 0.0;
                gradStopCol.Add(gradStop);
                gradStop = new GradientStop();
                gradStop.Color = Colors.White;
                gradStop.Offset = this.shineProgress;
                gradStopCol.Add(gradStop);
                gradStop = new GradientStop();
                gradStop.Color = fillColor;
                gradStop.Offset = this.shineProgress + 0.5;
                gradStopCol.Add(gradStop);
                this.frame.Fill = new LinearGradientBrush(gradStopCol, 45.0);
                this.frame.Stroke = new SolidColorBrush(this.strokeColor);

                this.shineProgress += 0.1 * this.speed;
                if (this.shineProgress > 2.0)
                    this.shineProgress = -1.0;
            }
            else
            {
                this.frame.Fill = new SolidColorBrush(this.fillColor);
                this.frame.Stroke = new SolidColorBrush(this.strokeColor);

                if (this.shineTime < DateTime.Now &&
                    this.type == Core.Block.Types.Normal)     // only normal blocks shine
                {
                    this.shineTime = DateTime.Now.AddSeconds(Process.RandGen.NextDouble() * this.shineDelay);
                    this.shineProgress = 0.0;
                }
            }
        }
    }

    public class Block : Object
    {
        private Size size;
        private double fontSize;
        private double shineDelay;

        private Core.Block model;
        private IceBlock iceBlock;

        private Core.Block.States state;
        private double progress;

        public Block(Canvas canvas, Rect bound, int zIndex, double speed,
                     Size size, double fontSize, double shineDelay)
            : base(canvas, bound, zIndex, speed)
        {
            this.size = size;
            this.fontSize = fontSize;
            this.shineDelay = shineDelay;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Block)model;

            this.iceBlock = new IceBlock(this.model.Type, this.bound, this.speed, this.shineDelay,
                                         this.size, this.model.Color, this.fontSize, this.zIndex);
            this.iceBlock.X = this.model.Col * this.size.Width + this.size.Width / 2.0;
            this.iceBlock.Y = this.model.Row * this.size.Height + this.size.Height / 2.0;
            this.iceBlock.Width = this.size.Width;
            this.iceBlock.Height = this.size.Height;
            this.iceBlock.Register(this.canvas);

            this.state = Core.Block.States.Dead;
            this.progress = 0.0;
        }

        public override void Unlink()
        {
            this.iceBlock.Unregister(this.canvas);
        }

        public override bool IsBusy()
        {
            return this.model.State != this.state;
        }

        public override void Animate() 
        {
            if (this.model.State != this.state)
            {
                switch (this.model.State)
                {
                    case Core.Block.States.Idle:
                        this.iceBlock.Dur = this.model.Dur;
                        this.state = this.model.State;
                        break;

                    case Core.Block.States.Moved:
                        this.iceBlock.Dur = this.model.Dur;
                        double x = this.model.Col * this.size.Width + this.size.Width / 2.0;
                        double y = this.model.Row * this.size.Height + this.size.Height / 2.0;
                        this.iceBlock.X = this.SlideMove(this.iceBlock.X, x);
                        this.iceBlock.Y = this.SlideMove(this.iceBlock.Y, y);
                        if (this.iceBlock.X == x && this.iceBlock.Y == y)
                            this.state = this.model.State;
                        break;

                    case Core.Block.States.Busted:
                        this.progress += 0.07 * this.speed;
                        if (this.progress < 1.0)
                        {
                            this.iceBlock.Width = this.size.Width + (Process.RandGen.NextDouble() - 0.5) * (this.size.Width * 0.2);
                            this.iceBlock.Height = this.size.Height + (Process.RandGen.NextDouble() - 0.5) * (this.size.Height * 0.2);
                        } else
                        {
                            this.iceBlock.Width = this.size.Width;
                            this.iceBlock.Height = this.size.Height;
                            this.progress = 0.0;
                            this.state = this.model.State;
                        }
                        break;

                    case Core.Block.States.Dead:
                        this.state = this.model.State;
                        break;
                }
            }
            

            this.iceBlock.Animate();
        }
    }

    public class BlockSeed : Object
    {
        private Size size;
        private double fontSize;
        private double shineDelay;

        private Core.BlockSeed model;
        private IceBlock iceBlock;

        public BlockSeed(Canvas canvas, Rect bound, int zIndex, double speed,
                         Size size, double fontSize, double shineDelay)
            : base(canvas, bound, zIndex, speed)
        {
            this.size = size;
            this.fontSize = fontSize;
            this.shineDelay = shineDelay;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.BlockSeed)model;

            this.iceBlock = new IceBlock(this.model.Type, this.bound, this.speed, this.shineDelay,
                                         this.size, this.model.Color, this.fontSize, this.zIndex);
            this.iceBlock.X = this.bound.Width * this.model.Pos;
            this.iceBlock.Y = this.bound.Height / 2.0;
            this.iceBlock.Width = this.size.Width;
            this.iceBlock.Height = this.size.Height;
            this.iceBlock.Dur = this.model.Dur;
            this.iceBlock.Register(this.canvas);
        }

        public override void Unlink()
        {
            this.iceBlock.Unregister(this.canvas);
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
            double x = this.bound.Width * this.model.Pos;
            this.iceBlock.X = this.SlideMove(this.iceBlock.X, x);
            this.iceBlock.Y = this.bound.Height / 2.0;

            this.iceBlock.Animate();
        }
    }

}