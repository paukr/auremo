using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Auremo
{
    public partial class CoverArtOverlay : UserControl
    {
        public CoverArtOverlay()
        {
            InitializeComponent();
        }

        public void SetDataModel(DataModel dataModel)
        {
            DataContext = dataModel;
        }

        private void OnMouse(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Visibility = Visibility.Collapsed;
        }

        private void OnKey(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            Visibility = Visibility.Collapsed;
        }
    }
}
