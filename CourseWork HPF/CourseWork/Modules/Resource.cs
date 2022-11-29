using System.Diagnostics.Contracts;
using System.ComponentModel;

namespace CourseWork
{
    class Resource : INotifyPropertyChanged
    {
        private Process activeProcess;
        public Process ActiveProcess
        {
            get { return activeProcess; }
            set { activeProcess = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ActiveProcess"));
            }
        }

        public void WorkingCycle()
        {
            if (!IsFree())
            {
                activeProcess.IncreaseWorkTime();
            }
        }

        [Pure]
        public bool IsFree()
        {
            return activeProcess == null;
        }

        public void Clear()
        {
            ActiveProcess = null;
        }
    }
}
