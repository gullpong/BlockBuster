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

        protected class Trajector
        {
            private Object obj;
            private double maxMomentum;
            private double momentum;

            public double Value { get; private set; }

            public Trajector(Object obj)
            {
                this.obj = obj;
                this.maxMomentum = Math.Max(this.obj.canvas.Width, this.obj.canvas.Height) * 0.10;
            }

            public Trajector(Object obj, double value)
                : this(obj)
            {
                this.Reset(value);
            }

            public void Reset(double value)
            {
                this.Value = value;
                this.momentum = 0.0;
            }

            public double Glide(double target)
            {
                this.momentum = Math.Min(this.maxMomentum,
                                            Math.Exp(4.0 * Math.Abs(this.Value - target) / this.maxMomentum));
                if (this.Value < target)
                {
                    this.Value = Math.Min(this.Value + (momentum * this.obj.speed), target);
                }
                else if (this.Value > target)
                {
                    this.Value = Math.Max(this.Value - (momentum * this.obj.speed), target);
                }
                return this.Value;
            }

            public double Fall(double target)
            {
                if (this.momentum == 0.0)
                    this.momentum = this.maxMomentum * 0.02;
                this.momentum = Math.Min(this.maxMomentum,
                                            this.momentum * 1.15);
                if (this.Value < target)
                {
                    this.Value = Math.Min(this.Value + (momentum * this.obj.speed), target);
                }
                else if (this.Value > target)
                {
                    this.Value = Math.Max(this.Value - (momentum * this.obj.speed), target);
                }
                if (this.Value == target)
                    this.momentum = 0.0;
                return this.Value;
            }
        }

        protected class Label
        {
            private Object obj;
            private TextBlock elem;
            private int align;
            private double x;
            private double y;

            public TextBlock Elem { get { return elem; } }

            public Label(Object obj, int align, double x, double y)
            {
                this.obj = obj;
                this.align = align;
                this.x = x;
                this.y = y;

                this.elem = new TextBlock();
                this.obj.canvas.Children.Add(this.elem);
                Canvas.SetLeft(this.elem, this.obj.canvas.Width + 1.0);
                Canvas.SetTop(this.elem, this.obj.canvas.Height + 1.0);
                Canvas.SetZIndex(this.elem, this.obj.zIndex);
            }

            public void Destroy()
            {
                this.obj.canvas.Children.Remove(this.elem);
            }

            public void Animate()
            {
                this.elem.Measure(new Size(this.obj.canvas.Width, this.obj.canvas.Height));

                double x = this.x;
                double y = this.y;
                if (this.align == 1 || this.align == 4 || this.align == 7)
                    x = this.x;
                if (this.align == 2 || this.align == 5 || this.align == 8)
                    x = this.x - this.elem.ActualWidth / 2.0;
                if (this.align == 3 || this.align == 6 || this.align == 9)
                    x = this.x - this.elem.ActualWidth;

                if (this.align == 1 || this.align == 2 || this.align == 3)
                    y = this.y - this.elem.ActualHeight;
                if (this.align == 4 || this.align == 5 || this.align == 6)
                    y = this.y - this.elem.ActualHeight / 2.0;
                if (this.align == 7 || this.align == 8 || this.align == 9)
                    y = this.y;

                Canvas.SetLeft(this.elem, x);
                Canvas.SetTop(this.elem, y);
            }
        }
    }
}
