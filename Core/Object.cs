using System;
using System.Collections.Generic;

namespace BlockBuster.Core
{
    using BustGroup = Tuple<List<Block>, List<Block>>;

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

    public class MessageSticker : Sticker
    {
        public string Message { get; private set; }

        public MessageSticker(ObjectPool pool, IObjectAnimation animation, 
                              string message)
            : base(pool)
        {
            this.Message = message;

            this.Animate(animation);
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

    public class Record : Object
    {
        private int scoreBase;

        public int Score { get; private set; }
        public int BustCount { get; private set; }
        public int MoveCount { get; private set; }
        public int MaxCombo { get; private set; }

        public Record(ObjectPool pool, IObjectAnimation animation, int scoreBase)
            : base(pool)
        {
            this.scoreBase = scoreBase;

            this.Score = 0;
            this.BustCount = 0;
            this.MoveCount = 0;
            this.MaxCombo = 0;

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

        public void ComboCount(int comboCount)
        {
            if (this.MaxCombo < comboCount)
                this.MaxCombo = comboCount;
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

    public class Time : Object
    {
        private double decrement;

        public double Remain { get; private set; }
        public bool Elapse { get; set; }

        public Time(ObjectPool pool, IObjectAnimation animation,
                    double decrement)
            : base(pool)
        {
            this.decrement = decrement;

            this.Remain = 1.0;
            this.Elapse = false;

            this.Animate(animation);
        }

        public override bool Do()
        {
            if (!base.Do())
                return false;
            if (!this.Elapse)
                return true;
            if (this.Remain > 0.0)
                this.Remain -= this.decrement;
            else
                this.Remain = 0.0;
            return true;
        }
    }

    public class Moves : Object
    {
        public double Remain { get; private set; }

        public Moves(ObjectPool pool, IObjectAnimation animation,
                     int remain)
            : base(pool)
        {
            this.Remain = remain;

            this.Animate(animation);
        }

        public void Decrese()
        {
            if (this.Remain > 0)
                this.Remain--;
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
