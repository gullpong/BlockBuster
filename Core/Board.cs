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

        private Block NewBlock(BlockSeed blockSeed, int row, int col)
        {
            // Create at the initial position based on the seed info.
            var block = new Block(this.pool,
                                  blockSeed.BlockAnimation, blockSeed.Type, blockSeed.Color, blockSeed.Toughness,
                                  row, col);
            this.Depend(block);
            this.Blocks.Add(block);
            block.Release += b =>
            {
                this.Blocks.Remove((Block)b);
            };
            return block;
        }

        public bool Slide(int dir)
        {
            var view = this.BuildView();
            bool slid = false;
            var dpos = new int[Math.Max(this.Rows, this.Cols)];
            var cpos = new int[Math.Max(this.Rows, this.Cols)];
            switch (dir)
            {
                case 8:
                    for (var col = 0; col < this.Cols; col++)
                        dpos[col] = cpos[col] = 0;
                    while (true)
                    {
                        var stop = true;
                        for (var col = 0; col < this.Cols; col++)
                        {
                            if (dpos[col] >= this.Rows)
                                continue;
                            stop = false;
                            Block block;
                            if (cpos[col] < this.Rows)
                                block = view[cpos[col], col];
                            else
                                block = this.NewBlock(this.next.Pop(), cpos[col], col);
                            if (block != null)
                            {
                                if (block.Row != dpos[col])
                                {
                                    if (cpos[col] < this.Rows)
                                        view[block.Row, block.Col] = null;
                                    block.Move(dpos[col], col);
                                    view[block.Row, block.Col] = block;
                                    slid = true;
                                }
                                dpos[col]++;
                            }
                            cpos[col]++;
                        }
                        if (stop)
                            break;
                    }
                    break;

                case 2:
                    for (var col = 0; col < this.Cols; col++)
                        dpos[col] = cpos[col] = this.Rows - 1;
                    while (true)
                    {
                        var stop = true;
                        for (var col = this.Cols - 1; col >= 0; col--)
                        {
                            if (dpos[col] < 0)
                                continue;
                            stop = false;
                            Block block;
                            if (cpos[col] >= 0)
                                block = view[cpos[col], col];
                            else
                                block = this.NewBlock(this.next.Pop(), cpos[col], col);
                            if (block != null)
                            {
                                if (block.Row != dpos[col])
                                {
                                    if (cpos[col] >= 0)
                                        view[block.Row, block.Col] = null;
                                    block.Move(dpos[col], col);
                                    view[block.Row, block.Col] = block;
                                    slid = true;
                                }
                                dpos[col]--;
                            }
                            cpos[col]--;
                        }
                        if (stop)
                            break;
                    }
                    break;

                case 4:
                    for (var row = 0; row < this.Rows; row++)
                        dpos[row] = cpos[row] = 0;
                    while (true)
                    {
                        var stop = true;
                        for (var row = this.Rows - 1; row >= 0; row--)
                        {
                            if (dpos[row] >= this.Cols)
                                continue;
                            stop = false;
                            Block block;
                            if (cpos[row] < this.Cols)
                                block = view[row, cpos[row]];
                            else
                                block = this.NewBlock(this.next.Pop(), row, cpos[row]);
                            if (block != null)
                            {
                                if (block.Col != dpos[row])
                                {
                                    if (cpos[row] < this.Cols)
                                        view[block.Row, block.Col] = null;
                                    block.Move(row, dpos[row]);
                                    view[block.Row, block.Col] = block;
                                    slid = true;
                                }
                                dpos[row]++;
                            }
                            cpos[row]++;
                        }
                        if (stop)
                            break;
                    }
                    break;

                case 6:
                    for (var row = 0; row < this.Rows; row++)
                        dpos[row] = cpos[row] = this.Cols - 1;
                    while (true)
                    {
                        var stop = true;
                        for (var row = 0; row < this.Rows; row++)
                        {
                            if (dpos[row] < 0)
                                continue;
                            stop = false;
                            Block block;
                            if (cpos[row] >= 0)
                                block = view[row, cpos[row]];
                            else
                                block = this.NewBlock(this.next.Pop(), row, cpos[row]);
                            if (block != null)
                            {
                                if (block.Col != dpos[row])
                                {
                                    if (cpos[row] >= 0)
                                        view[block.Row, block.Col] = null;
                                    block.Move(row, dpos[row]);
                                    view[block.Row, block.Col] = block;
                                    slid = true;
                                }
                                dpos[row]--;
                            }
                            cpos[row]--;
                        }
                        if (stop)
                            break;
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

