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
        private GameSettings settingsDialog;
        private ProcessSettings settings;
        private bool started;

        private double animateSpeed;
        private Size blockSize;
        private double fontSize;
        private double tabWidth;
        private double blockShineDelay;
        private Rect boardBound;
        private Rect comboBound;
        private Rect nextBound;
        private Rect timeBound;
        private Rect movesBound;
        private Rect recordBound;

        public MainPage()
        {
            this.InitializeComponent();

            this.settingsDialog = new GameSettings();

            CompositionTarget.Rendering += DoGame;
            CoreWindow.GetForCurrentThread().KeyDown += Page_KeyDown;

            this.started = false;
            this.RestartGame();
        }

        public IObjectAnimation CreateAnimation(Type type)
        {
            if (type == typeof(Core.MessageSticker))
                return new Animation.MessageSticker(this.GameView, this.boardBound, 60, this.animateSpeed,
                                                    this.fontSize * 2.0);

            if (type == typeof(Core.ScoreSticker))
                return new Animation.ScoreSticker(this.GameView, this.boardBound, 50, this.animateSpeed,
                                                  this.fontSize * 0.7, this.blockSize);

            if (type == typeof(Core.Combo))
                return new Animation.Combo(this.GameView, this.comboBound, 100, this.animateSpeed,
                                           this.fontSize);

            if (type == typeof(Core.Next))
                return new Animation.Next(this.GameView, this.nextBound, 100, this.animateSpeed,
                                          this.fontSize);

            if (type == typeof(Core.Record))
                return new Animation.Record(this.GameView, this.recordBound, 110, this.animateSpeed,
                                            this.fontSize, this.settings.bustScoreBase);

            if (type == typeof(Core.Time))
            {
                var inBound = this.timeBound;
                inBound.X += this.tabWidth;
                inBound.Width -= this.tabWidth;
                return new Animation.Time(this.GameView, this.timeBound, 100, this.animateSpeed,
                                          inBound, this.fontSize);
            }

            if (type == typeof(Core.Moves))
            {
                var inBound = this.movesBound;
                inBound.X += this.tabWidth;
                inBound.Width -= this.tabWidth;
                return new Animation.Moves(this.GameView, this.movesBound, 100, this.animateSpeed,
                                          inBound, this.fontSize);
            }

            if (type == typeof(Core.Focus))
                return new Animation.Focus(this.GameView, this.boardBound, 10, this.animateSpeed,
                                           this.blockSize);

            if (type == typeof(Core.Block))
                return new Animation.Block(this.GameView, this.boardBound, 0, this.animateSpeed,
                                           this.blockSize, this.fontSize * 0.7, this.blockShineDelay);

            if (type == typeof(Core.BlockSeed))
            {
                var inBound = this.nextBound;
                inBound.X += this.tabWidth;
                inBound.Width -= this.tabWidth;
                var size = this.blockSize;
                size.Width *= 0.7;
                size.Height *= 0.7;
                return new Animation.BlockSeed(this.GameView, inBound, 100, this.animateSpeed,
                                               size, this.fontSize * 0.5, this.blockShineDelay);
            }

            throw new Exception(String.Format("Unable to create animation for '{0}'.",
                                              type));
        }

        private async void RestartGame()
        {
            bool success = await PopoverControl.ShowAsync(this.settingsDialog);
            this.settings = this.settingsDialog.Result;
            this.animateSpeed = 1.0;
            if (this.settings.boardCols < 4)
                this.blockSize = new Size(70.0, 70.0);
            else if (this.settings.boardCols < 6)
                this.blockSize = new Size(60.0, 60.0);
            else
                this.blockSize = new Size(50.0, 50.0);
            this.fontSize = 50.0;
            this.tabWidth = 200;
            this.blockShineDelay = 20.0;
            this.boardBound = new Rect((this.GameView.Width - this.blockSize.Width * this.settings.boardCols) / 2.0,
                                       (this.GameView.Height - this.blockSize.Height * this.settings.boardRows) / 2.0,
                                       this.blockSize.Width * this.settings.boardCols,
                                       this.blockSize.Height * this.settings.boardRows);
            this.comboBound = new Rect(this.boardBound.Left - this.blockSize.Width,
                                       this.boardBound.Top - this.blockSize.Height * 2.0,
                                       this.boardBound.Width + this.blockSize.Width * 2.0,
                                       this.blockSize.Height * 2.0);
            this.nextBound = new Rect(this.boardBound.Left - this.tabWidth - this.blockSize.Width,
                                      this.boardBound.Bottom + this.blockSize.Height * 0.5,
                                      this.boardBound.Width + this.tabWidth + this.blockSize.Width * 2.0,
                                      this.blockSize.Height * 1.0);
            this.timeBound = new Rect(this.blockSize.Width * 0.5,
                                      this.blockSize.Height * 0.5,
                                      this.GameView.Width - this.blockSize.Width,
                                      this.blockSize.Height * 1.0);
            this.movesBound = new Rect(this.boardBound.Left - this.tabWidth - this.blockSize.Width,
                                       this.blockSize.Height * 0.5,
                                       this.boardBound.Width + this.tabWidth + this.blockSize.Width * 2.0,
                                       this.blockSize.Height * 1.0);
            this.recordBound = new Rect(0.0,
                                        this.GameView.Height - this.fontSize * 2.0,
                                        this.GameView.Width,
                                        this.fontSize * 2.0);

            Process.Instance.Initialize(this.settings, this.CreateAnimation);
            this.started = true;
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var msgDlg = new MessageDialog("BlockBuster for Windows\n(Prototype Ver 1.0.0)", "About");
            await msgDlg.ShowAsync();
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var msgDlg = new MessageDialog("Do you want to restart the game?", "Restart Game");
            msgDlg.Commands.Add(new UICommand("Yes"));
            msgDlg.Commands.Add(new UICommand("No"));
            msgDlg.DefaultCommandIndex = 0;
            msgDlg.CancelCommandIndex = 1;
            IUICommand command = await msgDlg.ShowAsync();
            if (command.Label == "Yes")
                this.RestartGame();
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
            if (!this.started)
                return;

            try
            {
                Process.Instance.Do();
                if (Process.Instance.Phase == Process.Phases.GameOver)
                {

                }
            } catch (Exception ex)
            {
                var stackTrace = ex.StackTrace;
                var errMessage = ex.Message;
            }
        }
    }
}
