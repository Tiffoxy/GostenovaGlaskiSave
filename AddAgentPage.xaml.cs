using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для AddAgentPage.xaml
    /// </summary>
    public partial class AddAgentPage : Page
    {
        Agent currentAgents;
        private GostenovaGlaskiSaveEntities _context;

        // Новые поля для работы с продуктами
        List<Product> _allProducts;
        ICollectionView _productsView;
        List<ProductSale> _sales;

        public AddAgentPage(Agent agent)
        {
            InitializeComponent();

            _context = new GostenovaGlaskiSaveEntities();

            // Загружаем все продукты для ComboBox
            _allProducts = _context.Product.ToList();
            _productsView = CollectionViewSource.GetDefaultView(_allProducts);
            ProductComboBox.ItemsSource = _productsView;

            if (agent != null && agent.ID != 0)  // существующий агент
            {
                currentAgents = _context.Agent
                    .Include("ProductSale.Product")
                    .FirstOrDefault(a => a.ID == agent.ID);

                if (currentAgents == null)
                {
                    currentAgents = new Agent();
                    currentAgents.ProductSale = new List<ProductSale>();
                    DeleteBtn.Visibility = Visibility.Hidden;
                }
                else
                {
                    ComboType.SelectedIndex = currentAgents.AgentTypeID - 1;
                    DeleteBtn.Visibility = Visibility.Visible;

                    if (!string.IsNullOrEmpty(currentAgents.Logo))
                    {
                        string imagePath = AppDomain.CurrentDomain.BaseDirectory + "Imgs\\agents\\" + currentAgents.Logo;
                        if (File.Exists(imagePath))
                        {
                            LogoImage.Source = new BitmapImage(new Uri(imagePath));
                        }
                    }

                    _sales = currentAgents.ProductSale.ToList();
                }
            }
            else // новый агент
            {
                currentAgents = new Agent();
                currentAgents.ProductSale = new List<ProductSale>();
                DeleteBtn.Visibility = Visibility.Hidden;
                _sales = new List<ProductSale>();
            }

            DataContext = currentAgents;
            SalesListView.ItemsSource = _sales;

            // Устанавливаем сегодняшнюю дату
            SaleDatePicker.SelectedDate = DateTime.Today;

            // подписка на TextChanged для поиска в ComboBox
            ProductComboBox.Loaded += (s, e) =>
            {
                if (ProductComboBox.Template.FindName("PART_EditableTextBox", ProductComboBox) is TextBox textBox)
                    textBox.TextChanged += ProductComboBox_TextChanged;
            };
        }

        private void ProductComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = ((TextBox)sender).Text;
            _productsView.Filter = obj =>
            {
                if (string.IsNullOrEmpty(filter)) return true;
                var product = obj as Product;
                return product.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
            };

            // Раскрываем список, если есть результаты и фильтр не пустой
            if (!string.IsNullOrEmpty(filter) && !_productsView.IsEmpty)
                ProductComboBox.IsDropDownOpen = true;
        }

        private void ChangePicureBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myOpenFileDialog = new OpenFileDialog();
            myOpenFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

            if (myOpenFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sourceFile = myOpenFileDialog.FileName;
                    if (!File.Exists(sourceFile))
                    {
                        MessageBox.Show("Исходный файл не найден.");
                        return;
                    }

                    // Определяем папку, куда будем копировать
                    string imgsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imgs", "agents");
                    Directory.CreateDirectory(imgsFolder);

                    string fileName = Path.GetFileName(sourceFile);
                    string destPath = Path.Combine(imgsFolder, fileName);

                    // Если файл с таким именем уже существует – добавляем суффикс
                    int count = 1;
                    while (File.Exists(destPath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        string newName = $"{nameWithoutExt}_{count}{ext}";
                        destPath = Path.Combine(imgsFolder, newName);
                        count++;
                    }

                    File.Copy(sourceFile, destPath);

                    // Сохраняем только имя файла (без пути)
                    currentAgents.Logo = Path.GetFileName(destPath);

                    // Обновляем изображение на странице
                    LogoImage.Source = new BitmapImage(new Uri(destPath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при копировании файла: {ex.Message}");
                }
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder errors = new StringBuilder();

                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(currentAgents.Title))
                    errors.AppendLine("Укажите наименование агента");
                if (string.IsNullOrWhiteSpace(currentAgents.Address))
                    errors.AppendLine("Укажите адрес агента");
                if (string.IsNullOrWhiteSpace(currentAgents.DirectorName))
                    errors.AppendLine("Укажите ФИО директора");
                if (ComboType.SelectedItem == null)
                    errors.AppendLine("Укажите тип агента");
                else
                {
                    currentAgents.AgentTypeID = ComboType.SelectedIndex + 1;
                }

                // Priority
                if (string.IsNullOrWhiteSpace(currentAgents.Priority.ToString()))
                    errors.AppendLine("Укажите приоритет агента");
                else
                {
                    int priorityValue;
                    if (int.TryParse(currentAgents.Priority.ToString(), out priorityValue))
                    {
                        currentAgents.Priority = priorityValue;
                    }
                    else
                    {
                        errors.AppendLine("Приоритет должен быть числом");
                    }
                }

                if (currentAgents.Priority <= 0)
                    errors.AppendLine("Укажите положительный приоритет агента");

                if (string.IsNullOrWhiteSpace(currentAgents.INN))
                    errors.AppendLine("Укажите ИНН агента");
                if (string.IsNullOrWhiteSpace(currentAgents.KPP))
                    errors.AppendLine("Укажите КПП агента");
                if (string.IsNullOrWhiteSpace(currentAgents.Phone))
                    errors.AppendLine("Укажите телефон агента");
                else
                {
                    string digitsOnly = new string(currentAgents.Phone.Where(char.IsDigit).ToArray());
                    if (digitsOnly.Length < 10)
                        errors.AppendLine("Телефон должен содержать минимум 10 цифр");
                }

                // Проверка email
                if (string.IsNullOrWhiteSpace(currentAgents.Email))
                {
                    errors.AppendLine("Укажите почту агента");
                }
                else
                {
                    string email = currentAgents.Email.Trim();

                    if (!email.Contains("@"))
                    {
                        errors.AppendLine("Email должен содержать символ @");
                    }
                    else
                    {
                        string[] parts = email.Split('@');
                        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                        {
                            errors.AppendLine("Некорректный формат email");
                        }
                        else if (!parts[1].Contains(".ru") && !parts[1].Contains(".com") && !parts[1].Contains(".net") && !parts[1].Contains(".org"))
                        {
                            errors.AppendLine("Email должен быть с доменом .ru, .com или .net или .org");
                        }
                        else if (email.Contains(" ") || email.Contains(";") || email.Contains(","))
                        {
                            errors.AppendLine("Email не должен содержать пробелы или спецсимволы");
                        }
                    }
                }

                // Проверка длины полей
                if (currentAgents.Title != null && currentAgents.Title.Length > 150)
                    errors.AppendLine("Наименование не может быть длиннее 150 символов");
                if (currentAgents.INN != null && currentAgents.INN.Length > 12)
                    errors.AppendLine("ИНН не может быть длиннее 12 символов");
                if (currentAgents.KPP != null && currentAgents.KPP.Length > 9)
                    errors.AppendLine("КПП не может быть длиннее 9 символов");
                if (currentAgents.Phone != null && currentAgents.Phone.Length > 20)
                    errors.AppendLine("Телефон не может быть длиннее 20 символов");

                if (errors.Length > 0)
                {
                    MessageBox.Show(errors.ToString());
                    return;
                }

                // СОХРАНЕНИЕ
                if (currentAgents.ID == 0)
                {
                    _context.Agent.Add(currentAgents);
                }

                // Добавляем продажи в контекст
                foreach (var sale in currentAgents.ProductSale)
                {
                    if (sale.ID == 0)
                    {
                        _context.ProductSale.Add(sale);
                    }
                }

                _context.SaveChanges();
                MessageBox.Show("Информация сохранена");

                Manager.MainFrame.GoBack();
                if (Manager.MainFrame.Content is AgentPage agentPage)
                {
                    agentPage.RefreshAgents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                if (ex.InnerException != null)
                {
                    MessageBox.Show($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentAgents.ProductSale.Count > 0)
                {
                    MessageBox.Show("Нельзя удалить агента, у которого есть продажи!");
                    return;
                }

                var result = MessageBox.Show("Вы точно хотите удалить агента?", "Внимание",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _context.Agent.Attach(currentAgents);
                    _context.Agent.Remove(currentAgents);
                    _context.SaveChanges();
                    MessageBox.Show("Агент удален");
                    Manager.MainFrame.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }

        //методы для работы с продажами
        private void AddSaleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите продукт", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!int.TryParse(CountTextBox.Text, out int count) || count <= 0)
            {
                MessageBox.Show("Введите положительное целое число", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (SaleDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату продажи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Получаем выбранный продукт
            Product selectedProduct = (Product)ProductComboBox.SelectedItem;

            ProductSale newSale = new ProductSale
            {
                ProductID = selectedProduct.ID,
                ProductCount = count,
                SaleDate = SaleDatePicker.SelectedDate.Value,
                Product = selectedProduct
            };

            currentAgents.ProductSale.Add(newSale);
            _sales.Add(newSale);
            SalesListView.Items.Refresh();

            // Очистка полей
            CountTextBox.Text = "";
            SaleDatePicker.SelectedDate = DateTime.Today;
            ProductComboBox.SelectedItem = null;
        }

        private void DeleteSaleBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            ProductSale sale = btn.Tag as ProductSale;
            if (sale != null)
            {
                try
                {
                    // Удаляем из контекста, если продажа уже сохранена в БД
                    if (sale.ID != 0)
                    {
                        var saleInContext = _context.ProductSale.Find(sale.ID);
                        if (saleInContext != null)
                        {
                            _context.ProductSale.Remove(saleInContext);
                        }
                    }

                    // Удаляем из коллекций
                    currentAgents.ProductSale.Remove(sale);
                    _sales.Remove(sale);

                    // Сохраняем изменения
                    _context.SaveChanges();

                    // Обновляем ListView
                    SalesListView.ItemsSource = null;
                    SalesListView.ItemsSource = _sales;

                    MessageBox.Show("Продажа удалена");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }
    }
}