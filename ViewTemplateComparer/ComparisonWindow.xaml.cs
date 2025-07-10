using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace ViewTemplateComparer
{
    public partial class ComparisonWindow : Window
    {
        private readonly UIDocument _uidoc;
        private readonly Document _doc;
        private readonly List<View> _allTemplates;
        private readonly Dictionary<ViewType, string> _viewTypeMap;

        public ComparisonWindow(UIDocument uidoc)
        {
            InitializeComponent();
            _uidoc = uidoc;
            _doc = uidoc.Document;

            // Получаем все шаблоны видов в проекте
            _allTemplates = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate)
                .OrderBy(v => v.Name)
                .ToList();

            // Создаем словарь для фильтрации
            _viewTypeMap = new Dictionary<ViewType, string>
            {
                { ViewType.FloorPlan, "Планы этажей" },
                { ViewType.CeilingPlan, "Планы потолков" },
                { ViewType.Elevation, "Фасады" },
                { ViewType.Section, "Разрезы" },
                { ViewType.ThreeD, "3D виды" },
                { ViewType.DraftingView, "Чертежные виды" },
                { ViewType.Legend, "Легенды" },
                { ViewType.Schedule, "Спецификации" }
            };

            InitializeControls();
        }

        private void InitializeControls()
        {
            // Заполняем фильтр по типу вида
            ViewTypeFilterComboBox.Items.Add("Все типы");
            foreach (var viewTypeName in _viewTypeMap.Values.OrderBy(name => name))
            {
                ViewTypeFilterComboBox.Items.Add(viewTypeName);
            }
            ViewTypeFilterComboBox.SelectedIndex = 0;

            // Первичное заполнение списков шаблонов
            PopulateTemplateComboBoxes();
        }

        private void PopulateTemplateComboBoxes()
        {
            var selectedFilter = ViewTypeFilterComboBox.SelectedItem as string;

            ViewType? targetViewType = null;
            if (selectedFilter != "Все типы")
            {
                targetViewType = _viewTypeMap.FirstOrDefault(kvp => kvp.Value == selectedFilter).Key;
            }

            var filteredTemplates = _allTemplates;
            if (targetViewType.HasValue)
            {
                filteredTemplates = _allTemplates.Where(t => t.ViewType == targetViewType.Value).ToList();
            }

            // Сохраняем выбранные элементы, чтобы восстановить их после обновления
            var selectedTemplate1 = Template1ComboBox.SelectedItem as View;
            var selectedTemplate2 = Template2ComboBox.SelectedItem as View;

            Template1ComboBox.ItemsSource = filteredTemplates;
            Template1ComboBox.DisplayMemberPath = "Name";

            Template2ComboBox.ItemsSource = filteredTemplates;
            Template2ComboBox.DisplayMemberPath = "Name";

            // Пытаемся восстановить выбор
            if (selectedTemplate1 != null && filteredTemplates.Contains(selectedTemplate1))
                Template1ComboBox.SelectedItem = selectedTemplate1;

            if (selectedTemplate2 != null && filteredTemplates.Contains(selectedTemplate2))
                Template2ComboBox.SelectedItem = selectedTemplate2;
        }


        private void ViewTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateTemplateComboBoxes();
        }

        private void TemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Кнопка "Сравнить" активна только если выбраны два РАЗНЫХ шаблона
            CompareButton.IsEnabled = Template1ComboBox.SelectedItem != null &&
                                      Template2ComboBox.SelectedItem != null &&
                                      Template1ComboBox.SelectedItem != Template2ComboBox.SelectedItem;
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            var template1 = Template1ComboBox.SelectedItem as View;
            var template2 = Template2ComboBox.SelectedItem as View;

            if (template1 == null || template2 == null)
            {
                TaskDialog.Show("Ошибка", "Пожалуйста, выберите два шаблона для сравнения.");
                return;
            }

            // Диалог сохранения файла
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "HTML файл (*.html)|*.html",
                Title = "Сохранить отчет о сравнении",
                FileName = $"Сравнение_{template1.Name}_vs_{template2.Name}.html"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Создаем экземпляр нашего компаратора
                    var comparer = new TemplateComparer(_doc, template1, template2);
                    // Получаем результаты
                    var results = comparer.Compare();

                    // Создаем экземпляр генератора HTML
                    var htmlGenerator = new HtmlGenerator(template1, template2);
                    // Генерируем HTML
                    string htmlContent = htmlGenerator.Generate(results);

                    // Сохраняем в файл
                    File.WriteAllText(saveFileDialog.FileName, htmlContent);

                    TaskDialog.Show("Успех", $"Отчет успешно сохранен по пути:\n{saveFileDialog.FileName}");
                    this.Close();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Критическая ошибка", $"Произошла ошибка при создании отчета: {ex.Message}");
                }
            }
        }
    }
}