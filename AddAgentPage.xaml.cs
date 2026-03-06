using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.IO;
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

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для AddAgentPage.xaml
    /// </summary>
    public partial class AddAgentPage : Page
    {
        Agent currentAgents;
        private GostenovaGlaskiSaveEntities _context;
        public AddAgentPage(Agent agent)
        {
            InitializeComponent();
            _context = new GostenovaGlaskiSaveEntities();
            

            
            if (agent != null && agent.ID != 0)  // существующий агент
            {
                currentAgents = _context.Agent.Include("ProductSale.Product")
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
                        string imagePath = AppDomain.CurrentDomain.BaseDirectory + "Imgs\\agents\\" + currentAgents.Logo; if (File.Exists(imagePath))
                        {
                            LogoImage.Source = new BitmapImage(new Uri(imagePath));
                        }
                    }

                }
            }
            else // новый агент
            {
                currentAgents = new Agent();                
                DeleteBtn.Visibility = Visibility.Hidden;

            }
            DataContext = currentAgents;     
           
        }        
        
        private void ChangePicureBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog myOpenFileDialog = new OpenFileDialog();
            myOpenFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

            if (myOpenFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Копируем файл в папку проекта
                    string fileName = System.IO.Path.GetFileName(myOpenFileDialog.FileName);
                    string destPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imgs", "agents", fileName);

                    // Создаем папку, если её нет
                    Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imgs", "agents"));

                    // Копируем файл (перезаписываем если существует)
                    File.Copy(myOpenFileDialog.FileName, destPath, true);

                    // Устанавливаем изображение
                    LogoImage.Source = new BitmapImage(new Uri(destPath));

                    // Сохраняем только имя файла в БД
                    currentAgents.Logo = fileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}");
                }
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //ПРЕОБРАЗОВАНИЕ ТИПОВ
                // Priority из TextBox (это string) в int
                if (!string.IsNullOrWhiteSpace(currentAgents.Priority.ToString()))
                {
                    
                    int priorityValue;
                    if (int.TryParse(currentAgents.Priority.ToString(), out priorityValue))
                    {
                        currentAgents.Priority = priorityValue;
                    }
                    else
                    {
                        MessageBox.Show("Приоритет должен быть числом");
                        return;
                    }
                }

                // AgentTypeID из ComboBox
                if (ComboType.SelectedValue != null)
                {
                    currentAgents.AgentTypeID = ComboType.SelectedIndex + 1;
                }

                // ПРОВЕРКА ОБЯЗАТЕЛЬНЫХ ПОЛЕЙ
                if (string.IsNullOrWhiteSpace(currentAgents.Title))
                {
                    MessageBox.Show("Укажите наименование агента");
                    return;
                }

                if (string.IsNullOrWhiteSpace(currentAgents.Address))
                {
                    MessageBox.Show("Укажите адрес агента");
                    return;
                }

                if (string.IsNullOrWhiteSpace(currentAgents.DirectorName))
                {
                    MessageBox.Show("Укажите ФИО директора");
                    return;
                }

                if (ComboType.SelectedValue == null)
                {
                    MessageBox.Show("Выберите тип агента");
                    return;
                }

                if (currentAgents.Priority <= 0)
                {
                    MessageBox.Show("Укажите положительный приоритет агента");
                    return;
                }

                // ПРОВЕРКА СТРОКОВЫХ ПОЛЕЙ (чтобы не были длиннее, чем в БД)
                if (currentAgents.Title.Length > 150)
                {
                    MessageBox.Show("Наименование не может быть длиннее 150 символов");
                    return;
                }

                if (currentAgents.INN != null && currentAgents.INN.Length > 12)
                {
                    MessageBox.Show("ИНН не может быть длиннее 12 символов");
                    return;
                }

                if (currentAgents.KPP != null && currentAgents.KPP.Length > 9)
                {
                    MessageBox.Show("КПП не может быть длиннее 9 символов");
                    return;
                }

                if (currentAgents.Phone != null && currentAgents.Phone.Length > 20)
                {
                    MessageBox.Show("Телефон не может быть длиннее 20 символов");
                    return;
                }

                if (string.IsNullOrWhiteSpace(currentAgents.INN))
                {
                    MessageBox.Show("Укажите ИНН агента");
                    return;
                }

                if (string.IsNullOrWhiteSpace(currentAgents.KPP))
                {
                    MessageBox.Show("Укажите КПП агента");
                    return;
                }

                if (string.IsNullOrWhiteSpace(currentAgents.Phone))
                {
                    MessageBox.Show("Укажите телефон агента");
                    return;
                }
                else
                {
                    string digitsOnly = new string(currentAgents.Phone.Where(char.IsDigit).ToArray());
                    if (digitsOnly.Length < 10)
                    {
                        MessageBox.Show("Телефон должен содержать минимум 10 цифр");
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(currentAgents.Email))
                {
                    MessageBox.Show("Укажите почту агента");
                    return;
                }
                else
                {
                    string email = currentAgents.Email.Trim();

                    // Проверка на наличие символа @
                    if (!email.Contains("@"))
                    {
                        MessageBox.Show("Email должен содержать символ @");
                        return;
                    }

                    // Проверка на наличие точки после @
                    string[] parts = email.Split('@');
                    if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                    {
                        MessageBox.Show("Некорректный формат email");
                        return;
                    }

                    // Проверка на наличие домена .ru (или других)
                    if (!parts[1].Contains(".ru") && !parts[1].Contains(".com") && !parts[1].Contains(".net"))
                    {
                        MessageBox.Show("Email должен быть с доменом .ru, .com или .net");
                        return;
                    }

                    // Дополнительная проверка на допустимые символы
                    if (email.Contains(" ") || email.Contains(";") || email.Contains(","))
                    {
                        MessageBox.Show("Email не должен содержать пробелы или спецсимволы");
                        return;
                    }
                }

                // СОХРАНЕНИЕ
                if (currentAgents.ID == 0)
                {
                    _context.Agent.Add(currentAgents);
                }

                _context.SaveChanges();
                MessageBox.Show("Информация сохранена");

                Manager.MainFrame.GoBack();
                if (Manager.MainFrame.Content is AgentPage agentPage)
                {
                    agentPage.RefreshAgents(); // Вызываем наш простой метод
                }
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"Ошибка формата данных: {ex.Message}");
            }
            catch (InvalidCastException ex)
            {
                MessageBox.Show($"Ошибка преобразования типов: {ex.Message}");
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
                // Проверка на наличие продаж
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
    }
}
