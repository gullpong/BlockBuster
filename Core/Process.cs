using System;
using System.Collections.Generic;
using Windows.System;

namespace BlockBuster.Core
{
    using BustGroup = Tuple<List<Block>, List<Block>>;
    using AnimationPair = Tuple<IObjectAnimation, IObjectAnimation>;

    public enum GameModes
    {
        Classic,
        TimeAttack
    }

    // Process settings
    public struct ProcessSettings
    {
        public int fps;
        public GameModes gameMode;

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
        public double comboTimeLimit;
        public double timeLimit;
        public int moveLimit;
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
        private Time time;
        private Moves moves;
        private Board board;
        private Queue<BustGroup> bustGroups;
        private Queue<string> messages;

        public enum Phases
        {
            CountDown,
            Ready,
            BlockSlide,
            BlockShuffle,
            BlockBust,
            NextCycle,
            GameOver
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
                                   1.0 / (double)this.settings.fps / this.settings.comboTimeLimit,
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

            if (this.settings.gameMode == GameModes.Classic)
            {
                this.moves = new Moves(this.pool, this.createAnimation(typeof(Moves)),
                                       this.settings.moveLimit);

                this.Phase = Phases.NextCycle;
            } else
            {
                this.time = new Time(this.pool, this.createAnimation(typeof(Time)),
                                     1.0 / (double)this.settings.fps / this.settings.timeLimit);

                this.Phase = Phases.CountDown;
                this.messages = new Queue<string>();
                this.messages.Enqueue("Ready");
                this.messages.Enqueue("3");
                this.messages.Enqueue("2");
                this.messages.Enqueue("1");
                this.messages.Enqueue("Go!");
            }

            this.board = new Board(this.pool, this.focus, this.next,
                                   this.settings.boardRows,
                                   this.settings.boardCols,
                                   this.settings.bustThreshold);
        }

        public void Do()
        {
            // Process object pool.
            this.pool.Do();

            // Wait until all messages are displayed.
            foreach (var obj in this.pool.Objects)
            {
                if (obj.GetType() == typeof(MessageSticker))
                    return;
            }

            // Process through phases.
            if (!this.board.IsReady())
                return;
            switch (this.Phase)
            {
                // --------------------------------------------------------------------
                case Phases.CountDown:
                    if (this.messages.Count > 0)
                    {
                        new MessageSticker(this.pool, this.createAnimation(typeof(MessageSticker)),
                                           this.messages.Dequeue());
                    }
                    else
                    {
                        this.time.Elapse = true;
                        this.Phase = Phases.NextCycle;
                    }
                    break;

                // --------------------------------------------------------------------
                case Phases.Ready:
                    if (this.settings.gameMode == GameModes.Classic)
                    {
                        if (this.moves.Remain <= 0)
                        {
                            this.Phase = Phases.GameOver;
                            this.combo.Break();
                            new MessageSticker(this.pool, this.createAnimation(typeof(MessageSticker)),
                                               "Out of Moves");
                            break;
                        }
                    } else
                    {
                        if (this.time.Remain <= 0.0)
                        {
                            this.Phase = Phases.GameOver;
                            this.combo.Break();
                            new MessageSticker(this.pool, this.createAnimation(typeof(MessageSticker)),
                                               "Time Over");
                            break;
                        }
                    }

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
                    if (this.settings.gameMode == GameModes.Classic)
                    {
                        if (this.Phase != Phases.Ready)
                        {
                            this.moves.Decrese();
                            this.record.AddMove();
                        }
                    }
                    else
                    {
                        if (this.Phase != Phases.Ready)
                        {
                            this.record.AddMove();
                            this.combo.Elapse = false;
                        }
                    }
                    break;

                // --------------------------------------------------------------------
                case Phases.BlockSlide:
                    this.bustGroups = this.board.GroupBlocksToBust();
                    if (this.bustGroups.Count > 0)
                    {
                        this.combo.Stack();
                        this.record.ComboCount(this.combo.Count);
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
                    if (this.settings.gameMode == GameModes.Classic)
                    {
                        this.next.Refill();
                        this.UserInput = VirtualKey.None;
                        this.Phase = Phases.Ready;
                    }
                    else
                    {
                        this.next.Refill();
                        this.combo.Elapse = true;
                        this.UserInput = VirtualKey.None;
                        this.Phase = Phases.Ready;
                    }
                    break;

                // --------------------------------------------------------------------
                case Phases.GameOver:
                    break;
            }
        }
    }
}
