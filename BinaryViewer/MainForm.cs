using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace BinaryViewer

{

    public partial class MainForm : Form
    {
        private string filePath;

        OpenFileDialog openFile = new OpenFileDialog();
        CancellationTokenSource _tokenSource;
        public MainForm()
        {
            InitializeComponent();
        }
        private delegate void UpdateTextDelegate(string text);
        private delegate void ChangeStatus(Button b, bool status);

        //Чтение файла
        private async void WriteTextAsynchronously()
        {

            _tokenSource = new CancellationTokenSource();
            CancellationToken cancelToken = _tokenSource.Token;
            try
            {
                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    textBoxContent.Clear();
                    filePath = openFile.FileName;
                }
                else return;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            await Task.Run(() =>
            {
                try
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        byte[] buffer = new byte[16];
                        int bytesRead;
                        long offset = 0;

                        StringBuilder charBuilder = new StringBuilder();
                        ProgramStatusRead();

                        while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            string text = $"0x{offset:X8}: ";

                            for (int i = 0; i < bytesRead; i++)
                            {
                                text += $"{buffer[i]:X2} ";
                                byte b = buffer[i];
                                if (b < 32 || b > 127) charBuilder.Append('.');
                                else charBuilder.Append((char)b);

                                if (i == bytesRead / 2 - 1)
                                    text += "| ";
                                if (i == bytesRead - 1)
                                {
                                    text += "| ";
                                    text += charBuilder.ToString();
                                    charBuilder.Clear();
                                }
                            }

                            text += Environment.NewLine;

                            Invoke(new UpdateTextDelegate(UpdateTextBoxContent), text);

                            offset += bytesRead;
                            cancelToken.ThrowIfCancellationRequested();

                        }
                        ProgramStatusNormal();
                    }
                }
                //обработка исключения при отмене чтения
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    ProgramStatusNormal();
                }
            });
        }
        //Изменение состояния кнопок при чтении файла
        private void ProgramStatusRead()
        {
            Invoke(new ChangeStatus(ChangeButtonStatus), buttonReadFile, false);
            Invoke(new ChangeStatus(ChangeButtonStatus), buttonFindOffset, false);
            Invoke(new ChangeStatus(ChangeButtonStatus), buttonCancel, true);
        }
        //Изменение состояния кнопок если чтение не происходит
        private void ProgramStatusNormal()
        {
            Invoke(new ChangeStatus(ChangeButtonStatus), buttonReadFile, true);
            Invoke(new ChangeStatus(ChangeButtonStatus), buttonFindOffset, true);
            Invoke(new ChangeStatus(ChangeButtonStatus), buttonCancel, false);
        }
        private void ChangeButtonStatus(Button b, bool status)
        {
            b.Enabled = status;
        }

        //Обновление текстового поля
        private void UpdateTextBoxContent(string text)
        {
            textBoxContent.AppendText(text);
        }

        //Кнопка чтения
        private void ButtonReadFile_Click(object sender, EventArgs e)
        {
            WriteTextAsynchronously();
        }
        //Кнопка поиска по смщенеию
        private void FindOffset_Click(object sender, EventArgs e)
        {
            try
            {
                int index = 0;
                string needFind = textBox1.Text;
                if (needFind.Length != 8) throw new Exception("Неверно введённый формат");
                index = textBoxContent.Find(needFind, index, RichTextBoxFinds.MatchCase);
                if (index != -1)
                {
                    textBoxContent.SelectionStart = index;
                    textBoxContent.ScrollToCaret();
                    textBoxContent.Focus();
                }
                else
                {
                    throw new Exception("Смещение не найдено");
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //Кнопка для отмены чтения
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            _tokenSource.Cancel();
        }
    }
}

