using System;
using System.Collections.Generic;

namespace BlockBuster.Core
{
    using BustGroup = Tuple<List<Block>, List<Block>>;

    class Board : Object
    {
        private Focus focus;
        private Next next;
        private int bustThreshold;

        public List<Block> Blocks { get; private set; }
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        public Board(ObjectPool pool, 
                     Focus focus, Next next, int rows, int cols, int bustThreshold)
            : base(pool)
        {
            this.focus = focus;
            this.next = next;
            this.Blocks = new List<Block>();
            this.Rows = rows;
            this.Cols = cols;
            this.bustThreshold = bustThreshold;
        }

        private Block[,] BuildView()
        {
            var view = new Block[this.Rows, this.Cols];
            foreach (var block in this.Blocks)
            {
                if (block.Row < 0 || block.Row >= this.Rows ||
                    block.Col < 0 || block.Col >= this.Cols)
                    continue;
                if (view[block.Row, block.Col] != null)
                    throw new Exception(String.Format("Overlapping blocks found at ({0}, {1}).",
                                                      block.Row, block.Col));
                view[block.Row, block.Col] = block;
            }
            return view;
        }

        private void GroupNormalBlocks(Block[,] view, List<Block> blocks, int color, int row, int col)
        {
            var block = row >= 0 && col >= 0 && row < this.Rows && col < this.Cols ?
                        view[row, col] : null;
            if (block == null)
                return;
            if (!this.focus.Contains(row, col))
                return;
            if (block.Type != Block.Types.Normal)
                return;
            if (block.Color != color)
                return;
            view[row, col] = null;
            blocks.Add(block);
            GroupNormalBlocks(view, blocks, color, row - 1, col);
            GroupNormalBlocks(view, blocks, color, row + 1, col);
            GroupNormalBlocks(view, blocks, color, row, col - 1);
            GroupNormalBlocks(view, blocks, color, row, col + 1);
        }

        private void GroupMudBlocks(Block[,] view, List<Block> blocks, int row, int col)
        {
            var block = row >= 0 && col >= 0 && row < this.Rows && col < this.Cols ?
                        view[row, col] : null;
            if (block == null)
                return;
            if (block.Type != Block.Types.Mud)
                return;
            view[row, col] = null;
            blocks.Add(block);
        }

        public Queue<BustGroup> GroupBlocksToBust()
        {
            var view = this.BuildView();
            var bustGroups = new Queue<BustGroup>();
            foreach (var block in this.Blocks)
            {
                if (view[block.Row, block.Col] == null)
                    continue;
                if (!this.focus.Contains(block))
                    continue;
                if (block.Type != Block.Types.Normal)
                    continue;

                var normalBlocks = new List<Block>();
                this.GroupNormalBlocks(view, normalBlocks, block.Color, block.Row, block.Col);
                if (normalBlocks.Count >= this.bustThreshold)
                {
                    var mudBlocks = new List<Block>();
                    foreach (var nblock in normalBlocks)
                    {
                        this.GroupMudBlocks(view, mudBlocks, nblock.Row + 1, nblock.Col);
                        this.GroupMudBlocks(view, mudBlocks, nblock.Row, nblock.Col + 1);
                        this.GroupMudBlocks(view, mudBlocks, nblock.Row - 1, nblock.Col);
                        this.GroupMudBlocks(view, mudBlocks, nblock.Row, nblock.Col - 1);
                        /*
                        this.GroupMudBlocks(view, mudBlocks, nblock.Row + 1, nblock.Col + 1);
                        this.GroupMudBlocks(view, mudBlocks, nblock.Row + 1, nblock.Col - 1);
                        this.GroupMudBlocks(view, mudBlocks, nblock.Row - 1, nblock.Col + 1);
                        this.GroupMudBlocks(view, mudBlocks, nblock.Row - 1, nblock.Col - 1);
                        */
                    }
                    bustGroups.Enqueue(new BustGroup(normalBlocks, mudBlocks));
                }
            }
            return bustGroups;
        }

        public void Bust(BustGroup bustGroup)
        {
            foreach (var block in bustGroup.Item1)
                block.Bust();
            foreach (var block in bustGroup.Item2)
                block.Bust();        
        }

        private void DeployBlock(BlockSeed blockSeed, int initRow, int initCol, int row, int col)
        {
            // Create at the initial position based on the seed info.
            var block = new Block(this.pool,
                                  blockSeed.BlockAnimation, blockSeed.Type, blockSeed.Color, blockSeed.Toughness,
                                  initRow, initCol);
            this.Depend(block);
            this.Blocks.Add(block);
            block.Release += b =>
            {
                this.Blocks.Remove((Block)b);
            };

            // Move to the target position.
            block.Move(row, col);
        }

