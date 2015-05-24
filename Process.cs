using System;
using System.Collections.Generic;
using Windows.System;

namespace BlockBuster.Core
{
    using BustGroup = Tuple<List<Block>, List<Block>>;
    using AnimationPair = Tuple<IObjectAnimation, IObjectAnimation>;

    // Process settings
    public struct ProcessSettings
    {
        public int gameMode;

        public int boardRows;
        public int boardCols;
        public int focusRow;
        public int focusCol;
        public int focusRowSpan;
        public int focusColSpan;

        public int blockColors;
        public int bubbleFreq;
        public int toughFreq;
        public int durabilityMax;
        public int bustThreshold;
        public int bustScoreBase;
        public double comboBonusCoeff;
        public double comboGuageDecrement;
    }

    public sealed class Process
    {
        // Global random generator
        public static Random RandGen { get; private set; }

        // Internal variables
        private ObjectPool pool;
        private ProcessSettings settings;
        private Func<Type, IObjectAnimation> createAnimation;
        private Record record;
        private Combo combo;
        private Focus focus;
        private Next next;
        private Board board;
        private Queue<BustGroup> bustGroups;

        public enum Phases
        {
            Ready,
            BlockSlide,
            BlockShuffle,
            BlockBust,
            NextCycle
        }
        public Phases Phase { get; private set; }
        public VirtualKey UserInput { get; set; }

        // Global Game Process Singleton
        private static Process _instance;
        public static Process Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Process();
                return _instance;
            }
        }
        private Process() 
        {
            // Initialize global random generator.
            Process.RandGen = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

            // Initialize object pool.
            this.pool = new ObjectPool();

            // Initialize process settings.
            this.settings = new ProcessSettings();
        }

        private AnimationPair CreateAnimationPair()
        {
            return new AnimationPair(this.createAnimation(typeof(BlockSeed)),
                                     this.createAnimation(typeof(Block)));
        }

        public void Initialize(ProcessSettings settings, Func<Type, IObjectAnimation> createAnimation)
        {
            // Reset object pool.
            this.pool.Reset();

            // Set process settings.
            this.settings = settings;

            // Set delegate for creating animations.
            this.createAnimation = createAnimation;

            // Create primary objects.
            this.record = new Record(this.pool, this.createAnimation(typeof(Record)),
                                     this.settings.bustScoreBase);

            this.combo = new Combo(this.pool, this.createAnimation(typeof(Combo)),
                                   this.settings.comboGuageDecrement,
                                   this.settings.comboBonusCoeff);

            this.focus = new Focus(this.pool, this.createAnimation(typeof(Focus)),
                                   this.settings.focusRow,
                                   this.settings.focusCol,
                                   this.settings.focusRowSpan,
                                   this.settings.focusColSpan);

            this.next = new Next(this.pool, this.createAnimation(typeof(Next)),
                                 this.CreateAnimationPair,
                                 this.settings.boardRows * this.settings.boardCols,
                                 this.settings.blockColors,
                                 this.settings.bubbleFreq,
                                 this.settings.toughFreq,
                                 this.settings.durabilityMax);

            this.board = new Board(this.pool, this.focus, this.next,
                                   this.settings.boardRows,
                                   this.settings.boardCols,
                                   this.settings.bustThreshold);

            this.Phase = Phases.NextCycle;
        }

        public void Do()
        {
            // Process object pool.
            this.pool.Do();

            // Process through phases.
            if (!this.board.IsReady())
                return;
            switch (this.Phase)
            {
                // --------------------------------------------------------------------
                case Phases.Ready:
                    switch (this.UserInput)
                    {
                        case VirtualKey.Left:
                            if (this.board.Slide(4))
                                this.Phase = Phases.BlockSlide;
                            break;

                        case VirtualKey.Right:
                            if (this.board.Slide(6))
                                this.Phase = Phases.BlockSlide;
                            break;

                        case VirtualKey.Up:
                            if (this.board.Slide(8))
                                this.Phase = Phases.BlockSlide;
                            break;

                        case VirtualKey.Down:
                            if (this.board.Slide(2))
                                this.Phase = Phases.BlockSlide;
                            break;

                        case VirtualKey.Space:
                            if (this.board.Shuffle())
                            {
                                this.combo.Break();
                                this.Phase = Phases.BlockShuffle;
                            }
                            break;
                    }
                    if (this.Phase != Phases.Ready)
                    {
                        this.record.AddMove();
                        this.combo.Elapse = false;
                    }
                    break;

                // --------------------------------------------------------------------
                case Phases.BlockSlide:
                    this.bustGroups = this.board.GroupBlocksToBust();
                    if (this.bustGroups.Count > 0)
                    {
                        this.combo.Stack();
                        /*
                        // Create Combo Sticker.
                        if (this.combo.Count > 0)
                            new ComboSticker(this.pool, this.createAnimation(typeof(ComboSticker)),
                                             this.combo.Count, this.combo.Bonus);
                         */
                    }
                    else
                        this.combo.Break();
                    this.Phase = Phases.BlockBust;
                    break;

                // --------------------------------------------------------------------
                case Phases.BlockShuffle:
                    this.bustGroups = this.board.GroupBlocksToBust();
                    this.Phase = Phases.BlockBust;
                    break;

                // --------------------------------------------------------------------
                case Phases.BlockBust:
                    if (this.bustGroups.Count > 0)
                    {
                        // Bust next group of blocks.
                        var bustGroup = this.bustGroups.Dequeue();
                        var score = this.record.AddBustScore(bustGroup, this.combo.Bonus);
                        this.board.Bust(bustGroup);

                        // Create Score Sticker.
                        var block = bustGroup.Item1[0];
                        new ScoreSticker(this.pool, this.createAnimation(typeof(ScoreSticker)),
                                         block.Row, block.Col, score, bustGroup.Item1.Count);
                    } else
                        this.Phase = Phases.NextCycle;
                    break;

                // --------------------------------------------------------------------
                case Phases.NextCycle:
                    this.next.Refill();
                    this.combo.Elapse = true;
                    this.UserInput = VirtualKey.None;
                    this.Phase = Phases.Ready;
                    break;
            }
        }
    }
}
