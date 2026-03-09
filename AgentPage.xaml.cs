using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для AgentPage.xaml
    /// </summary>
    public partial class AgentPage : Page
    {
        private int PageSize = 10;                  // Количество записей на странице
        private List<Agent> CurrentPageList = new List<Agent>();
        private int currentPage = 1;                 // Текущая страница (начиная с 1)
        private List<Agent> _filteredAgents;         // Отфильтрованные агенты

        public AgentPage()
        {
            InitializeComponent();
            ComboType.SelectedIndex = 0;
            ComboSort.SelectedIndex = 0;
            UpdateAgents();
        }

        private void ChangePage()
        {
            // Очищаем список номеров страниц
            PageListBox.Items.Clear();

            // Вычисляем количество страниц
            int totalPages = (_filteredAgents.Count + PageSize - 1) / PageSize;
            if (totalPages == 0) totalPages = 1;

            // Заполняем список номеров страниц
            for (int i = 1; i <= totalPages; i++)
            {
                PageListBox.Items.Add(i);
            }
            PageListBox.SelectedItem = currentPage;

            // Получаем данные для текущей страницы
            var agentsPage = _filteredAgents
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Отображаем данные
            AgentsListView.ItemsSource = agentsPage;

            // Обновляем информацию о записях
            /*int startRecord = (currentPage - 1) * PageSize + 1;
            int endRecord = Math.Min(currentPage * PageSize, _filteredAgents.Count);
            TBCount.Text = endRecord.ToString();
            TBAllRecords.Text = _filteredAgents.Count.ToString();*/
        }

        private void UpdateAgents()
        {
            try
            {
                var currentAgents = GostenovaGlaskiSaveEntities.GetContext()
                    .Agent
                    .Include("AgentType")
                    .Include("ProductSale")
                    .ToList();

                // 1. фильтрация по типу агента
                if (ComboType.SelectedIndex > 0)
                {
                    string selectedType = (ComboType.SelectedItem as TextBlock).Text;
                    currentAgents = currentAgents.Where(p => p.AgentTypeTitle == selectedType).ToList();
                }

                // 2. поиск по наименованию и контактным данным (email и телефон)
                if (!string.IsNullOrEmpty(TBSearch.Text))
                {
                    string searchText = TBSearch.Text.ToLower();
                    // Используем Replace для замены некорректных символов
                    searchText = searchText.Replace("ё", "е").Replace("Ё", "Е");
                    string cleanedSearchPhone = searchText
                        .Replace("+", "")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Replace("8", "7");

                    currentAgents = currentAgents.Where(p =>
                        (p.Title != null && p.Title.ToLower().Contains(searchText)) ||
                        (p.Email != null && p.Email.ToLower().Contains(searchText)) ||
                        (p.Phone != null && p.Phone
                        .Replace("+", "")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Replace("8", "7")
                        .Contains(cleanedSearchPhone))).ToList();
                }

                // 3. сортировка
                if (ComboSort.SelectedIndex > 0)
                {
                    string sortType = (ComboSort.SelectedItem as TextBlock).Text;

                    switch (sortType)
                    {
                        // Наименование
                        case "наименование по возрастанию":
                            currentAgents = currentAgents.OrderBy(p => p.Title).ToList();
                            break;
                        case "наименование по убыванию":
                            currentAgents = currentAgents.OrderByDescending(p => p.Title).ToList();
                            break;

                        // Скидка
                        case "скидка по возрастанию":
                            currentAgents = currentAgents.OrderBy(p => p.Discount).ToList();
                            break;
                        case "скидка по убыванию":
                            currentAgents = currentAgents.OrderByDescending(p => p.Discount).ToList();
                            break;

                        // Приоритет
                        case "приоритет по возрастанию":
                            currentAgents = currentAgents.OrderBy(p => p.Priority).ToList();
                            break;
                        case "приоритет по убыванию":
                            currentAgents = currentAgents.OrderByDescending(p => p.Priority).ToList();
                            break;

                        // Если выбран заголовок "Все" или "Сортировка" - не сортируем
                        default:
                            break;
                    }
                }

                _filteredAgents = currentAgents;
                currentPage = 1;
                ChangePage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        public void RefreshAgents()
        {
            UpdateAgents();
        }

        private void TBSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAgents();
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAgents();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAgents();
        }

        private void LeftDirBtn_Clik(object sender, RoutedEventArgs e)
        {
            int totalPages = (_filteredAgents.Count + PageSize - 1) / PageSize;
            if (currentPage > 1)
            {
                currentPage--;
                ChangePage();
            }
        }

        private void PageListBox_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PageListBox.SelectedItem != null)
            {
                int page = (int)PageListBox.SelectedItem;
                if (page != currentPage)
                {
                    currentPage = page;
                    ChangePage();
                }
            }
        }

        private void RightDirBtn_Clik(object sender, RoutedEventArgs e)
        {
            int totalPages = (_filteredAgents.Count + PageSize - 1) / PageSize;
            if (currentPage < totalPages)
            {
                currentPage++;
                ChangePage();
            }
        }

        private void AddAgent_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddAgentPage(null));
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddAgentPage(AgentsListView.SelectedItem as Agent));
        }

        private void AgentsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AgentsListView.SelectedItems.Count > 0)
            {
                ChangePriorityBtn.Visibility = Visibility.Visible;
            }
            else
            {
                ChangePriorityBtn.Visibility = Visibility.Hidden;
            }
        }

        private void ChangePriorityBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AgentsListView.SelectedItems.Count == 0)
                return;

            // Находим максимальный приоритет среди выбранных
            int maxPriority = 0;
            foreach (Agent selectedAgent in AgentsListView.SelectedItems)
            {
                if (selectedAgent.Priority > maxPriority)
                {
                    maxPriority = selectedAgent.Priority;
                }
            }

            // Создаем и открываем окно ввода нового приоритета
            // Вам нужно создать это окно (PriorChange.xaml) аналогично коду подруги
             PriorChange priorWindow = new PriorChange(maxPriority);
            priorWindow.ShowDialog();

            // Получаем новый приоритет из окна
            if (int.TryParse(priorWindow.TBPriority.Text, out int newPriority))
            {
                // Обновляем приоритет для всех выбранных агентов
                foreach (Agent agent in AgentsListView.SelectedItems)
                {
                    agent.Priority = newPriority;
                }

                try
                {
                    GostenovaGlaskiSaveEntities.GetContext().SaveChanges();
                    UpdateAgents(); // Обновляем список
                    MessageBox.Show("Приоритеты обновлены");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                // Полностью очищаем локальный кэш
                var context = GostenovaGlaskiSaveEntities.GetContext();

                // Отсоединяем ВСЕ загруженные сущности
                foreach (var entry in context.ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }
                UpdateAgents();
            }
        }
    }
}