        public bool Slide(int dir)
        {
            var view = this.BuildView();
            bool slid = false;
            switch (dir)
            {
                case 8:
                    for (var col = 0; col < this.Cols; col++)
                    {
                        for (var row = 0; row < this.Rows; row++)
                        {
                            var block = view[row, col];
                            if (block == null)
                                continue;
                            for (var nrow = row - 1; nrow >= 0; nrow--)
                            {
                                if (view[nrow, col] != null)
                                    break;
                                view[block.Row, block.Col] = null;
                                block.Move(nrow, col);
                                view[block.Row, block.Col] = block;
                                slid = true;
                            }
                        }
                    }
                    for (var col = 0; col < this.Cols; col++)
                    {
                        for (var row = 0; row < this.Rows; row++)
                        {
                            if (view[row, col] == null)
                            {
                                this.DeployBlock(this.next.Pop(), row + this.Rows, col, row, col);
                                slid = true;
                            }
                        }
                    }
                    break;

                case 2:
                    for (var col = 0; col < this.Cols; col++)
                    {
                        for (var row = this.Rows - 1; row >= 0; row--)
                        {
                            var block = view[row, col];
                            if (block == null)
                                continue;
                            for (var nrow = row + 1; nrow < this.Rows; nrow++)
                            {
                                if (view[nrow, col] != null)
                                    break;
                                view[block.Row, block.Col] = null;
                                block.Move(nrow, col);
                                view[block.Row, block.Col] = block;
                                slid = true;
                            }
                        }
                    }
                    for (var col = 0; col < this.Cols; col++)
                    {
                        for (var row = this.Rows - 1; row >= 0; row--)
                        {
                            if (view[row, col] == null)
                            {
                                this.DeployBlock(this.next.Pop(), row - this.Rows, col, row, col);
                                slid = true;
                            }
                        }
                    }
                    break;

                case 4:
                    for (var row = 0; row < this.Rows; row++)
                    {
                        for (var col = 0; col < this.Cols; col++)
                        {
                            var block = view[row, col];
                            if (block == null)
                                continue;
                            for (var ncol = col - 1; ncol >= 0; ncol--)
                            {
                                if (view[row, ncol] != null)
                                    break;
                                view[block.Row, block.Col] = null;
                                block.Move(row, ncol);
                                view[block.Row, block.Col] = block;
                                slid = true;
                            }
                        }
                    }
                    for (var row = 0; row < this.Rows; row++)
                    {
                        for (var col = 0; col < this.Cols; col++)
                        {
                            if (view[row, col] == null)
                            {
                                this.DeployBlock(this.next.Pop(), row, col + this.Cols, row, col);
                                slid = true;
                            }
                        }
                    }
                    break;

                case 6:
                    for (var row = 0; row < this.Rows; row++)
                    {
                        for (var col = this.Cols - 1; col >= 0; col--)
                        {
                            var block = view[row, col];
                            if (block == null)
                                continue;
                            for (var ncol = col + 1; ncol < this.Cols; ncol++)
                            {
                                if (view[row, ncol] != null)
                                    break;
                                view[block.Row, block.Col] = null;
                                block.Move(row, ncol);
                                view[block.Row, block.Col] = block;
                                slid = true;
                            }
                        }
                    }
                    for (var row = 0; row < this.Rows; row++)
                    {
                        for (var col = this.Cols - 1; col >= 0; col--)
                        {
                            if (view[row, col] == null)
                            {
                                this.DeployBlock(this.next.Pop(), row, col - this.Cols, row, col);
                                slid = true;
                            }
                        }
                    }
                    break;
            }
            return slid;
        }

        public bool Shuffle()
        {
            var view = this.BuildView();
            bool shuffled = false;
            foreach (var block in this.Blocks)
            {
                if (!this.focus.Contains(block))
                    continue;
                while (true)
                {
                    int row = Process.RandGen.Next() % this.Rows;
                    int col = Process.RandGen.Next() % this.Cols;
                    if (!this.focus.Contains(row, col))
                        continue;
                    var rblock = view[row, col];
                    view[block.Row, block.Col] = rblock;
                    if (rblock != null)
                        rblock.Move(block.Row, block.Col);
                    view[row, col] = block;
                    block.Move(row, col);
                    shuffled = true;
                    break;
                }
            }
            return shuffled;
        }

        public override bool IsReady()
        {
            bool allReady = true;
            foreach (var block in this.Blocks)
            {
                if (!block.IsReady())
                {
                    allReady = false;
                    break;
                }
            }
            return allReady;
        }
    }
}

