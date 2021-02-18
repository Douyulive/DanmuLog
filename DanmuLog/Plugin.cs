using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.ComponentModel;
using DouyuDM_PluginFramework;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace DanmuLog
{
    public class Plugin : DMPlugin
    {
        public string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), @"斗鱼弹幕姬\Plugins\DanmuLog");
        private PluginSettings Settings { get; }
        private SettingWindow SettingWnd { get; }
        public Plugin()
        {           
            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }
            string ConfigPath = Path.Combine(FilePath, "Config.json");
            Settings = new PluginSettings(ConfigPath);
            try
            {
                Settings.LoadConfig();
            }
            catch (Exception e)
            {
                Settings.SaveConfig();
                Log(e.ToString());
                Log("载入配置文件失败，请重新设置配置。");
            }
            if (Settings.Enabled)
            {
                this.Start();
            }
            SettingWnd = new SettingWindow(Settings);
            Settings.PropertyChanged += Class1_PropertyChanged;
            this.Connected += Class1_Connected;
            this.Disconnected += Class1_Disconnected;
            this.ReceivedMessage += Class1_ReceivedMessage;
            this.PluginAuth = "Coel Wu & 芒小七七七";
            this.PluginName = "弹幕日志";
            this.PluginCont = "coelwu78@protonmail.com";
            this.PluginVer = "1.0.0";
            this.PluginDesc = "输出弹幕日志";
        }

        private void Class1_ReceivedMessage(object sender, ReceivedMessageArgs e)
        {
            string Info;
            string Roomid = RoomId.ToString();
            switch (e.Message.MsgType)
            {
                case MsgTypeEnum.LiveStatusToggle:
                    {
                        if (e.Message.LiveStatus == 1)
                        {
                            Log(Roomid + "已开播");
                        } else if (e.Message.LiveStatus == 0)
                        {
                            Log(Roomid + "已下播");
                        }
                        break;
                    }
                case MsgTypeEnum.Comment:
                    {
                        if (Settings.DanmuLog)
                        {
                            Info = "【弹幕】" + DateTime.Now.ToString("HH:mm:ss.fff") + " : " + e.Message.UserName + " 说：" + e.Message.CommentText;
                            Output("Log", Info, Roomid);
                        }
                        else
                        {
                            Info = "{\"TimeStamp\":\"" + DateTime.Now.ToString("HH:mm:ss.fff") + "\", \"Uname\":\"" + e.Message.UserName + "\", \"Comment\":\"" + e.Message.CommentText + "\", \"Type\":\"弹幕\", \"SCTime\":0}";
                            Output("Data", Info, Roomid);
                        }
                        break;
                    }
            }
        }

        private void Class1_Disconnected(object sender, DisconnectEvtArgs e)
        {
            if (Settings.Enabled)
            {
                AddDM("直播间已断开", false);
                Log("直播间已断开");
            }
        }

        private void Class1_Connected(object sender, ConnectedEvtArgs e)
        {
            if (Settings.Enabled)
            {
                AddDM(RoomId.ToString() + "直播间已连接", false);
                Log(RoomId.ToString() + "直播间已连接");
                ShowMessage();
            }
        }

        // 输出
        public void Output(string LogData, string Info, string Roomid)
        {
            if (this.Dispatcher.CheckAccess())
            {
                try
                {
                    using (var file = new StreamWriter(Path.Combine(FilePath, LogData + "-" + Roomid + "-" + DateTime.Now.ToString("yyyyMMdd") + ".txt"), true))
                    {
                        file.WriteLine(Info);
                    }
                }
                catch (Exception) { }
            }
            else
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Output(LogData, Info, Roomid)));
            }
        }

        // 文件
        public void OutFile(string LogData, string FileName)
        {
            string TxtPath = Path.Combine(FilePath, LogData + "-" + FileName);
            if (!File.Exists(TxtPath))
            {
                FileStream fs = File.Create(TxtPath);
                AddDM(LogData + "-" + FileName + "已创建", false);
                Log(LogData + "-" + FileName + "已创建");
                fs.Close();
            }
        }

        public void Class1_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (RoomId != null)
            {
                ShowMessage();
            }
        }

        // 有连接房间时的提醒
        public void ShowMessage()
        {
            string FileName = RoomId.ToString() + "-" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            
            if (Settings.DanmuLog)
            {
                OutFile("Log", FileName);
            }
            else
            {
                OutFile("Data", FileName);
            }
        }

        public override void Admin()
        {
            base.Admin();
            SettingWnd.Show();
        }

        public override void Inited()
        {
            base.Inited();
        }

        public override void Stop()
        {
            base.Stop();
            //請勿使用任何阻塞方法
            Settings.Enabled = false;
            Console.WriteLine("Plugin Stoped!");
            this.Log("插件已停用");
            this.AddDM("插件已停用", false);
        }

        public override void Start()
        {
            base.Start();
            //請勿使用任何阻塞方法
            Settings.Enabled = true;
            Console.WriteLine("Plugin Started!");
            this.Log("插件已启用");
            this.AddDM("插件已启用", false);
            if (RoomId != null)
            {
                AddDM(RoomId.ToString() + "直播间已连接", false);
                Log(RoomId.ToString() + "直播间已连接");
                ShowMessage();
            }
        }
    }
}