﻿using Alturos.Yolo;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FutureNNAimbot
{
    public class TrainingApp
    {
        private bool screenshotMode;
        private GameProcess gp;
        private gController gc;
        private NeuralNet nn;
        Settings settings;
        public Rectangle trainBox;

        static Random random = new Random();

        public string[] TrainingNames { get; }

        public TrainingApp(GameProcess gp, gController gc, NeuralNet nn)
        {
            this.gp = gp;
            this.nn = nn;
            this.gc = gc;
            this.settings = gp.s;
            trainBox = new Rectangle(0, 0, settings.SizeX / 2, settings.SizeY / 2);

            trainBox.X = settings.SizeX / 2 - trainBox.Width / 2;
            trainBox.Y = settings.SizeY / 2 - trainBox.Height / 2;
            TrainingNames = File.ReadAllLines($"trainfiles/{settings.Game}.names");
        }

        public void startTrainingMode()
        {
            var Game = gp;
            File.Copy("defaultfiles/default_trainmore.cmd", $"darknet/{Game}_trainmore.cmd", true);
            if (File.Exists($"trainfiles/{Game}.cfg"))
                File.Copy($"trainfiles/{Game}.cfg", $"darknet/{Game}.cfg", true);
            else
                File.Copy("defaultfiles/default.cfg", $"darknet/{Game}.cfg", true);

            File.Copy("defaultfiles/default.conv.15", $"darknet/{Game}.conv.15", true);
            File.Copy("defaultfiles/default.data", $"darknet/data/{Game}.data", true);

            if (File.Exists($"trainfiles/{Game}.names"))
                File.Copy($"trainfiles/{Game}.names", $"darknet/{Game}.names", true);
            else
                File.Copy("defaultfiles/default.names", $"darknet/data/{Game}.names", true);

            File.Copy("defaultfiles/default.txt", $"darknet/data/{Game}.txt", true);
            File.Copy("defaultfiles/default.cmd", $"darknet/{Game}.cmd", true);

            Console.Write("How many objects will the NN be analyzing and training on? Write each object's name via the separator ',' without spaces (EX: 1,2): ");
            nn.TrainingNames = Console.ReadLine().Split(',');
        }


        public void ReadInput()
        {
            int rand = random.Next(5000, 999999);
            if (User32.GetAsyncKeyState(settings.ScreenshotModeKey) == -32767)
            {
                screenshotMode = screenshotMode == true ? false : true;
            }
            if (User32.GetAsyncKeyState(Keys.Left) != 0)
            {
                if (trainBox.Width <= 0)
                {
                    return;
                }
                else trainBox.Width -= 1;
            }

            if (User32.GetAsyncKeyState(Keys.Down) != 0)
            {
                if (trainBox.Height >= settings.SizeY)
                {
                    return;
                }
                else trainBox.Height += 1;
            }

            if (User32.GetAsyncKeyState(Keys.Right) != 0)
            {
                if (trainBox.Width >= settings.SizeX)
                {
                    return;
                }
                else trainBox.Width += 1;
            }

            if (User32.GetAsyncKeyState(Keys.Up) != 0)
            {
                if (trainBox.Height <= 0)
                {
                    return;
                }
                else trainBox.Height -= 1;
            }




            if (User32.GetAsyncKeyState(settings.ScreenshotKey) == -32767)
            {
                float relative_center_x = (float)(trainBox.X + trainBox.Width / 2) / settings.SizeX;
                float relative_center_y = (float)(trainBox.Y + trainBox.Height / 2) / settings.SizeY;
                float relative_width = (float)trainBox.Width / settings.SizeX;
                float relative_height = (float)trainBox.Height / settings.SizeY;
                
                gc.saveCapture(true, $"darknet/data/img/{settings.Game}{rand}.png");
                File.WriteAllText($"darknet/data/img/{settings.Game}{rand}.txt", string.Format("{0} {1} {2} {3} {4}", settings.selectedObject, relative_center_x, relative_center_y, relative_width, relative_height).Replace(",", "."));

                Console.Beep();
            }

            if (User32.GetAsyncKeyState(Keys.Back) == -32767)
            {

                gc.saveCapture(true, $"darknet/data/img/{settings.Game}{rand}.png");
                File.WriteAllText($"darknet/data/img/{settings.Game}{rand}.txt", "");

                Console.Beep();
            }

            if (User32.GetAsyncKeyState(Keys.End) == -32767)
            {
                Console.WriteLine("Okay, we have the pictures for training. Let's train the Neural Network....");
                File.WriteAllText($"darknet/{settings.Game}.cfg", File.ReadAllText($"darknet/{settings.Game}.cfg").Replace("NUMBER", nn.TrainingNames.Length.ToString()).Replace("FILTERNUM", ((nn.TrainingNames.Length + 5) * 3).ToString()));
                File.WriteAllText($"darknet/{settings.Game}.cfg", File.ReadAllText($"darknet/{settings.Game}.cfg").Replace("batch=1", "batch=64").Replace("subdivisions=1", "subdivisions=8"));
                File.WriteAllText($"darknet/data/{settings.Game}.data", File.ReadAllText($"darknet/data/{settings.Game}.data").Replace("NUMBER", nn.TrainingNames.Length.ToString()).Replace("GAME", settings.Game));
                File.WriteAllText($"darknet/{settings.Game}.cmd", File.ReadAllText($"darknet/{settings.Game}.cmd").Replace("GAME", settings.Game));
                File.WriteAllText($"darknet/{settings.Game}_trainmore.cmd", File.ReadAllText($"darknet/{settings.Game}_trainmore.cmd").Replace("GAME", settings.Game));
                File.WriteAllText($"darknet/data/{settings.Game}.names", string.Join("\n", TrainingNames));
                // DirectoryInfo d = ;//Assuming Test is your Folder
                FileInfo[] Files = new DirectoryInfo(Application.StartupPath + @"\darknet\data\img").GetFiles($"{settings.Game}*.png"); //Getting Text files
                string PathOfImg = "";
                foreach (FileInfo file in Files)
                {
                    PathOfImg += $"data/img/{file.Name}\r\n";
                }

                File.WriteAllText($"darknet/data/{settings.Game}.txt", PathOfImg);

                Process.GetProcessesByName(settings.Game)[0].Kill();
                if (File.Exists($"trainfiles/{settings.Game}.weights"))
                {
                    File.Copy($"trainfiles/{settings.Game}.weights", $"darknet/{settings.Game}.weights", true);
                    Process.Start("cmd", @"/C cd " + Application.StartupPath + $"/darknet/ & {settings.Game}_trainmore.cmd");
                }
                else Process.Start("cmd", @"/C cd " + Application.StartupPath + $"/darknet/ & {settings.Game}.cmd");

                Console.WriteLine("When you have finished training the NN, write \"done\" in this console.");

                while (true)
                {
                    if (Console.ReadLine() == "done")
                    {
                        File.Copy($"darknet/data/backup/{settings.Game}_last.weights", $"trainfiles/{settings.Game}.weights", true);
                        File.Copy($"darknet/data/{settings.Game}.names", $"trainfiles/{settings.Game}.names", true);
                        File.Copy($"darknet/{settings.Game}.cfg", $"trainfiles/{settings.Game}.cfg", true);
                        File.WriteAllText($"trainfiles/{settings.Game}.cfg", File.ReadAllText($"trainfiles/{settings.Game}.cfg").Replace("batch=64", "batch=1").Replace("subdivisions=8", "subdivisions=1"));
                        nn.yoloWrapper = new YoloWrapper($"trainfiles/{settings.Game}.cfg", $"trainfiles/{settings.Game}.weights", $"trainfiles/{settings.Game}.names");
                        nn.TrainingMode = false;
                        break;

                    }
                    else Console.WriteLine("When you have finished training the NN, write \"done\" in this console.");
                }
                Console.WriteLine("Okay! Training has finished. Let's check detection in the game!");
            }
        }


    }
}
