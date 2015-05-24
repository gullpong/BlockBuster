using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.System;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Popups;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BlockBuster
{
    using Core;
    using Animation;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ProcessSettings settings;

        private double animateSpeed;
        private Size blockSize;
        private double fontSize;
        private double tabWidth;
        private double blockShineDelay;
        private Rect boardBound;
        private Rect comboBound;
        private Rect nextBound;
        private Rect recordBound;

        public MainPage()
        {
            this.InitializeComponent();

            this.settings = new ProcessSettings();
            this.settings.gameMode = 0;

            this.settings.boardRows = 5;
            this.settings.boardCols = 5;
            this.settings.focusRow = 0;
            this.settings.focusCol = 0;
            this.settings.focusRowSpan = 5;
            this.settings.focusColSpan = 5;
            this.settings.blockColors = 4;

            //this.settings.bubbleFreq = 10;
            this.settings.bubbleFreq = 0;
            this.settings.toughFreq = 10;
            this.settings.durabilityMax = 2;
            this.settings.bustThreshold = 3;
            this.settings.bustScoreBase = 100;
            this.settings.comboBonusCoeff = 0.2;
            this.settings.comboGuageDecrement = 0.01;

            this.animateSpeed = 1.0;
            this.blockSize = new Size(60.0, 60.0);
            this.fontSize = 50.0;
            this.tabWidth = 200;
            this.blockShineDelay = 20.0;
            this.boardBound = new Rect((this.GameView.Width - this.blockSize.Width * this.settings.boardCols) / 2.0,
                                       (this.GameView.Height - this.blockSize.Height * this.settings.boardRows) / 2.0 - this.blockSize.Height,
                                       this.blockSize.Width * this.settings.boardCols,
                                       this.blockSize.Height * this.settings.boardRows);
            this.comboBound = new Rect(this.boardBound.Left - this.tabWidth,
                                       this.boardBound.Top - this.fontSize * 2.0 - this.blockSize.Height,
                                       this.boardBound.Width + this.tabWidth,
                                       this.fontSize * 2.0);
            this.nextBound = new Rect(this.boardBound.Left - this.tabWidth,
                                      this.boardBound.Bottom + this.fontSize,
                                      this.boardBound.Width + this.tabWidth,
                                      this.blockSize.Height);
            this.recordBound = new Rect(0.0,
                                        this.GameView.Height - this.fontSize * 2.0,
                                        this.GameView.Width,
                                        this.fontSize * 2.0);

            Process.Instance.Initialize(this.settings, this.CreateAnimation);

            CompositionTarget.Rendering += DoGame;
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;
        }

        public IObjectAnimation CreateAnimation(Type type)
        {
            if (type == typeof(Core.ScoreSticker))
                return new Animation.ScoreSticker(this.GameView, this.boardBound, 50, this.animateSpeed,
                                                  this.fontSize * 0.7, this.blockSize);

            if (type == typeof(Core.ComboSticker))
                return new Animation.ComboSticker(this.GameView, this.boardBound, 60, this.animateSpeed,
                                                  this.fontSize * 2.0);

            if (type == typeof(Core.Combo))
                return new Animation.Combo(this.GameView, this.comboBound, 100, this.animateSpeed,
                                           this.fontSize, this.tabWidth);

            if (type == typeof(Core.Next))
                return new Animation.Next(this.GameView, this.nextBound, 100, this.animateSpeed,
                                          this.fontSize);

            if (type == typeof(Core.Record))
                return new Animation.Record(this.GameView, this.recordBound, 110, this.animateSpeed,
                                            this.fontSize, this.settings.bustScoreBase);

            if (type == typeof(Core.Focus))
                return new Animation.Focus(this.GameView, this.boardBound, 10, this.animateSpeed,
                                           this.blockSize);

            if (type == typeof(Core.Block))
                return new Animation.Block(this.GameView, this.boardBound, 0, this.animateSpeed,
                                           this.blockSize, this.fontSize * 0.7, this.blockShineDelay);

            if (type == typeof(Core.BlockSeed))
            {
                var bound = this.nextBound;
                bound.X += this.tabWidth;
                bound.Width -= this.tabWidth;
                var size = this.blockSize;
                size.Width *= 0.5;
                size.Height *= 0.5;
                return new Animation.BlockSeed(this.GameView, bound, 100, this.animateSpeed,
                                               size, this.fontSize * 0.35, this.blockShineDelay);
            }

            throw new Exception(String.Format("Unable to create animation for '{0}'.",
                                              type));
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var msgDlg = new MessageDialog("BlockBuster for Windows\n(Prototype Ver 1.0.0)");
            await msgDlg.ShowAsync();
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var msgDlg = new MessageDialog("Restart the Game?");
            msgDlg.Commands.Add(new UICommand("Yes"));
            msgDlg.Commands.Add(new UICommand("No"));
            msgDlg.DefaultCommandIndex = 0;
            msgDlg.CancelCommandIndex = 1;
            IUICommand command = await msgDlg.ShowAsync();
            if (command.Label == "Yes")
            {
                var tmpDlg = new MessageDialog("Game Restarted!!!!");
                await tmpDlg.ShowAsync();
            }
        }

        private void GameView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            Process.Instance.UserInput = e.VirtualKey;
        }

        private void DoGame(object sender, object e)
        {
            Process.Instance.Do();
        }
    }
}

