using DeadCellsMultiplayerX.Client;
using IngameDebugConsole;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX
{
    internal static class DebugConsoleCommands
    {
        private static async void HoldAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
        }

        [ConsoleMethod("dcmp-start-host", "启动 Host")]
        public static void StartHost(TextWriter writer, string ip = "127.0.0.1", int port = 44567)
        {
            HoldAsync(ClientMain.Instance.StartHost(ip, port));
        }

        [ConsoleMethod("dcmp-start-guest", "启动 Guest")]
        public static void ConnectLobby(TextWriter writer, string ip, int port)
        {
            HoldAsync(ClientMain.Instance.StartGuest(ip, port));
        }

        [ConsoleMethod("dcmp-quit-lobby", "离开房间")]
        public static void QuitLobby(TextWriter writer)
        {
            ClientMain.Instance.CurrentGuestClient!.Quit();
        }

        [ConsoleMethod("dcmp-set-name", "修改玩家名称")]
        public static void SetName(TextWriter writer, string name)
        {
            ClientMain.Instance.CurrentGuestClient!.SetName(name);
        }
    }
}
