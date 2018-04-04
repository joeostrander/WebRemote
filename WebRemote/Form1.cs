using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;

namespace WebRemote
{
    public partial class Form1 : Form
    {

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, int dwData, UIntPtr dwExtraInfo);
        const int MOUSEEVENTF_LEFTDOWN = 0x2;
        const int MOUSEEVENTF_LEFTUP = 0x4;
        const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        const int MOUSEEVENTF_RIGHTUP = 0x10;
        const int MOUSEEVENTF_WHEEL = 0x0800;

        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        const int MOUSEEVENTF_MOVE = 0x0001;


        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        private static HttpListener httpListener;
        private static int port;
        private bool boolListening = false;

        private bool isElevated;

        private DateTime lastSnapshot;

        private long longImageQuality = 50;


        #region *** USER VARIABLES ***
        private static int intervalSeconds = 5;
        private static String strFilePath = "capture.jpg";

        //TO DO add textbox for filepath/name

        #endregion

        public Form1()
        {
            InitializeComponent();
            this.Text = Application.ProductName;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            labelStatus.Text = "";
            lastSnapshot = DateTime.Now.AddHours(-1);

            comboBoxImageQuality.Items.Add(0);
            for (int i = 10; i <= 100; i+=10)
            {
                comboBoxImageQuality.Items.Add(i);
            }
            comboBoxImageQuality.SelectedIndex = 5;

            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            if (!isElevated)
            {
                //MessageBox.Show("You may need to run this app elevated (as administrator)!", "Run this as Administrator", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            
        }

        private void buttonStartStop_Click(object sender, EventArgs e)
        {
            if (boolListening)
            {
                //Stop it
                ((Button)sender).Text = "&Start";
                boolListening = false;
                httpListener.Stop();
                httpListener.Close();
                labelStatus.Text = "Idle.";
            }
            else
            {
                //Start it
                Int32.TryParse(textBoxInterval.Text, out intervalSeconds);
                if (intervalSeconds < 1)
                {
                    intervalSeconds = 5;
                    textBoxInterval.Text = intervalSeconds.ToString();
                }

                longImageQuality = 100;
                long.TryParse(comboBoxImageQuality.Text, out longImageQuality);

                Int32.TryParse(textBoxPort.Text,out port);
                if (port>0)
                {
                    if (StartWebServer())
                    {
                        GetWindow();
                        ((Button)sender).Text = "&Stop";
                        labelStatus.Text = "Listening as:  http://" + Environment.MachineName + ":" + port;
                    }
                    else
                    {
                        //failed to start web...
                        return;
                    }
                }
                               
            }


            textBoxPort.Enabled = !boolListening;
            comboBoxImageQuality.Enabled = !boolListening;
            checkBoxTimestamp.Enabled = !boolListening;
            textBoxInterval.Enabled = !boolListening;


        }



        private void GetWindow()
        {
            CaptureDesktop();
            lastSnapshot = DateTime.Now;
        }

        private void CaptureDesktop()
        {
            ScreenShot.ScreenCapture SC = new ScreenShot.ScreenCapture();

            try
            {
                Image bmTemp;
                bmTemp = SC.CaptureScreen();
                

                using (Graphics g = Graphics.FromImage(bmTemp))
                {



                    if (checkBoxTimestamp.Checked)
                    {
                        String watermarkText = "Screenshot time:  " + (DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"));
                        Font watermarkFont = new Font("Arial", 20);

                        // Measure string.
                        SizeF stringSize = new SizeF();
                        stringSize = g.MeasureString(watermarkText, watermarkFont, bmTemp.Width);

                        int posx = (int)(bmTemp.Width / 2 - stringSize.Width / 2);
                        int posy = 0;
                        Rectangle rect = new Rectangle(posx, posy, (int)stringSize.Width + 1, (int)stringSize.Height + 1);

                        Pen mypen = new Pen(Brushes.CornflowerBlue, 6);

                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.FillRectangle(Brushes.Black, rect);
                        g.DrawRectangle(mypen, rect);
                        g.DrawString(watermarkText, watermarkFont, new SolidBrush(Color.CornflowerBlue), posx + ((int)mypen.Width / 2), posy + ((int)mypen.Width / 2));
                        Console.WriteLine("Width: {0}, Height:  {1}", bmTemp.Width, bmTemp.Height);
                        Console.WriteLine("String Width: {0}, String Height:  {1}", stringSize.Width, stringSize.Height);
                        Console.WriteLine("X Pos: {0}, Y Pos:  {1}", bmTemp.Width / 2 - stringSize.Width / 2, bmTemp.Height / 2 - stringSize.Height / 2);
                    }


                    ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                    System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                    //long myVal = 100;
                    //Int64.TryParse("50", out myVal);
                    //Byte.TryParse(comboBoxImageQuality.Text, out myVal);
                    EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, longImageQuality);
                    myEncoderParameters.Param[0] = myEncoderParameter;


                    bmTemp.Save(strFilePath, jpgEncoder, myEncoderParameters);
                    //output += "Window captured to:  " + strFilePath + "\n";
                    Console.WriteLine("Window captured to:  {0}", strFilePath);
                    bmTemp.Dispose();
                }
                
                               

            }
            catch (Exception ex)
            {
                //output += ex.Message + "\n";
                Console.WriteLine("Error:  {0}", ex.Message);
            }

        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        private bool StartWebServer()
        {

            bool boolSuccess = false;
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://+:" + port.ToString() + "/");  //must run elevated to do this... otherwise use localhost
                httpListener.Start();

                Console.WriteLine("listening on port {0}",port.ToString() + "...");
                boolListening = true;

                receive(ref httpListener);

                boolSuccess = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start httplistener on port " + port.ToString() + "\n\n" + ex.Message, "Failed!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                if (!isElevated)
                {

                    if (MessageBox.Show("Click OK to run:\n\nnetsh http add urlacl url=http://+:" + port.ToString() + "/ user=Everyone", "Try netsh...", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo("c:\\windows\\system32\\netsh.exe");
                        startInfo.Arguments = "http add urlacl url=http://+:" + port.ToString() + "/ user=Everyone";
                        startInfo.Verb = "runas";
                        System.Diagnostics.Process.Start(startInfo);
                        System.Threading.Thread.Sleep(5000);
                        boolSuccess = StartWebServer();
                    }


                }
            }

            return boolSuccess;
        }

        private void receive(ref HttpListener listener)
        {
            IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);

        }


        private void ListenerCallback(IAsyncResult result)
        {

            HttpListener listener = result.AsyncState as HttpListener;

            if (listener.IsListening)
            {

                HttpListenerContext context;
                try
                {
                    context = listener.EndGetContext(result);
                }
                catch (HttpListenerException hle)
                {
                    Console.WriteLine(hle.Message);
                    return;
                }
                
                HttpListenerRequest request = context.Request;
                
                if (System.IO.File.Exists(strFilePath))
                {
                   
                    Console.WriteLine("**** RAW URL:  {0}",request.RawUrl);
                    
                    HttpListenerResponse response = context.Response;

                    if (request.RawUrl.Contains("/capture"))
                    {
                        //how old is the image?
                        TimeSpan ts;
                        ts = DateTime.Now - lastSnapshot;
                        if (ts.TotalSeconds >= intervalSeconds)
                        {
                            Console.WriteLine("image is old...");
                            GetWindow();
                        }
                        else
                        {
                            Console.WriteLine("image is fine");
                        }

                        //send image
                        //Open image as byte stream to send to requestor
                        FileInfo fInfo = new FileInfo(strFilePath);
                        long numBytes = fInfo.Length;
                        
                        try
                        {
                            FileStream fStream = new FileStream(strFilePath, FileMode.Open, FileAccess.Read);

                            BinaryReader br = new BinaryReader(fStream);

                            byte[] buffer = br.ReadBytes((int)numBytes);

                            br.Close();

                            fStream.Close();

                            response.ContentType = "image/jpeg";
                            response.ContentLength64 = buffer.Length;

                            Stream OutputStream = response.OutputStream;
                            OutputStream.Write(buffer, 0, buffer.Length);
                            OutputStream.Close();

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }



                    }
                    else if (request.RawUrl.Contains("/mouse"))
                    {
                        

                        Uri myUri = new Uri("http://localhost"+request.RawUrl);
                        //string paramButton = HttpUtility.ParseQueryString(myUri.Query).Get("button");
                        string paramAction = HttpUtility.ParseQueryString(myUri.Query).Get("action");
                        string paramX = HttpUtility.ParseQueryString(myUri.Query).Get("x");
                        string paramY = HttpUtility.ParseQueryString(myUri.Query).Get("y");
                        
                        double multX;
                        double multY;
                        double.TryParse(paramX, out multX);
                        double.TryParse(paramY, out multY);
                        
                        int pos_x = (int)(Screen.PrimaryScreen.Bounds.Width * multX);
                        int pos_y = (int)(Screen.PrimaryScreen.Bounds.Height * multY);



                        Console.WriteLine("*** MOUSE {0} REQUEST ***",paramAction.ToUpper());
                        Console.WriteLine("X = {0}, Y = {1}", pos_x, pos_y);
           
                        if (paramAction.ToLower() == "leftdown")
                        {
                            LeftDown(pos_x, pos_y);
                            //if (paramButton.ToLower()=="left")
                            //{
                            //    LeftDown(pos_x, pos_y);
                            //}
                            //if (paramButton.ToLower() == "right")
                            //{
                            //    RightDown(pos_x, pos_y);
                            //}
                                
                        }
                        else if (paramAction.ToLower() == "leftup")
                        {
                            LeftUp(pos_x, pos_y);
                        }
                        else if (paramAction.ToLower() == "rightdown")
                        {
                            RightDown(pos_x, pos_y);
                        }
                        else if (paramAction.ToLower() == "rightup")
                        {
                            RightUp(pos_x, pos_y);
                        }
                        else if (paramAction.ToLower() == "move")
                        {
                            Cursor.Position = new Point(pos_x, pos_y);
                        }




                        //Console.WriteLine(sb.ToString());
                        try
                        {

                            byte[] buffer = Encoding.UTF8.GetBytes("<html><body>OK...MOUSE "+paramAction.ToUpper()+"</body></html>");
                            response.ContentLength64 = buffer.Length;
                            Stream OutputStream = response.OutputStream;
                            try
                            {
                                OutputStream.Write(buffer, 0, buffer.Length);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            OutputStream.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }


                    }
                    else
                    {
                        //send html
                        string responseString = Properties.Resources.index;
                        

                        //String responseString = String.Format(pattern, intervalSeconds.ToString());
                        // interpolation in multiline verbatim string literals gets tricky... let's do it manually
                        responseString = responseString.Replace("__INTERVAL__", intervalSeconds.ToString());

                        try
                        {

                            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = buffer.Length;
                            Stream OutputStream = response.OutputStream;
                            try
                            {
                                OutputStream.Write(buffer, 0, buffer.Length);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            OutputStream.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }


                    }

                            
                    

                    

 

                    if (boolListening)
                    {
                        receive(ref listener);
                    }
                }

                
                

            }

        }

        #region MOUSE STUFF
        private void LeftDown(int x,int y)
        {
            Cursor.Position = new Point(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Console.WriteLine("I'll left-click down at {0}x{1}", x, y);
        }

        private void LeftUp(int x, int y)
        {
            Cursor.Position = new Point(x, y);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            Console.WriteLine("I'll left-click up at {0}x{1}", x, y);
        }

        private void RightDown(int x, int y)
        {
            Cursor.Position = new Point(x, y);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            Console.WriteLine("I'll right-click down at {0}x{1}", x, y);
        }

        private void RightUp(int x, int y)
        {
            Cursor.Position = new Point(x, y);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            Console.WriteLine("I'll right-click up at {0}x{1}", x, y);
        }

        #endregion


    }
}
