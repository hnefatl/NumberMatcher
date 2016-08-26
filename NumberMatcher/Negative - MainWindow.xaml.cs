using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;

namespace NumberMatcher
{
    public partial class MainWindow
        : Window, INotifyPropertyChanged
    {
        private float _ProgressMaximum = 1;
        public float ProgressMaximum
        {
            get { return _ProgressMaximum; }
            set { _ProgressMaximum = value; OnPropertyChanged("ProgressMaximum"); }
        }

        private float _ProgressCurrent = 0;
        public float ProgressCurrent
        {
            get { return _ProgressCurrent; }
            set { _ProgressCurrent = value; OnPropertyChanged("ProgressCurrent"); }
        }

        private bool WorkerRun;
        private Thread Worker;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void ToggleInput(bool Enabled)
        {
            if (Dispatcher.CheckAccess())
            {
                Text_Input.IsEnabled = Enabled;
                Text_Number.IsEnabled = Enabled;
                Button_Stop.IsEnabled = !Enabled;
                Button_Run.IsEnabled = Enabled;
                Check_ShowProgress.IsEnabled = Enabled;
            }
            else
                Dispatcher.Invoke((Action<bool>)ToggleInput, Enabled);
        }
        private void AppendOutput(string Message)
        {
            if (Dispatcher.CheckAccess())
            {
                Text_Output.Text += Message + Environment.NewLine;
            }
            else
                Dispatcher.Invoke((Action<string>)AppendOutput, Message);
        }

        private void Button_Run_Click(object sender, RoutedEventArgs e)
        {
            ToggleInput(false);

            decimal Number;
            if (!decimal.TryParse(Text_Number.Text, out Number))
            {
                MessageBox.Show(Text_Number.Text + " is not a valid number.");
                ToggleInput(true);
                return;
            }

            List<string> StrNumbers = Regex.Matches(Text_Input.Text, @"[-+]?\d+(\.\d+)?").OfType<Match>().Select(m => m.Value).ToList();
            List<decimal> Numbers = new List<decimal>(StrNumbers.Count);
            foreach (string s in StrNumbers)
            {
                decimal d;
                if (!decimal.TryParse(s, out d))
                {
                    MessageBox.Show(s + " is not a valid number.");
                    ToggleInput(true);
                    return;
                }
                Numbers.Add(d);
            }
            Numbers.Sort();
            Text_Input.Text = Numbers.Aggregate("", (acc, f) => acc + f + Environment.NewLine);
            Text_Output.Text = string.Empty;

            bool ShowProgress = Check_ShowProgress.IsChecked.Value;

            WorkerRun = true;
            Worker = new Thread(() => { Solve(Numbers, Number, ShowProgress); ToggleInput(true); });
            Worker.Start();
        }
        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            WorkerRun = false;
        }

        private void Solve(List<decimal> Numbers, decimal Goal, bool ShowProgress)
        {
            ProgressCurrent = 0;
            ProgressMaximum = GetMaxProgress(Numbers);

            List<List<decimal>> Solutions = new List<List<decimal>>();
            DateTime Start = DateTime.Now;
            _Solve(Numbers, 0, new List<decimal>(), Solutions, Goal, ShowProgress);
            DateTime End = DateTime.Now;
            AppendOutput("Calculated " + ProgressCurrent.ToString() + "/" + ProgressMaximum.ToString() + " permutations.");
            AppendOutput(Solutions.Count + " solutions.");
            AppendOutput("Took " + (End - Start).TotalSeconds + "s.");
        }
        private void _Solve(List<decimal> Usable, decimal Total, List<decimal> Used, List<List<decimal>> Solutions, decimal Goal, bool ShowProgress)
        {
            for (int x = 0; x < Usable.Count && WorkerRun; x++)
            {
                List<decimal> NewUsed = Used.ToList();
                NewUsed.Add(Usable[x]);
                List<decimal> NewUsable = Usable.ToList();
                NewUsable.Remove(Usable[x]);

                if (Usable[x] + Total == Goal)
                {
                    bool Duplicate = false;
                    foreach (List<decimal> d in Solutions)
                    {
                        if (IsPermutation(d, NewUsed))
                        {
                            Duplicate = true;
                            break;
                        }
                    }
                    if (!Duplicate)
                    {
                        Solutions.Add(NewUsed);
                        foreach (decimal f in NewUsed)
                            AppendOutput(f.ToString());
                        AppendOutput("");
                    }
                }

                if (ShowProgress)
                    ProgressCurrent++;
                if (NewUsable.Count > 0)
                    _Solve(NewUsable, Total + Usable[x], NewUsed, Solutions, Goal, ShowProgress);
            }
        }

        private bool IsPermutation(List<decimal> A, List<decimal> B)
        {
            if (A.Count != B.Count)
                return false;

            Dictionary<decimal, int> Counts = new Dictionary<decimal, int>();
            for (int x = 0; x < A.Count; x++)
            {
                if (Counts.ContainsKey(A[x]))
                    Counts[A[x]]++;
                else
                    Counts.Add(A[x], 1);
            }

            for (int x = 0; x < B.Count; x++)
            {
                if (Counts.ContainsKey(B[x]))
                    Counts[B[x]]--;
                else
                    return false;
            }

            foreach (int i in Counts.Values)
                if (i != 0)
                    return false;

            return true;
        }

        private float GetMaxProgress(List<decimal> Numbers)
        {
            // Sum of nPk for 1<=k<=n
            float Numerator = Factorial(Numbers.Count);
            float Total = 0;

            for (int x = 1; x <= Numbers.Count; x++)
                Total += Numerator / Factorial(Numbers.Count - x);

            return Total;
        }
        private float Factorial(float n)
        {
            float f = 1;
            for (float x = 2; x <= n; x++)
                f *= x;
            return f;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string PropertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
        }

    }
}
