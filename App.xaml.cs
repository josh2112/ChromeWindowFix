using CommunityToolkit.Mvvm.Input;
using Hardcodet.Wpf.TaskbarNotification;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Windows;
using System.Text.Json;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;

namespace ChromeWindowFix
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon? taskbarIcon;

        public RelayCommand ExitCommand { get; }

        private WindowPositionFixer? windowPositionFixer;

        public App()
        {
            ExitCommand = new RelayCommand( Shutdown );

            var singleInstanceHelper = new SingleInstanceHelper( "{64f42670-5f7b-11ed-9b6a-0242ac120002}",
                () => { }, () => Shutdown() );

            Exit += ( s, e ) => singleInstanceHelper.Dispose();
        }
        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );

            taskbarIcon = (TaskbarIcon)FindResource( "taskbarIcon" );
            taskbarIcon.DataContext = this;

            var config = JsonSerializerExtensions.DeserializeAnonymousType( File.ReadAllText( "config.json" ),
                new { adjust = new { top = 0 } } );

            if( config == null )
            {
                MessageBox.Show( "Unable to read config file" );
                Shutdown();
                return;
            }

            windowPositionFixer = new WindowPositionFixer(
                p => p.MainWindowTitle.Contains( "Chrome" ) && p.ProcessName.Contains( "chrome" ),
                ( windowRect, desktopRect ) => windowRect.Height == desktopRect.Height && windowRect.Width != desktopRect.Width && windowRect.Top == 0,
                r => new System.Drawing.Rectangle( r.X, r.Y + config.adjust.top, r.Width, r.Height - config.adjust.top ));
        }
    }


    public class SingleInstanceHelper
    {
        public class AnotherInstanceOpenException : Exception { }

        private EventWaitHandle eventWaitHandle;

        public SingleInstanceHelper( string appGuid, Action showWindowAction, Action shutdownAction )
        {
            try
            {
                eventWaitHandle = EventWaitHandle.OpenExisting( appGuid );
                eventWaitHandle.Set();

                shutdownAction();
                throw new AnotherInstanceOpenException();
            }
            catch( WaitHandleCannotBeOpenedException )
            {
                // No instance found, create a new one
                eventWaitHandle = new EventWaitHandle( false, EventResetMode.AutoReset, appGuid );
            }

            // Watch for the handle to be signaled (by another instance starting up and create/open main window
            new Task( () => {
                while( eventWaitHandle.WaitOne() )
                    Application.Current.Dispatcher.BeginInvoke( showWindowAction );
            } ).Start();
        }

        public void Dispose() => eventWaitHandle.Close();
    }

    public static partial class JsonSerializerExtensions
    {
        public static T? DeserializeAnonymousType<T>( string json, T _, JsonSerializerOptions? options = default )
            => JsonSerializer.Deserialize<T>( json, options );
    }
}
