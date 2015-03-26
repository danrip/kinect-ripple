using System;
using System.Windows;
using System.Windows.Input;

namespace RippleEditor.Utilities
{
    /// <summary>
    /// Interaction logic for NewTemplateSelectorBox.xaml
    /// </summary>
    public partial class NewTemplateSelectorBox : Window
    {
        public TemplateOptions SelectedItem { get; set; }
        public NewTemplateSelectorBox()
        {
            InitializeComponent();
            TemplateOptionsBox.Items.Clear();
            foreach (var tempType in Enum.GetNames(typeof(TemplateOptions)))
            {
                TemplateOptionsBox.Items.Add(tempType);
            }
            TemplateOptionsBox.SelectedIndex = 0;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = (TemplateOptions)TemplateOptionsBox.SelectedIndex;
            DialogResult = true;
        }

        #region Header Drag Bar
        private void CloseButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
        #endregion
    }
}
