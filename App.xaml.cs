using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.UI.Core;


// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace BlockBuster
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        public static async void WriteFile()
        {
            // Create sample file; replace if exists.
            Windows.Storage.StorageFolder folder =
                Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile sampleFile =
                await folder.CreateFileAsync("sample.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(sampleFile, "Swift as a shadow");
        }
    }


    public partial class PopoverControl
    {
        private Popup popup;
        private bool shown;
        private bool succeeded;
        private TaskCompletionSource<bool> tcs;

        // Global PopoverControl Singleton
        private static PopoverControl _instance;
        public static PopoverControl Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PopoverControl();
                return _instance;
            }
        }
        private PopoverControl()
        {
        }

        public static Task<bool> ShowAsync(UserControl userControl)
        {
            if (userControl == null)
                throw new ArgumentNullException("userControl");
            if (PopoverControl.Instance.shown)
                throw new InvalidOperationException("Duplicate PopoverControl shown");

            PopoverControl.Instance.tcs = new TaskCompletionSource<bool>();
            PopoverControl.Instance.popup = new Popup();
            PopoverControl.Instance.popup.Child = userControl;
            PopoverControl.Instance.popup.Closed += PopoverControl.Instance.OnClosed;
            PopoverControl.Instance.popup.IsOpen = true;
            PopoverControl.Instance.shown = true;
            PopoverControl.Instance.succeeded = false;

            return PopoverControl.Instance.tcs.Task;
        }

        public static void Close(bool succeeded)
        {
            PopoverControl.Instance.succeeded = succeeded;
            PopoverControl.Instance.popup.Child = null;
            PopoverControl.Instance.popup.IsOpen = false;
        }

        private void OnClosed(object sender, object e)
        {
            this.shown = false;
            this.popup.Closed -= this.OnClosed;
            this.tcs.SetResult(this.succeeded);
        }
    }

    public sealed class PopoverView : ContentControl
    {
        public PopoverView()
        {
            this.DefaultStyleKey = typeof(PopoverView);
            this.Style = Application.Current.Resources["PopoverViewStyle"] as Style;
            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
        }

        /// <summary>
        /// Measure the size of this control: make it cover the full App window
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            Rect bounds = Window.Current.Bounds;
            availableSize = new Size(bounds.Width, bounds.Height);
            base.MeasureOverride(availableSize);
            return availableSize;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged += this.OnSizeChanged;
        }

        private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            base.InvalidateMeasure();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= this.OnSizeChanged;
        }
    }

}
