using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using DownloadManager.ViewModels;

namespace DownloadManager.Views
{
    /// <summary>
    /// Логика взаимодействия для AddUrlWindow.xaml
    /// </summary>
    public partial class AddUrlSelectWindow : Window
    {
        public AddUrlSelectWindow()
        {
            InitializeComponent();
            this.DataContext = new AddUrlWithSelectViewModel(this);
        }
    }
}
