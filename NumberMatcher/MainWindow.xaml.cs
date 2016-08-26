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
using System.Threading;

namespace NumberMatcher
{
    public partial class MainWindow
        : Window
    {
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

            List<string> StrNumbers = Regex.Matches(Text_Input.Text, @"\d+(\.\d+)?").OfType<Match>().Select(m => m.Value).ToList();
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

            WorkerRun = true;
            Worker = new Thread(() => { Solve(Numbers, Number); ToggleInput(true); });
            Worker.Start();
        }
        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            WorkerRun = false;
        }

        private void Solve(List<decimal> Numbers, decimal Goal)
        {
            List<List<decimal>> Solutions = new List<List<decimal>>();
            DateTime Start = DateTime.Now;
            _Solve(Numbers, 0, new List<decimal>(), Solutions, Goal);
            DateTime End = DateTime.Now;
            AppendOutput(Solutions.Count + " solutions.");
            AppendOutput("Took " + (End - Start).TotalSeconds + "s.");
        }
        private void _Solve(List<decimal> Usable, decimal Total, List<decimal> Used, List<List<decimal>> Solutions, decimal Goal)
        {
            List<decimal> NewUsable = Usable.ToList();
            for (int x = 0; x < Usable.Count && WorkerRun; x++)
            {
                List<decimal> NewUsed = Used.ToList();
                NewUsed.Add(Usable[x]);
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

                if (Usable[x] + Total < Goal && NewUsable.Count > 0)
                    _Solve(NewUsable, Total + Usable[x], NewUsed, Solutions, Goal);
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
    }
}
