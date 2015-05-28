using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BlockBuster
{
    public sealed partial class GameSettings : UserControl
    {
        private Core.ProcessSettings settings;
        public Core.ProcessSettings Result { get { return this.settings; } }

        public GameSettings()
        {
            this.InitializeComponent();

            this.settings = new Core.ProcessSettings();

            this.GameMode.Items.Add("Classic");
            this.GameMode.Items.Add("Time Attack");
            this.GameMode.SelectedItem = this.GameMode.Items[0];

            this.BlockColors.Minimum = 2;
            this.BlockColors.Maximum = 6;
            this.BlockColors.Value = 4;

            this.BoardSize.Items.Add("3x3");
            this.BoardSize.Items.Add("4x4");
            this.BoardSize.Items.Add("5x5");
            this.BoardSize.Items.Add("6x6");
            this.BoardSize.Items.Add("7x7");
            this.BoardSize.SelectedItem = this.BoardSize.Items[1];

            this.BustThreshold.Minimum = 2;
            this.BustThreshold.Maximum = 6;
            this.BustThreshold.Value = 3;

            this.MaxToughness.Minimum = 2;
            this.MaxToughness.Maximum = 10;
            this.MaxToughness.Value = 3;

            this.MudFrequency.Minimum = 0;
            this.MudFrequency.Maximum = 100;
            this.MudFrequency.Value = 0;

            this.ToughFrequency.Minimum = 0;
            this.ToughFrequency.Maximum = 100;
            this.ToughFrequency.Value = 0;

            this.MoveLimit.Minimum = 1;
            this.MoveLimit.Maximum = 100;
            this.MoveLimit.Value = 30;
            this.MoveLimit.StepFrequency = 5;

            this.ComboDelay.Minimum = 0.1;
            this.ComboDelay.Maximum = 10.0;
            this.ComboDelay.Value = 1.5;
            this.ComboDelay.StepFrequency = 0.5;

            this.TimeLimit.Minimum = 10;
            this.TimeLimit.Maximum = 300;
            this.TimeLimit.Value = 60;
            this.TimeLimit.StepFrequency = 10;
        }

        private void OkayClicked(object sender, RoutedEventArgs e)
        {
            this.settings.fps = 60;
            switch (this.GameMode.SelectedIndex)
            {
                case 0: this.settings.gameMode = Core.GameModes.Classic; break;
                case 1: this.settings.gameMode = Core.GameModes.TimeAttack; break;
            }
            switch (this.BoardSize.SelectedIndex)
            {
                case 0: this.settings.boardCols = this.settings.boardRows = 3; break;
                case 1: this.settings.boardCols = this.settings.boardRows = 4; break;
                case 2: this.settings.boardCols = this.settings.boardRows = 5; break;
                case 3: this.settings.boardCols = this.settings.boardRows = 6; break;
                case 4: this.settings.boardCols = this.settings.boardRows = 7; break;
            }
            this.settings.focusRow = 0;
            this.settings.focusCol = 0;
            this.settings.focusRowSpan = this.settings.boardRows;
            this.settings.focusColSpan = this.settings.boardCols;
            this.settings.blockColors = (int)this.BlockColors.Value;
            this.settings.mudFreq = (int)this.MudFrequency.Value;
            this.settings.toughFreq = (int)this.ToughFrequency.Value;
            this.settings.toughMax = (int)this.MaxToughness.Value;
            this.settings.bustThreshold = (int)this.BustThreshold.Value;
            this.settings.bustScoreBase = 100;
            this.settings.comboDelay = this.ComboDelay.Value;
            this.settings.comboBonusCoeff = 0.2;
            this.settings.timeLimit = this.TimeLimit.Value;
            this.settings.moveLimit = (int)this.MoveLimit.Value;

            PopoverControl.Close(true);
        }

        private void GameModeChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (this.GameMode.SelectedIndex)
            {
                case 0:     // Classic
                    this.MoveLimit.IsEnabled = true;
                    this.ComboDelay.IsEnabled = false;
                    this.TimeLimit.IsEnabled = false;                    
                    break;

                case 1:     // TimeAttack
                    this.MoveLimit.IsEnabled = false;
                    this.ComboDelay.IsEnabled = true;
                    this.TimeLimit.IsEnabled = true;                    
                    break;
            }

        }
    }
}
