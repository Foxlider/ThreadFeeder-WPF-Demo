using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ThreadFeeder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;     //DataContext 
            _instance = this;       //Instancer
        }

        /// <summary>
        ///     Gets the one and only instance.
        /// </summary>
        public static MainWindow Instance => _instance ??= new MainWindow();
        

        //List of tasks
        public ObservableCollection<ActionMaker> TaskList
        {
            get => _tl;
            set
            {
                _tl = value;
                OnPropertyChanged("TaskList");
            }
        }

        //List of threads
        public ObservableCollection<ActionTaker> ThreadList
        {
            get => _at;
            set
            {
                _at = value;
                OnPropertyChanged("ThreadList");
            }
        }

        //Are threads running
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged("IsRunning");
            }
        }

        //Are threads running
        public string TasksPerSec
        {
            get => _tasksPerSec;
            set
            {
                _tasksPerSec = value;
                OnPropertyChanged("TasksPerSec");
            }
        }

        public int TaskCount
        {
            get => _taskCount;
            set
            {
                _taskCount = value;
                OnPropertyChanged("TaskCount");
            }
        }

        public int ThreadCount
        {
            get => _threadCount;
            set
            {
                _threadCount = value;
                OnPropertyChanged("ThreadCount");
            }
        }

        //Private vars
        private static   MainWindow                        _instance;
        private          ObservableCollection<ActionMaker> _tl;
        private          ObservableCollection<ActionTaker> _at;
        private          bool                              _isRunning;
        private          int                               _threadCount;
        private          int                               _taskCount;
        private          string                            _tasksPerSec;
        private          System.Timers.Timer               _timerThread;
        public  readonly object                            locked = new object();
        public           int                               tempTasks;


        //Sync numericUpDown and slider
        #region sync slider and numeric Task
        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if(e.NewValue != null && slider !=null)
                slider.Value = e.NewValue ?? 0;
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(numeric != null)
                numeric.Value = e.NewValue;
        }
        #endregion

        //Sync numericUpDown and slider
        #region sync slider and numeric Thread
        private void NumericUpDownThread_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if(e.NewValue != null && slider !=null)
                sliderThreads.Value = e.NewValue ?? 0;
        }

        private void sliderThread_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(numericThreads != null)
                numericThreads.Value = e.NewValue;
        }
        #endregion

        private void btnGenThreads_Click(object sender, RoutedEventArgs e)
        { Task.Factory.StartNew(GenThreads, TaskCreationOptions.LongRunning); }

        private void GenThreads()
        {
            //Disable button
            Dispatcher?.Invoke(() => { btnGenThreads.IsEnabled = false; }, DispatcherPriority.Render);
            
            //Create new Collection and get thread number
            if(ThreadList == null)
                ThreadList = new ObservableCollection<ActionTaker>();
            double d = 0;
            Dispatcher?.Invoke(() => { d = sliderThreads.Value; });
            
            //Clean previous threads
            CleanThreads();

            if (_timerThread == null)
            {
                _timerThread          =  new System.Timers.Timer();
                _timerThread.Elapsed  += ThreadCounterTick;
                _timerThread.Interval =  new TimeSpan(0, 0, 1).TotalMilliseconds;
            }
            _timerThread.Start();
            //Create d threads
            for (var i = 0; i < d; i++)
            {
                Dispatcher?.Invoke(() =>
                {
                    ThreadList.Add(new ActionTaker());
                    ThreadCount = ThreadList.Count;
                });
            }
            IsRunning = true;

            //Reenable button
            Dispatcher?.Invoke(() => { btnGenThreads.IsEnabled = true; }, DispatcherPriority.Render);
        }


        private void ThreadCounterTick(object sender, EventArgs e)
        {
            int tasksPerSec;
            lock (locked)
            {
                tasksPerSec = tempTasks;
                tempTasks = 0;
            }
            
            //TasksPerSec = $"{tasksPerSec} Tasks/S";
            Dispatcher?.Invoke(() => tPerSec.Content = $"{tasksPerSec} Tasks/S", DispatcherPriority.Render);
        }

        
        private void CleanThreads()
        {
            _timerThread?.Stop();
            IsRunning = false;
            foreach (var thread in ThreadList)
            {
                Task.Factory.StartNew(() =>
                {
                    thread.isRunning = false;
                    ThreadCount = ThreadList.Count;
                });
            }
            Dispatcher?.Invoke(() => ThreadList.Clear());
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            TaskList.Clear();
            TaskCount = TaskList.Count;
            OnPropertyChanged("TaskList");
        }

        private void btnGen_Click(object sender, RoutedEventArgs e)
        { Task.Factory.StartNew(GenTasks, TaskCreationOptions.LongRunning); }

        private void GenTasks()
        {
            //Disable button and get the current number of tasks
            Dispatcher?.Invoke(() => { btnGen.IsEnabled = false; }, DispatcherPriority.Render);
            double d = 0;
            Dispatcher?.Invoke(() => { d = slider.Value; });

            //generate the new ObservableCollection
            var r = new Random();
            TaskList = new ObservableCollection<ActionMaker>();

            //Generate d numbers of actions contianing a random wait time 
            for (var i = 0; i < d; i++)
            {
                var rand = r.Next(1, 1000); 
                var task = new Action(() => { Task.Delay(rand); }); //The call is not awaited but it is executed. Use this to create actions
                //Add the task to TaskList collection
                Dispatcher?.Invoke(() => TaskList.Add(new ActionMaker(task, rand)));
                TaskCount = TaskList.Count;
            }
            //Reenable button
            Dispatcher?.Invoke(() => { btnGen.IsEnabled = true; }, DispatcherPriority.Render);
        }

        #region INotifyPropertyChanged implementation
 
        public event PropertyChangedEventHandler PropertyChanged;
 
        public void OnPropertyChanged(string propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        #endregion

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {

            foreach (var thread in ThreadList)
            {
                Task.Factory.StartNew(() => 
                                      { thread.ToggleRunner(IsRunning); });
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ActionMaker
    {
        public Action action;
        public short waitTime;
        public int id;

        public ActionMaker(Action a, int wait)
        {
            //Assign new ID
            id = MainWindow.Instance.TaskList.Count;
            action = a; //Use this to execute actions
            waitTime = Convert.ToInt16(wait);
        }

        public override string ToString()
        { return $" ({id}) Waiting {waitTime}ms"; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class ActionTaker : INotifyPropertyChanged
    {
        public ActionMaker am;  //CURRENT ActionMaker
        public Action action;   //Current action (but disabled for now. Uncomment to use actions)
        public Thread runner;   //Running thread
        public bool isRunning;  //Is the thread is running
        public int id;          //Thread ID
        private string _output; //ToString output private
        public string output    //ToStringOutput setter
        {
            get => _output;
            set
            {
                _output = value;
                OnPropertyChanged("output");
            }
        }

        /// <summary>
        /// Create a new ActionTaker
        /// </summary>
        public ActionTaker()
        {
            //Assign new ID
            id = MainWindow.Instance.ThreadList.Count;
            
            //Create runner code
            runner = new Thread(RunnerJob()) { IsBackground = true };

            //Start the runner
            isRunning = true;
            runner.Start();
        }

        /// <summary>
        /// Output of the ActionTaker
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            output = am != null? $"{id} : {am}" : $"{id} : No action";
            OnPropertyChanged("output");
            return output;
        }

        /// <summary>
        /// Toggle runner on or off
        /// </summary>
        /// <param name="run"></param>
        public void ToggleRunner(bool run)
        {
            isRunning = run;
            if (!isRunning) return;
            runner = new Thread(RunnerJob()) { IsBackground = true };
            runner.Start();
        }


        /// <summary>
        /// Action of the runner. Change this will change the code executed by the runner thread
        /// </summary>
        /// <returns></returns>
        private ThreadStart RunnerJob()
        {
            return async () =>
            {
                while(isRunning)
                {
                    //While we still have tasks to get
                    while (MainWindow.Instance.TaskList != null && MainWindow.Instance.TaskList.Count > 0 && isRunning)
                    {
                        //Reset current action
                        am = null;

                        //Get Current action
                        am = PopTask();

                        //When out of items to feed on, loop every second to wait for new items
                        output = am != null 
                            ? $"{id} : {am}" 
                            : $"{id} : No action";
                        OnPropertyChanged("output");

                        //If we have data run code
                        if(am != null)
                            await Task.Delay(am.waitTime);
                    }
                    am     = null;
                    output = $"{id} : No action";
                    OnPropertyChanged("output");
                    await Task.Delay(1000);
                }
            };
        }

        //Get first Task in the TaskList
        private ActionMaker PopTask()
        {
            ActionMaker popped;

            //Lock to avoid conflicts
            lock (MainWindow.Instance.TaskList)
            {
                if (MainWindow.Instance.TaskList.Count <= 0) return null;
                //get first element
                popped = MainWindow.Instance.TaskList.ElementAt(0);
                MainWindow.Instance.tempTasks++;
                //try to delete it
                MainWindow.Instance.Dispatcher?.Invoke(() =>
                {
                    try
                    {
                        MainWindow.Instance.TaskList.RemoveAt(0);
                        MainWindow.Instance.TaskCount = MainWindow.Instance.TaskList.Count;
                    }
                    catch
                    { /*ignored*/ }
                });
            }
            return popped;
        }

        //THIS IS UI CODE TO REFRESH DATA IN REAL TIME
        #region INotifyPropertyChanged implementation
 
        public event PropertyChangedEventHandler PropertyChanged;
 
        public void OnPropertyChanged(string propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }
 
        #endregion
    }
}
