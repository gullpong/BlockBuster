using System;
using System.Collections.Generic;

namespace BlockBuster.Core
{
    using AnimationPair = Tuple<IObjectAnimation, IObjectAnimation>;

    public class Block : Object
    {
        public enum Types
        {
            Normal,
            Mud
        }
        public Types Type { get; protected set; }
        public int Color { get; protected set; }
        public int Toughness { get; protected set; }
        public int Row { get; private set; }
        public int Col { get; private set; }
        public enum States
        {
            Idle,
            Moved,
            Busted,
            Dead
        }
        public States State { get; private set; }

        public Block(ObjectPool pool, IObjectAnimation animation,
                     Types type, int color, int toughness, int row, int col)
            : base(pool)
        {
            this.Type = type;
            this.Color = color;
            this.Toughness = toughness;
            this.Row = row;
            this.Col = col;
            this.State = States.Idle;

            this.Animate(animation);
        }

        public override bool IsReady()
        {
            return this.State == States.Idle;
        }

        override public bool Do()
        {
            if (!base.Do())
                return false;
            if (this.animation.IsBusy())
                return true;

            switch (this.State)
            {
                case States.Idle:
                    break;

                case States.Moved:
                    this.State = States.Idle;
                    break;

                case States.Busted:
                    this.Toughness--;
                    if (this.Toughness <= 0)
                        this.State = States.Dead;
                    else
                        this.State = States.Idle;
                    break;

                case States.Dead:
                    return false;
            }
            return true;
        }

        public void Move(int row, int col)
        {
            this.Row = row;
            this.Col = col;
            this.State = States.Moved;
        }

        public void Bust()
        {
            this.State = States.Busted;
        }
    }

    public class BlockSeed : Object
    {
        public IObjectAnimation BlockAnimation { get; private set; }
        public Block.Types Type { get; private set; }
        public int Color { get; private set; }
        public int Toughness { get; private set; }
        public int Rank { get; private set; }
        public int Total { get; private set; }

        private bool alive;

        public BlockSeed(ObjectPool pool, IObjectAnimation animation,
                         IObjectAnimation blockAnimation, Block.Types type, int color, int toughness,
                         int rank, int total)
            : base(pool)
        {
            this.BlockAnimation = blockAnimation;
            this.Type = type;
            this.Color = color;
            this.Toughness = toughness;
            this.Rank = rank;
            this.Total = total;

            this.alive = true;

            this.Animate(animation);
        }

        public override bool Do()
        {
            if (!base.Do())
                return false;
            return this.alive;
        }

        public void Remove()
        {
            this.alive = false;                        
        }
    
        public void Reorder(int rank, int total)
        {
            this.Rank = rank;
            this.Total = total;
        }
    }

    class Next : Object
    {
        private Queue<BlockSeed> Q;
        private Func<AnimationPair> createAnimationPair;
        private int qSize;
        private int blockColors;
        private int mudFreq;
        private int toughFreq;
        private int toughMax;

        public Next(ObjectPool pool, IObjectAnimation animation,
                    Func<AnimationPair> createAnimationPair,
                    int qSize, int blockColors, int mudFreq, int toughFreq, int toughMax)
            : base(pool)
        {
            this.Q = new Queue<BlockSeed>();
            this.Release += o =>
            {
                while (this.Q.Count > 0)
                    this.Pop();
            };

            this.createAnimationPair = createAnimationPair;
            this.qSize = qSize;
            this.blockColors = blockColors;
            this.mudFreq = mudFreq;
            this.toughFreq = toughFreq;
            this.toughMax = toughMax;

            this.Animate(animation);
        }

        public BlockSeed Pop()
        {
            var blockSeed = this.Q.Dequeue();
            blockSeed.Remove();

            int i = 0;
            foreach (var _blockSeed in this.Q)
            {
                _blockSeed.Reorder(i, this.qSize);
                i++;
            }

            return blockSeed;
        }

        public void Refill()
        {
            while (this.Q.Count < this.qSize)
            {
                Block.Types type = Process.RandGen.Next() % 100 < this.mudFreq ?
                                   Block.Types.Mud : Block.Types.Normal;
                var color = Process.RandGen.Next() % this.blockColors;
                var toughness = type == Block.Types.Normal && Process.RandGen.Next() % 100 < this.toughFreq ?
                                Process.RandGen.Next() % this.toughMax + 1 : 1;
                var rank = this.Q.Count - 1 + this.qSize;
                var aniPair = this.createAnimationPair();
                var blockSeed = new BlockSeed(this.pool, aniPair.Item1, aniPair.Item2, type, color, toughness,
                                              rank, this.qSize);
                this.Depend(blockSeed);
                this.Q.Enqueue(blockSeed);
                blockSeed.Reorder(this.Q.Count - 1, this.qSize);
            }
        }
    }
}
