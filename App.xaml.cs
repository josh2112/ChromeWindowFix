using CommunityToolkit.Mvvm.Input;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ChromeWindowFix
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon? taskbarIcon;

        public RelayCommand ExitCommand { get; }

        public List<WindowPositionFixer>? Fixers { get; private set; }

        private DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds( 1 ) };

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

            Fixers = JsonSerializer.Deserialize<List<WindowPositionFixer>>( File.ReadAllText( "config.json" ),
                new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                } );

            if( Fixers == null )
            {
                MessageBox.Show( "Unable to read config file" );
                Shutdown();
                return;
            }

            timer.Tick += ( s, e ) => {
                foreach( var fixer in Fixers ) fixer.Run();
            };
            timer.Start();
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
}
