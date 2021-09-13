using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace HzdTextureExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HzDCore m_core = null;

        public MainWindow()
        {

            Debug.WriteLine("Loaded");

            InitializeComponent();
        }

        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.

                String file = files[0];

                LoadCoreFile(file);

            }
        }

        private void ToolBar_Open(object sender, RoutedEventArgs e)
        {
            using(var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.InitialDirectory = m_core?.Path;
                dialog.Filter = "HzD Core Files (*.core)|*.core";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;

                if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LoadCoreFile(dialog.FileName);
                }
            }
        }
        private void ToolBar_UpdateSingle(object sender, RoutedEventArgs e)
        {
            Texture tex = Images.SelectedItem as Texture;
            if (tex == null)
            {
                MessageBox.Show("No Texture selected.");
                return;
            }

            using(var dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.InitialDirectory = m_core?.Path;
                dialog.Filter = "Direct Draw Surfaces (*.dds)|*.dds";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;

                if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    UpdateTexture(tex, dialog.FileName);
                    MessageBox.Show($"{tex.Name} updated from {dialog.FileName}.");
                }
            }
        }

        private void UpdateTexture(Texture tex, string file)
        {
            try
            {
                tex.UpdateImageData(file);
                if (tex == Images.SelectedItem)
                {
                    Preview.Source = tex.Image.Bitmap; // update preview
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while updating texture {tex.Name}: {ex.Message}");
            }

        }


        private void ToolBar_UpdateAll(object sender, RoutedEventArgs e)
        {
            if (m_core == null)
            {
                MessageBox.Show("No core file open.");
                return;
            }

            using(var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = false;

                if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (Texture tex in m_core.Textures)
                    {
                        string path = $"{dialog.SelectedPath}\\{tex.Name}.dds";

                        UpdateTexture(tex, path);
                    }

                    Texture sel = Images.SelectedItem as Texture;
                    if(sel != null)
                        Preview.Source = sel.Image.Bitmap;
                    MessageBox.Show($"{m_core.Textures.Count} textures updated from {dialog.SelectedPath}.");
                }
            }

        }

        private void ToolBar_ExportSingle(object sender, RoutedEventArgs e)
        {
            Texture tex = Images.SelectedItem as Texture;
            if (tex == null)
            {
                MessageBox.Show("No Texture selected.");
                return;
            }

            try
            {
                string path = Path.GetDirectoryName(m_core.Path);
                string file = $"{path}/{tex.Name}.dds";
                tex.WriteDds(file);
                MessageBox.Show($"Exported to {file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while exporting: {ex.Message}");
            }
        }

        private void ToolBar_ExportAll(object sender, RoutedEventArgs e)
        {
            if (m_core == null)
            {
                MessageBox.Show("No core file open.");
                return;
            }

            try
            {
                string path = Path.GetDirectoryName(m_core.Path);
                foreach (Texture tex in m_core.Textures)
                {
                    string file = $"{path}/{tex.Name}.dds";
                    tex.WriteDds(file);
                }

                MessageBox.Show($"Exported {m_core.Textures.Count} files to {path}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while exporting: {ex.Message}");
            }
        }

        private void Context_Export(object sender, RoutedEventArgs e)
        {
            Texture tex = e.Source as Texture;
            if (tex == null)
                return;
            try
            {
                string path = Path.GetDirectoryName(m_core.Path);
                string file = $"{path}\\{tex.Name}.dds";
                tex.WriteDds(file);
                MessageBox.Show($"Exported to {file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while exporting: {ex.Message}");
            }
        }

        private void Context_Update(object sender, RoutedEventArgs e)
        {

        }

        private void LoadCoreFile(String path)
        {
            string ext = Path.GetExtension(path);
            if(ext != ".core")
            {
                MessageBox.Show($"Invalid extension {ext}. Only .core files supported.");
                return;
            }

            string stream = path + ".stream";
            if(!File.Exists(stream))
            {
                MessageBox.Show($"No .core.stream file found at {stream}. Both core and stream file is required.");
                return;
            }

            try
            {

                m_core = new HzDCore(path);

                Debug.WriteLine("Loaded {path}");

                Images.Items.Clear();
                Images.DisplayMemberPath = "Name";

                foreach (Texture tex in m_core.Textures)
                {
                    Images.Items.Add(tex);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occured while loading: {e.Message}");
            }
        }

        private void AddInfo(string title, object item)
        {
            String str = $"{title}: {item.ToString()}";
            Info.Items.Add(str);
        }

        private void Image_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Info.Items.Clear();

            if (e.AddedItems.Count == 0)
                return;

            Texture tex = e.AddedItems[0] as Texture;
            ImageData data = tex.ImageData;

            AddInfo("Width", data.Width);
            AddInfo("Height", data.Height);
            AddInfo("Format", data.Format.ToString());

            try
            {
                Preview.Source = tex.Image.Bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while loading: {ex.Message}");
            }

        }
    }
}
