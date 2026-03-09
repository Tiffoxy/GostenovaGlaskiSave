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

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для PriorChange.xaml
    /// </summary>
    public partial class PriorChange : Window
    {
        public PriorChange(int maxPriority)
        {
            InitializeComponent();
            TBPriority.Text = maxPriority.ToString();
        }

        private void ChangePrior_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TBPriority.Text))
            {
                MessageBox.Show("Заполните поле");
                return;
            }

            if (Convert.ToInt32(TBPriority.Text) < 0)
            {
                MessageBox.Show("Приоритет не может быть отрицательным");
                return;
            }
            this.Close();
        }
    }
}
