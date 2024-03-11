using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HzdTextureLib;

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
            ITexture tex = Images.SelectedItem as ITexture;
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
                }
            }
        }

        private BitmapImage WpfImage(DDSImage image)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = image.DdsImage.Width;
            bitmap.DecodePixelHeight = image.DdsImage.Height;
            image.Stream.Position = 0;
            bitmap.StreamSource = image.Stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private bool UpdateTexture(ITexture tex, string file)
        {
            try
            {
                tex.UpdateImageData(file);
                if (tex == Images.SelectedItem)
                {
                    Preview.Source = WpfImage(tex.Image); // update preview
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while updating texture {tex.Name}: {ex.Message}");
                return false;
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
                        Preview.Source = WpfImage(sel.Image);
                }
            }
        }

        private void ExportTexture(string file, ITexture tex)
        {
            string ext = Path.GetExtension(file);

            if (ext == ".dds")
            {
                tex.WriteDds(file);
            }
            else if(ext == ".png")
            {
                tex.WritePng(file);
            }
            else if(ext == ".tga")
            {
                tex.WriteTga(file);
            }
            else
            {
                throw new HzDException($"Unknown file extension {ext}");
            }
        }

        private void ToolBar_ExportSingle(object sender, RoutedEventArgs e)
        {
            ITexture tex = Images.SelectedItem as ITexture;
            if (tex == null)
            {
                MessageBox.Show("No Texture selected.");
                return;
            }

            using(var dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.InitialDirectory = m_core?.Path;
                dialog.Filter = "Direct Draw Surfaces (*.dds)|*.dds|PNG Image File (*.png)|*.png|Targa Image File (*.tga)|*.tga";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                dialog.DefaultExt = "dds";
                dialog.FileName = tex.Name;
                dialog.Title = $"Export {tex.Name}";
                    
                if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        ExportTexture(dialog.FileName, tex);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occured while exporting: {ex.Message}");
                    }
                }
            }

        }

        private void ToolBar_ExportAll(object sender, RoutedEventArgs e)
        {
            if (m_core == null)
            {
                MessageBox.Show("No core file open.");
                return;
            }

            using(var dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.InitialDirectory = m_core?.Path;
                dialog.Filter = "Direct Draw Surfaces (*.dds)|*.dds|PNG Image File (*.png)|*.png|Targa Image File (*.tga)|*.tga";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                dialog.DefaultExt = "dds";
                dialog.Title = $"Export all textures";
                    
                if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        string path = Path.GetDirectoryName(dialog.FileName);
                        string baseName = Path.GetFileNameWithoutExtension(dialog.FileName);
                        string ext = Path.GetExtension(dialog.FileName);
                        foreach (Texture tex in m_core.Textures)
                        {
                            ExportTexture($"{path}\\{baseName}_{tex.Name}{ext}", tex);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occured while exporting: {ex.Message}");
                    }
                }
            }
        }

        private void LoadCoreFile(String path)
        {
            string ext = Path.GetExtension(path);
            try
            {
                m_core = new HzDCore(path);

                Debug.WriteLine("Loaded {path}");

                Images.Items.Clear();
                Images.DisplayMemberPath = "Name";

                foreach (ITexture tex in m_core.Textures)
                {
                    Images.Items.Add(tex);
                }

                foreach (UITexture uitex in m_core.UITextures)
                {
                    foreach (ITexture tex in uitex.TextureItems)
                    {
                        Images.Items.Add(tex);
                    }
                }
                Images.SelectedItem = Images.Items.GetItemAt(0);

            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occured while loading: {e.Message}");
            }
        }

        private void AddInfo(InfoItem item)
        {
            String str = $"{item.Title}: {item.Value}";
            Info.Items.Add(str);
        }

        private void Image_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Info.Items.Clear();

            if (e.AddedItems.Count == 0)
                return;

            ITexture tex = e.AddedItems[0] as ITexture;

            if (tex == null)
                return;

            IList<InfoItem> infos = tex.Info;

            foreach (var info in infos)
            {
                AddInfo(info);
            }

            try
            {
                Preview.Source = WpfImage(tex.Image);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while loading: {ex.Message}");
            }

        }
    }
}
