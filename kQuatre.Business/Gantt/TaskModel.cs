using Infragistics.Controls.Schedules;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Guiet.kQuatre.Business.Gantt
{
    public class TaskModel : INotifyPropertyChanged
    {
        public string ChildTasks { get; set; } //Tasks collection -> CSV's.
        public string Name { get; set; } //TaskName        
        public DateTime EndTime { get; set; } //Finish

        public bool IsManualTask { get; set; } //IsManual
        public TimeSpan Length { get; set; } //Duration   
        public int TaskID { get; set; } //DataItemID        
        public DateTime? ConstraintDate { get; set; } //Default.
        public ProjectTaskConstraintType ConstraintType { get; set; } //Default.
        public string TaskResources { get; set; }

        #region Event

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        private DateTime _startTime;
        public DateTime StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged();
                }
            }
        } //Start   

        private decimal _percentComplete;
        public decimal PercentComplete
        {
            get { return _percentComplete; }
            set
            {
                if (_percentComplete != value)
                {
                    _percentComplete = value;
                    OnPropertyChanged();
                }
            }
        }


        private string _pred;
        public string Preds //Predecessors
        {
            get { return _pred; }
            set
            {
                if (_pred != value)
                {
                    _pred = value;
                    OnPropertyChanged();
                }
            }
        }
        public ProjectDurationFormat Format { get; set; } //DurationFormat      

        public SolidColorBrush _taskBrush;
        public SolidColorBrush TaskBrush
        {
            get { return _taskBrush; }
            set
            {
                if (_taskBrush != value)
                {
                    _taskBrush = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
