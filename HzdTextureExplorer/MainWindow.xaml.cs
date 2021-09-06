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
        private void ToolBar_Save(object sender, RoutedEventArgs e)
        {
            SaveCoreFile();
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

            m_core = new HzDCore(path);

            Debug.WriteLine("Loaded {path}");

            Images.Items.Clear();
            Images.DisplayMemberPath = "Name";

            foreach(Texture tex in m_core.Textures)
            {
                Images.Items.Add(tex);
            }
        }

        private void SaveCoreFile()
        {
            // todo
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

            Stream textureStream = m_core.OpenTexture(tex);

            Preview.Source = tex.Image.Bitmap;
        }
    }
}
