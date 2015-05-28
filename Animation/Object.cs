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

    class MessageSticker : Object
    {
        private double fontSize;

        private Core.MessageSticker model;
        private Label message;
        private Label shadow;
        private Trajector size;
        private int phase;
        private double progress;

        public MessageSticker(Canvas canvas, Rect bound, int zIndex, double speed,
                              double fontSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.MessageSticker)model;

            this.shadow = new Label(this, 5,
                                     this.bound.X + this.bound.Width / 2.0 + this.fontSize * 0.1,
                                     this.bound.Y + this.bound.Height / 2.0 + this.fontSize * 0.1);
            this.shadow.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.shadow.Elem.FontSize = 1.0;
            this.shadow.Elem.Text = this.model.Message;
            var col = Colors.Black;
            col.A = 196;
            this.shadow.Elem.Foreground = new SolidColorBrush(col);

            this.message = new Label(this, 5,
                                     this.bound.X + this.bound.Width / 2.0,
                                     this.bound.Y + this.bound.Height / 2.0);
            this.message.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.message.Elem.FontSize = 1.0;
            this.message.Elem.Text = this.model.Message;
            this.message.Elem.Foreground = new SolidColorBrush(Colors.PaleGreen);

            this.size = new Trajector(this, 1.0);

            this.phase = 0;
            this.progress = 0.0;
        }

        public override void Unlink()
        {
            this.message.Destroy();
            this.shadow.Destroy();
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
                case 1:
                case 2:
                    double[] targetSizes = {
                                               this.fontSize,
                                               this.fontSize * 0.85,
                                               this.fontSize
                                           };
                    this.size.Fall(targetSizes[this.phase]);
                    this.message.Elem.FontSize = this.size.Value;
                    this.shadow.Elem.FontSize = this.size.Value;
                    if (this.size.Value == targetSizes[this.phase])
                        this.phase++;
                    break;

                case 3:
                    this.progress += 0.03 * this.speed;
                    if (this.progress >= 1.0)
                    {
                        this.progress = 0;
                        this.phase = 4;
                    }
                    break;

                case 4:
                    return;
            }

            this.message.Animate();
            this.shadow.Animate();
        }
    }

    class ScoreSticker : Object
    {
        private double fontSize;
        private Size blockSize;

        private Core.ScoreSticker model;
        private Label score;
        private int phase;
        private double progress;
        private double blink;

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

            this.score = new Label(this, 5,
                                   this.bound.X + this.model.Col * this.blockSize.Width + this.blockSize.Width / 2.0,
                                   this.bound.Y + this.model.Row * this.blockSize.Height + this.blockSize.Height / 2.0);
            this.score.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.score.Elem.FontSize = this.fontSize * (1.0 + 0.2 * (this.model.Count - 3));
            this.score.Elem.Text = "0";

            this.phase = 0;
            this.progress = 0.0;
            this.blink = 0.0;
        }

        public override void Unlink()
        {
            this.score.Destroy();
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
                    this.score.Elem.Text = String.Format("{0:N0}", (int)(this.model.Score * this.progress));
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

            Color[] col = { Colors.White, Colors.Gray, Colors.Black };
            this.score.Elem.Foreground = new SolidColorBrush(col[(int)this.blink % 3]);
            this.blink += 0.5 * this.speed;

            this.score.Animate();
        }
    }

    class Combo : Object
    {
        private double fontSize;

        private Core.Combo model;
        private Label count;
        private Label bonus;
        private Rectangle bar;
        private Trajector guage;
        private double blink;
        private bool showBonus;

        public Combo(Canvas canvas, Rect bound, int zIndex, double speed,
                     double fontSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Combo)model;

            this.bar = new Rectangle();
            this.bar.RadiusX = this.fontSize * 0.2;
            this.bar.RadiusY = this.fontSize * 0.2;
            this.bar.Width = 0;
            this.bar.Height = this.fontSize * 0.8;
            Canvas.SetLeft(this.bar, this.bound.X);
            Canvas.SetTop(this.bar, this.bound.Y + (this.bound.Height - this.fontSize) / 2.0);
            Canvas.SetZIndex(this.bar, this.zIndex);
            this.canvas.Children.Add(this.bar);

            this.count = new Label(this, 8,
                                   this.bound.X + this.bound.Width / 2.0,
                                   this.bound.Y);
            this.count.Elem.Text = "COMBO";
            this.count.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.count.Elem.FontSize = this.fontSize;

            this.bonus = new Label(this, 2,
                                   this.bound.X + this.bound.Width / 2.0,
                                   this.bound.Bottom);
            this.bonus.Elem.Text = "Bonus";
            this.bonus.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.bonus.Elem.FontSize = this.fontSize * 0.9;

            this.guage = new Trajector(this);
            this.blink = 0.0;
            this.showBonus = false;            
        }

        public override void Unlink()
        {
            this.count.Destroy();
            this.bonus.Destroy();
            this.canvas.Children.Remove(this.bar);
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
            if (this.model.Guage >= 1.0 && this.model.Bonus > 0.0)
                this.showBonus = true;
            if (this.model.Elapse || this.model.Bonus <= 0.0)
                this.showBonus = false;

            if (this.model.Count > 0)
            {
                this.count.Elem.Foreground = new SolidColorBrush(Colors.Ivory);
                if (this.model.Count > 1)
                    this.count.Elem.Text = String.Format("{0} COMBOs!!", this.model.Count);
                else
                    this.count.Elem.Text = "COMBO!!";
                this.count.Elem.Foreground = new SolidColorBrush(Colors.Ivory);
                this.count.Elem.FontSize = this.fontSize * (1.0 + (Process.RandGen.NextDouble() - 0.5) * 0.1);
                this.blink += 0.5 * this.speed;
            } else
            {
                this.count.Elem.Foreground = null;
                this.bonus.Elem.Foreground = null;
            }

            if (this.model.Guage > 0.0)
            {
                this.bar.Fill = new SolidColorBrush(Colors.DimGray);
                this.bar.Width = this.guage.Glide(this.model.Guage) * this.bound.Width;

                if (this.showBonus)
                {
                    this.bonus.Elem.Text = String.Format("Bonus +{0}%", this.model.Bonus * 100.0);
                    Color[] col = { Colors.DarkRed, Colors.LightPink, Colors.Yellow, Colors.LightPink };
                    this.bonus.Elem.Foreground = new SolidColorBrush(col[(int)this.blink % 4]);
                }
                else
                {
                    this.bonus.Elem.Foreground = null;
                }
            }
            else
            {
                this.guage.Reset(0.0);
                this.bar.Fill = null;
                this.bar.Stroke = null;
            }

            this.count.Animate();
            this.bonus.Animate();
        }
    }

    class Next : Object
    {
        private double fontSize;

        private Core.Next model;
        private Label label;

        public Next(Canvas canvas, Rect bound, int zIndex, double speed,
                     double fontSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Next)model;

            this.label = new Label(this, 4,
                                   this.bound.X,
                                   this.bound.Y + this.bound.Height / 2.0);
            this.label.Elem.Text = "NEXT";
            this.label.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.label.Elem.FontSize = this.fontSize;
            this.label.Elem.Foreground = new SolidColorBrush(Colors.Ivory);
        }

        public override void Unlink()
        {
            this.label.Destroy();
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
            this.label.Animate();
        }
    }

    class Record : Object
    {
        private double fontSize;
        private double scoreBase;

        private Core.Record model;
        private Label scoreLabel;
        private Label bustLabel;
        private Label moveLabel;
        private Label comboLabel;
        private Label scoreNum;
        private Label bustNum;
        private Label moveNum;
        private Label comboNum;
        private Trajector score;
        private int bustCount;

        public Record(Canvas canvas, Rect bound, int zIndex, double speed,
                     double fontSize, int scoreBase)
            : base(canvas, bound, zIndex, speed)
        {
            this.fontSize = fontSize;
            this.scoreBase = (double)scoreBase;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Record)model;

            this.scoreLabel = new Label(this, 8,
                                        this.bound.X + this.bound.Width / 2.0,
                                        this.bound.Y);
            this.scoreLabel.Elem.Text = "SCORE";
            this.scoreLabel.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.scoreLabel.Elem.FontSize = this.fontSize * 0.5;
            this.scoreLabel.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.bustLabel = new Label(this, 8,
                                       this.bound.X + this.fontSize * 3.0,
                                       this.bound.Y);
            this.bustLabel.Elem.Text = "BUSTED BLOCKS";
            this.bustLabel.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.bustLabel.Elem.FontSize = this.fontSize * 0.3;
            this.bustLabel.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.moveLabel = new Label(this, 8,
                                       this.bound.Right - this.fontSize * 4.0,
                                       this.bound.Y);
            this.moveLabel.Elem.Text = "TOTAL MOVES";
            this.moveLabel.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.moveLabel.Elem.FontSize = this.fontSize * 0.3;
            this.moveLabel.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.comboLabel = new Label(this, 8,
                                        this.bound.Right - this.fontSize,
                                        this.bound.Y);
            this.comboLabel.Elem.Text = "MAX COMBO";
            this.comboLabel.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.comboLabel.Elem.FontSize = this.fontSize * 0.3;
            this.comboLabel.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.scoreNum = new Label(this, 2,
                                        this.bound.X + this.bound.Width / 2.0,
                                        this.bound.Bottom);
            this.scoreNum.Elem.Text = "0";
            this.scoreNum.Elem.FontFamily = new FontFamily("OCR A Std");
            this.scoreNum.Elem.FontSize = this.fontSize;
            this.scoreNum.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.bustNum = new Label(this, 2,
                                       this.bound.X + this.fontSize * 3.0,
                                       this.bound.Bottom);
            this.bustNum.Elem.Text = "0";
            this.bustNum.Elem.FontFamily = new FontFamily("OCR A Std");
            this.bustNum.Elem.FontSize = this.fontSize * 0.7;
            this.bustNum.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.moveNum = new Label(this, 2,
                                     this.bound.Right - this.fontSize * 4.0,
                                     this.bound.Bottom);
            this.moveNum.Elem.Text = "0";
            this.moveNum.Elem.FontFamily = new FontFamily("OCR A Std");
            this.moveNum.Elem.FontSize = this.fontSize * 0.7;
            this.moveNum.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.comboNum = new Label(this, 2,
                                      this.bound.Right - this.fontSize,
                                      this.bound.Bottom);
            this.comboNum.Elem.Text = "0";
            this.comboNum.Elem.FontFamily = new FontFamily("OCR A Std");
            this.comboNum.Elem.FontSize = this.fontSize * 0.7;
            this.comboNum.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.score = new Trajector(this);
            this.bustCount = 0;
        }

        public override void Unlink()
        {
            this.scoreLabel.Destroy();
            this.bustLabel.Destroy();
            this.moveLabel.Destroy();
            this.comboLabel.Destroy();
            this.scoreNum.Destroy();
            this.bustNum.Destroy();
            this.moveNum.Destroy();
            this.comboNum.Destroy();
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
            this.score.Glide((double)this.model.Score / this.scoreBase);
            this.scoreNum.Elem.Text = String.Format("{0:N0}", (int)(this.score.Value * this.scoreBase));

            if (this.bustCount < this.model.BustCount)
                this.bustCount++;
            this.bustNum.Elem.Text = String.Format("{0}", this.bustCount);

            this.moveNum.Elem.Text = String.Format("{0}", this.model.MoveCount);

            this.comboNum.Elem.Text = String.Format("{0}", this.model.MaxCombo);

            this.scoreLabel.Animate();
            this.bustLabel.Animate();
            this.moveLabel.Animate();
            this.comboLabel.Animate();
            this.scoreNum.Animate();
            this.bustNum.Animate();
            this.moveNum.Animate();
            this.comboNum.Animate();
        }
    }

    class Time : Object
    {
        private Rect inBound;
        private double fontSize;

        private Core.Time model;
        private TextBlock label;
        private Rectangle bar;
        private Rectangle barShade;
        private Trajector remain;
        private int phase;
        private double progress;

        public Time(Canvas canvas, Rect bound, int zIndex, double speed,
                     Rect inBound, double fontSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.inBound = inBound;
            this.fontSize = fontSize;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Time)model;

            this.label = new TextBlock();
            this.label.Text = "TIME";
            this.label.FontFamily = new FontFamily("Bauhaus 93");
            this.label.FontSize = this.fontSize;
            this.label.Foreground = new SolidColorBrush(Colors.Ivory);
            Canvas.SetLeft(this.label, this.bound.X);
            Canvas.SetTop(this.label, this.bound.Y + (this.bound.Height - this.fontSize) / 2.0);
            Canvas.SetZIndex(this.label, this.zIndex);

            this.bar = new Rectangle();
            this.bar.RadiusX = this.fontSize * 0.4;
            this.bar.RadiusY = this.fontSize * 0.4;
            this.bar.Width = 0;
            this.bar.Height = this.fontSize * 0.5;
            this.bar.Fill = new SolidColorBrush(Colors.DeepPink);
            this.bar.Stroke = new SolidColorBrush(Colors.DarkRed);
            this.bar.StrokeThickness = this.fontSize * 0.05;
            Canvas.SetLeft(this.bar, this.inBound.X);
            Canvas.SetTop(this.bar, this.inBound.Y + (this.inBound.Height - this.fontSize / 2.0) / 2.0);
            Canvas.SetZIndex(this.bar, this.zIndex);

            this.barShade = new Rectangle();
            this.barShade.RadiusX = this.fontSize * 0.4;
            this.barShade.RadiusY = this.fontSize * 0.4;
            this.barShade.Width = 0;
            this.barShade.Height = this.fontSize * 0.5;
            Canvas.SetLeft(this.barShade, this.inBound.X);
            Canvas.SetTop(this.barShade, this.inBound.Y + (this.inBound.Height - this.fontSize / 2.0) / 2.0);
            Canvas.SetZIndex(this.barShade, this.zIndex);

            this.canvas.Children.Add(this.label);
            this.canvas.Children.Add(this.bar);
            this.canvas.Children.Add(this.barShade);

            this.remain = new Trajector(this);
            this.phase = 0;
            this.progress = 0.0;
        }

        public override void Unlink()
        {
            this.canvas.Children.Remove(this.label);
            this.canvas.Children.Remove(this.bar);
            this.canvas.Children.Remove(this.barShade);
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
            var col = Colors.White;

            switch (this.phase)
            {
                case 0:
                    var increment = Math.Max(0.005,
                                            (1.0 - this.model.Remain) * 0.05);
                    this.progress += increment * this.speed;
                    if (this.progress >= 1.0)
                    {
                        this.progress = 0.0;
                        this.phase = 1;
                    }
                    col.A = 0;
                    break;

                case 1:
                    this.progress += 0.1 * this.speed;
                    if (this.progress >= 1.0)
                    {
                        this.progress = 1.0;
                        this.phase = 2;
                    }
                    col.A = (Byte)(128.0 * this.progress);
                    break;

                case 2:
                    this.progress -= 0.1 * this.speed;
                    if (this.progress <= 0.0)
                    {
                        this.progress = 0.0;
                        this.phase = 0;
                    }
                    col.A = (Byte)(128.0 * this.progress);
                    break;
            }

            if (this.model.Remain > 0.0)
            {
                this.bar.Width = this.remain.Glide(this.model.Remain) * this.inBound.Width;
                this.barShade.Width = this.remain.Glide(this.model.Remain) * this.inBound.Width;
                this.barShade.Fill = new SolidColorBrush(col);
            }
            else
            {
                this.bar.Fill = null;
                this.bar.Stroke = null;
                this.barShade.Fill = null;
            }
        }
    }

    class Moves : Object
    {
        private Rect inBound;
        private double fontSize;

        private Core.Moves model;
        private Label label;
        private Label count;
        private Label countShade;
        private int phase;
        private double progress;

        public Moves(Canvas canvas, Rect bound, int zIndex, double speed,
                     Rect inBound, double fontSize)
            : base(canvas, bound, zIndex, speed)
        {
            this.inBound = inBound;
            this.fontSize = fontSize;
        }

        public override void Link(Core.Object model)
        {
            this.model = (Core.Moves)model;

            this.label = new Label(this, 4,
                                   this.bound.X,
                                   this.bound.Y + this.bound.Height / 2.0);
            this.label.Elem.Text = "MOVES";
            this.label.Elem.FontFamily = new FontFamily("Bauhaus 93");
            this.label.Elem.FontSize = this.fontSize;
            this.label.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.count = new Label(this, 5,
                                   this.inBound.X + this.inBound.Width / 2.0,
                                   this.inBound.Y + this.inBound.Height / 2.0);
            this.count.Elem.FontFamily = new FontFamily("OCR A Std");
            this.count.Elem.FontSize = this.fontSize * 1.3;
            this.count.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.countShade = new Label(this, 5,
                                        this.inBound.X + this.inBound.Width / 2.0,
                                        this.inBound.Y + this.inBound.Height / 2.0);
            this.countShade.Elem.FontFamily = new FontFamily("OCR A Std");
            this.countShade.Elem.FontSize = this.fontSize * 1.3;
            this.countShade.Elem.Foreground = new SolidColorBrush(Colors.Ivory);

            this.phase = 0;
            this.progress = 0.0;
        }

        public override void Unlink()
        {
            this.label.Destroy();
            this.count.Destroy();
            this.countShade.Destroy();
        }

        public override bool IsBusy()
        {
            return false;
        }

        public override void Animate()
        {
            var col = Colors.Red;

            switch (this.phase)
            {
                case 0:
                    var increment = Math.Max(0.005,
                                            (double)(30 - this.model.Remain) * 0.001);
                    this.progress += increment * this.speed;
                    if (this.progress >= 1.0)
                    {
                        this.progress = 0.0;
                        this.phase = 1;
                    }
                    col.A = 0;
                    break;

                case 1:
                    this.progress += 0.1 * this.speed;
                    if (this.progress >= 1.0)
                    {
                        this.progress = 1.0;
                        this.phase = 2;
                    }
                    col.A = (Byte)(128.0 * this.progress);
                    break;

                case 2:
                    this.progress -= 0.1 * this.speed;
                    if (this.progress <= 0.0)
                    {
                        this.progress = 0.0;
                        this.phase = 0;
                    }
                    col.A = (Byte)(128.0 * this.progress);
                    break;
            }

            this.count.Elem.Text = String.Format("{0}", this.model.Remain);
            this.countShade.Elem.Text = String.Format("{0}", this.model.Remain);

            if (this.model.Remain > 0)
            {
                this.count.Elem.Foreground = new SolidColorBrush(Colors.Ivory);
                this.countShade.Elem.Foreground = new SolidColorBrush(col);
            }
            else
            {
                this.count.Elem.Foreground = new SolidColorBrush(Colors.DimGray);
                this.countShade.Elem.Foreground = null;
            }

            this.label.Animate();
            this.count.Animate();
            this.countShade.Animate();
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
            this.rim.Width = this.model.ColSpan * this.blockSize.Width + this.blockSize.Width * 0.4;
            this.rim.Height = this.model.RowSpan * this.blockSize.Height + this.blockSize.Height * 0.4;
            Canvas.SetLeft(this.rim, this.model.Col * this.blockSize.Width + this.bound.X - this.blockSize.Width * 0.2);
            Canvas.SetTop(this.rim, this.model.Row * this.blockSize.Height + this.bound.Y - this.blockSize.Height * 0.2);
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
        private int toughness;

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

            double strokeThickness;

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
                strokeThickness = Math.Min(size.Width, size.Height) * 0.02;
            }
            else
            {
                this.fillColor = Colors.Gray;
                this.strokeColor = Colors.Black;
                strokeThickness = Math.Min(size.Width, size.Height) * 0.1;                
            }

            this.frame = new Rectangle();
            this.frame.StrokeThickness = strokeThickness;
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
                this.X = this.X;
            }
        }
        public double Height
        {
            get { return this.height; }
            set
            {
                this.height = value;
                this.frame.Height = this.height;
                this.Y = this.Y;
            }
        }
        public int Toughness
        {
            get { return this.toughness; }
            set 
            {
                this.toughness = value;
                this.mark.Text = String.Format("{0}", this.toughness);
                if (this.toughness > 1)
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
        private Trajector blockX;
        private Trajector blockY;

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

            this.blockX = new Trajector(this, this.iceBlock.X);
            this.blockY = new Trajector(this, this.iceBlock.Y);
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
                        this.iceBlock.Toughness = this.model.Toughness;
                        this.state = this.model.State;
                        break;

                    case Core.Block.States.Moved:
                        this.iceBlock.Toughness = this.model.Toughness;
                        double x = this.model.Col * this.size.Width + this.size.Width / 2.0;
                        double y = this.model.Row * this.size.Height + this.size.Height / 2.0;
                        this.iceBlock.X = this.blockX.Fall(x);
                        this.iceBlock.Y = this.blockY.Fall(y);
                        if (this.iceBlock.X == x && this.iceBlock.Y == y)
                            this.state = this.model.State;
                        break;

                    case Core.Block.States.Busted:
                        this.progress += 0.05 * this.speed;
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
        private Trajector blockX;

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
            this.iceBlock.X = this.bound.Width * (double)this.model.Rank / (double)this.model.Total;
            this.iceBlock.Y = this.bound.Height / 2.0;
            this.iceBlock.Width = this.size.Width;
            this.iceBlock.Height = this.size.Height;
            this.iceBlock.Toughness = this.model.Toughness;
            this.iceBlock.Register(this.canvas);

            this.blockX = new Trajector(this, this.iceBlock.X);
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
            double x = this.bound.Width * (double)this.model.Rank / (double)this.model.Total;
            this.iceBlock.X = this.blockX.Fall(x);

            this.iceBlock.Animate();
        }
    }

}