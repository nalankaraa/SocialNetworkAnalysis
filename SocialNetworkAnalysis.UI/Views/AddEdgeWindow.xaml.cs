using System.Windows;

namespace SocialNetworkAnalysis.UI.Views
{
    public partial class AddEdgeWindow : Window
    {
        public int SourceId { get; private set; }
        public int TargetId { get; private set; }
        public double Weight { get; private set; }

        public AddEdgeWindow()
        {
            InitializeComponent();
            TxtSourceId.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSourceId.Text) || string.IsNullOrWhiteSpace(TxtTargetId.Text))
            {
                MessageBox.Show("Please enter both Source and Target IDs.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtSourceId.Text, out int sourceId))
            {
                MessageBox.Show("Source ID must be an integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtTargetId.Text, out int targetId))
            {
                MessageBox.Show("Target ID must be an integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sourceId == targetId)
            {
                MessageBox.Show("Source and Target cannot be the same.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double weight = 1.0;
            if (!string.IsNullOrWhiteSpace(TxtWeight.Text) && !double.TryParse(TxtWeight.Text, out weight))
            {
                MessageBox.Show("Weight must be a valid number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SourceId = sourceId;
            TargetId = targetId;
            Weight = weight;

            DialogResult = true;
            Close();
        }
    }
}
