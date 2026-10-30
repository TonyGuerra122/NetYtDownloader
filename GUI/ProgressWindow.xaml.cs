using System.Windows;

namespace GUI;

public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    public void UpdateProgress(int percent, string message)
    {
        ProgressBar.Value = Math.Min(percent, 100);
        MessageText.Text = message;
        PercentText.Text = $"{percent}%";
    }
}
