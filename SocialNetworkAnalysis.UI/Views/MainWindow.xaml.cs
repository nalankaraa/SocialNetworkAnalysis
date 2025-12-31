using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SocialNetworkAnalysis.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(ViewModels.MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Setup Auto-Fit Timer (Debounce)
            _autoFitTimer = new System.Windows.Threading.DispatcherTimer();
            _autoFitTimer.Interval = TimeSpan.FromMilliseconds(250);
            _autoFitTimer.Tick += (s, e) =>
            {
                _autoFitTimer.Stop();
                ZoomToFit();
            };

            // Subscribe to Nodes changes
            if (viewModel.Nodes != null)
            {
                viewModel.Nodes.CollectionChanged += (s, e) =>
                {
                    _autoFitTimer.Stop();
                    _autoFitTimer.Start();
                };
            }

            // Initial Zoom to Fit after layout update
            this.Loaded += (s, e) => ZoomToFit();
        }

        private System.Windows.Threading.DispatcherTimer _autoFitTimer;

        private Point? _lastMousePosition;
        private bool _isDragging;

        private void Graph_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            e.Handled = true;

            var transformGroup = (TransformGroup)GraphContainer.RenderTransform;
            var scaleTransform = (ScaleTransform)transformGroup.Children[0];
            var translateTransform = (TranslateTransform)transformGroup.Children[1];

            Point mousePos = e.GetPosition(border);

            double zoomFactor = e.Delta > 0 ? 1.1 : 0.90909;
            double newScaleX = scaleTransform.ScaleX * zoomFactor;
            double newScaleY = scaleTransform.ScaleY * zoomFactor;

            if (newScaleX < 0.1) return;
            if (newScaleX > 20) return;

            double newX = mousePos.X - (mousePos.X - translateTransform.X) * zoomFactor;
            double newY = mousePos.Y - (mousePos.Y - translateTransform.Y) * zoomFactor;

            scaleTransform.ScaleX = newScaleX;
            scaleTransform.ScaleY = newScaleY;
            translateTransform.X = newX;
            translateTransform.Y = newY;
        }

        private void Graph_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                _lastMousePosition = e.GetPosition(border);
                _isDragging = true;
                border.CaptureMouse();
                Cursor = Cursors.SizeAll;
            }
        }

        private void Graph_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _lastMousePosition.HasValue)
            {
                var border = sender as Border;
                if (border != null)
                {
                    var currentPos = e.GetPosition(border);
                    var delta = currentPos - _lastMousePosition.Value;

                    var transformGroup = (TransformGroup)GraphContainer.RenderTransform;
                    var translateTransform = (TranslateTransform)transformGroup.Children[1];

                    translateTransform.X += delta.X;
                    translateTransform.Y += delta.Y;

                    _lastMousePosition = currentPos;
                }
            }
        }

        private void Graph_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                var border = sender as Border;
                if (border != null)
                {
                    border.ReleaseMouseCapture();
                    Cursor = Cursors.Arrow;
                }
                _isDragging = false;
                _lastMousePosition = null;
            }
        }



        private void ZoomToFit()
        {
            var border = (Border)System.Windows.Media.VisualTreeHelper.GetParent(GraphContainer);
            if (border == null) return;

            double viewportWidth = border.ActualWidth;
            double viewportHeight = border.ActualHeight;

            if (viewportWidth == 0 || viewportHeight == 0) return;

            // World bounds (Logic from ViewModel)
            double contentWidth = 1000;
            double contentHeight = 700;
            double padding = 50;

            double scaleX = (viewportWidth - 2 * padding) / contentWidth;
            double scaleY = (viewportHeight - 2 * padding) / contentHeight;
            double minScale = Math.Min(scaleX, scaleY);

            if (minScale > 2) minScale = 2;

            var transformGroup = (TransformGroup)GraphContainer.RenderTransform;
            var scaleTransform = (ScaleTransform)transformGroup.Children[0];
            var translateTransform = (TranslateTransform)transformGroup.Children[1];

            scaleTransform.ScaleX = minScale;
            scaleTransform.ScaleY = minScale;

            double scaledContentWidth = contentWidth * minScale;
            double scaledContentHeight = contentHeight * minScale;

            translateTransform.X = (viewportWidth - scaledContentWidth) / 2;
            translateTransform.Y = (viewportHeight - scaledContentHeight) / 2;
        }
    }
}