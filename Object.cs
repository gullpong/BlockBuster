using System;
using System.Collections.Generic;

namespace BlockBuster.Core
{
    using BustGroup = Tuple<List<Block>, List<Block>>;

    public class ObjectPool
    {
        public List<Object> Objects { get; private set; }

        public ObjectPool()
        {
            this.Objects = new List<Object>();            
        }

        public void Reset()
        {
            while (this.Objects.Count > 0)
            {
                var obj = this.Objects[0];
                obj.Release(obj);
            }
        }

        public void Add(Object obj)
        {
            this.Objects.Add(obj);
            obj.Release += o =>
            {
                this.Objects.Remove(o);
            };
        }

        public void Do()
        {
            var todoObjs = new Queue<Object>(this.Objects);
            var doneObjs = new List<Object>();
            var deadObjs = new List<Object>();
            while (todoObjs.Count > 0)
            {
                var obj = todoObjs.Dequeue();
                bool allDone = true;
                foreach (var dep in obj.Dependents)
                {
                    if (!doneObjs.Contains(dep))
                    {
                        allDone = false;
                        break;
                    }
                }
                if (allDone)
                {
                    if (!obj.Do())
                        deadObjs.Add(obj);
                    doneObjs.Add(obj);
                }
                else
                {
                    todoObjs.Enqueue(obj);
                }
            }
            foreach (var obj in deadObjs)
                obj.Release(obj);
        }
    }

    public interface IObjectAnimation
    {
        bool IsBusy();

        void Link(Object model);
        void Unlink();
        void Animate();
    }

    public class Object
    {
        protected ObjectPool pool;
        protected IObjectAnimation animation;

        public List<Object> Dependents { get; protected set; }
        public Action<Object> Release { get; set; }

        public Object(ObjectPool pool) 
        { 
            this.Dependents = new List<Object>();
            this.pool = pool;
            this.pool.Add(this);
        }

        public void Animate(IObjectAnimation animation)
        {
            this.animation = animation;
            this.animation.Link(this);
            this.Release += o =>
            {
                this.animation.Unlink();
            };
        }

        public void Depend(Object obj)
        {
            this.Dependents.Add(obj);
            obj.Release += o =>
            {
                this.Dependents.Remove(obj);
            };
        }

        public virtual bool IsReady()
        {
            return true;
        }

        public virtual bool Do()
        {
            if (this.animation != null)
                this.animation.Animate();

            // Returning 'false' would remove this Object.
            return true;
        }
    }

    public class Sticker : Object
    {
        public Sticker(ObjectPool pool)
            : base(pool)
        {
        }

        public override bool Do()
        {
            if (!base.Do())
                return false;
            return this.animation.IsBusy();
        }
    }

    public class ScoreSticker : Sticker
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public int Score { get; private set; }
        public int Count { get; private set; }

        public ScoreSticker(ObjectPool pool, IObjectAnimation animation, 
                            int row, int col, int score, int count)
            : base(pool)
        {
            this.Row = row;
            this.Col = col;
            this.Score = score;
            this.Count = count;

            this.Animate(animation);
        }
    }

    public class ComboSticker : Sticker
    {
        public int Count { get; private set; }
        public double Bonus { get; private set; }

        public ComboSticker(ObjectPool pool, IObjectAnimation animation,
                            int count, double bonus)
            : base(pool)
        {
            this.Count = count;
            this.Bonus = bonus;

            this.Animate(animation);
        }
    }

    public class Record : Object
    {
        private int scoreBase;

        public int Score { get; private set; }
        public int BustCount { get; private set; }
        public int MoveCount { get; private set; }

        public Record(ObjectPool pool, IObjectAnimation animation, int scoreBase)
            : base(pool)
        {
            this.scoreBase = scoreBase;

            this.Score = 0;
            this.BustCount = 0;
            this.MoveCount = 0;

            this.Animate(animation);
        }

        public int AddBustScore(BustGroup bustGroup, double comboBonus)
        {
            var score = bustGroup.Item1.Count * bustGroup.Item1.Count * this.scoreBase;
            score += (int)((double)score * comboBonus);

            this.Score += score;
            this.BustCount += bustGroup.Item1.Count;

            return score;
        }

        public void AddMove()
        {
            this.MoveCount++;
        }
    }

    public class Combo : Object
    {
        private double decrement;
        private double bonusCoeff;

        public int Count { get; private set; }
        public double Guage { get; private set; }
        public bool Elapse { get; set; }
        public double Bonus 
        {
            get { return this.Count * this.bonusCoeff; } 
        }

        public Combo(ObjectPool pool, IObjectAnimation animation, 
                     double decrement, double bonusCoeff)
            : base(pool)
        {
            this.decrement = decrement;
            this.bonusCoeff = bonusCoeff;

            this.Count = 0;
            this.Guage = 0.0;
            this.Elapse = false;

            this.Animate(animation);
        }

        public override bool Do()
        {
            if (!base.Do())
                return false;
            if (!this.Elapse)
                return true;
            if (this.Guage > 0.0)
            {
                this.Guage -= this.decrement;
                if (this.Guage <= 0.0)
                    this.Break();
            }
            return true;
        }

        public void Stack()
        {
            if (this.Guage > 0.0)
                this.Count++;
            this.Guage = 1.0;
        }

        public void Break()
        {
            this.Count = 0;
            this.Guage = 0.0;
        }
    }

    public class Focus : Object
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public int RowSpan { get; private set; }
        public int ColSpan { get; private set; }

        public Focus(ObjectPool pool, IObjectAnimation animation, 
                     int row, int col, int rowSpan, int colSpan)
            : base(pool)
        {
            this.Row = row;
            this.Col = col;
            this.RowSpan = rowSpan;
            this.ColSpan = colSpan;

            this.Animate(animation);
        }

        public bool Contains(int row, int col)
        {
            return (row >= this.Row && row <= this.Row + this.RowSpan &&
                    col >= this.Col && col <= this.Col + this.ColSpan);
        }

        public bool Contains(Block block)
        {
            return this.Contains(block.Row, block.Col);
        }
    }
}
